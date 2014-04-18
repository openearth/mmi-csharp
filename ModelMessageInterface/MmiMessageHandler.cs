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
            var json = socket.Recv(Encoding.UTF8);

            var message = new MmiMessage();

            message.FillFromJson(json);

            // receive data
            if (socket.RcvMore)
            {
                var bytes = socket.Recv();
                message.Values = BytesToArray(bytes, message.DataType, message.Shape);
            }

            return message;
        }

        public static void SendMessageAndData(Socket socket, MmiMessage message)
        {
            // send message
            if (message.Values == null)
            {
                socket.Send(message.ToJson(), Encoding.UTF8);
            }
            else
            {
                socket.Send(message.ToJson(), Encoding.UTF8, SendRecvOpt.SNDMORE);

                // send data
                var bytes = ArrayToBytes(message.Values);
                socket.Send(bytes);
            }
        }
    }
}
