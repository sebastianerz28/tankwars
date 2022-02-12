using Microsoft.VisualStudio.TestTools.UnitTesting;
using TankWars;
namespace ServerTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Server s = new Server();
            s.Run();

            Assert.AreEqual(24, s.world.Walls.Count);
        }
    }
}
