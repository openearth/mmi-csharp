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
            model.Expect(m => m.Initialize(Arg<string>.Is.Equal(configFile)));

            // start server and connect client to it, call Initialize on client
            using (var server = new MmiModelServer(connectionString, model))
            using (var client = new MmiModelClient(connectionString))
            {
                server.Bind();
                client.Connect();

                // call method in background (due to request / reply)
                var initializeTask = new Task(() => client.Initialize(configFile));
                initializeTask.Start();

                server.ProcessNextMessage();

                initializeTask.Wait();
            }

            // validate
            model.VerifyAllExpectations();
        }
    }
}