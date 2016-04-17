using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Management
{
    [DataContract]
    public class ArmError
    {
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public string Code { get; set; }
    }

    [DataContract]
    public class ArmErrorBase
    {
        [DataMember]
        public ArmError Error { get; set; }
    }
    [DataContract]
    public class ServicebusAuthorizationKeys : ArmErrorBase
    {
        [DataMember]
        public string PrimaryConnectionString { get; set; }
        [DataMember]
        public string SecondaryConnectionString { get; set; }
        [DataMember]
        public string PrimaryKey { get; set; }
        [DataMember]
        public string SecondaryKey { get; set; }


        public bool IsAccessible {
            get { return Error == null; }
        }
    }
    public class ArmClient
    {


        protected HttpClient Client { get; set; }
        public ArmClient(AuthenticationHeaderValue authorization)
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Authorization = authorization;
        }

        public ArmClient(string accessToken) : this(new AuthenticationHeaderValue("bearer",accessToken))
        {
            
        }

        public Task<T> ListKeysAsync<T>(string resourceId, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}/listkeys?api-version={apiVersion}";

            return Client.PostAsync(resourceUrl, new StringContent(string.Empty))
                .As<T>();
        }

        public Task<T> PatchAsync<T>(string resourceId, T value, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}?api-version={apiVersion}";
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), resourceUrl);
            var valuestr = JsonConvert.SerializeObject(value);
            request.Content = new StringContent(valuestr, Encoding.UTF8, "application/json");

            return Client.SendAsync(request)
                .As<T>();
        }

        public Task<T> GetAsync<T>(string resourceId, string apiVersion)
        {
            var resourceUrl = $"https://management.azure.com/{resourceId.Trim('/')}?api-version={apiVersion}";
            return Client.GetAsync(resourceUrl).As<T>();
        }
    }
}
