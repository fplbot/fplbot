using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fpl.Client
{
    internal class JsonConvert
    {
        public static T DeserializeObject<T>(string thing)
        {
            return JsonSerializer.Deserialize<T>(thing,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
        }

    }
}
