using GameBoySharp.Emu.GamePak;

namespace GameBoySharp.Emu.Core
{
    public class GameRom
    {
        protected byte[] ROM;

        public string Name { get; private set; }

        public CartridgeType CartridgeType => (CartridgeType)ROM[0x147];

        public byte RomSize => ROM[0x148];

        public byte RamSize => ROM[0x149];

        public bool IsJapan => ROM[0x14A] == 0;

        public int Version => ROM[0x14C];

        public int CheckSum => ROM[0x14D];

        protected GameRom(byte[] rom)
        {
            ROM = rom;
            Name = "Unknown";
        }

        public virtual byte ReadLoROM(ushort addr)
        {
            return ROM[addr];
        }

        public virtual byte ReadHiROM(ushort addr)
        {
            return ROM[addr];
        }

        public virtual void WriteROM(ushort addr, byte value)
        {
            // Memory Bank Controller 0 ignores writes
        }

        public virtual byte ReadERAM(ushort addr)
        {
            // Memory Bank Controller 0 doesn't have ERAM
            return 0xFF;
        }

        public virtual void WriteERAM(ushort addr, byte value)
        {
            // Memory Bank Controller 0 ignores writes
        }

        public static GameRom Load(byte[] rom)
        {
            var cartType = (CartridgeType)rom[0x147];
            GameRom gp = cartType switch
            {
                CartridgeType.Plain => new GameRom(rom),
                CartridgeType.MBC1 or CartridgeType.MBC1_RAM or CartridgeType.MBC1_RAM_BATT => new MBC1(rom),
                CartridgeType.MBC2 or CartridgeType.MBC2_BATTERY => new MBC2(rom),
                CartridgeType.MBC3 or CartridgeType.MBC3_RAM or CartridgeType.MBC3_RAM_BATT or CartridgeType.MBC3_TIMER_BATT or CartridgeType.MBC3_TIMER_RAM_BATT => new MBC3(rom),
                CartridgeType.MBC5 or CartridgeType.MBC5_RAM or CartridgeType.MBC5_RAM_BATT => new MBC5(rom),
                _ => throw new InvalidDataException($"Unsupported Cartridge Type: {cartType}"),
            };

            // Read the Name
            string name = string.Empty;
            for (var i = 0x134; i <= 0x144; i++)
            {
                var c = rom[i];
                if (c > 0) name += (char)c;
                else break;
            }
            if (name.Length > 0)
            {
                gp.Name = name;
            }

            return gp;
        }
    }
}
