using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Management
{
    [DataContract]
    public class ArmError
    {
        public string Message { get; set; }
        public string Code { get; set; }
    }

    [DataContract]
    public class ArmErrorBase
    {
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


         
    }
}
