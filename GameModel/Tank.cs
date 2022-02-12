// Author: Grant Nations
// Author: Sebastian Ramirez
// Tank class for CS 3500 TankWars Client (PS8)

using Newtonsoft.Json;
using TankWars;

namespace GameModel
{
    /// <summary>
    /// Tank class represents a player
    /// Contains fields representing how to identify it, and how to draw it. 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {
        private const int MaxHP = 3;

        [JsonProperty(PropertyName = "tank")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get; set; }

        [JsonProperty(PropertyName = "bdir")]
        public Vector2D Orientation { get; set; }

        [JsonProperty(PropertyName = "tdir")]
        public Vector2D Aiming { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "hp")]
        public int HP { get; set; }

        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }

        [JsonProperty(PropertyName = "died")]
        public bool Died { get; set; }

        [JsonProperty(PropertyName = "dc")]
        public bool Disconnected { get; set; }

        [JsonProperty(PropertyName = "join")]
        public bool Joined { get; set; }

        /// <summary>
        /// Default Tank constructor for Json serialization
        /// </summary>
        public Tank()
        {

        }

        /// <summary>
        /// Tank constructor to be used in server
        /// </summary>
        /// <param name="id">the ID of the tank</param>
        /// <param name="location">the location vector of the tank</param>
        /// <param name="orientation">the vector determining the direction that the tank is facing</param>
        /// <param name="name">the player name associated with the tank</param>
        /// <param name="aiming">the vector determining the direction of the tank's turret</param>
        public Tank(int id, Vector2D location, Vector2D orientation, string name, Vector2D aiming)
        {
            ID = id;
            Location = location;
            Orientation = orientation;
            Name = name;
            Aiming = aiming;
            Score = 0;
            Died = false;
            Disconnected = false;
            Joined = true;
            HP = MaxHP;
        }
    }
}
