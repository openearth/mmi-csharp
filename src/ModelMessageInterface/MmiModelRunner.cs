using System;
using System.Diagnostics;
using BasicModelInterface;
using NetMQ;
using Newtonsoft.Json.Linq;

namespace ModelMessageInterface
{
    /// <summary>
    /// Runs a server that wraps a BMI model and exposes it using MMI in synchroneous way.
    /// </summary>
    public class MmiModelRunner : IDisposable
    {
        private readonly IBasicModelInterface model;

        private bool running;
        private bool finished;

        private NetMQContext context;
        private NetMQSocket socket;
        
        private readonly string protocol;
        private readonly string host;
        private readonly uint port;

        public MmiModelRunner(string connectionString, IBasicModelInterface model)
        {
            this.model = model;

            MmiHelper.ParseConnectionString(connectionString, out protocol, out host, out port);
        }

        public IBasicModelInterface Model { get { return model; } }

        public string Protocol { get { return protocol; } }

        public string Host { get { return host; } }

        public uint Port { get { return port; } }

        public void Start()
        {
            running = true;
            
            while (running)
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
                    model.Finish();
                    SendReply();
                    running = false;
                    break;
                case "get_var":
                    break;
                case "set_var":
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
            context = NetMQContext.Create();

            socket = context.CreateResponseSocket();

            Trace.Assert(Protocol.ToLower().Equals("tcp"), "Only TCP is supported for now");

            socket.Bind("tcp://" + Host + ":" + Port);
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