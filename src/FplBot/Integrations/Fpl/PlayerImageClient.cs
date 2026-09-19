using Fpl.Client.Abstractions;

namespace Fpl.Client;

public class PlayerImageClient(HttpClient httpClient) : IPlayerImageClient
{
    private const string FallbackImageUrl =
        "https://user-images.githubusercontent.com/206726/73577018-207e4100-447c-11ea-98e3-9cc598c56519.png";

    public async Task<string> GetPlayerImageUrl(int playerCode)
    {
        var imageUrl = $"premierleague/photos/players/110x140/p{playerCode}.png";
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, imageUrl));
        return response.IsSuccessStatusCode ? new Uri(httpClient.BaseAddress!, imageUrl).ToString() : FallbackImageUrl;
    }
}
