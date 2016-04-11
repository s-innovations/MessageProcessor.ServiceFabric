using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions.Models;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Abstractions
{
    public class VariableReplacerConverter : JsonConverter
    {
        private Dictionary<string, string> dictionary;

        public VariableReplacerConverter(Dictionary<string, string> dictionary)
        {
            this.dictionary = dictionary;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(string) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
           var token= JToken.Load(reader).ToString();
            if (token.StartsWith("["))
            {
                token= Regex.Replace(token, "\\[variables\\('(.*)'\\)\\]", m =>
                  {
                      if (m.Success)
                          return dictionary[m.Groups[1].Value];
                      return m.Value;
                  });
            }
            return token;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageClusterResourceBaseConverter : JsonConverter
    {
       
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var type = item["type"].Value<string>();

            MessageClusterResourceBase obj = null;

            switch (type)
            {

                case MessageClusterResourceBase.ClusterQueueType:
                    obj = new ClusterQueueInfo();
                    serializer.Populate(item.CreateReader(), obj);
                    break;
                 

                case MessageClusterResourceBase.MessageClusterType:
                    obj = new MessageClusterResource();
                    var vars = new VariableReplacerConverter(item["variables"].ToObject<Dictionary<string, string>>());
                    serializer.Converters.Add(vars);
                    serializer.Populate(item.CreateReader(), obj);
                    serializer.Converters.Remove(vars);
                   
                    break;

                default:
                    throw new NotImplementedException();
            }
            return obj;

            
        }

   

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
