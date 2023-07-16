using System.Diagnostics.CodeAnalysis;

namespace GameBoySharp.Emu.Utils
{
    /// <summary>
    /// Represents a 32bit color with transparency.
    /// </summary>
    public struct Color
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Color()
        {
            A = R = G = B = 0;
        }

        public Color(byte lightness)
        {
            A = 0xFF;
            R = G = B = lightness;
        }

        public Color(byte r, byte g, byte b)
        {
            A = 255;
            R = r;
            G = g;
            B = b;
        }

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Converts to an ARGB 32-bit integer color value.
        /// </summary>
        /// <returns>A 32-bit integer</returns>
        public int ToInt()
        {
            return (A << 24) | (R << 16) | (G << 8) | B;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Color"/> class from the 32bit ARGB integer color value.
        /// </summary>
        /// <param name="value">The 32bit integer color value.</param>
        /// <returns>A Color.</returns>
        public static Color FromInt(int value)
        {
            return new Color()
            {
                A = (byte)(value >> 24 & 0xFF),
                R = (byte)(value >> 16 & 0xFF),
                G = (byte)(value >> 8 & 0xFF),
                B = (byte)(value & 0xFF)
            };
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Color"/> class from the ARGB 32bit integer color value.
        /// </summary>
        /// <param name="value">The 32bit integer color value.</param>
        /// <returns>A Color.</returns>
        public static Color FromInt(uint value)
        {
            return new Color()
            {
                A = (byte)(value >> 24 & 0xFF),
                R = (byte)(value >> 16 & 0xFF),
                G = (byte)(value >> 8 & 0xFF),
                B = (byte)(value & 0xFF)
            };
        }

        #region Operators

        /// <summary>
        /// Returns <c>true</c> if this color has the same value as the object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>A bool.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Color c)
            {
                return c.A == A && c.R == R && c.G == G && c.B == B;
            }
            return false;
        }

        /// <summary>
        /// Gets the hash code (converts to 32bit value).
        /// </summary>
        /// <returns>The int value</returns>
        public override int GetHashCode()
        {
            return ToInt();
        }

        /// <summary>
        /// Converts to a hex ARGB color string.
        /// </summary>
        /// <example>
        /// Green will return #FF00FF00
        /// </example>
        /// <returns>A string.</returns>
        public override string ToString()
        {
            return $"#{A:X2}{R:X2}{G:X2}{B:X2}";
        }

        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
