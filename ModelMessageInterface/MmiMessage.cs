using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelMessageInterface
{
    public class MmiMessage
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

        /// <summary>
        /// Optional. Json string containing arguments. 
        /// </summary>
        public string Arguments; 

        public string Name;

        public int[] Shape;

        public string DataType;

        public DateTime TimeStamp;

        [JsonIgnore] public Array Values;

        [JsonIgnore] public string JsonString;

        [JsonIgnore] public JObject Json;
    }
}