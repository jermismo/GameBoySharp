namespace GameBoySharp.Emu.Core
{
    public class MmuDataArgs : EventArgs
    {
        public ushort Address { get; set; }
        public byte Data { get; set; }
        public MmuDataArgs(ushort address, byte data)
        {
            Address = address;
            Data = data;
        }
    }
}
