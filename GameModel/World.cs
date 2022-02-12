// Author: Grant Nations
// Author: Sebastian Ramirez
// World class for CS 3500 TankWars Client (PS8)
using System.Collections.Generic;
namespace GameModel
{
    /// <summary>
    /// Stores information about walls, tanks, projectiles, beams, and powerups
    /// </summary>
    public class World
    {
        public Dictionary<int, Wall> Walls { get; private set; }
        public Dictionary<int, Tank> Tanks { get; private set; }
        public Dictionary<int, Projectile> Projectiles { get; private set; }
        public Dictionary<int, Beam> Beams { get; private set; }
        public Dictionary<int, Powerup> Powerups { get; private set; }
        public int PlayerID { get; set; }
        public int WorldSize { get; set; }

        /// <summary>
        /// World constructor
        /// </summary>
        public World()
        {
            Tanks = new Dictionary<int, Tank>();
            Walls = new Dictionary<int, Wall>();
            Projectiles = new Dictionary<int, Projectile>();
            Beams = new Dictionary<int, Beam>();
            Powerups = new Dictionary<int, Powerup>();
        }
    }
}
