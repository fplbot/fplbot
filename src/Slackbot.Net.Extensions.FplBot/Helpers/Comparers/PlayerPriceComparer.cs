using System.Collections.Generic;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.PriceMonitoring
{
    public class PlayerPriceComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player x, Player y)
        {
            if (x == null && y == null)
                return true;
            
            if (x == null || y == null)
                return false;
            
            return x.Id == y.Id && x.CostChangeEvent == y.CostChangeEvent;
        }

        public int GetHashCode(Player obj)
        {
            return obj.Id;
        }
    }

    public class StatusComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player x, Player y)
        {
            if (x == null && y == null)
                return true;
            
            if (x == null || y == null)
                return false;
            
            return x.Id == y.Id && x.Status == y.Status && x.News == y.News;
        }

        public int GetHashCode(Player obj)
        {
            return obj.Id;
        }
    }
    
    public class PlayerTransfersComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player x, Player y)
        {
            if (x == null && y == null)
                return true;
            
            if (x == null || y == null)
                return false;
            
            return x.Id == y.Id && x.TransfersInEvent == y.TransfersInEvent &&  x.TransfersOutEvent == y.TransfersOutEvent;
        }

        public int GetHashCode(Player obj)
        {
            return obj.Id;
        }
    }
}