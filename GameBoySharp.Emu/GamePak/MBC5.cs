using GameBoySharp.Emu.Core;

namespace GameBoySharp.Emu.GamePak
{
    public class MBC5 : GameRom
    {
        private readonly byte[] ERAM = new byte[0x20000]; //MBC5 MAX 128 KBytes (16 banks of 8KBytes each)
        private bool ERAM_ENABLED;
        private int ROM_BANK_LOW_BITS = 1; //default as 0 is 0x000 - 0x3FFF fixed
        private int ROM_BANK_HI_BIT;
        private const int ROM_OFFSET = 0x4000;
        private const int ERAM_OFFSET = 0x2000;
        private int RAM_BANK;

        public MBC5(byte[] rom) : base(rom) { }

        public override byte ReadERAM(ushort addr)
        {
            if (ERAM_ENABLED)
            {
                return ERAM[(ERAM_OFFSET * RAM_BANK) + (addr & 0x1FFF)];
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
            return ROM[(ROM_OFFSET * (ROM_BANK_HI_BIT + ROM_BANK_LOW_BITS)) + (addr & 0x3FFF)];
        }

        public override void WriteERAM(ushort addr, byte value)
        {
            if (ERAM_ENABLED)
            {
                ERAM[(ERAM_OFFSET * RAM_BANK) + (addr & 0x1FFF)] = value;
            }
        }

        public override void WriteROM(ushort addr, byte value)
        {
            switch (addr)
            {
                case ushort _ when addr >= 0x0000 && addr <= 0x1FFF:
                    ERAM_ENABLED = value == 0x0A;
                    break;
                case ushort _ when addr >= 0x2000 && addr <= 0x2FFF:
                    ROM_BANK_LOW_BITS = value;
                    break;
                case ushort _ when addr >= 0x3000 && addr <= 0x3FFF:
                    ROM_BANK_HI_BIT = value;
                    break;
                case ushort _ when addr >= 0x4000 && addr <= 0x5FFF:
                    RAM_BANK = value & 0xF;
                    break;
            }
        }

    }
}
