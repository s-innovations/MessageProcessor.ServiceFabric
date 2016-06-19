using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SInnovations.Azure.MessageProcessor.ServiceFabric.Models
{
    public class VariableReplacableAttribute : Attribute
    {

    }
    public class VariableReplacerConverter : JsonConverter
    {
        private JToken variables;

        public VariableReplacerConverter(JToken variables)
        {
            this.variables = variables;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(string) == objectType || Attribute.IsDefined(objectType,typeof(VariableReplacableAttribute));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                var strToken = token.ToString();
                if (strToken.StartsWith("["))
                {
                    token = JToken.Parse(Regex.Replace(strToken, "\\[variables\\('(.*)'\\)\\]", m =>
                       {
                           if (m.Success)
                           {
                               var newValue = variables.SelectToken(m.Groups[1].Value);
                               if (newValue.Type == JTokenType.String)
                                   return $"'{newValue.ToString()}'";
                               return newValue.ToString();
                           }
                           return $"'{m.Value}'";
                       }));
                }
                if (objectType == typeof(string))
                    return token.ToString();

                //  serializer.Deserialize(JObject.Parse( token.ToString() ).CreateReader(), objectType);
            }
            else if (token.Type == JTokenType.Null)
                return null;

        

            var obj = Activator.CreateInstance(objectType);
            serializer.Populate(token.CreateReader(), obj);
            return obj;
            
        }
        public override bool CanWrite
        { get { return false; } }

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
                case MessageClusterResourceBase.ProcessorNodeType:
                    obj = new ClusterProcessorNode();
                    serializer.Populate(item.CreateReader(), obj);
                    break;
                case MessageClusterResourceBase.ClusterQueueType:
                    obj = new ClusterQueueInfo();
                    serializer.Populate(item.CreateReader(), obj);
                    break;
                case MessageClusterResourceBase.TopicType:
                    obj = new TopicInfo();
                    serializer.Populate(item.CreateReader(), obj);
                    break;

                case MessageClusterResourceBase.DispatcherType:
                    obj = new ClusterDispatcherInfo();
                    serializer.Populate(item.CreateReader(), obj);
                    break;

                case MessageClusterResourceBase.MessageClusterType:
                    obj = new MessageClusterResource();
                    var vars = new VariableReplacerConverter(item.SelectToken("variables"));
                    serializer.Converters.Add(vars);
                    serializer.Populate(item.CreateReader(), obj);
                    serializer.Converters.Remove(vars);
                   
                    break;

                default:
                    throw new NotImplementedException();
            }
            return obj;

            
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
