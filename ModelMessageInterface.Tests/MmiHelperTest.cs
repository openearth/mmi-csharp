using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rhino.Mocks;
using SharpTestsEx;
using ZMQ;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiHelperTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void ArrayToBytes()
        {
            var values = new double[] {1, 2, 3};
            var expectedBytes = values.SelectMany(BitConverter.GetBytes).ToArray();

            var bytes = MmiHelper.ArrayToBytes(values);

            bytes.Should().Have.SameSequenceAs(expectedBytes);
        }

        [Test]
        public void BytesToArray()
        {
            var valuesExpected = new double[] {1, 2, 3};
            var bytes = valuesExpected.SelectMany(BitConverter.GetBytes).ToArray();

            var values = (double[]) MmiHelper.BytesToArray(bytes, "float64", new[] {3});

            values.Should().Have.SameSequenceAs(valuesExpected);
        }

        [Test]
        public void SendAndReceive()         {
            const int arrayLength = 1;
            const string host = "127.0.0.1";
            const uint port = 5558;

            var mocks = new MockRepository();

            using (var context = new Context())
            using (var server = context.Socket(SocketType.REP)) // server
            using (var client = context.Socket(SocketType.REQ)) // client
            {
                server.Bind(Transport.TCP, host, port);
                client.Connect(Transport.TCP, host, port);

                var stopwatch = new Stopwatch();

                var receiveDataTask = new Task(() =>
                {
                    for (var i = 0; i < 10; i++)
                    {
                        stopwatch.Start();

                        // test message
                        var message = MmiHelper.ReceiveMessageAndData(client);

                        message.Values.Cast<double>().Should().Have.SameSequenceAs(Enumerable.Repeat(1.0, arrayLength).ToArray());

                        stopwatch.Stop();

                        Debug.WriteLine("BG: message {0}, delay (latency): {1} ms, received in: {2} ms", i, (DateTime.Now - message.TimeStamp).TotalMilliseconds, stopwatch.ElapsedTicks*1e-4);

                        stopwatch.Reset();
                    }

                });

                receiveDataTask.Start();

                // send on the MmiModelServer side
                var stopwatchServer = new Stopwatch();
                for (var i = 0; i < 10; i++)
                {
                    stopwatchServer.Start();
                    MmiHelper.SendMessageAndData(server, new MmiMessage { Values = Enumerable.Repeat(1.0, arrayLength).ToArray() });
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
