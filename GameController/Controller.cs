// Author: Grant Nations
// Author: Sebastian Ramirez
// Controller class for CS 3500 TankWars Client (PS8)

using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using GameModel;
using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TankWars;

namespace GameController
{
    /// <summary>
    /// Controller handles updating the the world(model), telling the view to draw a frame,
    /// connecting to the server, and sending user inputs to the server
    /// </summary>
    public class Controller
    {
        private string playerName;
        public int id;
        private World world;
        private string prevKeyPress = "none";
        private string currentKeyPress = "none";
        private string jsonString;
        private ControlCmd controlCmd;

        public delegate void ErrorOccuredHandler(string ErrorMessage);
        public delegate void ServerUpdateHandler();
        public delegate void WorldReadyHandler();
        public delegate void SetExplosionCountHandler(int ID);
        public delegate void RemoveTankExplosionCountHandler(int ID);
        public delegate void SetBeamCounterHandler(int id);

        public event SetBeamCounterHandler SetBeamCounter;
        public event RemoveTankExplosionCountHandler RemoveTankExplosionCount;
        public event SetExplosionCountHandler SetExplosionCounter;
        public event ServerUpdateHandler UpdateArrived;
        public event ErrorOccuredHandler ErrorOccurred;
        public event WorldReadyHandler WorldReady;

        /// <summary>
        /// Initializes a new Controller object with default control commands
        /// </summary>
        public Controller()
        {
            controlCmd = new ControlCmd();
            world = new World();
            controlCmd.Moving = "none";
            controlCmd.Fire = "none";
        }

        /// <summary>
        /// Attempts connection to a server at address hostName on port 11000 using playerName as the player's name
        /// in the TankWars game
        /// </summary>
        /// <param name="hostName">The address name</param>
        /// <param name="playerName">the player name</param>
        public void Connect(string hostName, string playerName)
        {
            this.playerName = playerName;
            Networking.ConnectToServer(FirstContact, hostName, 11000);
        }

        /// <summary>
        /// The initial OnNetworkAction callback for connecting to the server
        /// </summary>
        /// <param name="state">the SocketState object</param>
        private void FirstContact(SocketState state)
        {
            if (state.ErrorOccurred == true)
            {
                ErrorOccurred("Could not connect to the server.");
                return;
            }

            state.OnNetworkAction = ReceiveStartup;
            Networking.Send(state.TheSocket, playerName + "\n");
            Networking.GetData(state);
        }

        /// <summary>
        /// The OnNetworkAction callback for recieving player ID and world size as the handshake between server and client
        /// </summary>
        /// <param name="state">the SocketState object</param>
        private void ReceiveStartup(SocketState state)
        {
            if (state.ErrorOccurred == true)
            {
                ErrorOccurred("Lost connection to server");
                return;
            }
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            if (parts.Length < 2 || (!parts[0].EndsWith("\n") && !parts[1].EndsWith("\n")))
            {
                Networking.GetData(state);
                return;
            }
            else
            {
                lock (world)
                {
                    if (int.TryParse(parts[0], out id))
                    {
                        world.PlayerID = id;
                    }
                    else
                    {
                        ErrorOccurred("Id could not be parsed");
                        return;
                    }
                    if (int.TryParse(parts[1], out int worldSize))
                    {
                        world.WorldSize = worldSize;
                    }
                    else
                    {
                        ErrorOccurred("World Size could not be parsed");
                        return;
                    }
                }

                state.RemoveData(0, parts[0].Length + parts[1].Length);
                state.OnNetworkAction = ReceiveWalls;
                Networking.GetData(state);
            }
        }

        /// <summary>
        /// The OnNetworkAction callback for recieving the walls as JSON
        /// </summary>
        /// <param name="state">the SocketState object</param>
        private void ReceiveWalls(SocketState state)
        {
            if (state.ErrorOccurred == true)
            {
                ErrorOccurred("Lost connection to server");
                return;
            }
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

                    JToken token = obj["wall"];
                    if (token != null)
                    {
                        Wall w = JsonConvert.DeserializeObject<Wall>(s);
                        if (world.Walls.ContainsKey(w.ID))
                        {
                            world.Walls[w.ID] = w;
                        }
                        else
                        {
                            world.Walls.Add(w.ID, w);
                        }
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                    if (HandleNotWallJSON("tank", obj, state))
                        break;  // object is a tank
                    if (HandleNotWallJSON("proj", obj, state))
                        break;  // object is a projectile
                    if (HandleNotWallJSON("beam", obj, state))
                        break;  // object is a beam
                    if (HandleNotWallJSON("power", obj, state))
                        break;  // object is a powerup
                }
            }
            Networking.GetData(state);
        }

        /// <summary>
        /// Helper method for the RecieveWalls callback. This method performs appropriate actions when JSON is not a wall object
        /// </summary>
        /// <param name="objType">the type of object to cast to</param>
        /// <param name="jObj">the JObject to cast</param>
        /// <param name="state">true if the jObject jObj can be casted to an objType object</param>
        /// <returns></returns>
        private bool HandleNotWallJSON(string objType, JObject jObj, SocketState state)
        {
            JToken token = jObj[objType];
            if (token != null)
            {
                WorldReady();
                state.OnNetworkAction = ReceiveWorld;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The OnNetworkAction callback for recieving the continuous game data.
        /// </summary>
        /// <param name="state">the SocketState object</param>
        private void ReceiveWorld(SocketState state)
        {
            if (state.ErrorOccurred == true)
            {
                ErrorOccurred("Lost connection to server");
                return;
            }
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");
            lock (world)
            {
                foreach (string s in parts)
                {
                    if (!s.EndsWith("\n"))
                    {
                        continue;
                    }

                    // Ignore empty strings added by the regex splitter
                    if (s.Length == 0)
                    {
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                    JObject obj = JObject.Parse(s);

                    JToken token = obj["tank"];
                    if (token != null)
                    {
                        Tank tank = JsonConvert.DeserializeObject<Tank>(s);
                        if (world.Tanks.ContainsKey(tank.ID))
                        {
                            if (tank.Disconnected)
                            {
                                world.Tanks.Remove(tank.ID);
                                RemoveTankExplosionCount(tank.ID);
                            }
                            else
                            {
                                world.Tanks[tank.ID] = tank;
                                if (tank.Died)
                                {
                                    SetExplosionCounter(tank.ID);
                                }
                            }
                        }
                        else
                        {
                            SetExplosionCounter(tank.ID);
                            world.Tanks.Add(tank.ID, tank);
                        }
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                    token = obj["proj"];
                    if (token != null)
                    {
                        Projectile proj = JsonConvert.DeserializeObject<Projectile>(s);
                        if (world.Projectiles.ContainsKey(proj.ID))
                        {
                            if (proj.Died)
                                world.Projectiles.Remove(proj.ID);
                            else
                            {
                                world.Projectiles[proj.ID] = proj;
                            }
                        }
                        else
                        {
                            if (!proj.Died)
                                world.Projectiles.Add(proj.ID, proj);
                        }
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                    token = obj["beam"];
                    if (token != null)
                    {

                        Beam beam = JsonConvert.DeserializeObject<Beam>(s);
                        if (world.Beams.ContainsKey(beam.ID))
                        {
                            world.Beams[beam.ID] = beam;
                            SetBeamCounter(beam.ID);
                        }
                        else
                        {
                            SetBeamCounter(beam.ID);
                            world.Beams.Add(beam.ID, beam);
                        }
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                    token = obj["power"];
                    if (token != null)
                    {
                        Powerup powerup = JsonConvert.DeserializeObject<Powerup>(s);
                        if (world.Powerups.ContainsKey(powerup.ID))
                        {
                            if (powerup.Died)
                            {
                                world.Powerups.Remove(powerup.ID);
                            }
                            else
                            {
                                world.Powerups[powerup.ID] = powerup;
                            }
                        }
                        else
                        {
                            if (!powerup.Died)
                                world.Powerups.Add(powerup.ID, powerup);
                        }
                        state.RemoveData(0, s.Length);
                        continue;
                    }

                }
            }

            // Notify any listeners (the view) that a new game world has arrived from the server if UpdateArrived is not null
            UpdateArrived?.Invoke();

            if (controlCmd.Tdir != null)
            {
                jsonString = JsonConvert.SerializeObject(controlCmd) + "\n";
                Networking.Send(state.TheSocket, jsonString);
            }
            Networking.GetData(state);
        }

        /// <summary>
        /// Handles the control command direction appropriately for a mouse move event to ensure correct turret position
        /// </summary>
        /// <param name="mousePosition">the point of the mouse in the drawing panel</param>
        /// <param name="viewSize">The size of the form drawing panel</param>
        public void HandleMouseMove(Point mousePosition, int viewSize)
        {
            lock (controlCmd)
            {
                controlCmd.Tdir = new Vector2D(mousePosition.X - (viewSize / 2.0), mousePosition.Y - (viewSize / 2.0));
                controlCmd.Tdir.Normalize();
            }
        }

        /// <summary>
        /// Handles the control command fire property to ensure that a tank is not firing after a mouse press has
        /// been released
        /// </summary>
        /// <param name="e"></param>
        public void CancelMouseRequest(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                controlCmd.Fire = "none";
            }
            else if (e.Button == MouseButtons.Right)
            {
                controlCmd.Fire = "none";
            }
        }

        /// <summary>
        /// Handles the control command fire property to ensure that a tank is firing after a mouse is clicked
        /// </summary>
        /// <param name="e"></param>
        public void HandleMouseRequest(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                controlCmd.Fire = "main";
            }
            else if (e.Button == MouseButtons.Right)
            {
                controlCmd.Fire = "alt";
            }
        }

        /// <summary>
        /// Checks if a move key is being pressed and handles it appropriately to ensure the proper direction
        /// is being sent in the command control JSON
        /// </summary>
        /// <param name="e"></param>
        public void HandleMoveRequest(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                HandleMoveRequest("up");
            }
            else if (e.KeyCode == Keys.A)
            {
                HandleMoveRequest("left");
            }
            else if (e.KeyCode == Keys.D)
            {
                HandleMoveRequest("right");
            }
            else if (e.KeyCode == Keys.S)
            {
                HandleMoveRequest("down");
            }
        }

        /// <summary>
        /// Helper method for the HandleMoveRequest(KeyEventArgs) method. If the current key press is not direction, 
        /// sets appropriate values to ensure the proper direction is sent in the control command JSON
        /// </summary>
        /// <param name="direction">The direction associated with the key</param>
        private void HandleMoveRequest(string direction)
        {
            if (currentKeyPress != direction)
            {
                prevKeyPress = currentKeyPress;
                currentKeyPress = direction;
                controlCmd.Moving = direction;
            }
        }

        /// <summary>
        /// Checks if a move key is being released and handles it appropriately to ensure the proper direction
        /// is being sent in the command control JSON
        /// </summary>
        /// <param name="e"></param>
        public void CancelMoveRequest(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W)
            {
                CancelMoveRequest("up");
            }
            else if (e.KeyCode == Keys.A)
            {
                CancelMoveRequest("left");
            }
            else if (e.KeyCode == Keys.D)
            {
                CancelMoveRequest("right");
            }
            else if (e.KeyCode == Keys.S)
            {
                CancelMoveRequest("down");
            }
        }

        /// <summary>
        /// Helper method for the CancelMoveRequest(KeyEventArgs) method. If the current or previous key press is direction, 
        /// sets appropriate values to ensure the proper direction is sent in the control command JSON.
        /// </summary>
        /// <param name="direction">The direction associated with the key</param>
        private void CancelMoveRequest(string direction)
        {
            if (currentKeyPress == direction)
            {
                currentKeyPress = prevKeyPress;
                controlCmd.Moving = prevKeyPress;
                prevKeyPress = "none";
            }
            else if (prevKeyPress == direction)
            {
                prevKeyPress = "none";
            }
        }

        /// <summary>
        /// Returns the World instance of the object
        /// </summary>
        /// <returns></returns>
        public World GetWorld()
        {
            return world;
        }
    }

}
