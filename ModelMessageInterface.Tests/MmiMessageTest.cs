using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using SharpTestsEx;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiMessageTest
    {
        [Test]
        public void ToJson()
        {
            var message = new MmiMessage
            {
                DataType = "float32",
                Name = "water flow",
                Shape = new[] {2, 3},
                TimeStamp = new DateTime(),
                Values = new float[] {1, 2, 3, 4, 5, 6}
            };

            var json = message.ToJson();

            var expected = @"{'name':'water flow','shape':[2,3],'dtype':'float32','timestamp':'0001-01-01T00:00:00'}".Replace('\'', '\"');

            json.Should().Be.EqualTo(expected);
        }

        [Test]
        public void FillFromJson()
        {
            var json = @"{'name':'water flow','shape':[2,3],'dtype':'float32','timestamp':'0001-01-01T00:00:00'}".Replace('\'', '\"');
            var message = new MmiMessage();
            message.FillFromJson(json);

            message.Name.Should().Be.EqualTo("water flow");
            message.Shape.Should().Have.SameSequenceAs(new[] {2, 3});
            message.TimeStamp.Should().Be.EqualTo(new DateTime());
            message.DataType.Should().Be.EqualTo("float32");
        }
    }
}
