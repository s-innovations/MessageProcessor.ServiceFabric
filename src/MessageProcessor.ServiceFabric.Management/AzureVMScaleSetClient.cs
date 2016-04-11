using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Management
{
    public class AzureVMScaleSetClient
    {
        protected HttpClient Client { get; set; }
        public AzureVMScaleSetClient(AuthenticationHeaderValue authorization)
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Authorization = authorization;


        }


    }
}
