using System.Diagnostics;

namespace GameBoySharp.Emu.Tests
{
    [TestClass]
    public class BlarggCPU
    {

        const int defaultTimeoutSeconds = 10;
        private Emulator emu = new Emulator();

        [TestInitialize]
        public void Init()
        {
            emu?.PowerOff();
        }

        [TestMethod]
        public async Task Special_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\01-special.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task Interrupts_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\02-interrupts.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task OpSpHl_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\03-op sp,hl.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task OpRImm_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\04-op r,imm.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task OpRp_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\05-op rp.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task LdRR_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\06-ld r,r.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task JrJpCallRetRst_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\07-jr,jp,call,ret,rst.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task MiscInstrs_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\08-misc instrs.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task OpRR_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\09-op r,r.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task BitOps_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\10-bit ops.gb", defaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task OpAHl_Test()
        {
            await TestHarness.RunBlarggTest(emu, "TestRoms\\Blargg\\CPU\\11-op a,(hl).gb", defaultTimeoutSeconds);
        }

    }
}
