using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ZMQ;

namespace ModelMessageInterface
{
    public static class MmiHelper
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

        public static string GetDataTypeName(Type type)
        {
            return SupportedTypes.Keys.ElementAt(SupportedTypes.Values.ToList().IndexOf(type));
        }

        public static Array BytesToArray(byte[] bytes, string valueType, int[] shape)
        {
            var values = Array.CreateInstance(SupportedTypes[valueType], shape);
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            return values;
        }

        public static byte[] ArrayToBytes(Array array)
        {
            var elementSize = System.Runtime.InteropServices.Marshal.SizeOf(array.GetValue(0));
            var size = array.Length * elementSize;
            var bytes = new byte[size];

            Buffer.BlockCopy(array, 0, bytes, 0, size);

            return bytes;
        }

        public static MmiMessage ReceiveMessageAndData(Socket socket)
        {
            // receive message
            string json;

            MmiMessage message;

            lock (socket)
            {
                json = socket.Recv(Encoding.UTF8);

                message = new MmiMessage {JsonString = json};
                message.FillFromJson(json);

                // receive data
                if (socket.RcvMore)
                {
                    byte[] bytes;
                    bytes = socket.Recv();

                    message.Values = BytesToArray(bytes, message.DataType, message.Shape);
                }
            }

            return message;
        }

        public static void SendMessageAndData(Socket socket, MmiMessage message)
        {
            message.TimeStamp = DateTime.Now;

            var values = message.Values;
            if (values != null)
            {
                if (message.Shape == null)
                {
                    var shape = new int[values.Rank];
                    for (var i = 0; i < values.Rank; i++)
                    {
                        shape[i] = values.GetLength(i);
                    }
                    message.Shape = shape;
                }

                if (string.IsNullOrEmpty(message.DataType))
                {
                    message.DataType = GetDataTypeName(values.GetValue(0).GetType());
                }
            }

            var json = message.ToJson();

            if (values == null)
            {
                lock (socket)
                {
                    socket.Send(json, Encoding.UTF8);
                }
            }
            else
            {
                lock (socket)
                {
                    socket.Send(json, Encoding.UTF8, SendRecvOpt.SNDMORE);

                    // send data
                    var bytes = ArrayToBytes(values);

                    socket.Send(bytes);
                }
            }
        }

        public static void SendMessage(Socket socket, object o, Array values = null)
        {
            var json = JsonConvert.SerializeObject(o);
            
            lock (socket)
            {
                if (values == null)
                {
                    socket.Send(json, Encoding.UTF8);
                }
                else
                {
                    socket.Send(json, Encoding.UTF8, SendRecvOpt.SNDMORE);

                    // send data
                    var bytes = ArrayToBytes(values);
                    socket.Send(bytes);
                }
            }
        }

        /// <summary>
        /// Parse connection string in a form "protocol://host:port"
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static void ParseConnectionString(string connectionString, out string protocol, out string host, out uint port)
        {
            var str = connectionString.Split(':');

            protocol = str[0];
            host = str[1].Substring(2);
            port = uint.Parse(str[2]);
        }
    }
}
