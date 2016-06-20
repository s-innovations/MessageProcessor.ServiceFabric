using System;
using System.Fabric;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Services;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Configuration;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Management;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.ARM;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Stores;

namespace ServiceFabricTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var str = "ew0KICAgICJkYXRhIjogICJNSUlLbHdJQkF6Q0NDbE1HQ1NxR1NJYjNEUUVIQWFDQ0NrUUVnZ3BBTUlJS1BEQ0NCZzRHQ1NxR1NJYjNEUUVIQWFDQ0JmOEVnZ1g3TUlJRjl6Q0NCZk1HQ3lxR1NJYjNEUUVNQ2dFQ29JSUUvakNDQlBvd0hBWUtLb1pJaHZjTkFRd0JBekFPQkFqaFlpYVhzZkF6a3dJQ0I5QUVnZ1RZalpCM0g2a21ZVmlsQVEyN2M3MkZncXF2cXZGalAxcDRwVlNzZGg1M3VqN3FFODRTZWdDN25USHZTKzZFRzJieWRKSHMvRDdVREhrYlR2SXowYjRhdzQvQ3A0NHhUMjNqN3RPYWFvTHJhOUliMWF0QVB2TFdWRnZxd1ZVUzM3NTV2WTluWk9SalA5cit3eUQ4LzBPWGswSnZ1S0VCNk0vUE82cjgySHZsbk9GZlY1SGMrWGt1emxFR3N0MW81MFZYa0RPRG5BRFkvU3lUZTl6Uy82TWc0bk9TZUVlYUpzWUNkYTZQUWhQNzlNcy8zK1hVdThMK2dlSkJxK2FXU1dKRWdPa2J4L0dOSUk3NTE5V0lJSU9GYnZIN3VXejlINHdObmZlQ0UydEF3bXhSNnBhTVFZZjA5bStjSlNYeEMxcTVOL1RnZmF5R0ltYytKMFBHUTI3YWkwRDR5VUJibWJnMlBtZ2tuTDlWVHJ3QkVVUVZDZTNJVEpEOWI1WEd4aDF3bnprd0FFSHJaVFNxUWdkQmprMXZUNHdZMm1FZFJuVkUyUUZTTFVpNEhWYWRUNENTNFpJS3plbW9FOUoyczZFSkREeFB2a2QxS05HVlVSZndvWWE4OWl5Vi9pMmxMLzgwSFgxMHNkYUtVbzV3clBNc3ZycnFEcUdldng4OHlDUDZTTFJTVllydHlObTEwd2pLZGdtSWVVWENKQ01rQllhanVkd1czL0s5WjFMOE1jeldDaDBWc0FWbWdYSzlJN0NhSkF3WStIdmgvNTVNcjNmWlJhT1kyYVpId1V3eENETUNlRXVOUVZsNlhEOUpPWEtkL1pRV05aZnB6Yk1hTDlWOTYzdElsY0E0di81dlhMZ3N4SUJGTTIwV3R2bm4wV1c1T1ZJdkRSdy9hVnQ1clRWSXoxOUdlZ1RublJCNjlDTHJYNHZXT0hKREZ3ZWJVZVJOd000UFFsdjdpd25YTXIrNWxYcWNmRDhiY3BCcG11eldGVWlvenhzL00wbThXWmM4K0drU0dyTEpydDRYM2dMbDNTLzk3NGE0WWY4aUNKdzBPUjFjRXJhbzN1dUVVcGQ3TFlVd0JqWjcxUVlmdytoV25oTVFLc3dHb2N4dGFGS1N1YjlCdTFEcjNDTTBoUEdEdVZURDVlVG5sS1NXNWxNSzNiUXBKTFNWTGxoR05ERy9QdTgwaVprWG9odlZQWlpkRlByVnBCL0J1dnpvaDlmMk5ONFp1ZXlWYkc3dXFvWUQzSG56MEgxakNRdjdTdElRdGI3ckRhcGlRb3Y1UkpkTWpCM0gwbXBITFIvbWtXeTJXTllwNFZaMXk5d25DWjQvZVZ0T1RLSVRjdHdVUllaVXRNYjNQK2Q4ZUNZSjVKSHdndzNGSFMxNElISkdQUm51NS9PVTJLeEtPMERLam9NRktReHFlWkFYQzJ5VEZ1L0RyVXVQdDhkeGk1T25ObFJqV1BiKy9tQk1XWUhTMXpZa2hucmNXT2J3Q1c1cVAwd2xPVmRsWGRKK292d1Vhc3Z0MC9Gc282eVFmRGIyeElLcEFXdTh5THRJVmZ1NjlOR0dKMk5pUCtWdThGZnQ1a3J1M3NLV2JKS3J2ZU83MGxWeFZFbGQ0eTlPN1hBdmJrdXlDOHhsS0M3bEVGNXVJUDNudkxYdGdzOTc5ckpTNmdjUTlKNGpvbDR2TGlEblNMRjVrOHY3eUNtWkRHV2J6MjZBYzVtVjVUa0FjRUFrdXI2R0diS0FHOGk4ZkhQZFBqOUZCbjdOL0hoMkV4b044dEViK05JK1BtM00xaVNqYlhRQjRhQWZvcTRBdFErZnNMZm05c24zSmx4VWxEazhMNytrQXpwQm04Zm1BZVNGcHhVUTdXczZ1RzJRcTNWeExVYW0zb2R2R0NJL0hZRnluTVlQUnVXbThRMWZwWHMvS2RKSGtjMmg5UzlTenBVYlA5UDZuN3hYRDlkdGxoUDNzZ2pnbG81U1BURmo0azZWKzQ1SFVvaWdIQ3lCdmljM2pmYXN1R0ZsemhCT2NIYWdZYkFLT2V4M1dwMGhCaHVJUk5yY3VVajd3Slk4QWs4aGlZTFZWbjllZW16TVNZU1FsdFIvTVpkMDk3QkU4Ly8vNG5Hcmd0SGd4MWFKTk0rbDFDNm1XSUVVN05xRGptS0FpVGhqdlp0bGwxTkpxUlJFbmJHRlRRMC82REdCNFRBVEJna3Foa2lHOXcwQkNSVXhCZ1FFQVFBQUFEQmRCZ2txaGtpRzl3MEJDUlF4VUI1T0FIUUFaUUF0QUdVQU13QmpBRGdBTUFBMEFHRUFOd0F0QURrQU5RQXpBRGdBTFFBMEFEZ0FNd0ExQUMwQVlnQXhBREVBT1FBdEFEVUFZd0F3QUdNQU9BQTBBRE1BT1FBMEFEUUFZd0JsTUdzR0NTc0dBUVFCZ2pjUkFURmVIbHdBVFFCcEFHTUFjZ0J2QUhNQWJ3Qm1BSFFBSUFCRkFHNEFhQUJoQUc0QVl3QmxBR1FBSUFCREFISUFlUUJ3QUhRQWJ3Qm5BSElBWVFCd0FHZ0FhUUJqQUNBQVVBQnlBRzhBZGdCcEFHUUFaUUJ5QUNBQWRnQXhBQzRBTURDQ0JDWUdDU3FHU0liM0RRRUhBYUNDQkJjRWdnUVRNSUlFRHpDQ0JBc0dDeXFHU0liM0RRRU1DZ0VEb0lJRHNUQ0NBNjBHQ2lxR1NJYjNEUUVKRmdHZ2dnT2RCSUlEbVRDQ0E1VXdnZ0o5b0FNQ0FRSUNFRWk5NldaYWJ6dUJTTEE0TTJuTWM4Z3dEUVlKS29aSWh2Y05BUUVGQlFBd1BURTdNRGtHQTFVRUF3d3lkM2QzTG5CcmMzTmxjblpwWTJWbVlXSnlhV011ZDJWemRHVjFjbTl3WlM1amJHOTFaR0Z3Y0M1aGVuVnlaUzVqYjIwd0hoY05NVFl3TXpBMU1UUXdPVE00V2hjTk1UY3dNekExTVRReU9UTTRXakE5TVRzd09RWURWUVFERERKM2QzY3VjR3R6YzJWeWRtbGpaV1poWW5KcFl5NTNaWE4wWlhWeWIzQmxMbU5zYjNWa1lYQndMbUY2ZFhKbExtTnZiVENDQVNJd0RRWUpLb1pJaHZjTkFRRUJCUUFEZ2dFUEFEQ0NBUW9DZ2dFQkFMeWd5cyt3UE9laWhDVG5aUVJNTHJoS2Q2Zjh0SHpma1o5bFRFOTRnK083aHBHM2UyWnU1ZFBOMHdMRDdWRmJMMW1rUG1RSEtnNWFiSm93NHlaZE9EUHhEVWdtYjVWdzZWNXJCMDVrclhRZUJ2S2hmVzcrVzM2U2hObFRLYllBdExXOVhxR3VIQVJQa0g2RkJhMm9VUDBSMEE3cXBvSFZZS1ZnM0tkOXBXN1pVcmlZZ2orT3MrVXAxUDFzUzJNM2xkT2hZUzNhSzlRcDEzaGs0TXFSRGlObjFodThJdzV1T2dGRHVRNE9qS2ovQUI4RzF0SitiNnZZVmJiWWRFU1VYU0xaMzEwTTFJUGU3S3VaUk95eVUyUE55TjJETGVrWDgweVl5YTdGNkt3RUNtRkJ4eUp1azA1bDlWRGVGL09Dbks3NmRZN1ZMUlFtRFlHU0FmelRNNTBDQXdFQUFhT0JrRENCalRBT0JnTlZIUThCQWY4RUJBTUNCYUF3SFFZRFZSMGxCQll3RkFZSUt3WUJCUVVIQXdJR0NDc0dBUVVGQndNQk1EMEdBMVVkRVFRMk1EU0NNbmQzZHk1d2EzTnpaWEoyYVdObFptRmljbWxqTG5kbGMzUmxkWEp2Y0dVdVkyeHZkV1JoY0hBdVlYcDFjbVV1WTI5dE1CMEdBMVVkRGdRV0JCU2g1QTVFSlhReXl6dXhmazBNZDVCYmE1MUFIREFOQmdrcWhraUc5dzBCQVFVRkFBT0NBUUVBVlZWeE0vcklPK0hnazI5UUJnTjhHUWxZeXpBSWE3M3FhVVNhY21lU0FFMEY5MEU4SlJodVo1TmRmUW5pMk84UzA3Y1VPZUE0SWxqaHo3VEtDczhVa0R1SjZFWm9hblRGSm5DWTNqekN0T3V4R2dkbHVkNEM5R0xLcDNvR251R2JpaUV3ZUZSeDIrNWpWenoyM2x3VFk0eEk1T08ySllIZENJOVJEd29YelNaYmVRejBHcmljQ0lkdTVXRkZhUVk1ZmJ2V1ZjcmFHei9TNGorV3puZjUzK0MrcTlTN1ZxWnFFREJYY1JleE12T2tBZHZTY3JTeC9ETkRLK0YvL3pZVHhZRWMyK3E5NW5UUktqQVllay9uQnFFSjhKREpmTWx4dGp0MDZnSXB6d2JZZVpCSHc3SWtESXZselhXdHhVU2hENEJnZXVuYUhtTTFpZEhBOXA1b0V6RkhNQk1HQ1NxR1NJYjNEUUVKRlRFR0JBUUJBQUFBTURBR0Npc0dBUVFCZ2pjUkEwY3hJZ1FnUkFCRkFGTUFTd0JVQUU4QVVBQXRBRVFBTWdCUUFFTUFUd0JVQUU0QUFBQXdPekFmTUFjR0JTc09Bd0lhQkJSR1l5U3BmT3dHanhuaVdMOXk5bzV5cHRHbjdBUVVSSXFyOVdPc1RNRzZwbGw1Vi9QYy95ZVVHSDBDQWdmUSIsDQogICAgImRhdGFUeXBlIjogICJwZngiLA0KICAgICJwYXNzd29yZCI6ICAiMTIzNDU2Ig0KfQ==";
            var obj = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(str)));
            var cert = new X509Certificate2(Convert.FromBase64String(obj["data"].ToString()), obj["password"].ToString());

            var valueToEncrypt = "adsada";
            byte[] encoded = System.Text.UTF8Encoding.UTF8.GetBytes(valueToEncrypt);
            var content = new ContentInfo(encoded);
            var env = new EnvelopedCms(content);
            env.Encrypt(new CmsRecipient(cert));

            string encrypted64 = Convert.ToBase64String(env.Encode());
            Console.WriteLine(encrypted64);

            var envelope = new EnvelopedCms();
            envelope.Decode(Convert.FromBase64String(encrypted64));
            envelope.Decrypt();

            Console.WriteLine(Encoding.UTF8.GetString(envelope.ContentInfo.Content));

        }
        [TestMethod]
        public async Task TestMethod2()
        {
            var encrypted64 = "MIICLgYJKoZIhvcNAQcDoIICHzCCAhsCAQAxggFFMIIBQQIBADApMBUxEzARBgNVBAMTCmNpdGVzdGNlcnQCEG7nJgAd1pGtROAqXTINhgowDQYJKoZIhvcNAQEBBQAEggEARESvyeELwgWe08JSmoBKOIdFC3Tgw9wPpG6/BLFVtDI0kYgAZr74ftz/QSj7cyt9mTeJGt1WO8MHR/Q3QV/dkxyW/7NPU7dIrM8xliHwtCHQO5gZVnjARbpwPdYEi8NayqB3iRd+h8Sa4B3GudwX5xzZr5d2GmJ6FDL2F5UHYTF6zm7HUps28UybGgzgmRymE+9qoChnEA/DATPGq4w019pcIdkgZP8kodacqCVe9Bl4Izlgl44NTgkW9YSgGH550SMkK60Z/Z9TMzHcBRKyePUyYkwNeZ1uoMMilA7Zm5jK93iqD1ThTAdOodpgG4gy7CfIReqtN+T8yRa5ZmYUTzCBzAYJKoZIhvcNAQcBMBQGCCqGSIb3DQMHBAj3/gDO9Af1J4CBqOFSI4FKmu0gulZaaBp+uRwoLAAaNN0R981CItIkhuhFs4BsqK+YyjB7zZDwQnM8dI6R4V62Ys/ToiOVXYlvMP0OkfDmiHOqlWeIhTThSXc9/3uxZW/A1c32FJZ3DGD7SbUdJHcTIbTsXRl88SGY8mMPgdAwlDhMvphqXjMbf6YQZYVGJrn8xRHDk5FMRjIeOHBV2VYUHGGz+pStzjaQ+hETr3fihyZUig==";

            var envelope = new EnvelopedCms();
            envelope.Decode(Convert.FromBase64String(encrypted64));
            envelope.Decrypt();

            Console.WriteLine(Encoding.Unicode.GetString(envelope.ContentInfo.Content));
        }

        [TestMethod]
        public async Task TestMethod3()
        {
            using (var container = new UnityContainer())
            {
                var type = ActorProxyTypeFactory.CreateType<MessageClusterActor>();
                container.RegisterType<IActorDeactivationInterception, OnActorDeactivateInterceptor>(new HierarchicalLifetimeManager());
                container.RegisterType<IMessageClusterConfigurationStore, InMemoryClusterStore>(new HierarchicalLifetimeManager());
                var a = container.Resolve<IActorDeactivationInterception>();
                var b = container.Resolve<IMessageClusterConfigurationStore>();
                container.RegisterType(typeof(MessageClusterActor), type, new HierarchicalLifetimeManager());
                //   container.WithActor<MessageClusterActor>();
                //   container.WithActor<QueueListenerActor>();
                var actor = Activator.CreateInstance(type, new Object[] { a, b });
                var myActor = container.Resolve(type);
                //  var myActor1 = container.Resolve<QueueListenerActor>();
            }
            //var myActorType = Proxy.CreateType<MessageClusterActor>();
            //var myActor = Proxy.Create<MessageClusterActor>(new disposer(new UnityContainer()),null);

            //await myActor.Test();
        }
        [TestMethod]
        public async Task TestMethod4()
        {
            var a = "{\"id\":\"a\",\r\n  \"error\": {\r\n    \"code\": \"NotFound\",\r\n    \"message\": \"The entity was not found.\"\r\n  }\r\n}";
            
            var b = JsonConvert.DeserializeObject<ArmErrorBase>(a.Replace("\r\n",""));

         // /  var a = new test();
       // var tes=await   a.GetMessageClusterAsync("");
        }

        [TestMethod]
        public async Task TestMethod5()
        {

            var stream = typeof(ServiceFabricConstants).Assembly.GetManifestResourceStream("SInnovations.Azure.MessageProcessor.ServiceFabric.Resources.sampleConfiguration.json");
            var cluster = JsonConvert.DeserializeObject<MessageClusterResource>(await new StreamReader(stream).ReadToEndAsync(), new JsonSerializerSettings { });


            
          
        }
    }
}
