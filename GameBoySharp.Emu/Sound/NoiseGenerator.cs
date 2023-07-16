using GameBoySharp.Emu.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameBoySharp.Emu.Sound
{
    public class NoiseGenerator
    {
        static readonly bool[] randomValues;

        static NoiseGenerator()
        {
            var rand = new Random();
            randomValues = new bool[0x10000];
            for (int i = 0; i < randomValues.Length; i++)
            {
                randomValues[i] = rand.NextDouble() > 0.5;
            }
        }

        private int _length;
        private int _frequency;

        public bool DAC { get; set; }
        public bool Repeat { get; set; }
        public int LengthCounter { get; set; }
        public bool EnvelopeType { get; set; }
        public int EnvelopeTime { get; set; }
        public int EnvelopeVolume { get; set; }
        public int EnvelopeCounter { get; set; }
        public bool EnvelopeOn { get; set; }
        public int ShiftClockFrequency { get; set; }
        public bool CounterStep { get; set; }
        public int Amplitude { get; set; }
        public int CycleLengthNumerator { get; set; }
        public int CycleLengthDenominator { get; set; }
        public float CyclePos { get; set; }
        public int NoisePos { get; set; }
        public bool CounterFlip { get; set; }

        public int Length
        {
            get => _length;
            set
            {
                LengthCounter = 0x40 - value;
                _length = value;
            }
        }

        public int Frequency
        {
            get => _frequency;
            set
            {
                CycleLengthNumerator = (int)(APU.NativeSampleRate / 64 * (value == 0 ? 0.5 : value)) << (ShiftClockFrequency + 1);
                CycleLengthDenominator = 0x80000 / 64;
                _frequency = value;
            }
        }

        public bool Enabled => (LengthCounter > 0 || Repeat) && DAC && Amplitude > 0;

        public void SetEnvelope(int value)
        {
            EnvelopeTime = (value & 0x7);
            EnvelopeCounter = EnvelopeTime;
            EnvelopeType = BitOps.IsBitSet(3, value);
            EnvelopeVolume = (value & 0xf0) >> 4;
            EnvelopeOn = EnvelopeTime > 0;
            DAC = (value & 0xf8) > 0;
        }

        public void SetPolynomial(int value)
        {
            ShiftClockFrequency = (value & 0xf0) >> 4;
            CounterStep = BitOps.IsBitSet(3, value);
            Frequency = (value & 0x7);
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
            if (LengthCounter > 0)
            {
                --LengthCounter;
            }
        }

        public int Play(float rate)
        {
            var val = 0;

            if (Enabled)
            {
                CyclePos += (CycleLengthDenominator * APU.NativeSampleRatio) * rate;
                if (CyclePos >= CycleLengthNumerator)
                {
                    CyclePos -= CycleLengthNumerator;
                    NoisePos++;
                    if (CounterStep && (NoisePos & 7) == 0)
                    {
                        if (CounterFlip)
                            NoisePos -= 8;
                        CounterFlip = !CounterFlip;
                    }
                    NoisePos %= randomValues.Length;
                }
                val = (randomValues[NoisePos]) ? Amplitude : -Amplitude;
            }

            return val;
        }

        public void Reset()
        {
            Amplitude = EnvelopeVolume;
            EnvelopeCounter = EnvelopeTime;
            if (LengthCounter == 0) LengthCounter = 0x40;
            CyclePos = 0;
        }
    }
}
