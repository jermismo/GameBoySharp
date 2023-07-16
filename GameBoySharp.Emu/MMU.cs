using System.Runtime.CompilerServices;
using GameBoySharp.Emu.Core;
using static GameBoySharp.Emu.Utils.BitOps;

namespace GameBoySharp.Emu
{
    /// <summary>
    /// Memory Management Unit
    /// </summary>
    public class MMU
    {
        #region Constants

        public const int LoROMEnd = 0x3FFF;
        public const int HighROMEnd = 0x7FFF;
        public const int VRamEnd = 0x9FFF;
        public const int ERamEnd = 0xBFFF;
        public const int WRam0End = 0xCFFF;
        public const int WRam1End = 0xDFFF;
        public const int WRam0Echo = 0xEFFF;
        public const int WRam1Echo = 0xFDFF;
        public const int OamEnd = 0xFE9F;
        public const int UnusedEnd = 0xFEFF;
        public const int IoEnd = 0xFF7F;
        public const int HRamEnd = 0xFFFF;

        public const int VRamMask = 0x1FFF;

        #endregion

        /// <summary>
        /// The currently loaded game.
        /// </summary>
        private GameRom? gameRom;

        public Action<char> DebugCallback { get; set; }

        public event EventHandler<MmuDataArgs> DataWrite;
                
        #region Memory

        /// <summary>
        /// Video Ram
        /// </summary>
        private byte[] VRAM;

        /// <summary>
        /// Working Ram Bank 0
        /// </summary>
        private byte[] WRAM0;

        /// <summary>
        /// Working Ram Bank 1
        /// </summary>
        private byte[] WRAM1;

        /// <summary>
        /// Object Attribute Map (Sprites)
        /// </summary>
        private byte[] OAM;

        /// <summary>
        /// Memory Mapped IO Data
        /// </summary>
        private byte[] IO;

        /// <summary>
        /// High Ram - like WRAM but faster
        /// </summary>
        private byte[] HRAM;

        #endregion

        #region Timer IO Registers

        /// <summary>
        /// Divider Register - a counter that always increments at 16,384 Hz. When written to using MMU.WriteByte it is reset to 0.
        /// </summary>
        /// <remarks>
        /// DIV - IO FF04
        /// </remarks>
        public byte Divider { get { return IO[0x04]; } set { IO[0x04] = value; } }

        /// <summary>
        /// Timer Counter Register - increments at the rate defined in the Timer Control (TAC) register.
        /// When it hits 0xFF it gets reset to the value in Timer Modulo (TMA).
        /// </summary>
        /// <remarks>
        /// TIMA - IO FF05
        /// </remarks>
        public byte TimerCounter { get { return IO[0x05]; } set { IO[0x05] = value; } }

        /// <summary>
        /// Timer Modulo Register
        /// </summary>
        /// <remarks>
        /// TMA - IO FF06
        /// </remarks>
        public byte TimerModulo { get { return IO[0x06]; } set { IO[0x06] = value; } }

        /// <summary>
        /// Timer Control Register - used to control the timer.
        /// </summary>
        /// <remarks>
        /// TAC - IO FF07
        /// </remarks>
        public byte TimerControl { get { return IO[0x07]; } set { IO[0x07] = value; } }

        /// <summary>
        /// Timer Enabled
        /// </summary>
        /// <remarks>
        /// TAC bit 2 != 0
        /// </remarks>
        public bool TimerEnabled { get { return (IO[0x07] & 0x4) != 0; } }

        /// <summary>
        /// Timer Frequency Hz (00=4069, 01=26144, 02=65536, 03=16384)
        /// </summary>
        /// <remarks>
        /// TAC bits 0 and 1
        /// </remarks>
        public byte TimerFrequency { get { return (byte)(IO[0x07] & 0x3); } }

        #endregion

        #region Interrupt IO Flags
        
        /// <summary>
        /// Marks which interrupts are currently enabled, by bit, where 1 = enabled.
        /// </summary>        
        /// <remarks>
        /// Bit 0: V-Blank (INT 40h)
        /// Bit 1: LCD Stat (INT 48h)
        /// Bit 2: Timer Interrupt (INT 50h)
        /// Bit 3: Serial Interrupt (INT 58h)
        /// Bit 4: Joypad Interrupt (INT 60h)
        /// </remarks>
        public byte InterruptEnable { get { return HRAM[0x7F]; } set { HRAM[0x7F] = value; } }

        /// <summary>
        /// Marks which interrupts are currently flagged, by bit, where 1 = flagged.
        /// </summary>        
        /// <remarks>
        /// Bit 0: V-Blank (INT 40h)
        /// Bit 1: LCD Stat (INT 48h)
        /// Bit 2: Timer Interrupt (INT 50h)
        /// Bit 3: Serial Interrupt (INT 58h)
        /// Bit 4: Joypad Interrupt (INT 60h)
        /// </remarks>
        public byte InterruptFlags { get { return IO[0x0F]; } set { IO[0x0F] = value; } }

        #endregion

        #region PPU UI Registers

        /// <summary>
        /// LCD Control Register (LCDC)
        /// </summary>
        public byte LcdControl { get { return IO[0x40]; } }
        
        /// <summary>
        /// LCD Status Register (STAT)
        /// </summary>
        public byte LcdStatus { get { return IO[0x41]; } set { IO[0x41] = value; } }

        /// <summary>
        /// Scroll Y Register (SCY)
        /// </summary>
        public byte ScrollY { get { return IO[0x42]; } }
        
        /// <summary>
        /// Scroll X Register (SCX)
        /// </summary>
        public byte ScrollX { get { return IO[0x43]; } }
        
        /// <summary>
        /// LCD Y-Coordinate (LY)
        /// </summary>
        public byte LcdY { get { return IO[0x44]; } set { IO[0x44] = value; } }
        
        /// <summary>
        /// LCD Y-Coordinate Compare (LYC)
        /// </summary>
        public byte LcdYCompare { get { return IO[0x45]; } }
        
        /// <summary>
        /// Window Y Position (WY)
        /// </summary>
        public byte WindowY { get { return IO[0x4A]; } }
        
        /// <summary>
        /// Window X Position (WX)
        /// </summary>
        public byte WindowX { get { return IO[0x4B]; } }

        /// <summary>
        /// Background Palette Data (GBP)
        /// </summary>
        /// <remarks>
        /// Non Gameboy Color Only
        /// </remarks>
        public byte BackgroundPalette { get { return IO[0x47]; } }

        /// <summary>
        /// Object Palette 0 Data (OBP0)
        /// </summary>
        /// <remarks>
        /// Non Gameboy Color Only
        /// </remarks>
        public byte ObjectPalette0 { get { return IO[0x48]; } }

        /// <summary>
        /// Object Palette 1 Data (OBP1)
        /// </summary>
        /// <remarks>
        /// Non Gameboy Color Only
        /// </remarks>
        public byte ObjectPalette1 { get { return IO[0x49]; } }

        /// <summary>
        /// Joypad IO Data
        /// </summary>
        public byte Joypad { get { return IO[0x00]; } set { IO[0x00] = value; } }

        #endregion

        public MMU()
        {
            Reset();
        }

        public void Reset()
        {
            VRAM = new byte[0x2000];
            WRAM0 = new byte[0x1000];
            WRAM1 = new byte[0x1000];
            OAM = new byte[0xA0];
            IO = new byte[0x80];
            HRAM = new byte[0x80];

            //FF4D - KEY1 - CGB Mode Only - Prepare Speed Switch
            //HardCoded to FF to identify DMG as 00 is GBC
            IO[0x4D] = 0xFF;

            IO[0x10] = 0x80;
            IO[0x11] = 0xBF;
            IO[0x12] = 0xF3;
            IO[0x14] = 0xBF;
            IO[0x16] = 0x3F;
            IO[0x19] = 0xBF;
            IO[0x1A] = 0x7F;
            IO[0x1B] = 0xFF;
            IO[0x1C] = 0x9F;
            IO[0x1E] = 0xBF;
            IO[0x20] = 0xFF;
            IO[0x23] = 0xBF;
            IO[0x24] = 0x77;
            IO[0x25] = 0xF3;
            IO[0x26] = 0xF1;
            IO[0x40] = 0x91;
            IO[0x47] = 0xFC;
            IO[0x48] = 0xFF;
            IO[0x49] = 0xFF; 
        }

        public byte ReadByte(ushort address)
        {
            // General Memory Map 64KB

            return address switch
            {                                            
                //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00)
                ushort _ when address <= LoROMEnd => gameRom?.ReadLoROM(address) ?? 0,

                // 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                ushort _ when address <= HighROMEnd => gameRom?.ReadHiROM(address) ?? 0,

                // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                ushort _ when address <= VRamEnd => VRAM[address & 0x1FFF],

                // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any)
                ushort _ when address <= ERamEnd => gameRom?.ReadERAM(address) ?? 0,

                // C000-CFFF 4KB Work RAM Bank 0(WRAM)
                ushort _ when address <= WRam0End => WRAM0[address & 0xFFF],

                // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode)
                ushort _ when address <= WRam1End => WRAM1[address & 0xFFF],

                // E000-FDFF Same as 0xC000-DDFF(ECHO)  
                ushort _ when address <= WRam0Echo => WRAM0[address & 0xFFF],

                // E000-FDFF Same as 0xC000-DDFF(ECHO)
                ushort _ when address <= WRam1Echo => WRAM1[address & 0xFFF],

                // FE00-FE9F Sprite Attribute Table(OAM)
                ushort _ when address <= OamEnd => OAM[address - 0xFE00],

                // FEA0-FEFF Not Usable 0
                ushort _ when address <= UnusedEnd => 0x00,

                // FF00-FF7F IO Ports
                ushort _ when address <= IoEnd => IO[address & 0x7F],

                // FF80-FFFE High RAM(HRAM)
                ushort _ when address <= HRamEnd => HRAM[address & 0x7F],

                _ => 0xFF,
            };
        }

        public void WriteByte(ushort address, byte value)
        {
            // General Memory Map 64KB
            switch (address)
            {                            
                case ushort _ when address <= HighROMEnd: //0000-3FFF 16KB ROM Bank 00 (in cartridge, private at bank 00) 4000-7FFF 16KB ROM Bank 01..NN(in cartridge, switchable bank number)
                    gameRom?.WriteROM(address, value);
                    break;
                case ushort _ when address <= VRamEnd:    // 8000-9FFF 8KB Video RAM(VRAM)(switchable bank 0-1 in CGB Mode)
                    VRAM[address & 0x1FFF] = value;
                    break;
                case ushort _ when address <= ERamEnd:    // A000-BFFF 8KB External RAM(in cartridge, switchable bank, if any)
                    gameRom?.WriteERAM(address, value);
                    break;
                case ushort _ when address <= WRam0End:    // C000-CFFF 4KB Work RAM Bank 0(WRAM) <br/>
                    WRAM0[address & 0xFFF] = value;
                    break;
                case ushort _ when address <= WRam1End:    // D000-DFFF 4KB Work RAM Bank 1(WRAM)(switchable bank 1-7 in CGB Mode)
                    WRAM1[address & 0xFFF] = value;
                    break;
                case ushort _ when address <= WRam0Echo:    // E000-FDFF Same as 0xC000-DDFF(ECHO)  
                    WRAM0[address & 0xFFF] = value;
                    break;
                case ushort _ when address <= WRam1Echo:    // E000-FDFF Same as 0xC000-DDFF(ECHO)
                    WRAM1[address & 0xFFF] = value;
                    break;
                case ushort _ when address <= OamEnd:       // FE00-FE9F Sprite Attribute Table(OAM)
                    OAM[address & 0x9F] = value;
                    break;
                case ushort _ when address <= UnusedEnd:    // FEA0-FEFF Not Usable
                    //Console.WriteLine("Warning: Tried to write to NOT USABLE space");
                    break;
                case ushort _ when address <= IoEnd:    // FF00-FF7F IO Ports
                    switch (address)
                    {
                        case 0xFF0F: value |= 0xE0; break; // IF returns 1 on first 3 unused bits
                        case 0xFF04:                       // DIV on write = 0
                        case 0xFF44: value = 0; break;     // LY on write = 0
                        case 0xFF46: DMA(value); break;
                    }

                    // Serial Link output for debug
                    if (address == 0xFF02) {
                        if (value == 0x81) DebugCallback?.Invoke(Convert.ToChar(ReadByte(0xFF01)));
                    }

                    IO[address & 0x7F] = value;
                    
                    // APU writes
                    //if (address >= 0xFF10 && address <= 0xFF3F)
                    //{
                    //    DataWrite?.Invoke(this, new MmuDataArgs(address, value));
                    //}
                    break;
                case ushort _ when address <= HRamEnd:    // FF80-FFFE High RAM(HRAM)
                    HRAM[address & 0x7F] = value;
                    break;
            }
        }

        public ushort ReadWord(ushort addr)
        {
            return (ushort)(ReadByte((ushort)(addr + 1)) << 8 | ReadByte(addr));
        }

        public void WriteWord(ushort addr, ushort w)
        {
            WriteByte((ushort)(addr + 1), (byte)(w >> 8));
            WriteByte(addr, (byte)w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadOAM(int addr)
        {
            return OAM[addr];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadVRAM(int addr)
        {
            return VRAM[addr & 0x1FFF];
        }

        public Span<byte> GetOam() => new Span<byte>(OAM);
        public Span<byte> GetVRam() => new Span<byte>(VRAM);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RequestInterrupt(byte b)
        {
            InterruptFlags = BitSet(b, InterruptFlags);
        }

        /// <summary>
        /// Direct Memory Access transfer. Fill OAM from Memory at the address.
        /// </summary>
        /// <param name="address"></param>
        private void DMA(byte address)
        {
            ushort addr = (ushort)(address << 8);
            for (byte i = 0; i < OAM.Length; i++)
            {
                OAM[i] = ReadByte((ushort)(addr + i));
            }
        }

        public void LoadGamePak(string cartName)
        {
            byte[] rom = File.ReadAllBytes(cartName);
            Reset();
            gameRom = GameRom.Load(rom);
        }
    }
}
