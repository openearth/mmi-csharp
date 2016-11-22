using System;
using NUnit.Framework;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiClientTest
    {
        MmiModelClient modelClient = new MmiModelClient(@"tcp://0.0.0.0:0");

        [Test]
        public void TimeoutCheckDefaultAndCanSet()
        {
            Assert.AreEqual(new TimeSpan(0, 0, 0, 5), modelClient.Timeout);
            modelClient.Timeout = new TimeSpan(0, 0, 0, 1);
            Assert.AreEqual(new TimeSpan(0, 0, 0, 1), modelClient.Timeout);
        }
    }
}