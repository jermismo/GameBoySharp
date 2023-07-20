namespace GameBoySharp.Emu.Sound
{
    public class SoundBuffer
    {
        internal float[] data;
        internal int start;

        public int Length { get; private set; }

        public float this[int i]
        {
            get => data[(start + i) % data.Length];
            set => data[(start + i) % data.Length] = value;
        }

        public SoundBuffer(int length)
        {
            data = new float[length];
        }

        public void Push(float value)
        {
            data[(start + Length) % data.Length] = value;
            Length++;
            if (Length > data.Length)
            {
                start = (start + 1) % data.Length;
            }
        }

        public float Pop()
        {
            var val = this[0];
            if (Length > 0)
            {
                Length--;
                start++;
                if (start >= data.Length)
                {
                    start -= data.Length;
                }
            }            
            return val;
        }

        public void Clear()
        {
            start = Length = 0;
        }

        public Span<float> ToSpan()
        {
            // No wrap-around, can return a single span
            if (start + Length <= data.Length)
            {
                return new Span<float>(data, start, Length);
            }
            else
            {
                // Data wraps around from the end of the array to the beginning,
                // need to copy data into a new array to return a single span
                float[] copiedData = new float[data.Length];
                int lengthAtEnd = data.Length - start;
                Array.Copy(data, start, copiedData, 0, lengthAtEnd);
                Array.Copy(data, 0, copiedData, lengthAtEnd, data.Length - lengthAtEnd);
                return copiedData.AsSpan();
            }
        }
    }
}