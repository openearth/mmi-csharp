using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelMessageInterface
{
    public class MmiMessage
    {

        /// <summary>
        /// Possible actions (messages) are:
        /// 
        ///     initialize
        ///     update
        /// 
        ///     remote : [ pause | play | skip ]
        /// 
        ///     finalize
        /// 
        ///     get_var_count
        ///     get_var
        ///     get_var_info - query only variable information (no data)
        ///     set_var
        /// 
        /// </summary>
        public string Action;

        /// <summary>
        /// Optional. Json string containing arguments. 
        /// </summary>
        public string Arguments;

        [JsonIgnore]
        public string JsonString;

        [JsonIgnore]
        public dynamic Json;

        // TODO: move to VariableInfo
        public string Name;

        public int[] Shape;

        public string DataType;

        public DateTime TimeStamp;

        [JsonIgnore] public Array Values;
    }
}