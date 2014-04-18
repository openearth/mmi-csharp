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

            message.Name = jsonObject.Value<string>("name");
            message.Shape = jsonObject["shape"].Values<int>().ToArray();
            message.DataType = jsonObject.Value<string>("dtype");
            message.TimeStamp = jsonObject.Value<DateTime>("timestamp");

            // special case, zero-rank array (single value)
            message.Shape = message.Shape.Length == 0 ? new[] { 1 } : message.Shape;
        }

        public static string ToJson(this MmiMessage message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, serializerSettings);
        }

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings { ContractResolver = new LowercaseContractResolver() };

        public class LowercaseContractResolver : DefaultContractResolver
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