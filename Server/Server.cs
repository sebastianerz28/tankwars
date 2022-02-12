// Author: Grant Nations
// Author: Sebastian Ramirez
// Server class for CS 3500 TankWars Server (PS9)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using GameModel;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TankWars
{
    public class Server
    {
        private const int TankSpeed = 3;
        private const int ProjSpeed = 25;
        private const int WallSize = 50;
        private const int TankSize = 60;
        private const int PowerupRespawnRate = 420;
        private const int MaxPowerups = 4;

        private int UniverseSize = -1;
        private int MSPerFrame = -1;
        private int FramesPerShot = -1;
        private int RespawnRate = -1;

        private int wallCount = 0;
        private int tankID = 0;
        private int projectileID = 0;
        private int beamID = 0;
        private int powerupID = 0;
        private int powerupCounter = 0;


        private Vector2D newTankDir = new Vector2D(0, 1);

        private Dictionary<long, int> tankIDs; //Holds socket to tankID

        private Dictionary<int, Vector2D> tankVelocities; //holds TankID to current velocity

        private Dictionary<long, Socket> sockets; //Holds socketstateID to its socket

        private Dictionary<int, int> numOfPowerups; //maps tankID to how many powerups it has collected

        private Dictionary<int, int> shotDelays; //Maps tankID to time before it can fire again

        private Dictionary<int, int> respawnDelay; //Maps tankID to time before it can respawn

        private List<int> deadProjectiles; //List of IDs of projectiles that have come into contact with something

        private List<int> deadPowerups; //List of powerup IDs that have been collected

        private List<int> disconnectedTanks; //List of tanks that disconnected

        public World world = new World();

        private Stopwatch watch = new Stopwatch();
        /// <summary>
        /// Creates new server object and starts by calling run
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Server s = new Server();
            s.Run();
            Console.Read();
        }

        /// <summary>
        /// Constructor for server
        /// Initializes all lists and dictionaries
        /// </summary>
        public Server()
        {
            tankIDs = new Dictionary<long, int>();
            tankVelocities = new Dictionary<int, Vector2D>();
            sockets = new Dictionary<long, Socket>();
            deadProjectiles = new List<int>();
            deadPowerups = new List<int>();
            numOfPowerups = new Dictionary<int, int>();
            shotDelays = new Dictionary<int, int>();
            disconnectedTanks = new List<int>();
            respawnDelay = new Dictionary<int, int>();
        }

        /// <summary>
        /// To be called by a server object
        /// Parses XML file with the server settings
        /// Starts server and listens for new connections while updating world based on server setting MSPerFrame
        /// </summary>
        public void Run()
        {
            try
            {
                ReadSettingsXml();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            TcpListener listener = Networking.StartServer(SetupMessageReceive, 11000);
            Console.WriteLine("Server is running.");
            watch.Start();
            while (true)
            {
                while (watch.ElapsedMilliseconds < MSPerFrame)
                { }
                watch.Restart();
                UpdateWorld();
            }

        }

        /// <summary>
        /// Updates the world and serializes all objects in JSON to be sent to each client
        /// Updates projectile locations checking for collisions updating score and health as needed
        /// Checks beam and powerup collisions updating each object as required
        /// Calculates tank location based on velocity, and sets new tank location upon respawn
        /// </summary>
        private void UpdateWorld()
        {
            lock (world)
            {
                if (++powerupCounter > PowerupRespawnRate) //If enough time has passed a new powerup can be added to the game
                {
                    if (world.Powerups.Count < MaxPowerups)
                        world.Powerups.Add(powerupID, new Powerup(powerupID++, RandomSpawnLocation(0)));
                    powerupCounter = 0;
                }


                StringBuilder sb = new StringBuilder();
                string jsonString = "";


                foreach (Projectile p in world.Projectiles.Values)
                {
                    Vector2D v = p.Direction;
                    v *= ProjSpeed;

                    bool collided = false;
                    foreach (Tank t in world.Tanks.Values)
                    {
                        if (t.ID != p.Owner && t.HP > 0 && TankHit(t.Location, p.Location)) //Checks if tank was hit by the projectile
                        {
                            if (--t.HP == 0)
                            {

                                world.Tanks[p.Owner].Score++; // increment the score of the tank who shot the bullet
                                t.Died = true;
                            }
                            collided = true;
                            break;
                        }
                    }
                    foreach (Wall w in world.Walls.Values)
                    {
                        if (ObjCollidesWithWall(p.Location, w, 0)) //Checks if projectile hit wall
                        {
                            p.Died = true;
                            deadProjectiles.Add(p.ID);
                            collided = true;
                        }
                    }

                    if (!collided) //Updates location
                    {
                        p.Location += v;
                        if (Math.Abs(p.Location.GetX()) > UniverseSize / 2)
                        {
                            p.Died = true;
                        }
                        else if (Math.Abs(p.Location.GetX()) > UniverseSize / 2)
                        {
                            p.Died = true;
                        }
                    }
                        
                    else
                    {
                        p.Died = true;
                        deadProjectiles.Add(p.ID);
                    }


                    jsonString = JsonConvert.SerializeObject(p);
                    sb.Append(jsonString);
                    sb.Append('\n');
                }

                foreach (int proj in deadProjectiles) //Removes any projectiles marked as dead from dictionary
                {
                    world.Projectiles.Remove(proj);
                }

                deadProjectiles.Clear();

                foreach (Beam b in world.Beams.Values)
                {
                    foreach (Tank t in world.Tanks.Values)
                    {
                        if (t.ID != b.Owner && Intersects(b.Origin, b.Direction, t.Location, TankSize / 2)) //Checks if beam hit tank
                        {
                            t.Died = true;
                            t.HP = 0;
                            world.Tanks[b.Owner].Score++;
                        }
                    }

                    jsonString = JsonConvert.SerializeObject(b);
                    sb.Append(jsonString);
                    sb.Append('\n');
                }

                world.Beams.Clear();

                foreach (Powerup pow in world.Powerups.Values)
                {


                    foreach (Tank t in world.Tanks.Values)
                    {
                        if (TankHit(t.Location, pow.Location)) //Checks if powerup was collected
                        {
                            numOfPowerups[t.ID]++;
                            pow.Died = true;
                            deadPowerups.Add(pow.ID);
                        }
                    }


                    jsonString = JsonConvert.SerializeObject(pow);
                    sb.Append(jsonString);
                    sb.Append('\n');
                }
                foreach (int ID in deadPowerups) //Removes any projectiles marked as dead from dictionary
                {
                    world.Powerups.Remove(ID);
                }
                deadPowerups.Clear();



                foreach (Tank t in world.Tanks.Values)
                {
                    if (!t.Disconnected)
                    {
                        if (t.HP == 0)
                        {

                            if (--respawnDelay[t.ID] == 0) //Check if enough time has passed so tank can respawn
                            {
                                t.HP = 3;
                                respawnDelay[t.ID] = RespawnRate;
                                t.Location = RandomSpawnLocation(TankSize);
                            }


                        }
                        if (shotDelays[t.ID] != 0)
                        {
                            shotDelays[t.ID]--; // increment shot delay for each tank if the tank's shot delay isn't 0
                        }
                        if (t.HP != 0)
                        {
                            bool collided = false;
                            foreach (Wall w in world.Walls.Values)
                            {
                                if (ObjCollidesWithWall(t.Location + tankVelocities[t.ID], w, TankSize))
                                {
                                    collided = true;
                                    break;
                                }
                            }

                            if (!collided)
                            {
                                t.Location += tankVelocities[t.ID];
                                if (Math.Abs(t.Location.GetX()) > UniverseSize/2)
                                {
                                    t.Location = new Vector2D(-1 * t.Location.GetX(), t.Location.GetY());
                                }
                                else if(Math.Abs(t.Location.GetX()) > UniverseSize / 2)
                                {
                                    t.Location = new Vector2D(t.Location.GetX(), -1 * t.Location.GetY());
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        disconnectedTanks.Add(t.ID);
                        t.HP = 0;
                    }

                    jsonString = JsonConvert.SerializeObject(t);
                    sb.Append(jsonString);
                    sb.Append('\n');

                    if (t.Joined)
                        t.Joined = false;

                    if (t.Died)
                        t.Died = false;
                }

                foreach (int tankID in disconnectedTanks) // remove tanks that disconnected from dictionaries and world
                {
                    world.Tanks.Remove(tankID);
                    shotDelays.Remove(tankID);
                    tankVelocities.Remove(tankID);
                    numOfPowerups.Remove(tankID);
                    respawnDelay.Remove(tankID);
                }

                disconnectedTanks.Clear();

                foreach (Socket socket in sockets.Values)
                {
                    Networking.Send(socket, sb.ToString());
                }

            }
        }


        /// <summary>
        /// Call back for all new incoming connections
        /// Sets state.OnNetworkAction to the call back that validates name
        /// </summary>
        /// <param name="state">the client state</param>
        private void SetupMessageReceive(SocketState state)
        {
            state.OnNetworkAction = CheckName;

            Networking.GetData(state);
        }

        /// <summary>
        /// Checks that the name is valid and prints that player has joined
        /// Adds player to all necessary dictionaries
        /// Sets state.OnNetworkAction SendWalls then invokes it
        /// </summary>
        /// <param name="state">the client state</param>
        private void CheckName(SocketState state)
        {

            string s = state.GetData();
            if (s.EndsWith('\n'))
            {
                state.RemoveData(0, s.Length);

                //send worldsize and id and walls
                Console.WriteLine("Player " + tankID + " " + s.Trim('\n') + " joined.");
                lock (world)
                {
                    Tank t = new Tank(tankID, RandomSpawnLocation(TankSize), newTankDir, s.Trim('\n'), newTankDir);

                    Networking.Send(state.TheSocket, tankID + "\n" + UniverseSize + "\n");
                    string jsonString = null;
                    foreach (Wall w in world.Walls.Values)
                    {
                        jsonString = JsonConvert.SerializeObject(w) + '\n';
                        Networking.Send(state.TheSocket, jsonString);
                    }
                    shotDelays.Add(tankID, FramesPerShot);
                    tankVelocities.Add(tankID, new Vector2D(0, 0));
                    respawnDelay.Add(tankID, RespawnRate);
                    world.Tanks.Add(tankID, t);
                    numOfPowerups.Add(tankID, 0);
                    sockets.Add(state.ID, state.TheSocket);
                    tankIDs.Add(state.ID, tankID++);
                }



                state.OnNetworkAction = ReceiveControlCommands;
                Networking.GetData(state);
            }

        }



        /// <summary>
        /// Event loop that continously receives data(control commands) from the client
        /// Stops the event loop if the client disconnects
        /// Ensures that all elements of the control command are valid through helper methods
        /// </summary>
        /// <param name="state">the client state</param>
        private void ReceiveControlCommands(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                lock (world)
                {
                    Console.WriteLine("Cient " + tankIDs[state.ID] + " disconnected.");
                    world.Tanks[tankIDs[state.ID]].Disconnected = true;
                    tankIDs.Remove(state.ID);
                    sockets.Remove(state.ID);
                }
                return;
            }
            try
            {
                string totalData = state.GetData();
                string[] parts = Regex.Split(totalData, @"(?<=[\n])");
                lock (world)
                {
                    foreach (string s in parts)
                    {
                        // This will be the final "part" in the data buffer
                        if (!s.EndsWith("\n"))
                            continue;

                        // Ignore empty strings added by the regex splitter
                        if (s.Length == 0)
                        {
                            state.RemoveData(0, s.Length);
                            continue;
                        }

                        JObject obj = JObject.Parse(s);

                        JToken token = obj["moving"];
                        if (token != null)
                        {
                            ControlCmd cmd = JsonConvert.DeserializeObject<ControlCmd>(s);

                            //Handle each component of control command seperately
                            HandleTdir(cmd.Tdir, state.ID);
                            HandleMoving(cmd.Moving, state.ID);
                            HandleFire(cmd.Fire, state.ID);

                            state.RemoveData(0, s.Length);
                            continue;
                        }
                    }
                }
                Networking.GetData(state);
            }
            catch
            {
                lock (world)
                {
                    Console.WriteLine("Cient " + tankIDs[state.ID] + " disconnected.");
                    world.Tanks[tankIDs[state.ID]].Disconnected = true;
                    tankIDs.Remove(state.ID);
                    sockets.Remove(state.ID);
                    state.TheSocket.Close();

                }

                return;
            }

        }

        /// <summary>
        /// Private helper method to update direction that turret is facing
        /// </summary>
        /// <param name="tdir"></param>
        /// <param name="stateID"></param>
        private void HandleTdir(Vector2D tdir, long stateID)
        {
            if (tankIDs.TryGetValue(stateID, out int tankID))
            {
                world.Tanks[tankID].Aiming = tdir;
            }
        }

        /// <summary>
        /// Ensures all fire commands are valid
        /// Main shot will only be processed if the delay between shots has passed
        /// Beam shots will only be processed if the player has a powerup
        /// Commands will only be processed if the hp is above 0
        /// </summary>
        /// <param name="fire"></param>
        /// <param name="stateID"></param>
        private void HandleFire(string fire, long stateID)
        {
            if (tankIDs.TryGetValue(stateID, out int tankID))
            {
                if (world.Tanks[tankID].HP != 0)
                {
                    if (fire == "main" && shotDelays[tankID] == 0)
                    {
                        shotDelays[tankID] = FramesPerShot;
                        world.Projectiles.Add(projectileID, new Projectile(projectileID, world.Tanks[tankID].Location, world.Tanks[tankID].Aiming, tankID));
                        projectileID++;
                    }
                    else if (fire == "alt" && numOfPowerups[tankID] > 0)
                    {
                        numOfPowerups[tankID]--;
                        world.Beams.Add(beamID, new Beam(beamID, world.Tanks[tankID].Location, world.Tanks[tankID].Aiming, tankID));
                        beamID++;
                    }
                }

            }

        }

        /// <summary>
        /// Updates the tank velocity in the direction state by "moving"
        /// </summary>
        /// <param name="moving"> Direction the tank is moving </param>
        /// <param name="stateID"></param>
        private void HandleMoving(string moving, long stateID)
        {
            if (tankIDs.TryGetValue(stateID, out int tankID))
            {
                switch (moving)
                {
                    case "up":
                        world.Tanks[tankID].Orientation = new Vector2D(0, -1);
                        tankVelocities[tankID] = new Vector2D(0, -1) * TankSpeed;
                        break;
                    case "left":
                        world.Tanks[tankID].Orientation = new Vector2D(-1, 0);
                        tankVelocities[tankID] = new Vector2D(-1, 0) * TankSpeed;
                        break;
                    case "down":
                        world.Tanks[tankID].Orientation = new Vector2D(0, 1);
                        tankVelocities[tankID] = new Vector2D(0, 1) * TankSpeed;
                        break;
                    case "right":
                        world.Tanks[tankID].Orientation = new Vector2D(1, 0);
                        tankVelocities[tankID] = new Vector2D(1, 0) * TankSpeed;
                        break;
                    default:
                        tankVelocities[tankID] = new Vector2D(0, 0);
                        break;
                }

            }
        }

        /// <summary>
        /// Reads the XML file containing all the server settings
        /// </summary>
        private void ReadSettingsXml()
        {
            using (XmlReader reader = XmlReader.Create(@"..\..\..\..\Resources\settings.xml"))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "UniverseSize":
                                ParseSettingsXmlVal(reader, ref UniverseSize);
                                break;
                            case "MSPerFrame":
                                ParseSettingsXmlVal(reader, ref MSPerFrame);
                                break;
                            case "FramesPerShot":
                                ParseSettingsXmlVal(reader, ref FramesPerShot);
                                break;
                            case "RespawnRate":
                                ParseSettingsXmlVal(reader, ref RespawnRate);
                                break;
                            case "Wall":
                                ParseWallXml(reader);
                                break;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Helper method to read the properties of a wall
        /// Creates a new Wall object and adds it the the dictionary in the world that contains walls
        /// </summary>
        /// <param name="reader"></param>
        private void ParseWallXml(XmlReader reader)
        {
            // at this point, the reader has encountered a <Wall> tag
            string x1 = null;
            string y1 = null;
            string x2 = null;
            string y2 = null;

            Vector2D p1 = null;
            Vector2D p2 = null;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "p1":
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "x":
                                                reader.Read();
                                                if (reader.NodeType == XmlNodeType.Text)
                                                    x1 = reader.Value;
                                                break;
                                            case "y":
                                                reader.Read();
                                                if (reader.NodeType == XmlNodeType.Text)
                                                    y1 = reader.Value;
                                                break;
                                        }
                                    }
                                    else if (reader.NodeType == XmlNodeType.EndElement)
                                    {
                                        if (reader.Name == "p1")
                                            break;
                                    }
                                }
                                break;
                            case "p2":
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "x":
                                                reader.Read();
                                                if (reader.NodeType == XmlNodeType.Text)
                                                    x2 = reader.Value;
                                                break;
                                            case "y":
                                                reader.Read();
                                                if (reader.NodeType == XmlNodeType.Text)
                                                    y2 = reader.Value;
                                                break;
                                        }
                                    }
                                    else if (reader.NodeType == XmlNodeType.EndElement)
                                    {
                                        if (reader.Name == "p2")
                                            break;
                                    }
                                }
                                break;

                        }

                        if (double.TryParse(x1, out double x1Dub) && double.TryParse(y1, out double y1Dub))
                        {
                            p1 = new Vector2D(x1Dub, y1Dub);
                        }

                        if (double.TryParse(x2, out double x2Dub) && double.TryParse(y2, out double y2Dub))
                        {
                            p2 = new Vector2D(x2Dub, y2Dub);
                        }

                        break;
                    case XmlNodeType.EndElement:
                        if (reader.Name == "Wall")
                        {
                            if (p1 is null || p2 is null)
                            {
                                throw new IOException("Could not parse Wall");
                            }
                            else
                            {
                                world.Walls.Add(wallCount, new Wall(wallCount, p1, p2));
                                wallCount++;
                            }
                            return;
                        }
                        break;
                }


            }
        }

        /// <summary>
        /// Private helper method that parses a numeric setting from the XML file and assigns it
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="settingsVal"></param>
        private void ParseSettingsXmlVal(XmlReader reader, ref int settingsVal)
        {
            reader.Read();
            if (reader.NodeType == XmlNodeType.Text)
            {
                string toBeRead = reader.Value;
                if (!int.TryParse(toBeRead, out settingsVal))
                {
                    throw new IOException("Could not parse" + reader.Name);
                }
            }
        }

        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns></returns>
        private static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substituting to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }

        /// <summary>
        /// Checks if an object collides with a wall
        /// Determines if it is a vertical or horizontal wall
        /// Calculates the max perimeter of the wall and determines if obj X and y are within
        /// Returns true if within, false otherwise
        /// </summary>
        /// <param name="obj"> Objects location </param>
        /// <param name="wall"> Wall to check against </param>
        /// <param name="objSize"> Size of the object </param>
        /// <returns> Returns true if collides, false otherwise </returns>
        private static bool ObjCollidesWithWall(Vector2D obj, Wall wall, int objSize)
        {
            int halfWallSize = WallSize / 2;
            Vector2D p1Wall = wall.P1;
            Vector2D p2Wall = wall.P2;
            int halfobjSize = objSize / 2;
            if (p1Wall.GetX() == p2Wall.GetX())
            {
                double leftX = p1Wall.GetX() - halfWallSize - halfobjSize;
                double rightX = p1Wall.GetX() + halfWallSize + halfobjSize;
                double topY;
                double botY;
                if (p1Wall.GetY() <= p2Wall.GetY())
                {
                    topY = p1Wall.GetY() - halfWallSize - halfobjSize;
                    botY = p2Wall.GetY() + halfWallSize + halfobjSize;
                }
                else
                {
                    topY = p2Wall.GetY() - halfWallSize - halfobjSize;
                    botY = p1Wall.GetY() + halfWallSize + halfobjSize;
                }
                double objX = obj.GetX();
                double objY = obj.GetY();
                if (objX <= rightX && objX >= leftX && objY >= topY && objY <= botY)
                {
                    return true;
                }
            }
            else
            {
                double topY = p1Wall.GetY() - halfWallSize - halfobjSize;
                double botY = p1Wall.GetY() + halfWallSize + halfobjSize;
                double leftX;
                double rightX;
                if (p1Wall.GetX() <= p2Wall.GetX())
                {
                    leftX = p1Wall.GetX() - halfWallSize - halfobjSize;
                    rightX = p2Wall.GetX() + halfWallSize + halfobjSize;
                }
                else
                {
                    leftX = p2Wall.GetX() - halfWallSize - halfobjSize;
                    rightX = p1Wall.GetX() + halfWallSize + halfobjSize;
                }
                double objX = obj.GetX();
                double objY = obj.GetY();
                if (objX <= rightX && objX >= leftX && objY >= topY && objY <= botY)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines if tank and a projectile/powerup make contact
        /// Calculates perimeter of tank and returns true if tank
        /// collided and false otherwise
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="projectile"></param>
        /// <returns></returns>
        private static bool TankHit(Vector2D tank, Vector2D projectile)
        {
            int halfTankSize = TankSize / 2;
            double leftX = tank.GetX() - halfTankSize;
            double rightX = tank.GetX() + halfTankSize;
            double topY = tank.GetY() - halfTankSize;
            double botY = tank.GetY() + halfTankSize;
            double objX = projectile.GetX();
            double objY = projectile.GetY();
            if (objX <= rightX && objX >= leftX && objY >= topY && objY <= botY)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Generates random location within the world that does not come into contact with a wall or tanks
        /// Uses ObjCollidesWithWall to determine if random location is in a wall
        /// 
        /// </summary>
        /// <param name="size"> size of the obj </param>
        /// <returns></returns>
        private Vector2D RandomSpawnLocation(int size)
        {
            while (true)
            {
                bool collision = false;
                Random rand = new Random();
                double x = rand.Next((-UniverseSize / 2) + WallSize, (UniverseSize / 2) - WallSize);
                double y = rand.Next((-UniverseSize / 2) + WallSize, (UniverseSize / 2) - WallSize);

                Vector2D randLoc = new Vector2D(x, y);
                foreach (Wall w in world.Walls.Values)
                {
                    if (ObjCollidesWithWall(randLoc, w, size))
                    {
                        collision = true;
                    }
                }
                foreach (Tank t in world.Tanks.Values)
                {
                    if (TankHit(t.Location, randLoc))
                        collision = true;
                }

                if (!collision)
                    return randLoc;
            }
        }
    }
}