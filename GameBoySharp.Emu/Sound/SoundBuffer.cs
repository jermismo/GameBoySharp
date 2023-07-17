namespace GameBoySharp.Emu.Sound
{
    public class SoundBuffer
    {
        private float[] data;
        private int start;

        public int Length { get; private set; }

        public float Last
        {
            get => this.Length == 0 ? 0 : data[this.Length - 1];
            set => this.Push(value);
        }

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
        }

        public float Pop()
        {
            var val = this[0];
            Length--;
            start++;
            if (start >= data.Length) 
            { 
                start -= data.Length;
            }
            return val;
        }

        public float Lerp(float i)
        {
            int bottom = (int)Math.Floor(i);
            int top = (int)Math.Ceiling(i);
            float t = i - bottom;
            return (1 - t) * this[bottom] + t * this[top];
        }

        public void Clear()
        {
            start = Length = 0;
        }
    }
}
