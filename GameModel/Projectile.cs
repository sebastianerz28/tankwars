// Author: Grant Nations
// Author: Sebastian Ramirez
// Projectile class for CS 3500 TankWars Client (PS8)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TankWars;

namespace GameModel
{
    /// <summary>
    /// Projectile class contains fields related to location, movement, how to identify it
    /// and whether it should be drawn.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        [JsonProperty(PropertyName = "proj")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "loc")]
        public Vector2D Location { get; set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D Direction { get; set; }

        [JsonProperty(PropertyName = "died")]
        public bool Died { get; set; }

        [JsonProperty(PropertyName = "owner")]
        public int Owner { get; set; }

        /// <summary>
        /// Default constructor for Json serialization
        /// </summary>
        public Projectile()
        {

        }

        /// <summary>
        /// Projectile constructor to be used in server
        /// </summary>
        /// <param name="id">the ID of the projectile</param>
        /// <param name="location">the location vector of the projectile</param>
        /// <param name="direction">the direction vector of the projectile</param>
        /// <param name="owner">the ID of the tank that shot the projectile</param>
        public Projectile(int id, Vector2D location, Vector2D direction, int owner)
        {
            ID = id;
            Location = location;
            Direction = direction;
            Died = false;
            Owner = owner;
        }

    }
}
