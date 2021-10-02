using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Slackbot.Net.Models.BlockKit;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Responses;

namespace Slackbot.Net.SlackClients.Http.Extensions
{
    internal static class HttpClientExtensions
    {
        private static readonly JsonSerializerOptions JsonSerializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new TypeDiscriminatorConverter<IBlock>(),
                new TypeDiscriminatorConverter<IElement>()
            },
            PropertyNamingPolicy = new LowerCaseNaming()
        };

        public static async Task<T> PostJson<T>(this HttpClient httpClient, object payload, string api, Action<string> logger = null) where T:Response
        {
            var serializedObject = JsonSerializer.Serialize(payload, JsonSerializerSettings);
            logger?.Invoke(serializedObject);
            var httpContent = new StringContent(serializedObject, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, api)
            {
                Content = httpContent
            };

            var response =  await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            logger?.Invoke($"{response.StatusCode} - {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                logger?.Invoke(serializedObject);
                logger?.Invoke(responseContent);
            }

            response.EnsureSuccessStatusCode();

            var resObj = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerSettings);

            if (!resObj.Ok)
            {
                logger?.Invoke(serializedObject);
                logger?.Invoke(resObj.Error);
                throw new WellKnownSlackApiException(error:$"{resObj.Error}",responseContent:responseContent);
            }

            return resObj;
        }

        public static async Task<T> PostParametersAsForm<T>(this HttpClient httpClient, IEnumerable<KeyValuePair<string, string>> parameters, string api, Action<string> logger = null) where T: Response
        {
            var request = new HttpRequestMessage(HttpMethod.Post, api);

            if (parameters != null && parameters.Any())
            {
                var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
                var requestContent = await formUrlEncodedContent.ReadAsStringAsync();
                var httpContent = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");
                httpContent.Headers.ContentType.CharSet = string.Empty;
                request.Content = httpContent;
            }

            var response =  await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger?.Invoke($"{response.StatusCode} \n {responseContent}");
            }

            response.EnsureSuccessStatusCode();

            var resObj = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerSettings);

            if(!resObj.Ok)
                throw new WellKnownSlackApiException(error: $"{resObj.Error}", responseContent:responseContent);

            return resObj;
        }
    }

    internal class LowerCaseNaming : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLower();
        }
    }

    public class TypeDiscriminatorConverter<T> : JsonConverter<T> where T : IHaveType
    {
        private readonly IEnumerable<Type> _types;

        public TypeDiscriminatorConverter()
        {
            var type = typeof(T);
            _types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToList();
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using (var jsonDocument = JsonDocument.ParseValue(ref reader))
            {
                if (!jsonDocument.RootElement.TryGetProperty(nameof(IBlock.type), out var typeProperty))
                {
                    throw new JsonException();
                }

                var type = _types.FirstOrDefault(x => x.Name == typeProperty.GetString());
                if (type == null)
                {
                    throw new JsonException();
                }

                var jsonObject = jsonDocument.RootElement.GetRawText();
                var result = (T) JsonSerializer.Deserialize(jsonObject, type, options);

                return result;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
