namespace GameBoySharp.Emu.Tests
{
    [TestClass]
    public class OtherBlarggTests
    {

        private Emulator emu = new Emulator();

        [TestInitialize]
        public void Init()
        {
            emu?.PowerOff();
        }

        [TestMethod]
        public async Task HaltBug_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\halt_bug.gb", 4);
        }

        [TestMethod]
        public async Task InstrTiming_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\instr_timing.gb", 2);
        }

        [TestMethod]
        public async Task InterruptTime_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\interrupt_time.gb", 2);
        }

        [TestMethod]
        public async Task OamBug_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\oam_bug.gb", 30);
        }

    }
}
