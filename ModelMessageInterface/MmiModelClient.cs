using System;
using BasicModelInterface;
using Newtonsoft.Json;
using ZMQ;

namespace ModelMessageInterface
{
    /// <summary>
    /// Connects to a remote BMI model using MMI.
    /// </summary>
    public class MmiModelClient : IBasicModelInterface, IDisposable
    {
        private static Context context;
        private static Socket socket;

        readonly string protocol;
        readonly string host;
        readonly uint port;

        public MmiModelClient(string connectionString)
        {
            MmiHelper.ParseConnectionString(connectionString, out protocol, out host, out port);

            if (protocol.ToLower() != "tcp")
            {
                throw new NotSupportedException("Only TCP is supported for now");
            }
        }

        public void Connect()
        {
            context = new Context();
            socket = context.Socket(SocketType.REQ);
            socket.Connect(Transport.TCP, host, port);
        }

        public void Initialize(string configFile)
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "initialize", Arguments = JsonConvert.SerializeObject(new { config_file = configFile }) });
            ReceiveReply();
        }

        public void Update()
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "update", Arguments = JsonConvert.SerializeObject(new { time_step = TimeStep.TotalSeconds }) });
            ReceiveReply();
        }

        public void Finish()
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "finalize" });
            ReceiveReply();
        }

        public int[,] GetIntValues2D(string variable)
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "get_2d_int", Name = variable });
            var reply = MmiHelper.ReceiveMessageAndData(socket);
            return (int[,]) reply.Values;
        }

        public double[] GetDoubleValues1D(string variable)
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage {Action = "get_1d_double", Name = variable});
            var reply = MmiHelper.ReceiveMessageAndData(socket);
            return (double[]) reply.Values;
        }

        public int[] GetIntValues1D(string variable)
        {
            MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "get_1d_int", Name = variable });
            var reply = MmiHelper.ReceiveMessageAndData(socket);
            return (int[])reply.Values;
        }

        public void SetDoubleValue1DAtIndex(string variable, int index, double value)
        {
            var message = new MmiMessage { Action = "set_1d_double_at_index", Name = variable, Arguments = JsonConvert.SerializeObject(new { index = index, value = value, }) };
            MmiHelper.SendMessageAndData(socket, message);
            ReceiveReply();
        }

        public DateTime StartTime { get; private set; }

        public DateTime StopTime { get; private set; }

        public DateTime CurrentTime
        {
            get
            {
                MmiHelper.SendMessageAndData(socket, new MmiMessage { Action = "get_current_time" });
                var reply = MmiHelper.ReceiveMessageAndData(socket);
                var time = (double) reply.Values.GetValue(0);

                return StartTime.AddSeconds(time);
            }
        }

        public TimeSpan TimeStep { get; set; }

        public string[] VariableNames { get; private set; }

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

        private static void ReceiveReply()
        {
            MmiHelper.ReceiveMessageAndData(socket); // reply
        }
    }
}