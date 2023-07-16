namespace GameBoySharp.Emu.GamePak
{
    public enum CartridgeType
    {
        Plain = 0x00,
        MBC1 = 0x01,
        MBC1_RAM = 0x02,
        MBC1_RAM_BATT = 0x03,
        MBC2 = 0x05,
        MBC2_BATTERY = 0x06,
        RAM = 0x08,
        RAM_BATTERY = 0x09,
        MMM01 = 0x0B,
        MMM01_SRAM = 0x0C,
        MMM01_SRAM_BATT = 0x0D,
        MBC3_TIMER_BATT = 0x0F,
        MBC3_TIMER_RAM_BATT = 0x10,
        MBC3 = 0x11,
        MBC3_RAM = 0x12,
        MBC3_RAM_BATT = 0x13,
        MBC_UNKNOWN_BATT = 0x17,
        MBC5 = 0x19,
        MBC5_RAM = 0x1A,
        MBC5_RAM_BATT = 0x1B,
        MBC5_RUMBLE = 0x1C,
        MBC5_RUMBLE_SRAM = 0x1D,
        MBC5_RUMBLE_SRAM_BATT = 0x1E,
        POCKET_CAMERA = 0x1F,
        BANDAI_TAMA5 = 0xFD,
        HUDSON_HUC3 = 0xFE,
        HUDSON_HUC1 = 0xFF,
    }
}
