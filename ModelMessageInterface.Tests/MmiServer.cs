using System;
using ZMQ;

namespace ModelMessageInterface.Tests
{
    internal class MmiServer : IDisposable
    {
        private readonly Context context;
        private Socket socket;

        public MmiServer()
        {
            context = new Context();
        }

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Dispose();
            }

            context.Dispose();
        }

        public void Start(uint port)
        {
            socket = context.Socket(SocketType.PUB);
            socket.Bind(Transport.TCP, "*", port);
        }

        public void Send<T>(T[] data)
        {
            var message = new MmiMessage
            {
                TimeStamp = DateTime.Now,
                DataType = MmiMessageHandler.GetDataTypeName(typeof(T)),
                Name = "water level",
                Shape = GetShape(data),
                Values = data
            };

            MmiMessageHandler.SendMessageAndData(socket, message);
        }

        private int[] GetShape(Array data)
        {
            var shape = new int[data.Rank];
            for (var i = 0; i < data.Rank; i++)
            {
                shape[i] = data.GetLength(i);
            }

            return shape;
        }
    }
}