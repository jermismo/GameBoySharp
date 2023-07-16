using GameBoySharp.Emu.Utils;

namespace GameBoySharp.Emu.Sound
{
    public class WaveTableGenerator
    {
        private int _length;
        private int _frequency;

        private int cycleLengthNumerator = 1;
        private int cycleLengthDenominator = 1;
        private float cyclePos;
        private int sampleLength = 1;

        public bool CanPlay { get; set; }
        public bool DAC { get; set; }
        public bool Repeat { get; set; }
        public int LengthCounter { get; set; }
        public byte[] WaveData { get; set; }
        public int OutputLevel { get; set; }

        public bool Enabled => (LengthCounter > 0 || Repeat) && DAC && CanPlay && OutputLevel > 0;

        public int Length
        {
            get => _length;
            set
            {
                LengthCounter = 0x100 - value;
                _length = value;
            }
        }

        public int Frequency
        {
            get => _frequency;
            set
            {
                cycleLengthNumerator = (APU.NativeSampleRate / 64) * (0x800 - value);
                cycleLengthDenominator = 0x10000 / 64;
                sampleLength = cycleLengthNumerator / 32;
                _frequency = value;
            }
        }

        public WaveTableGenerator()
        {
            WaveData = new byte[32];
        }

        public void SetOutput(int value)
        {
            OutputLevel = (value & 0x60) >> 5;
            DAC = (value & 0xf8) > 0;
        }

        public void LengthClock()
        {
            if (LengthCounter > 0)
            {
                --LengthCounter;
            }
        }

        public int Play(float rate)
        {
            var val = 0;
            cyclePos += (cycleLengthDenominator * APU.NativeSampleRatio) * rate;
            if (cyclePos >= cycleLengthNumerator) cyclePos -= cycleLengthNumerator;

            if (Enabled)
            {
                var val1 = WaveData[(int)Math.Floor(cyclePos / sampleLength) & 0x1f];
                var val2 = WaveData[(int)Math.Ceiling(cyclePos / sampleLength) & 0x1f];
                var t = (cyclePos / sampleLength) % 1;
                val = (int)Math.Round(Num.Lerp(val1, val2, t));
                if (OutputLevel > 1) val >>= (OutputLevel - 1);
            }

            return val;
        }

        public void Reset()
        {
            if (LengthCounter == 0) LengthCounter = 0x100;
            cyclePos = 0;
        }
    }
}
