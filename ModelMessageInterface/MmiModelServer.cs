using System;
using System.Diagnostics;
using BasicModelInterface;
using Newtonsoft.Json.Linq;
using ZMQ;

namespace ModelMessageInterface
{
    /// <summary>
    /// Runs a server that wraps a BMI model and exposes it using MMI in synchroneous way.
    /// </summary>
    public class MmiModelServer : IDisposable
    {
        private readonly IBasicModelInterface model;

        private Context context;
        private Socket socket;
        
        private readonly string connectionString;
        private readonly string protocol;
        private readonly string host;
        private readonly uint port;

        public MmiModelServer(string connectionString, IBasicModelInterface model)
        {
            this.model = model;
            this.connectionString = connectionString;

            MmiHelper.ParseConnectionString(connectionString, out protocol, out host, out port);
        }

        public IBasicModelInterface Model { get { return model; } }

        public string Protocol { get { return protocol; } }

        public string Host { get { return host; } }

        public uint Port { get { return port; } }

        public void MessageLoop()
        {
            while (true)
            {
                ProcessNextMessage();
            }
        }

        public void ProcessNextMessage()
        {
            var msg = MmiHelper.ReceiveMessageAndData(socket);
            Debug.WriteLine("Server: " + msg.Json);

            ProcessMessage(msg);
        }

        private void ProcessMessage(MmiMessage msg)
        {
            dynamic arguments = null;
            if (msg.Arguments != null)
            {
                arguments = JObject.Parse(msg.Arguments);
            }

            switch (msg.Action)
            {
                case "initialize":
                    if (arguments == null || arguments.config_file == null)
                    {
                        throw new InvalidOperationException("config_file argument must be speficied for initialize()");
                    }

                    model.Initialize((string)arguments.config_file);
                    SendReply();
                    break;
                case "update":
                    break;
                case "finalize":
                    break;
                case "get_current_time":
                    break;
                case "get_1d_double":
                    break;
                case "get_1d_double_at_index":
                    break;
                case "get_1d_int":
                    break;
                case "get_2d_int":
                    break;
            }
        }

        private void SendReply(MmiMessage reply = null)
        {
            if (reply == null)
            {
                reply = new MmiMessage();
            }

            MmiHelper.SendMessageAndData(socket, reply);
        }

        public void Bind()
        {
            context = new Context();

            socket = context.Socket(SocketType.REP);

            Trace.Assert(Protocol.ToLower().Equals("tcp"), "Only TCP is supported for now");

            socket.Bind(Transport.TCP, Host, Port);
        }

        public void Send<T>(T[] data)
        {
            var message = new MmiMessage
            {
                TimeStamp = DateTime.Now,
                DataType = MmiHelper.GetDataTypeName(typeof(T)),
                Name = "water level",
                Shape = GetShape(data),
                Values = data
            };

            MmiHelper.SendMessageAndData(socket, message);
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

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Dispose();
            }

            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}