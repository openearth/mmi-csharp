using NUnit.Framework;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiEmbeddedModelServerTest
    {
        [Test]
        public void StartInitializeKill()
        {
            const string lib = @"D:\src\GitHub\bmi\models\vs2013\bin\Debug\model-cpp.dll";
            const string config = @".\config";
            
            var model = MmiEmbeddedModelServer.StartModel(lib, config);
            model.Update(-1);

            MmiEmbeddedModelServer.StopModel(model);
        }
    }
}