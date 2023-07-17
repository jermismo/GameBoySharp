namespace GameBoySharp.Emu.Sound
{
    public class LowPassFilter
    {
        private SoundBuffer buffer;
        private float cutoff;
        private float[] coefficients;
        private float convolved;

        public LowPassFilter(int inputSampleRate, int outputSampleRate, int filterOrder)
        {
            buffer = new SoundBuffer(filterOrder + 1);
            coefficients = new float[filterOrder + 1];

            cutoff = (outputSampleRate / 2f) / inputSampleRate;

            var factor = cutoff * 2;
            var halfOrder = filterOrder >> 1;

            for(int i = 0; i < coefficients.Length; i++)
            {
                var c = factor * SinC(factor * (i - halfOrder));

                // blackman window
                c *= 0.42 - 0.5 * Math.Cos(2.0 * Math.PI * i / filterOrder) + 0.08 * Math.Cos(4.0 * Math.PI * i / filterOrder);
                coefficients[i] = (float)c;
            }
        }

        public void AddSample(float sample)
        {
            buffer.Push(sample);
        }

        public float GetSample()
        {
            convolved = 0;
            for (int i = 0; i < Math.Min(coefficients.Length, buffer.Length); i++)
            {
                convolved += buffer[buffer.Length - i - 1] * coefficients[i];
            }

            return convolved;
        }

        public static double SinC(float x)
        {
            if (x == 0)
            {
                return 1;
            }
            else
            {
                var xpi = Math.PI * x;
                return (Math.Sin(xpi) / xpi);
            }
        }
    }
}
