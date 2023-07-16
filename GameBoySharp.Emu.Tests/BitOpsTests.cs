using GameBoySharp.Emu.Utils;

namespace GameBoySharp.Emu.Tests
{
    [TestClass]
    public class BitOpsTests
    {
        [TestMethod]
        public void BitOps_BitSet()
        {
            // 0000 0000
            byte test = 0;

            // 0000 0001
            test = BitOps.BitSet(0, test);
            Assert.AreEqual(1, test, "Setting the 0 bit should make value 0000 0001");

            // 0000 0011
            test = BitOps.BitSet(1, test);
            Assert.AreEqual(3, test, "Setting the 1 bit should now make the value 0000 0011");

            test = BitOps.BitSet(0, test);
            Assert.AreEqual(3, test, "Setting the 0 bit when it is already set should not change the value.");
        }

        [TestMethod]
        public void BitOps_BitClear()
        {
            byte test = 3;

            test = BitOps.BitClear(0, test);
            Assert.AreEqual(2, test, "Clearing the 0 bit should make the value 0000 0010");

            test = BitOps.BitClear(7, test);
            Assert.AreEqual(2, test, "Clearing a bit that is already 0 should not change the value");
        }

        [TestMethod]
        public void BitOps_IsBitSet()
        {
            byte test = 3;

            Assert.IsTrue(BitOps.IsBitSet(0, test), "The 0 bit is set to 1, so this should return true");
            Assert.IsTrue(BitOps.IsBitSet(1, test), "The 1 bit is set to 1, so this should return true");
            Assert.IsFalse(BitOps.IsBitSet(2, test), "The 2 bit is set to 0, so this should return false");
        }
    }
}