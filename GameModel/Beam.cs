// Author: Grant Nations
// Author: Sebastian Ramirez
// Beam class for CS 3500 TankWars Client (PS8)

using TankWars;
using Newtonsoft.Json;
namespace GameModel
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Beam
    {
        [JsonProperty(PropertyName = "beam")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "org")]
        public Vector2D Origin { get; private set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D Direction { get; private set; }

        [JsonProperty(PropertyName = "owner")]
        public int Owner { get; private set; }
        
        /// <summary>
        /// Default constructor for Json serialization 
        /// </summary>
        public Beam()
        {

        }

        /// <summary>
        /// Beam constructor to be used in server
        /// </summary>
        /// <param name="id">the ID of the beam</param>
        /// <param name="origin">origin vector of the beam</param>
        /// <param name="direction">direction vecotr of the beam</param>
        /// <param name="owner">the ID of the tank that fired the beam</param>
        public Beam(int id, Vector2D origin, Vector2D direction, int owner)
        {
            ID = id;
            Origin = origin;
            Direction = direction;
            Owner = owner;
        }
    }
}
