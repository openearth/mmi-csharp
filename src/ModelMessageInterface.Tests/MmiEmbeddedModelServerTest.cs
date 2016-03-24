using System.IO;
using NUnit.Framework;

namespace ModelMessageInterface.Tests
{
    [TestFixture]
    public class MmiEmbeddedModelServerTest
    {
        [Test]
        public void StartInitializeKill()
        {
            string lib = Path.GetFullPath(@"..\..\..\..\shared\simpleBmiModel\model-cpp.dll");
            string config = Path.GetFullPath(@"..\..\..\..\shared\simpleBmiModel\config");
            MmiEmbeddedModelServer.MmiRunnerPath = Path.GetFullPath(@"..\..\..\..\shared\simpleBmiModel\mmi-runner.exe");
            
            var model = MmiEmbeddedModelServer.StartModel(lib, config);
            model.Update(-1);

            MmiEmbeddedModelServer.StopModel(model);
        }
    }    
}