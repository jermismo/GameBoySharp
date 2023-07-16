namespace GameBoySharp.Emu.Tests
{
    [TestClass]
    public class BlarggMemory
    {

        private Emulator emu = new Emulator();

        [TestInitialize]
        public void Init()
        {
            emu?.PowerOff();
        }

        [TestMethod]
        public async Task ReadTiming_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\MemTiming\\01-read_timing.gb", 10);
        }

        [TestMethod]
        public async Task WriteTiming_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\MemTiming\\02-write_timing.gb", 10);
        }

        [TestMethod]
        public async Task ModifyTiming_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\MemTiming\\03-modify_timing.gb", 10);
        }

    }
}
