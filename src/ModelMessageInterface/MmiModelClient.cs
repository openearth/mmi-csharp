﻿using System;
using System.Linq;
using System.Net.Sockets;
using BasicModelInterface;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;


namespace ModelMessageInterface
{
    /// <summary>
    /// Connects to a remote BMI model using MMI.
    /// </summary>
    public class MmiModelClient : IBasicModelInterface, IDisposable
    {
        private static NetMQ.NetMQContext context;
        private NetMQSocket socket;

        readonly string protocol;
        readonly string host;
        readonly uint port;

        internal bool IsServerExited { get; set; }

        public string Host
        {
            get { return host; }
        }

        public uint Port
        {
            get { return port; }
        }

        public string Protocol
        {
            get { return protocol; }
        }

        public MmiModelClient(string connectionString)
        {
            MmiHelper.ParseConnectionString(connectionString, out protocol, out host, out port);

            if (protocol.ToLower() != "tcp")
            {
                throw new NotSupportedException("Only TCP is supported for now");
            }
        }

        public TimeSpan Timeout
        {
            get { return MmiHelper.Timeout; }
            set { MmiHelper.Timeout = value; }
        }

        public void Connect()
        {
            if (context == null)
            {
                context = NetMQContext.Create();
            }
            if (socket != null)
            {
                throw new NetMQException("Attempt to set socket that was already defined. This would prevent successful dispose of the context later on (since we can no longer close all sockets).");
            }
            socket = context.CreateRequestSocket();
            socket.Options.Linger = new TimeSpan(0, 0, 0, 1);
            socket.Connect("tcp://" + host + ":" + port);
        }

        public int Initialize(string configFile)
        {
            MmiHelper.SendMessage(socket, new { initialize = configFile, timestamp = DateTime.Now });
            ReceiveReply();
            return 0;
        }

        public int Update(double dt)
        {
            MmiHelper.SendMessage(socket, new { update = dt, timestamp = DateTime.Now });
            ReceiveReply();
            return 0;
        }

        public int Finish()
        {
            MmiHelper.SendMessage(socket, new { finalize = 0, timestamp = DateTime.Now });
            ReceiveReply();
            return 0;
        }

        public int[] GetShape(string variable)
        {
            MmiHelper.SendMessage(socket, new { get_var = variable, timestamp = DateTime.Now });
            var reply = MmiHelper.ReceiveMessageAndData(socket);
            var values = reply.Values;
            return Enumerable.Range(0, values.Rank).Select(values.GetLength).ToArray();
        }

        public Array GetValues(string variable)
        {
            MmiHelper.SendMessage(socket, new { get_var = variable, timestamp = DateTime.Now });
            var reply = MmiHelper.ReceiveMessageAndData(socket);
            return reply.Values;
        }

        public Array GetValues(string variable, int[] index)
        {
            throw new NotImplementedException();
        }

        public Array GetValues(string variable, int[] start, int[] count)
        {
            throw new NotImplementedException();
        }

        public void SetValues(string variable, Array values)
        {
            var shape = new int[values.Rank];
            for (var i = 0; i < values.Rank; i++)
            {
                shape[i] = values.GetLength(i);
            }

            var dtype = MmiHelper.GetDataTypeName(values.GetValue(0).GetType());

            MmiHelper.SendMessage(socket, new { set_var = variable, timestamp = DateTime.Now, dtype = dtype, shape = shape }, values);
            MmiHelper.ReceiveMessageAndData(socket);
        }

        public void SetValues(string variable, int[] start, int[] count, Array values)
        {
            var shape = new int[values.Rank];
            for (var i = 0; i < values.Rank; i++)
            {
                shape[i] = values.GetLength(i);
            }

            var dtype = MmiHelper.GetDataTypeName(values.GetValue(0).GetType());

            MmiHelper.SendMessage(socket, new { set_var_slice = variable, start = start, count = count, dtype = dtype, shape = shape, timestamp = DateTime.Now }, values);
            MmiHelper.ReceiveMessageAndData(socket);
        }

        public void SetValues(string variable, int[] index, Array values)
        {
            var shape = new int[values.Rank];
            for (var i = 0; i < values.Rank; i++)
            {
                shape[i] = values.GetLength(i);
            }

            var dtype = MmiHelper.GetDataTypeName(values.GetValue(0).GetType());

            MmiHelper.SendMessage(socket, new { set_var_index = variable, index = index, dtype = dtype, shape = shape, timestamp = DateTime.Now }, values);
            MmiHelper.ReceiveMessageAndData(socket);
        }

        public DateTime StartTime
        {
            get
            {
                MmiHelper.SendMessage(socket, new { get_start_time = string.Empty, timestamp = DateTime.Now });
                var reply = MmiHelper.ReceiveMessageAndData(socket);
                var time = reply.Json.get_start_time.Value;
                return new DateTime().AddSeconds(time);
            }
            private set { }
        }

        public DateTime StopTime
        {
            get
            {
                MmiHelper.SendMessage(socket, new { get_end_time = string.Empty, timestamp = DateTime.Now });
                var reply = MmiHelper.ReceiveMessageAndData(socket);
                var time = reply.Json.get_end_time.Value;
                return new DateTime().AddSeconds(time);
            }
            private set { }
        }

        public TimeSpan TimeStep
        {
            get
            {
                MmiHelper.SendMessage(socket, new { get_time_step = string.Empty, timestamp = DateTime.Now });
                var reply = MmiHelper.ReceiveMessageAndData(socket);
                var time = reply.Json.get_time_step.Value;
                return new TimeSpan(0, 0, 0, (int)time);
            }
        }

        public DateTime CurrentTime
        {
            get
            {
                MmiHelper.SendMessage(socket, new { get_current_time = string.Empty, timestamp = DateTime.Now });
                var reply = MmiHelper.ReceiveMessageAndData(socket);
                var time = reply.Json.get_current_time.Value;
                return StartTime.AddSeconds(time);
            }
        }

        public string[] VariableNames { get; private set; }
        
        public Logger Logger { get; set; }

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Dispose();
                socket = null;
            }

            if (context != null)
            {
                context.Dispose();
            }
        }

        private void ReceiveReply()
        {
            CheckServerStatus();
            MmiHelper.ReceiveMessageAndData(socket); // reply
        }

        private void CheckServerStatus()
        {
            if (IsServerExited)
            {
                throw new NetMQException("Server has exited");
            }
        }
    }
}

/*
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

 */
