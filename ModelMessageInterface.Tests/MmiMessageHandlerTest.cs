using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SharpTestsEx;
using ZMQ;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiMessageHandlerTest
    {
        [SetUp]
        public void SetUp()
        {
            server = new Server();
        }

        [TearDown]
        public void TearDown()
        {
            server.Dispose();
        }

        private Server server;

        private class Server : IDisposable
        {
            private readonly Context context;
            private Socket socket;

            public Server()
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

            public void Start()
            {
                socket = context.Socket(SocketType.PUB);
                socket.Bind(Transport.TCP, "*", Port);
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

        private const string Host = "127.0.0.1";

        private const int Port = 5558;

        [Test]
        public void ArrayToBytes()
        {
            var values = new double[] {1, 2, 3};
            var expectedBytes = values.SelectMany(BitConverter.GetBytes).ToArray();

            var bytes = MmiMessageHandler.ArrayToBytes(values);

            bytes.Should().Have.SameSequenceAs(expectedBytes);
        }

        [Test]
        public void BytesToArray()
        {
            var valuesExpected = new double[] {1, 2, 3};
            var bytes = valuesExpected.SelectMany(BitConverter.GetBytes).ToArray();

            var values = (double[]) MmiMessageHandler.BytesToArray(bytes, "float64", new[] {3});

            values.Should().Have.SameSequenceAs(valuesExpected);
        }

        [Test]
        public void SendAndReceive()
        {
            var elementCount = 500000;

            server.Start(); // start server

            using (var context = new Context())
            using (var socket = context.Socket(SocketType.SUB))
            {
                socket.Subscribe(new byte[] {});
                socket.Connect(Transport.TCP, Host, Port);

                var stopwatch = new Stopwatch();

                var receiveDataTask = new Task(() =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        stopwatch.Start();

                        var message = MmiMessageHandler.ReceiveMessageAndData(socket);

                        message.Values.Cast<double>().Should().Have.SameSequenceAs(Enumerable.Repeat(1.0, elementCount).ToArray());

                        stopwatch.Stop();

                        Debug.WriteLine("BG: message {0}, delay (latency): {1} ms, received in: {2} ms", i, (DateTime.Now - message.TimeStamp).TotalMilliseconds, stopwatch.ElapsedTicks*1e-4);

                        stopwatch.Reset();
                    }

                });

                receiveDataTask.Start();

                // send on the server side
                var stopwatchServer = new Stopwatch();
                for (var i = 0; i < 10; i++)
                {
                    stopwatchServer.Start();
                    server.Send(Enumerable.Repeat(1.0, elementCount).ToArray());
                    stopwatchServer.Stop();
                    Debug.WriteLine("MAIN: {0}, sent in {1} ms", i, stopwatchServer.ElapsedTicks*1e-4);
                    stopwatchServer.Reset();
                
                    //Thread.Sleep(20); // try commenting this, latency seems to depend a lot on whether we send messages in a row of after delay (10-90ms)
                }

                receiveDataTask.Wait();
            }
        }
    }
}
