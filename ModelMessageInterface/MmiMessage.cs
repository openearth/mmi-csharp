using System;
using Newtonsoft.Json;

namespace ModelMessageInterface
{
    public struct MmiMessage
    {
        /// <summary>
        /// Possible actions (request messages) are:
        /// 
        ///     initialize
        ///     update
        ///     finalize
        /// 
        ///     get_variable_count
        ///     get_variable
        ///     get_variable_info - query only variable information (no data)
        ///     set_variable
        /// 
        /// </summary>
        public string Action;
        
        public string Name;

        public int[] Shape;

        public string DataType;

        public DateTime TimeStamp;

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