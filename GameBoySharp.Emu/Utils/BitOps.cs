using System.Runtime.CompilerServices;

namespace GameBoySharp.Emu.Utils
{
    /// <summary>
    /// Helper class for bit operations
    /// </summary>
    public static class BitOps
    {
        /// <summary>
        /// Sets the bit number (0-based) to 1 and returns the new value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte BitSet(byte bitNumber, byte value)
        {
            return value |= (byte)(1 << bitNumber);
        }

        /// <summary>
        /// Sets the bit number (0-based) to 0 and returns the new value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte BitClear(int bitNumber, byte value)
        {
            return value &= (byte)~(1 << bitNumber);
        }

        /// <summary>
        /// Returns <c>true</c> if the bit number (0-based) is set to 1 in the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBitSet(int bitNumber, int value)
        {
            return ((value >> bitNumber) & 1) == 1;
        }
    }
}
