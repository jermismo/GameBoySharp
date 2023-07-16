using System.Runtime.CompilerServices;

namespace GameBoySharp.Emu.Utils
{
    public static class Num
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float i)
        {
            var t = i - Math.Floor(i);
            return (float)(((1 - t) * a) + (t * b));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float n, float a, float b)
        {
            return (n < a) ? a : ((n > b) ? b : n);
        }

    }
}
