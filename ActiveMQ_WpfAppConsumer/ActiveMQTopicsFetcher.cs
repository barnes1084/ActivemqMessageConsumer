using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Schedule;
using System.Collections.Generic;
using System.Linq;

namespace ActiveMQ_WpfAppConsumer
{
    public class ActiveMQTopicsFetcher
    {
        private readonly HttpClient httpClient = new HttpClient();

        public async Task<List<string>> GetActiveMQTopicsAsync(string jolokiaUrl)
        {
            try
            {
                var byteArray = Encoding.ASCII.GetBytes($"admin:admin");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                HttpResponseMessage response = await httpClient.GetAsync(jolokiaUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseBody);
                var topicNames = json["value"]
                    .Children()
                    .Cast<JProperty>()
                    .Select(jp => new {
                        Name = jp.Name,
                        ConsumerCount = (int?)(jp.Value["ConsumerCount"] ?? 0),
                        ProducerCount = (int?)(jp.Value["ProducerCount"] ?? 0),
                        EnqueueCount = (int?)(jp.Value["EnqueueCount"] ?? 0),
                        DequeueCount = (int?)(jp.Value["DequeueCount"] ?? 0)
                    })
                    .Where(jp => jp.Name.Contains("destinationType=Topic")
                                 && (jp.ConsumerCount > 0))// || jp.ProducerCount > 0 || jp.EnqueueCount > 0 || jp.DequeueCount > 0))
                    .Select(jp => {
                        var parts = jp.Name.Split(',');
                        var destinationPart = parts.FirstOrDefault(part => part.StartsWith("destinationName="));
                        return destinationPart?.Split('=')[1];
                    })
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                return topicNames ?? new List<string>();

            }
            catch (Exception e)
            {
                Log.ToFile($"Exception Caught: {e.Message}");
                return new List<string>();
            }
        }
    }
}


