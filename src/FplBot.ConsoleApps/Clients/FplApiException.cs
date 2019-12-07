using System;

namespace FplBot.ConsoleApps.Clients
{
    internal class FplApiException : Exception
    {
        public FplApiException(string message) : base(message)
        {
        }
    }
}