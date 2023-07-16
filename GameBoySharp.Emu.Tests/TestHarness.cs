using System.Diagnostics;

namespace GameBoySharp.Emu.Tests
{
    public static class TestHarness
    {
        public static async Task RunBlarggTest(Emulator emu, string romPath, int timeoutSeconds)
        {
            string output = string.Empty;
            bool passed = false;

            emu.MMU.DebugCallback = (c) =>
            {
                output += c;
                if (output.EndsWith("Passed"))
                {
                    passed = true;
                    emu.PowerOff();
                }
                else if (output.EndsWith("Failed"))
                {
                    passed = false;
                    emu.PowerOff();
                }
            };
            
            emu.PowerOn(romPath);

            var sw = new Stopwatch();
            sw.Start();
            
            while (emu.PowerSwitch)
            {
                await Task.Delay(250);
                if (sw.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    emu.PowerOff();
                    output += "\nTIMEOUT";
                }
            }
            Assert.IsTrue(passed, $"Failed with message:\n{output}");
        }
    }
}
