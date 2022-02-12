// Author: Grant Nations
// Author: Sebastian Ramirez
// Wall class for CS 3500 TankWars Client (PS8)

using Newtonsoft.Json;
using TankWars;

namespace GameModel
{
    /// <summary>
    /// Wall class represents the borders of the world and the obstacles that act as cover in the world
    /// Contains fields on how to identify it, and it's location/orientation
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        [JsonProperty(PropertyName = "wall")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "p1")]
        public Vector2D P1 { get; private set; }

        [JsonProperty(PropertyName = "p2")]
        public Vector2D P2 { get; private set; }

        /// <summary>
        /// Wall constructor to be used in server
        /// </summary>
        /// <param name="ID">the ID of the wall</param>
        /// <param name="p1">the initial endpoint of the Wall</param>
        /// <param name="p2">the final endpoint of the Wall</param>
        public Wall(int ID, Vector2D p1, Vector2D p2)
        {
            this.ID = ID;
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Default constructor for Json serialization
        /// </summary>
        public Wall()
        { }
    }
}
