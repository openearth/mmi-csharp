using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ModelMessageInterface
{
    public struct MmiMessage
    {
        public string Name;

        public int[] Shape;
        
        public DateTime TimeStamp;

        public string DataType;

        [JsonIgnore]
        public Array Values;

        public static MmiMessage FromJson(string json)
        {
            return MmiMessageHandler.FromJson(json);
        }

        public string ToJson()
        {
            return MmiMessageHandler.ToJson(this);
        }
    }
}