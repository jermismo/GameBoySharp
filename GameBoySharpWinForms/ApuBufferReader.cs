using GameBoySharp.Emu;
using NAudio.Wave;

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

            int leftPops = 0;
            int rightPops = 0;

            for(int i = offset; i < count; i += 2)
            {
                if (apu.BufferLeft.Length > 0)
                {
                    buffer[i] = apu.BufferLeft.Pop();
                    leftPops++;
                }
                if (apu.BufferRight.Length > 0)
                {   
                    buffer[i + 1] = apu.BufferRight.Pop();
                    rightPops++;
                }
                read += 2;
            }

            return read;
        }
    }
}
