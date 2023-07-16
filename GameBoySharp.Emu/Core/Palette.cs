using GameBoySharp.Emu.Utils;

namespace GameBoySharp.Emu.Core
{
    public struct PaletteInfo
    {
        public string Name { get; set; }
        public Color[] Colors { get; set; }
        public PaletteInfo(string name, Color[] colors)
        {
            Name = name;
            Colors = colors;
        }
    }
}
