using System;
using System.Threading.Tasks;
using BasicModelInterface;
using NUnit.Framework;
using Rhino.Mocks;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiModelClientServerTest
    {
        const string connectionString = "tcp://127.0.0.1:5558";

        [Test]
        public void BindAndConnect()
        {
            // setup a stub model, expect initialize is called
            const string configFile = "test.config";
            var model = MockRepository.GenerateStub<IBasicModelInterface>();
            model.Expect(m => m.Initialize(Arg<string>.Is.Equal(configFile))).Return(0);
            model.Expect(m => m.Finish()).Return(0);

            // start server and connect client to it, call Initialize on client
            using (var runner = new MmiModelRunner(connectionString, model))
            using (var client = new MmiModelClient(connectionString))
            {
                runner.Bind();
                client.Connect();

                // start message loop on server 
                var serverTask = new Task(() => runner.Start());
                serverTask.Start();

                // initialize
                client.Initialize(configFile);
                client.Finish();

                // stop server
                serverTask.Wait();
            }

            // validate
            model.VerifyAllExpectations();
        }

        [Test]
        [Ignore("run mmi locally before running this test")]
        public void BindAndConnect_Localhost()
        {
            // start server and connect client to it, call Initialize on client
            using (var client = new MmiModelClient("tcp://localhost:5600"))
            {
                client.Connect();

                Console.WriteLine(client.CurrentTime);
                client.GetValues("s1");
                client.Update(-1);
                var values = client.GetValues("s1");
                client.Update(-1);
                //client.SetValues("s1", values);
                //client.SetValue("s1", 0, 0.0);

                var start = new[] { 0 };
                var count = new[] { 1 };
                values = new[] { 1.0 };
                client.SetValues("s1", start, count, values);
            }
        }
    }
}