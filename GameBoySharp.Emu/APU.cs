// https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware

using GameBoySharp.Emu.Core;
using GameBoySharp.Emu.Sound;
using GameBoySharp.Emu.Utils;
using System.Diagnostics;
using System.Formats.Asn1;

namespace GameBoySharp.Emu
{
    /// <summary>
    /// Audio Processing Unit
    /// </summary>
    [DebuggerDisplay("APU on:{SoundEnabled} Volume:{VolumeLeft}/{VolumeRight}")]
    public class APU
    {
        public const int SampleRate = 44100;
        public const int NativeSampleRate = 4296600; // 465 * 154 * 60
        public const int NativeSampleRatio = 4;
        const int SequencerRate = 8230; // NativeSampleRate / 8 / 64
        const int BufferLength = 0x8000;
        const int MaxVolume = 16;
        const int FilterOrder = 63;

        private MMU mmu;

        private SquareWaveGenerator channel1;
        private SquareWaveGenerator channel2;
        private WaveTableGenerator channel3;
        private NoiseGenerator channel4;
        private LowPassFilter filter1;
        private LowPassFilter filter2;

        int currentCycle = 0;
        int subCycles = SequencerRate;
        int sampleCounter;
        float sampleSync;
        float sampleStepSize = SampleRate * NativeSampleRatio;
        float[] samples = new float[4];

        float sampleLeft, sampleRight, smplLeftPrev, smplRightPrev;
        float? t;

        int cycleSkip = NativeSampleRatio;

        public float ch1vol = 1;
        public float ch2vol = 1;
        public float ch3vol = 1;
        public float ch4vol = 1;

        public SoundBuffer BufferLeft { get; set; }
        public SoundBuffer BufferRight { get; set; }

        #region Channel 1 (Square 1) Registers

        /// <summary>
        /// Square 1 Sweep, Period, Negate, Shift
        /// </summary>
        /// <remarks>
        /// Bits: -PPP NSSS
        /// </remarks>
        public byte NR10 { get => mmu.ReadByte(0xFF10); set => mmu.WriteByte(0xFF10, value); }

        /// <summary>
        /// Square 1 Duty, Length Load
        /// </summary>
        /// <remarks>
        /// Bits: DDLL LLLL
        /// </remarks>
        public byte NR11 { get => mmu.ReadByte(0xFF11); set => mmu.WriteByte(0xFF11, value); }

        /// <summary>
        /// Square 1 Starting volume, envelope add mode, period
        /// </summary>
        /// <remarks>
        /// Bits: VVVV APPP
        /// </remarks>
        public byte NR12 { get => mmu.ReadByte(0xFF12); set => mmu.WriteByte(0xFF12, value); }

        /// <summary>
        /// Square 1 Frequency LSB
        /// </summary>
        /// <remarks>
        /// Bits: FFFF FFFF
        /// </remarks>
        public byte NR13 { get => mmu.ReadByte(0xFF13); set => mmu.WriteByte(0xFF13, value); }

        /// <summary>
        /// Square 1 Trigger, Length enable, Frequency MSB
        /// </summary>
        /// <remarks>
        /// Bits: TL-- -FFF
        /// </remarks>
        public byte NR14 { get => mmu.ReadByte(0xFF14); set => mmu.WriteByte(0xFF14, value); }

        #endregion

        #region Channel 2 (Square 2) Registers
                
        /// <summary>
        /// Square 2 Duty, Length Load
        /// </summary>
        /// <remarks>
        /// Bits: DDLL LLLL
        /// </remarks>
        public byte NR21 { get => mmu.ReadByte(0xFF16); set => mmu.WriteByte(0xFF16, value); }

        /// <summary>
        /// Square 2 Starting volume, envelope add mode, period
        /// </summary>
        /// <remarks>
        /// Bits: VVVV APPP
        /// </remarks>
        public byte NR22 { get => mmu.ReadByte(0xFF17); set => mmu.WriteByte(0xFF17, value); }

        /// <summary>
        /// Square 2 Frequency LSB
        /// </summary>
        /// <remarks>
        /// Bits: FFFF FFFF
        /// </remarks>
        public byte NR23 { get => mmu.ReadByte(0xFF18); set => mmu.WriteByte(0xFF18, value); }

        /// <summary>
        /// Square 2 Trigger, Length enable, Frequency MSB
        /// </summary>
        /// <remarks>
        /// Bits: TL-- -FFF
        /// </remarks>
        public byte NR24 { get => mmu.ReadByte(0xFF19); set => mmu.WriteByte(0xFF19, value); }

        #endregion

        #region Channel 3 (Wave) Registers

        /// <summary>
        /// Wave DAC Power
        /// </summary>
        /// <remarks>
        /// Bits: E--- ----
        /// </remarks>
        public byte NR30 { get => mmu.ReadByte(0xFF1A); set => mmu.WriteByte(0xFF1A, value); }

        /// <summary>
        /// Wave Length Load
        /// </summary>
        /// <remarks>
        /// Bits: LLLL LLLL
        /// </remarks>
        public byte NR31 { get => mmu.ReadByte(0xFF1B); set => mmu.WriteByte(0xFF1B, value); }

        /// <summary>
        /// Wave Volume Code (00=0%, 01=100%, 10=50%, 11=25%
        /// </summary>
        /// <remarks>
        /// Bits: -VV- ----
        /// </remarks>
        public byte NR32 { get => mmu.ReadByte(0xFF1C); set => mmu.WriteByte(0xFF1C, value); }

        /// <summary>
        /// Wave Frequency LSB
        /// </summary>
        /// <remarks>
        /// Bits: FFFF FFFF
        /// </remarks>
        public byte NR33 { get => mmu.ReadByte(0xFF1D); set => mmu.WriteByte(0xFF1D, value); }

        /// <summary>
        /// Wave Trigger, Length Enable, Frequency MSB
        /// </summary>
        /// <remarks>
        /// Bits: TL-- -FFF
        /// </remarks>
        public byte NR34 { get => mmu.ReadByte(0xFF1E); set => mmu.WriteByte(0xFF1E, value); }

        #endregion

        #region Channel 4 (Noise) Registers

        /// <summary>
        /// Noise Length Load
        /// </summary>
        /// <remarks>
        /// Bits: --LL LLLL
        /// </remarks>
        public byte NR41 { get => mmu.ReadByte(0xFF20); set => mmu.WriteByte(0xFF20, value); }

        /// <summary>
        /// Noise Starting volume, envelope add mode, period
        /// </summary>
        /// <remarks>
        /// Bits: VVVV-APPP
        /// </remarks>
        public byte NR42 { get => mmu.ReadByte(0xFF21); set => mmu.WriteByte(0xFF21, value); }

        /// <summary>
        /// Noise Clock shift, width mode of LFSR, divisor code
        /// </summary>
        /// <remarks>
        /// Bits: SSSS WDDD
        /// </remarks>
        public byte NR43 { get => mmu.ReadByte(0xFF22); set => mmu.WriteByte(0xFF22, value); }

        /// <summary>
        /// Noise Tigger, length enable
        /// </summary>
        /// <remarks>
        /// Bits: TL-- ----
        /// </remarks>
        public byte NR44 { get => mmu.ReadByte(0xFF23); set => mmu.WriteByte(0xFF23, value); }

        #endregion

        #region Control / Status Registers

        /// <summary>
        /// Vin Left enable, left volume, vin right enable, right volume
        /// </summary>
        /// <remarks>
        /// Bits: ALLLBRRR
        /// </remarks>
        public byte NR50 { get => mmu.ReadByte(0xFF24); set => mmu.WriteByte(0xFF24, value); }

        /// <summary>
        /// Left enables, right enables
        /// </summary>
        /// <remarks>
        /// Bits: NW21 NW21
        /// </remarks>
        public byte NR51 { get => mmu.ReadByte(0xFF25); set => mmu.WriteByte(0xFF25, value); }

        /// <summary>
        /// Power control / status, channel length statuses
        /// </summary>
        /// <remarks>
        /// Bits: P--- NW21
        /// </remarks>
        public byte NR52 { get => mmu.ReadByte(0xFF26); set => mmu.WriteByte(0xFF26, value); }

        public int VolumeLeft => (NR50 & 0x7) + 1;
        public int VolumeRight => ((NR50 >> 4) & 0x7) + 1;

        public bool Channel1_Left_On => BitOps.IsBitSet(0, NR51);
        public bool Channel2_Left_On => BitOps.IsBitSet(1, NR51);
        public bool Channel3_Left_On => BitOps.IsBitSet(2, NR51);
        public bool Channel4_Left_On => BitOps.IsBitSet(3, NR51);
        public bool Channel1_Right_On => BitOps.IsBitSet(4, NR51);
        public bool Channel2_Right_On => BitOps.IsBitSet(5, NR51);
        public bool Channel3_Right_On => BitOps.IsBitSet(6, NR51);
        public bool Channel4_Right_On => BitOps.IsBitSet(7, NR51);

        public bool SoundEnabled => BitOps.IsBitSet(7, NR52);

        #endregion

        public APU(MMU mmu)
        {
            this.mmu = mmu;
            mmu.IODataWrite += Mmu_DataWrite;
            Reset();
        }

        public void Reset()
        {
            BufferLeft = new SoundBuffer(BufferLength);
            BufferRight = new SoundBuffer(BufferLength);

            channel1 = new SquareWaveGenerator();
            channel2 = new SquareWaveGenerator() { IgnoreSweep = true };
            channel3 = new WaveTableGenerator();
            channel4 = new NoiseGenerator();

            filter1 = new LowPassFilter(NativeSampleRate / NativeSampleRatio, SampleRate, FilterOrder);
            filter2 = new LowPassFilter(NativeSampleRate / NativeSampleRatio, SampleRate, FilterOrder);
        }

        private void Mmu_DataWrite(object? sender, MmuDataArgs e)
        {
            // APU IO Addresses
            if (e.Address >= 0xFF10 && e.Address <= 0xFF24)
            {
                switch (e.Address)
                {
                    // channel 1
                    case 0xFF10:
                        if (SoundEnabled) channel1.SetSweep(e.Data); 
                        break;
                    case 0xFF11:
                        channel1.SetDuty(e.Data); 
                        break;
                    case 0xFF12:
                        if (SoundEnabled) channel1.SetEnvelope(e.Data); 
                        break;
                    case 0xFF13:
                        if (SoundEnabled) channel1.BaseFrequency = (channel1.BaseFrequency & 0x700) | e.Data;
                        break;
                    case 0xFF14:
                        if (SoundEnabled)
                        {
                            channel1.BaseFrequency = (channel1.BaseFrequency & 0xFF) | ((e.Data & 0x7) << 8);
                            channel1.Repeat = !BitOps.IsBitSet(6, e.Data);
                            if (BitOps.IsBitSet(7, e.Data))
                            {
                                channel1.Reset();
                            }
                        }
                        break;
                    // channel 2
                    case 0xFF16:
                        channel2.SetDuty(e.Data); 
                        break;
                    case 0xFF17:
                        if (SoundEnabled) channel2.SetEnvelope(e.Data);
                        break;
                    case 0xF18:
                        if (SoundEnabled) channel2.BaseFrequency = (channel2.BaseFrequency & 0x700) | e.Data;
                        break;
                    case 0xF19:
                        if (SoundEnabled)
                        {
                            channel2.BaseFrequency = (channel2.BaseFrequency & 0xFF) | ((e.Data & 0x7) << 8);
                            channel2.Repeat = !BitOps.IsBitSet(6, e.Data);
                            if (BitOps.IsBitSet(7, e.Data))
                            {
                                channel2.Reset();
                            }
                        }
                        break;
                    // channel 3
                    case 0xFF1A:
                        if (SoundEnabled) channel3.CanPlay = BitOps.IsBitSet(7, e.Data);
                        break;
                    case 0xFF1B:
                        channel3.Length = e.Data;
                        break;
                    case 0xFF1C:
                        if (SoundEnabled) channel3.SetOutput(e.Data);
                        break;
                    case 0xFF1D:
                        if (SoundEnabled) channel3.Frequency = (channel3.Frequency & 0x700) | e.Data;
                        break;
                    case 0xFF1E:
                        if (SoundEnabled)
                        {
                            channel3.Frequency = (channel3.Frequency & 0xFF) | ((e.Data & 0x7) << 8);
                            channel3.Repeat = !BitOps.IsBitSet(6, e.Data);
                            if (BitOps.IsBitSet(7, e.Data))
                            {
                                channel3.Reset();
                            }
                        }
                        break;
                    case 0xFF20:
                        channel4.Length = e.Data & 0x3F;
                        break;
                    case 0xFF21:
                        if (SoundEnabled) channel4.SetEnvelope(e.Data);
                        break;
                    case 0xFF22:
                        if (SoundEnabled) channel4.SetPolynomial(e.Data);
                        break;
                    case 0xFF23:
                        if (SoundEnabled)
                        {
                            channel4.Repeat = !BitOps.IsBitSet(6, e.Data);
                            if (BitOps.IsBitSet(7, e.Data))
                            {
                                channel4.Reset();
                            }
                        }
                        break;
                }
            }
            else if (e.Address >= 0xFF30 && e.Address <= 0xFF3F)
            {
                // Wave Table data
                int a = (e.Address - 0xFF30) * 2;
                channel3.WaveData[a] = (byte)((e.Data & 0xF0) >> 4);
                channel3.WaveData[a + 1] = (byte)(e.Data & 0xF);
            }
        }

        public void Update(int cycles)
        {
            while(cycles > 0)
            {
                var runTo = Math.Min(Math.Max(subCycles, 1), cycles);
                cycles -= runTo;
                for (int i = 0; i < runTo; i++)
                {
                    GenerateSample();
                }
                subCycles -= runTo;
                while (subCycles < 0)
                {
                    subCycles += SequencerRate;
                    RunCycle();
                }
            }
        }

        private void RunCycle()
        {
            switch(currentCycle)
            {
                case 0: LengthClock(); break;
                case 2: LengthClock(); SweepClock(); break;
                case 4: LengthClock(); break;
                case 6: LengthClock(); SweepClock(); break;
                case 7: EnvelopeClock(); currentCycle = 0; return;
            }
            currentCycle++;
        }

        private void LengthClock()
        {
            channel1.LengthClock();
            channel2.LengthClock();
            channel3.LengthClock();
            channel4.LengthClock();
        }

        private void SweepClock()
        {
            channel1.SweepClock();
        }

        private void EnvelopeClock()
        {
            channel1.EnvelopeClock();
            channel2.EnvelopeClock();
            channel4.EnvelopeClock();
        }

        private void GenerateSample()
        {
            sampleCounter++;
            if (sampleCounter >= cycleSkip)
            {
                sampleCounter -= cycleSkip;
                sampleSync += sampleStepSize;

                if (SoundEnabled)
                {
                    GetSoundOut1();
                    GetSoundOut2();
                }
                else
                {
                    sampleLeft = sampleRight = 0;
                }

                filter1.AddSample(sampleLeft);
                filter2.AddSample(sampleRight);

                if (NativeSampleRate - sampleSync < sampleStepSize)
                {
                    if (t == null)
                    {
                        smplLeftPrev = filter1.GetSample();
                        smplRightPrev = filter2.GetSample();
                        t = (NativeSampleRate - sampleSync) / sampleStepSize;
                    }
                    else
                    {
                        var f1Smple = filter1.GetSample();
                        var f2Smple = filter2.GetSample();

                        var lerpLeft = Num.Lerp(smplLeftPrev, f1Smple, t.Value);
                        var lefpRight = Num.Lerp(smplRightPrev, f2Smple, t.Value);

                        BufferLeft.Push(lerpLeft / MaxVolume);
                        BufferRight.Push(lefpRight / MaxVolume);
                        sampleSync -= NativeSampleRate;
                        t = null;
                    }
                }
            }
        }

        private void GetSoundOut1()
        {
            sampleLeft = 0;

            if (Channel1_Left_On)
            {
                sampleLeft += samples[0] + channel1.Play(1) * ch1vol;
            }
            if (Channel2_Left_On)
            {
                sampleLeft += samples[1] + channel2.Play(1) * ch2vol;
            }
            if (Channel3_Left_On)
            {
                sampleLeft += samples[2] + channel3.Play(1) * ch3vol;
            }
            if (Channel4_Left_On)
            {
                sampleLeft += samples[3] + channel1.Play(1) * ch4vol;
            }

            sampleLeft *= VolumeLeft;
        }

        private void GetSoundOut2()
        {
            sampleRight = 0;

            if (Channel1_Right_On)
            {
                sampleRight += Channel1_Left_On ? samples[0] : channel1.Play(1) * ch1vol;
            }
            if (Channel2_Right_On)
            {
                sampleRight += Channel1_Left_On ? samples[1] : channel2.Play(1) * ch2vol;
            }
            if (Channel3_Right_On)
            {
                sampleRight += Channel1_Left_On ? samples[2] : channel3.Play(1) * ch3vol;
            }
            if (Channel4_Right_On)
            {
                sampleRight += Channel1_Left_On ? samples[3] : channel4.Play(1) * ch4vol;
            }

            sampleRight *= VolumeRight;
        }

    }
}
