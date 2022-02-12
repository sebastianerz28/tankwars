// Author: Grant Nations
// Author: Sebastian Ramirez
// Control command class for CS 3500 TankWars Client (PS8)

using TankWars;
using Newtonsoft.Json;

namespace GameModel
{
    /// <summary>
    /// Control commands are how the client will tell the server what it wants to do (moving, firing, tdir). 
    /// Properties are updated in the controller
    /// </summary>
    [JsonObject]
    public class ControlCmd
    {
        [JsonProperty(PropertyName = "moving")]
        public string Moving { get; set; }

        [JsonProperty(PropertyName = "fire")]
        public string Fire { get; set; }

        [JsonProperty(PropertyName = "tdir")]
        public Vector2D Tdir { get; set; }
    }
}
