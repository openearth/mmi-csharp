using System;
using System.Diagnostics;
using System.Linq;
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
            mmiServer = new MmiServer();
        }

        [TearDown]
        public void TearDown()
        {
            mmiServer.Dispose();
        }

        private MmiServer mmiServer;

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
            var elementCount = 1;

            mmiServer.Start(Port); // start MmiServer

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

                // send on the MmiServer side
                var stopwatchServer = new Stopwatch();
                for (var i = 0; i < 10; i++)
                {
                    stopwatchServer.Start();
                    mmiServer.Send(Enumerable.Repeat(1.0, elementCount).ToArray());
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
