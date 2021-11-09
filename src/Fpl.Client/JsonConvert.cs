using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fpl.Client;

internal class JsonConvert
{
    public static JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
