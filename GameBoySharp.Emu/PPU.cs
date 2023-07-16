using GameBoySharp.Emu.Core;
using GameBoySharp.Emu.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static GameBoySharp.Emu.Utils.BitOps;

namespace GameBoySharp.Emu
{
    /// <summary>
    /// Picture Processing Unit
    /// </summary>
    public class PPU
    {
        private const int SCREEN_WIDTH = 160;
        private const int SCREEN_HEIGHT = 144;
        private const int SCREEN_VBLANK_HEIGHT = 153;
        private const int OAM_CYCLES = 80;
        private const int VRAM_CYCLES = 172;
        private const int HBLANK_CYCLES = 204;
        private const int SCANLINE_CYCLES = 456;

        private const int VBLANK_INTERRUPT = 0;
        private const int LCD_INTERRUPT = 1;

        /// <summary>
        /// The color palette to draw the screen with. 4 colors: 0=white, 1=light gray, 2=dark gray, 3=black.
        /// </summary>
        public PaletteInfo Palette { get; set; } = Emulator.Palettes[2];

        private int scanlineCounter;

        /// <summary>
        /// Fires when a new frame is ready to be drawn.
        /// </summary>
        public event EventHandler? FrameReady;

        /// <summary>
        /// The bitmap containing the current frame.
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        public PPU()
        {
            Bitmap = new Bitmap(SCREEN_WIDTH, SCREEN_HEIGHT, PixelFormat.Bgra32);
        }

        public void Reset()
        {
            Bitmap.Clear(new Color(0));
            scanlineCounter = 0;
        }

        public void Update(int cycles, MMU mmu)
        {
            scanlineCounter += cycles;
            byte currentMode = (byte)(mmu.LcdStatus & 0x3); //Current Mode Mask

            if (IsLCDEnabled(mmu.LcdControl))
            {
                switch (currentMode)
                {
                    case 2: //Accessing OAM - Mode 2 (80 cycles)
                        if (scanlineCounter >= OAM_CYCLES)
                        {
                            ChangeStatMode(3, mmu);
                            scanlineCounter -= OAM_CYCLES;
                        }
                        break;
                    case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles
                        if (scanlineCounter >= VRAM_CYCLES)
                        {
                            ChangeStatMode(0, mmu);
                            DrawScanLine(mmu);
                            scanlineCounter -= VRAM_CYCLES;
                        }
                        break;
                    case 0: //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
                        if (scanlineCounter >= HBLANK_CYCLES)
                        {
                            mmu.LcdY++;
                            scanlineCounter -= HBLANK_CYCLES;

                            if (mmu.LcdY == SCREEN_HEIGHT)
                            { //check if we arrived Vblank
                                ChangeStatMode(1, mmu);
                                mmu.RequestInterrupt(VBLANK_INTERRUPT);
                                OnRenderFrame();
                            }
                            else
                            { //not arrived yet so return to 2
                                ChangeStatMode(2, mmu);
                            }
                        }
                        break;
                    case 1: //VBLANK - Mode 1 (4560 cycles - 10 lines)
                        if (scanlineCounter >= SCANLINE_CYCLES)
                        {
                            mmu.LcdY++;
                            scanlineCounter -= SCANLINE_CYCLES;

                            if (mmu.LcdY > SCREEN_VBLANK_HEIGHT)
                            { //check end of VBLANK
                                ChangeStatMode(2, mmu);
                                mmu.LcdY = 0;
                            }
                        }
                        break;
                }

                if (mmu.LcdY == mmu.LcdYCompare)
                { //handle coincidence Flag
                    mmu.LcdStatus = BitSet(2, mmu.LcdStatus);
                    if (IsBitSet(6, mmu.LcdStatus))
                    {
                        mmu.RequestInterrupt(LCD_INTERRUPT);
                    }
                }
                else
                {
                    mmu.LcdStatus = BitClear(2, mmu.LcdStatus);
                }

            }
            else
            { //LCD Disabled
                scanlineCounter = 0;
                mmu.LcdY = 0;
                mmu.LcdStatus = (byte)(mmu.LcdStatus & ~0x3);
            }
        }

        private void ChangeStatMode(int mode, MMU mmu)
        {
            byte STAT = (byte)(mmu.LcdStatus & ~0x3);
            mmu.LcdStatus = (byte)(STAT | mode);
            
            //Accessing OAM - Mode 2 (80 cycles)
            if (mode == 2 && IsBitSet(5, STAT))
            { // Bit 5 - Mode 2 OAM Interrupt         (1=Enable) (Read/Write)
                mmu.RequestInterrupt(LCD_INTERRUPT);
            }

            //case 3: //Accessing VRAM - Mode 3 (172 cycles) Total M2+M3 = 252 Cycles

            //HBLANK - Mode 0 (204 cycles) Total M2+M3+M0 = 456 Cycles
            else if (mode == 0 && IsBitSet(3, STAT))
            { // Bit 3 - Mode 0 H-Blank Interrupt     (1=Enable) (Read/Write)
                mmu.RequestInterrupt(LCD_INTERRUPT);
            }

            //VBLANK - Mode 1 (4560 cycles - 10 lines)
            else if (mode == 1 && IsBitSet(4, STAT))
            { // Bit 4 - Mode 1 V-Blank Interrupt     (1=Enable) (Read/Write)
                mmu.RequestInterrupt(LCD_INTERRUPT);
            }

        }

        private void DrawScanLine(MMU mmu)
        {
            byte LCDC = mmu.LcdControl;
            if (IsBitSet(0, LCDC))
            { //Bit 0 - BG Display (0=Off, 1=On)
                RenderBG(mmu);
            }
            if (IsBitSet(1, LCDC))
            { //Bit 1 - OBJ (Sprite) Display Enable
                RenderSprites(mmu);
            }
        }

        private void RenderBG(MMU mmu)
        {
            byte WX = (byte)(mmu.WindowX - 7); //WX needs -7 Offset
            byte WY = mmu.WindowY;
            byte LY = mmu.LcdY;

            if (LY > SCREEN_HEIGHT) return;

            byte LCDC = mmu.LcdControl;
            byte SCY = mmu.ScrollY;
            byte SCX = mmu.ScrollX;
            byte BGP = mmu.BackgroundPalette;
            bool isWin = IsWindow(LCDC, WY, LY);

            byte y = isWin ? (byte)(LY - WY) : (byte)(LY + SCY);
            byte tileLine = (byte)((y & 7) * 2);

            ushort tileRow = (ushort)(y / 8 * 32);
            ushort tileMap = isWin ? GetWindowTileMapAdress(LCDC) : GetBGTileMapAdress(LCDC);

            byte hi = 0;
            byte lo = 0;

            var vram = mmu.GetVRam();

            for (int p = 0; p < SCREEN_WIDTH; p++)
            {
                byte x = isWin && p >= WX ? (byte)(p - WX) : (byte)(p + SCX);
                if ((p & 0x7) == 0 || ((p + SCX) & 0x7) == 0)
                {
                    ushort tileCol = (ushort)(x / 8);
                    ushort tileAdress = (ushort)(tileMap + tileRow + tileCol);

                    ushort tileLoc;
                    if (IsSignedAdress(LCDC))
                    {
                        tileLoc = (ushort)(GetTileDataAdress(LCDC) + vram[tileAdress & MMU.VRamMask] * 16);
                    }
                    else
                    {
                        tileLoc = (ushort)(GetTileDataAdress(LCDC) + ((sbyte)vram[tileAdress & MMU.VRamMask] + 128) * 16);
                    }

                    lo = vram[(tileLoc + tileLine) & MMU.VRamMask];     // mmu.ReadVRAM((ushort)(tileLoc + tileLine));
                    hi = vram[(tileLoc + tileLine + 1) & MMU.VRamMask]; // mmu.ReadVRAM((ushort)(tileLoc + tileLine + 1));
                }

                int colorBit = 7 - (x & 7); //inversed
                int colorId = GetColorIdBits(colorBit, lo, hi);
                int colorIdThroughtPalette = GetColorIdThroughtPalette(BGP, colorId);

                Bitmap.SetPixel(p, LY, Palette.Colors[colorIdThroughtPalette]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetColorIdBits(int colorBit, byte l, byte h)
        {
            int hi = (h >> colorBit) & 0x1;
            int lo = (l >> colorBit) & 0x1;
            return (hi << 1 | lo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetColorIdThroughtPalette(int palette, int colorId)
        {
            return (palette >> colorId * 2) & 0x3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSignedAdress(byte LCDC)
        {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return IsBitSet(4, LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetBGTileMapAdress(byte LCDC)
        {
            //Bit 3 - BG Tile Map Display Select     (0=9800-9BFF, 1=9C00-9FFF)
            return IsBitSet(3, LCDC) ? (ushort)0x9C00 : (ushort)0x9800;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetWindowTileMapAdress(byte LCDC)
        {
            //Bit 6 - Window Tile Map Display Select(0 = 9800 - 9BFF, 1 = 9C00 - 9FFF)
            return IsBitSet(6, LCDC) ? (ushort)0x9C00 : (ushort)0x9800;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort GetTileDataAdress(byte LCDC)
        {
            //Bit 4 - BG & Window Tile Data Select   (0=8800-97FF, 1=8000-8FFF)
            return IsBitSet(4, LCDC) ? (ushort)0x8000 : (ushort)0x8800; //0x8800 signed area
        }

        private void RenderSprites(MMU mmu)
        {
            byte LY = mmu.LcdY;
            byte LCDC = mmu.LcdControl;
            var vram = mmu.GetVRam();
            for (int i = 0x9C; i >= 0; i -= 4)
            { 
                //0x9F OAM Size, 40 Sprites x 4 bytes:
                int y = mmu.ReadOAM(i) - 16;    //Byte0 - Y Position //needs 16 offset
                int x = mmu.ReadOAM(i + 1) - 8; //Byte1 - X Position //needs 8 offset
                byte tile = mmu.ReadOAM(i + 2); //Byte2 - Tile/Pattern Number
                byte attr = mmu.ReadOAM(i + 3); //Byte3 - Attributes/Flags

                if ((LY >= y) && (LY < (y + SpriteSize(LCDC))))
                {
                    byte palette = IsBitSet(4, attr) ? mmu.ObjectPalette1 : mmu.ObjectPalette0; //Bit4   Palette number  **Non CGB Mode Only** (0=OBP0, 1=OBP1)

                    int tileRow = IsYFlipped(attr) ? SpriteSize(LCDC) - 1 - (LY - y) : (LY - y);

                    ushort tileddress = (ushort)(0x8000 + (tile * 16) + (tileRow * 2));
                    byte lo = vram[tileddress & MMU.VRamMask]; // mmu.ReadVRAM(tileddress);
                    byte hi = vram[(tileddress + 1) & MMU.VRamMask]; // mmu.ReadVRAM((ushort)(tileddress + 1));

                    for (int p = 0; p < 8; p++)
                    {
                        int IdPos = IsXFlipped(attr) ? p : 7 - p;
                        int colorId = GetColorIdBits(IdPos, lo, hi);
                        int colorIdThroughtPalette = GetColorIdThroughtPalette(palette, colorId);

                        if ((x + p) >= 0 && (x + p) < SCREEN_WIDTH)
                        {
                            if (!IsTransparent(colorId) && (IsAboveBG(attr) || IsBGWhite(mmu.BackgroundPalette, x + p, LY)))
                            {
                                Bitmap.SetPixel(x + p, LY, Palette.Colors[colorIdThroughtPalette]);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsBGWhite(byte BGP, int x, int y)
        {
            int id = BGP & 0x3;
            return Bitmap.GetPixel(x, y) == Palette.Colors[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAboveBG(byte attr)
        {
            //Bit7 OBJ-to - BG Priority(0 = OBJ Above BG, 1 = OBJ Behind BG color 1 - 3)
            return attr >> 7 == 0;
        }

        public void OnRenderFrame()
        {
            this.FrameReady?.Invoke(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLCDEnabled(byte LCDC)
        {
            //Bit 7 - LCD Display Enable
            return IsBitSet(7, LCDC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SpriteSize(byte LCDC)
        {
            //Bit 2 - OBJ (Sprite) Size (0=8x8, 1=8x16)
            return IsBitSet(2, LCDC) ? 16 : 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsXFlipped(int attr)
        {
            //Bit5   X flip(0 = Normal, 1 = Horizontally mirrored)
            return IsBitSet(5, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsYFlipped(byte attr)
        {
            //Bit6 Y flip(0 = Normal, 1 = Vertically mirrored)
            return IsBitSet(6, attr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsTransparent(int b)
        {
            return b == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsWindow(byte LCDC, byte WY, byte LY)
        {
            //Bit 5 - Window Display Enable (0=Off, 1=On)
            return IsBitSet(5, LCDC) && WY <= LY;
        }
    }
}
