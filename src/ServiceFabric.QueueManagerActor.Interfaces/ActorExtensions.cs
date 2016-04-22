using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Actors
{
   
    [DataContract]
    public class JsonModel<T>
    {
        private static JsonSerializer _serialiser = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() });

        public string StateName { get; set; }

        private Lazy<string> _rawJson;
        private Lazy<T> _model;
        private MessageClusterResource model;

        public JsonModel(T model)
        {
            Model = model;
        }

        [DataMember]
        public string Value { get { return _rawJson.Value; } set { _rawJson = new Lazy<string>(()=> value); _model = new Lazy<T>(deserializer); } }

        public T Model { get { return _model.Value; } set { _model = new Lazy<T>(() => value); _rawJson = new Lazy<string>(serializer); } }


        private T deserializer()
        {
           return _serialiser.Deserialize<T>(new JsonTextReader(new StringReader(_rawJson.Value)));
        }

       
        private string serializer()
        {
            var ms = new MemoryStream();
            using (var wr = new StreamWriter(ms))
            {
                _serialiser.Serialize(wr, _model.Value);
                wr.Flush();
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public bool HasValue { get { return !string.IsNullOrEmpty(_rawJson.Value); } }


   
    }
    public static class ActorExtensions
    {
        private static JsonSerializer _serialiser = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() });

        public static string GetClusterName(this IMessageClusterActor actor)
        {
            return actor.GetActorId().GetStringId().Split('/').Last();
        }


        //public static async Task<JsonModel<T>> GetJsonModelAsync<T>(this IActorStateManager actor, string stateName)
        //{

        //    return await actor.TryGetStateAsync<JsonModel<T>>(stateName);

        //    if (json.HasValue) {
        //        var str = Encoding.UTF8.GetString(json.Value);
        //        return new JsonModel<T> {
        //            Model = _serialiser.Deserialize<T>(new JsonTextReader(new StringReader(str))),
        //            Value = str,
        //           StateName = stateName
        //        };
        //    }
        //    return new JsonModel<T>();
        //}
        public static async Task<T> GetJsonModelAsync<T>(this IActorStateManager actor,string stateName)
        {
            var json = await actor.GetStateAsync<JsonModel<T>>(stateName);
            return json.Model;
        }
        public static Task<JsonModel<T>> SetJsonModelAsync<T>(this IActorStateManager actor, string stateName, T model)
        {
            var jsonValue = new JsonModel<T>(model);
            return actor.AddOrUpdateStateAsync(stateName, jsonValue,(k,v)=>jsonValue);


        }
        public static Task<bool> TrySetJsonModelAsync<T>(this IActorStateManager actor, string stateName, T model)
        {
            var jsonValue = new JsonModel<T>(model);
            return actor.TryAddStateAsync(stateName, jsonValue);

        }

        public static async Task<string> SetProvisioningStatus(this Actor actor, string status)
        {
            await actor.StateManager.SetStateAsync("provisioningStatus", status);
            return status;
        }
        public static async Task<string> GetProvisioningStatus(this Actor actor)
        {
            var provisioningStatus = await actor.StateManager.TryGetStateAsync<string>("provisioningStatus");
            return provisioningStatus.HasValue? provisioningStatus.Value: ClusterActorProvisioningStatus.Unprovisioned;
        }

        public static async Task<bool> IsInitialized(this Actor actor)
        {
            var isProvisioned=await actor.StateManager.TryGetStateAsync<bool>("isInitialized");
            return isProvisioned.HasValue && isProvisioned.Value;
        }
        public static Task SetIsInitialized(this Actor actor, bool isInitialized)
        {
            return actor.StateManager.SetStateAsync("isInitialized", isInitialized);
        }
    }
    public static class ClusterActorProvisioningStatus
    {
        public const string Unprovisioned = "Unprovisioned";
        public const string Provisioning = "Provisioning";
        public const string Initializing = "Initializing";
        public const string Updating = "Updating";
    }
}
