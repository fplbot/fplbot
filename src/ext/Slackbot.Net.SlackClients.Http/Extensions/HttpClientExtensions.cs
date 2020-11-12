using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Responses;

namespace Slackbot.Net.SlackClients.Http.Extensions
{
    internal static class HttpClientExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static async Task<T> PostJson<T>(this HttpClient httpClient, object payload, string api, Action<string> logger = null) where T:Response
        {
            var serializedObject = JsonConvert.SerializeObject(payload, JsonSerializerSettings);
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
            
            var resObj = JsonConvert.DeserializeObject<T>(responseContent, JsonSerializerSettings);

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

            var resObj = JsonConvert.DeserializeObject<T>(responseContent, JsonSerializerSettings);
            
            if(!resObj.Ok)
                throw new WellKnownSlackApiException(error: $"{resObj.Error}", responseContent:responseContent);
            
            return resObj;        
        }
    }
}