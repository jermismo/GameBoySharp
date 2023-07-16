using GameBoySharp.Emu.Utils;

namespace GameBoySharp.Emu.Sound
{
    public class SquareWaveGenerator
    {
        static readonly bool[][] dutyLookup;

        static SquareWaveGenerator()
        {
            dutyLookup = new bool[4][];
            dutyLookup[0] = new bool[] { true, false, false, false, false, false, false, false };
            dutyLookup[1] = new bool[] { false, false, false, false, true, true, false, false };
            dutyLookup[2] = new bool[] { true, true, true, true, false, false, false, false };
            dutyLookup[3] = new bool[] { true, true, true, true, true, true, false, false };
        }

        private int _length;
        private int _duty;
        private bool[] cachedDuty = dutyLookup[0];
        private int _baseFrequency;
        private int _frequency;

        public bool IgnoreSweep { get; set; }
        public bool DAC { get; set; }
        public int LengthCounter { get; set; }

        public int Length
        {
            get => _length;
            set
            {
                LengthCounter = 0x40 - value;
                _length = value;
            }
        }

        public int Duty
        {
            get => _duty;
            set
            {
                cachedDuty = dutyLookup[value];
                _duty = value;
            }
        }

        public bool SweepDecrease { get; set; }
        public int SweepDiv { get; set; }
        public int SweepTime { get; set; }
        public int SweepCounter { get; set; }
        public bool Swept { get; set; }
        public bool SweepFault { get; set; }
        public int ShadowFrequency { get; set; }

        public bool EnvelopeType { get; set; }
        public int EnvelopeTime { get; set; }
        public int EnvelopeVolume { get; set; }
        public int EnvelopeCounter { get; set; }
        public bool EnvelopeOn { get; set; }

        public bool Repeat { get; set; }
        public int CycleLengthNumerator { get; set; }
        public int CycleLengthDenominator { get; set; }
        public float CyclePos { get; set; }
        public int DutyLength { get; set; }
        public int Amplitude { get; set; }

        public int BaseFrequency
        {
            get => _baseFrequency;
            set
            {
                _baseFrequency = Frequency = value;
            }
        }

        public int Frequency
        {
            get => _frequency;
            set
            {
                SweepFault = false;
                CycleLengthNumerator = (APU.NativeSampleRate / 64) * (0x800 - value);
                CycleLengthDenominator = 0x20000 / 64;
                DutyLength = CycleLengthNumerator / 8;
                _frequency = value;
            }
        }

        public bool Enabled => (Repeat || LengthCounter > 0) && !SweepFault && DAC && (Amplitude > 0 || (EnvelopeType && EnvelopeTime > 0));

        public int SweepRegister => SweepDiv | (SweepDecrease ? 0x8 : 0) | (SweepTime << 4);

        public int DutyRegister => Length | (Duty << 6);

        public int EnvelopeRegister => EnvelopeTime | (EnvelopeType ? 0x8 : 0) | (EnvelopeVolume << 4);

        public void SetSweep(int value)
        {
            SweepDiv = value & 0x7;
            if (SweepDecrease && !BitOps.IsBitSet(3, value)) SweepFault = true;
            else SweepFault = false;
            SweepDecrease = BitOps.IsBitSet(3, value);
            SweepTime = (value & 0x70) >> 4;
            SweepCounter = SweepTime;
        }

        public void SetDuty(int value)
        {
            Length = value & 0x3f;
            Duty = (value & 0xc0) >> 6;
        }

        public void SetEnvelope(int value)
        {
            EnvelopeTime = (value & 0x7);
            EnvelopeCounter = EnvelopeTime;
            EnvelopeType = BitOps.IsBitSet(3, value);
            EnvelopeVolume = (value & 0xf0) >> 4;
            EnvelopeOn = EnvelopeTime > 0;
            DAC = (value & 0xf8) > 0;
        }

        public void SweepClock()
        {
            if (--SweepCounter == 0)
            {
                if (SweepDiv > 0)
                {
                    if (SweepDecrease)
                    {
                        ShadowFrequency -= ShadowFrequency >> SweepDiv;
                        Frequency = (ShadowFrequency) & 0x7ff;
                    }
                    else
                    {
                        ShadowFrequency += ShadowFrequency >> SweepDiv;
                        if (ShadowFrequency + (ShadowFrequency >> SweepDiv) > 0x7ff)
                        {
                            // overflow
                            SweepFault = true;
                        }

                        else
                        {
                            Frequency = ShadowFrequency;
                        }
                    }
                    Swept = true;
                }
                SweepCounter = SweepTime;
            }
        }

        public void SweepDummy()
        {
            if (SweepDiv > 0)
            {
                if (!SweepDecrease)
                {
                    ShadowFrequency += ShadowFrequency >> SweepDiv;
                    if (ShadowFrequency + (ShadowFrequency >> SweepDiv) > 0x7ff)
                    {
                        // overflow
                        SweepFault = true;
                    }
                }
                Swept = true;
            }
        }

        public void EnvelopeClock()
        {
            if (EnvelopeOn && EnvelopeTime > 0)
            {
                if (--EnvelopeCounter == 0)
                {
                    if (EnvelopeType)
                    {
                        if (++Amplitude >= 0xf)
                        {
                            Amplitude = 0xf;
                            EnvelopeOn = false;
                        }
                        else EnvelopeCounter = EnvelopeTime;
                    }

                    else
                    {
                        if (--Amplitude <= 0)
                        {
                            Amplitude = 0;
                            EnvelopeOn = false;
                        }
                        else EnvelopeCounter = EnvelopeTime;
                    }
                }
            }
        }

        public void LengthClock()
        {
            if (LengthCounter > 0) --LengthCounter;
        }

        public int Play(float rate)
        {
            var val = 0;
            if (Enabled)
            {
                CyclePos += (CycleLengthDenominator * APU.NativeSampleRatio) * rate;
                if (CyclePos >= CycleLengthNumerator) CyclePos -= CycleLengthNumerator;
                val = (cachedDuty[(int)(CyclePos / DutyLength) & 0x7]) ? Amplitude : -Amplitude;
            }

            return val;
        }

        public void Reset()
        {
            Amplitude = EnvelopeVolume;
            EnvelopeCounter = EnvelopeTime;
            EnvelopeOn = EnvelopeTime > 0;
            SweepCounter = SweepTime;
            Swept = SweepFault = false;
            if (LengthCounter == 0) LengthCounter = 0x40;
            CyclePos = 0;
            Frequency = BaseFrequency;
            ShadowFrequency = Frequency;
            SweepDummy();
        }

    }
}
