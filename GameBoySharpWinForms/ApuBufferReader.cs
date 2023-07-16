using GameBoySharp.Emu;
using NAudio.Wave;
using System.Diagnostics;

namespace GameBoySharpWinForms
{
    internal class ApuBufferReader : ISampleProvider
    {
        private WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        private APU apu;

        public WaveFormat WaveFormat => format;

        public ApuBufferReader(APU apu)
        {
            this.apu = apu;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = 0;
            for(int i = 0; i < count; i += 2)
            {
                if (apu.BufferLeft.Length == 0)
                {
                    buffer[i] = 0;
                    buffer[i + 1] = 0;
                }
                else
                {
                    buffer[i] = apu.BufferLeft.Pop();
                    buffer[i + 1] = apu.BufferRight.Pop();
                    if (buffer[i] > 0) Debugger.Break();
                }
                read++;
            }
            return read;
        }
    }
}
