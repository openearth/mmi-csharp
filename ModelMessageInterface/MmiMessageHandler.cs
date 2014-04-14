using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ZMQ;
using ZMQ.ZMQDevice;

namespace ModelMessageInterface
{
    public static class MmiMessageHandler
    {
        /// <summary>
        /// Note, we use string representation of Python types here.
        /// </summary>
        public static readonly IDictionary<string, Type> SupportedTypes = new Dictionary<string, Type>
        {
            {"bool", typeof (bool)},
            {"int32", typeof (int)},
            {"float32", typeof (float)},
            {"float64", typeof (double)}
        };

        public static Array BytesToArray(byte[] bytes, string valueType, int[] shape)
        {
            var values = Array.CreateInstance(SupportedTypes[valueType], shape);
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return values;
        }

        public static byte[] ArrayToBytes(Array array)
        {
            if (array.GetValue(0) is double)
            {
                return array.Cast<double>().SelectMany(BitConverter.GetBytes).ToArray();
            }
            if (array.GetValue(0) is float)
            {
                return array.Cast<float>().SelectMany(BitConverter.GetBytes).ToArray();
            }
            if (array.GetValue(0) is int)
            {
                return array.Cast<int>().SelectMany(BitConverter.GetBytes).ToArray();
            }
            
            return array.Cast<double>().SelectMany(BitConverter.GetBytes).ToArray();
        }

        public static MmiMessage ReceiveMessageAndData(Socket socket)
        {
            // receive message
            var json = socket.Recv(Encoding.UTF8);
            var message = MmiMessage.FromJson(json);

            Debug.WriteLine(json);

            // receive data
            var bytes = socket.Recv();
            message.Values = BytesToArray(bytes, message.DataType, message.Shape);

            return message;
        }

        public static void SendMessageAndData(Socket socket, MmiMessage message)
        {
            // send message
            socket.Send(message.ToJson(), Encoding.UTF8, SendRecvOpt.SNDMORE);
            
            // send data
            var bytes = ArrayToBytes(message.Values);
            socket.Send(bytes);
        }

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings {ContractResolver = new LowercaseContractResolver()};

        public static MmiMessage FromJson(string json)
        {
            var jsonObject = JObject.Parse(json);

            var name = jsonObject.Value<string>("name");
            var shape = jsonObject["shape"].Values<int>().ToArray();
            var dtype = jsonObject.Value<string>("dtype");
            var timestamp = jsonObject.Value<DateTime>("timestamp");

            // special case, zero-rank array (single value)
            shape = shape.Length == 0 ? new[] { 1 } : shape;

            return new MmiMessage { Name = name, Shape = shape, DataType = dtype, TimeStamp = timestamp };
        }

        public static string ToJson(MmiMessage message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, serializerSettings);
        }

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
