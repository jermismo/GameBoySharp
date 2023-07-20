using System.Numerics;

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
                c *= (float)(0.42 - 0.5 * Math.Cos(2.0 * Math.PI * i / filterOrder) + 0.08 * Math.Cos(4.0 * Math.PI * i / filterOrder));
                coefficients[i] = c;
            }
        }

        public void AddSample(float sample)
        {
            buffer.Push(sample);
        }

        public float GetSample()
        {
            //convolved = 0;
            //var len = Math.Min(coefficients.Length, buffer.Length);
            //for (int i = 0; i < len; i++)
            //{
            //    convolved += buffer[buffer.Length - i - 1] * coefficients[i];
            //}

            int len = Math.Min(coefficients.Length, buffer.Length);
            int simdLength = Vector<float>.Count;

            Vector<float> total = Vector<float>.Zero;
            int i;
            var bfrSpan = buffer.ToSpan();

            for (i = 0; i <= len - simdLength; i += simdLength)
            {
                var coeffVector = new Vector<float>(coefficients, i);
                var bufferVector = new Vector<float>(bfrSpan.Slice(i));

                total += Vector.Multiply(coeffVector, bufferVector);
            }
            float convolved = Vector.Dot(total, Vector<float>.One);

            // if the size is not a multiple of Vector<float>.Count, then handle the remainers
            for (; i < len; i++)
            {
                convolved += bfrSpan[i] * coefficients[i];
            }

            return convolved;
        }

        public static float SinC(float x)
        {
            if (x == 0)
            {
                return 1;
            }
            else
            {
                var xpi = Math.PI * x;
                return (float)(Math.Sin(xpi) / xpi);
            }
        }
    }
}
