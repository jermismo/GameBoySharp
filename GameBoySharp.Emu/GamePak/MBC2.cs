using GameBoySharp.Emu.Core;

namespace GameBoySharp.Emu.GamePak
{
    public class MBC2 : GameRom
    {

        private readonly byte[] ERAM = new byte[0x200]; //MBC2 512x4b internal ram
        private bool ERAM_ENABLED;
        private int ROM_BANK;
        private const int ROM_OFFSET = 0x4000;

        public MBC2(byte[] rom) : base(rom) { }

        public override byte ReadERAM(ushort addr)
        {
            if (ERAM_ENABLED)
            {
                return ERAM[addr & 0x1FFF];
            }
            else
            {
                return 0xFF;
            }
        }

        public override byte ReadLoROM(ushort addr)
        {
            return ROM[addr];
        }

        public override byte ReadHiROM(ushort addr)
        {
            return ROM[(ROM_OFFSET * ROM_BANK) + (addr & 0x3FFF)];
        }

        public override void WriteERAM(ushort addr, byte value)
        {
            if (ERAM_ENABLED)
            {
                ERAM[addr & 0x1FFF] = value;
            }
        }

        public override void WriteROM(ushort addr, byte value)
        {
            switch (addr)
            {
                case ushort _ when addr >= 0x0000 && addr <= 0x1FFF:
                    ERAM_ENABLED = ((value & 0x1) == 0x0);
                    break;
                case ushort _ when addr >= 0x2000 && addr <= 0x3FFF:
                    ROM_BANK = value & 0xF; //only last 4bits are used
                    break;
            }
        }

    }
}
