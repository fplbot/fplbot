using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public record EventStatusResponse
    {
        [JsonProperty("status")]
        public ICollection<EventStatus> Status { get; set; } = new List<EventStatus>();
        
        [JsonProperty("leagues")]
        public string Leagues { get; set; }
    }

    public record EventStatus
    {
        [JsonProperty("bonus_added")]
        public bool BonusAdded { get; set; }
        
        /// <summary>
        /// YYYY-MM-dd
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }
        
        [JsonProperty("event")]
        public int Event { get; set; }
        
        [JsonProperty("points")]
        public string PointsStatus { get; set; }
    }

    public class EventStatusConstants
    {
        public class LeaguesStatus
        {
            public const string Nothing = "";
            public const string Updated = "Updated";
        }
        
        public class PointStatus
        {
            public const string Nothing = "";
            public const string Ready = "r";    
        }
    }
}