using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace FplBot.ConsoleApps
{
    public class FplClientHttpClientHandler : HttpClientHandler
    {
        public FplClientHttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip;
            SslProtocols = SslProtocols.Tls12;
        }
    }
}