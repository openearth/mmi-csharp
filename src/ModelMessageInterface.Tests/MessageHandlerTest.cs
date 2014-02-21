using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SharpTestsEx;
using ZMQ;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MessageHandlerTest
    {
        [Test]
        public void ArrayTest()
        {
            var valuesExpected = new double[] { 1, 2, 3 };
            var bytes = valuesExpected.SelectMany(BitConverter.GetBytes).ToArray();

            var valuesConverted = (double[])MessageHandler.ToArray(bytes, "float64", new[] { 3 });

            valuesConverted.Should().Have.SameSequenceAs(valuesExpected);
        }

        [Test]
        public void Receive()
        {
            using (var context = new Context())
            {
                var socket = context.Socket(SocketType.SUB);

                socket.Subscribe(new byte[] { });
                socket.Connect(Transport.TCP, "192.168.2.135", 5558);

                var dt = new Stopwatch();

                for (var i = 0; i < 100; i++)
                {
                    dt.Start();

                    var msg = MessageHandler.GetMessage(socket);

                    dt.Stop();

                    Debug.WriteLine(dt.ElapsedTicks * 1e-4 + " ms");

                    if (msg.Name == "dps")
                    {
                        var valuesF = (float[,])msg.Values;
                        var valuesD = new double[msg.Shape[0], msg.Shape[1]];

                        for (var r = 0; r < msg.Shape[0]; r++)
                        {
                            for (var c = 0; c < msg.Shape[1]; c++)
                            {
                                var v = valuesF[r, c];
                                valuesD[r, c] = v < 0 ? 0 : v;
                            }
                        }

                        // ShowImage(valuesD);
                        //Application.DoEvents();
                    }

                    dt.Reset();
                }
            }
        }
    }
}