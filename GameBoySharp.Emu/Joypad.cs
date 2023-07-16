using static GameBoySharp.Emu.Utils.BitOps;

namespace GameBoySharp.Emu
{
    public class Joypad
    {
        private const int JOYPAD_INTERRUPT = 4;
        private const byte PAD_MASK = 0x10;
        private const byte BUTTON_MASK = 0x20;
        private byte pad = 0xF;
        private byte buttons = 0xF;

        public void Up(bool isDown) { HandleButton(0x14, isDown); }
        
        public void Down(bool isDown) { HandleButton(0x18, isDown); }

        public void Left(bool isDown) { HandleButton(0x12, isDown); }

        public void Right(bool isDown) { HandleButton(0x11, isDown); }

        public void AButton(bool isDown) { HandleButton(0x21, isDown); }

        public void BButton(bool isDown) { HandleButton(0x22, isDown); }

        public void StartButton(bool isDown) { HandleButton(0x28, isDown); }

        public void SelectButton(bool isDown) { HandleButton(0x24, isDown); }

        internal void HandleButton(byte b, bool isDown)
        {
            if ((b & PAD_MASK) == PAD_MASK)
            {
                pad = isDown ? (byte)(pad & ~(b & 0xF)) : (byte)(pad | (b & 0xF));
            }
            else if ((b & BUTTON_MASK) == BUTTON_MASK)
            {
                buttons = isDown ? (byte)(buttons & ~(b & 0xF)) : (byte)(buttons | (b & 0xF));
            }
        }

        public void Update(MMU mmu)
        {
            byte JOYP = mmu.Joypad;
            if (!IsBitSet(4, JOYP))
            {
                mmu.Joypad = (byte)((JOYP & 0xF0) | pad);
                if (pad != 0xF) mmu.RequestInterrupt(JOYPAD_INTERRUPT);
            }
            if (!IsBitSet(5, JOYP))
            {
                mmu.Joypad = (byte)((JOYP & 0xF0) | buttons);
                if (buttons != 0xF) mmu.RequestInterrupt(JOYPAD_INTERRUPT);
            }
            if ((JOYP & 0b00110000) == 0b00110000) mmu.Joypad = 0xFF;
        }
    }
}
