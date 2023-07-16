namespace GameBoySharp.Emu.Utils
{
    /// <summary>
    /// Holds constant values
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Gameboy clock speed.
        /// </summary>
        public const int DMG_4Mhz = 4194304;
        
        /// <summary>
        /// LCD screen refresh rate
        /// </summary>
        public const float REFRESH_RATE = 59.7275f;
        
        /// <summary>
        /// The number of CPU cycles per screen update
        /// </summary>
        public const int CYCLES_PER_UPDATE = (int)(DMG_4Mhz / REFRESH_RATE);
        
        /// <summary>
        /// The number of milliseconds per frame
        /// </summary>
        public const float MILLIS_PER_FRAME = 16.74f;
    }
}
