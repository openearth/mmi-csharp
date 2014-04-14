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

            public void Send(Array data)
            {
                var message = new MmiMessage
                {
                    TimeStamp = DateTime.Now,
                    DataType = "float32",
                    Name = "water level",
                    Shape = GetShape(data),
                    Values = data
                };

                MmiMessageHandler.SendMessage(socket, message);
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
            server.Start(); // start server

            using (var context = new Context())
            using (var socket = context.Socket(SocketType.SUB))
            {
                socket.Subscribe(new byte[] {});
                socket.Connect(Transport.TCP, Host, Port);

                var stopwatch = new Stopwatch();

                var finished = false;
                var receiveDataTask = new Task(() =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        stopwatch.Start();

                        var message = MmiMessageHandler.ReceiveMessage(socket);

                        stopwatch.Stop();

                        Debug.WriteLine("BG: message {0}, delay: {1}, received in: {2} ms", i,
                            (DateTime.Now - message.TimeStamp), stopwatch.ElapsedTicks*1e-4);

                        if (message.Name == "dps")
                        {
                            var valuesF = (float[,]) message.Values;
                            var valuesD = new double[message.Shape[0], message.Shape[1]];

                            for (var r = 0; r < message.Shape[0]; r++)
                            {
                                for (var c = 0; c < message.Shape[1]; c++)
                                {
                                    var v = valuesF[r, c];
                                    valuesD[r, c] = v < 0 ? 0 : v;
                                }
                            }

                            // ShowImage(valuesD);
                            //Application.DoEvents();
                        }

                        stopwatch.Reset();
                    }


                    finished = true;
                });

                receiveDataTask.Start();

                // send on the server side
                for (var i = 0; i < 10; i++)
                {
                    server.Send(new float[] {1, 2, 3});
                    Trace.WriteLine("MAIN: " + i);
                }

                receiveDataTask.Wait();
            }
        }
    }
}