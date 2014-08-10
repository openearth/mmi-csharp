using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ModelMessageInterface
{
    public static class MmiMessageExtensions
    {
        public static void FillFromJson(this MmiMessage message, string json)
        {
            var jsonObject = JObject.Parse(json);

            message.Json = jsonObject;

            message.Name = jsonObject.Value<string>("name");
            
            message.DataType = jsonObject.Value<string>("dtype");
            message.TimeStamp = jsonObject.Value<DateTime>("timestamp");

            var jsonObjectShape = jsonObject["shape"];
            if (jsonObjectShape != null)
            {
                message.Shape = jsonObjectShape.Values<int>().ToArray();
            }

            message.Arguments = jsonObject.Value<string>("arguments");


            // TODO: check keywords, if JSON has key 'get_var' - action = 'get_var'
            message.Action = jsonObject.Value<string>("action");

            // special case, zero-rank array (single value)
            if (message.Shape != null)
            {
                message.Shape = message.Shape.Length == 0 ? new[] {1} : message.Shape;
            }
        }

        public static string ToJson(this MmiMessage message)
        {
            message.JsonString = JsonConvert.SerializeObject(message, Formatting.None, SerializerSettings);

            return message.JsonString;
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new LowercaseContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private class LowercaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                if (propertyName.Equals("DataType"))
                {
                    return "dtype";
                }

                return propertyName.ToLower();
            }
        }

    }
}