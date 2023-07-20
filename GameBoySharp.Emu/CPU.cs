using GameBoySharp.Emu.Core;
using GameBoySharp.Emu.Utils;
using System.Diagnostics;
using System.Reflection.Emit;
using static GameBoySharp.Emu.Utils.BitOps;

namespace GameBoySharp.Emu
{
    /// <summary>
    /// Central Processing Unit
    /// </summary>
    [DebuggerDisplay("{OpCode,nq} AF:{AF_Str,nq} BC:{BC_Str,nq} DE:{DE_Str,nq} HL:{HL_Str,nq} SP:{SP_Str,nq} PC:{PC_Str,nq}")]
    public class CPU
    {
        long ticksPerSample = Stopwatch.Frequency / APU.SampleRate;

        private Emulator emu;
        private MMU mmu;
        
        #region Registers

        private ushort PC;
        private ushort SP;

        private byte A, B, C, D, E, F, H, L;

        /// <summary>
        /// Register AF
        /// </summary>
        private ushort AF { get { return (ushort)(A << 8 | F); } set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); } }
        
        /// <summary>
        /// Register BC
        /// </summary>
        private ushort BC { get { return (ushort)(B << 8 | C); } set { B = (byte)(value >> 8); C = (byte)value; } }
        
        /// <summary>
        /// Register DE
        /// </summary>
        private ushort DE { get { return (ushort)(D << 8 | E); } set { D = (byte)(value >> 8); E = (byte)value; } }
        
        /// <summary>
        /// Register HL
        /// </summary>
        private ushort HL { get { return (ushort)(H << 8 | L); } set { H = (byte)(value >> 8); L = (byte)value; } }

        /// <summary>
        /// Zero Flag
        /// </summary>
        private bool FlagZ { get { return (F & 0x80) != 0; } set { F = value ? (byte)(F | 0x80) : (byte)(F & ~0x80); } }
        
        /// <summary>
        /// Subtract (negative) Flag
        /// </summary>
        private bool FlagN { get { return (F & 0x40) != 0; } set { F = value ? (byte)(F | 0x40) : (byte)(F & ~0x40); } }
        
        /// <summary>
        /// Half-Carry Flag
        /// </summary>
        private bool FlagH { get { return (F & 0x20) != 0; } set { F = value ? (byte)(F | 0x20) : (byte)(F & ~0x20); } }
        
        /// <summary>
        /// Carry Flag
        /// </summary>
        private bool FlagC { get { return (F & 0x10) != 0; } set { F = value ? (byte)(F | 0x10) : (byte)(F & ~0x10); } }

        #endregion

        /// <summary>
        /// IME - enables or disables all interrupt flags from being acted on. They can still be read/written.
        /// </summary>
        private bool InterruptMasterEnable;

        private bool IMEEnabler;
        private bool HALTED;
        private bool HALT_BUG;

        private int cycles;

        public bool Halted => HALTED;

        private Stopwatch timerWatch = new Stopwatch();

        #region Debug Info

        /// <summary>
        /// The currently running Operation
        /// </summary>
        public CpuOperation OpCode { get; set; }

        public string AF_Str => AF.ToString("X4");
        public string BC_Str => BC.ToString("X4");
        public string DE_Str => DE.ToString("X4");
        public string HL_Str => HL.ToString("X4");
        public string SP_Str => SP.ToString("X4");
        public string PC_Str => PC.ToString("X4");

        public Action? DebugOpCodeHit { get; set; }

        #endregion

        /// <summary>
        /// Create a new instance of the CPU.
        /// </summary>
        /// <param name="mmu"></param>
        public CPU(Emulator emu)
        {
            this.emu = emu;
            this.mmu = emu.MMU;
            Reset();
        }

        /// <summary>
        /// Resets the CPU to initial values.
        /// </summary>
        public void Reset()
        {   
            // set the registers to the value they would have
            // immediately after executing the boot rom
            AF = 0x01B0;
            BC = 0x0013;
            DE = 0x00D8;
            HL = 0x014D;
            SP = 0xFFFE;
            PC = 0x100; // start of loaded game rom
            InterruptMasterEnable = false;
            IMEEnabler = false;
            HALTED = false;
            HALT_BUG = false;
            cycles = 0;
        }

        public void RunCycles()
        {
            int cpuCycles;
            int cyclesThisUpdate = 0;

            if (emu.LockFrameRate) timerWatch.Restart();

            while (cyclesThisUpdate < Constants.CYCLES_PER_UPDATE)
            {
                cpuCycles = RunNextOperation();
                cyclesThisUpdate += cpuCycles;

                emu.Timer.Update(cpuCycles, mmu);
                emu.PPU.Update(cpuCycles, mmu);
                emu.Joypad.Update(mmu);

                if (emu.SoundEnabled) emu.APU.Update(cpuCycles);

                HandleInterrupts();
            }
            if (emu.LockFrameRate)
            {
                while (timerWatch.ElapsedTicks < Constants.TICKS_PER_REFRESH)
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Executes the next operation loaded in register PC.
        /// </summary>
        /// <returns>The number of cycles the operation took.</returns>
        public int RunNextOperation()
        {
            byte opcode = mmu.ReadByte(PC++);
            OpCode = (CpuOperation)opcode;
            if (HALT_BUG)
            {
                PC--;
                HALT_BUG = false;
            }
            //debug(opcode);
            cycles = 0;

            if (opcode != 0xCB)
            {
                Execute8BitOperation(opcode);
                cycles += Cycles.Value[opcode];
            }
            else
            {
                var cbOP = mmu.ReadByte(PC++);

                Execute16BitOperation(cbOP);
                cycles += Cycles.CBValue[cbOP];
                cycles += Cycles.Value[opcode];
            }

            return cycles;
        }

        /// <summary>
        /// Runs an 8-bit operation
        /// </summary>
        /// <param name="opcode"></param>
        public void Execute8BitOperation(byte opcode)
        {
            switch (opcode)
            {
                #region 00-0F
                case 0x00:                                   break; //NOP        1 4     ----
                case 0x01: BC = mmu.ReadWord(PC); PC += 2;   break; //LD BC,D16  3 12    ----
                case 0x02: mmu.WriteByte(BC, A);             break; //LD (BC),A  1 8     ----
                case 0x03: BC += 1;                          break; //INC BC     1 8     ----
                case 0x04: B = Increment(B);                 break; //INC B      1 4     Z0H-
                case 0x05: B = Decrement(B);                 break; //DEC B      1 4     Z1H-
                case 0x06: B = mmu.ReadByte(PC); PC += 1;    break; //LD B,D8    2 8     ----

                case 0x07: //RLCA 1 4 000C
                    F = 0;
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (A >> 7));
                    break;

                case 0x08: mmu.WriteWord(mmu.ReadWord(PC), SP); PC += 2; break; //LD (A16),SP 3 20   ----
                case 0x09: DAD(BC);                          break; //ADD HL,BC   1 8    -0HC
                case 0x0A: A = mmu.ReadByte(BC);             break; //LD A,(BC)   1 8    ----
                case 0x0B: BC -= 1;                          break; //DEC BC      1 8    ----
                case 0x0C: C = Increment(C);                 break; //INC C       1 8    Z0H-
                case 0x0D: C = Decrement(C);                 break; //DEC C       1 8    Z1H-
                case 0x0E: C = mmu.ReadByte(PC); PC += 1;    break; //LD C,D8     2 8    ----

                case 0x0F: //RRCA 1 4 000C
                    F = 0;
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (A << 7));
                    break;
                #endregion

                #region 10-1F
                case 0x10: STOP();                          break; //STOP        2 4    ----
                case 0x11: DE = mmu.ReadWord(PC); PC += 2;  break; //LD DE,D16   3 12   ----
                case 0x12: mmu.WriteByte(DE, A);            break; //LD (DE),A   1 8    ----
                case 0x13: DE += 1;                         break; //INC DE      1 8    ----
                case 0x14: D = Increment(D);                break; //INC D       1 8    Z0H-
                case 0x15: D = Decrement(D);                break; //DEC D       1 8    Z1H-
                case 0x16: D = mmu.ReadByte(PC); PC += 1;   break; //LD D,D8     2 8    ----

                case 0x17://RLA 1 4 000C
                    bool prevC = FlagC;
                    F = 0;
                    FlagC = ((A & 0x80) != 0);
                    A = (byte)((A << 1) | (prevC ? 1 : 0));
                    break;

                case 0x18: JumpRelative(true);              break; //JR R8       2 12   ----
                case 0x19: DAD(DE);                         break; //ADD HL,DE   1 8    -0HC
                case 0x1A: A = mmu.ReadByte(DE);            break; //LD A,(DE)   1 8    ----
                case 0x1B: DE -= 1;                         break; //DEC DE      1 8    ----
                case 0x1C: E = Increment(E);                break; //INC E       1 8    Z0H-
                case 0x1D: E = Decrement(E);                break; //DEC E       1 8    Z1H-
                case 0x1E: E = mmu.ReadByte(PC); PC += 1;   break; //LD E,D8     2 8    ----

                case 0x1F://RRA 1 4 000C
                    bool preC = FlagC;
                    F = 0;
                    FlagC = ((A & 0x1) != 0);
                    A = (byte)((A >> 1) | (preC ? 0x80 : 0));
                    break;
                #endregion

                #region 20-2F
                case 0x20: JumpRelative(!FlagZ);                break; //JR NZ R8    2 12/8 ---- 
                case 0x21: HL = mmu.ReadWord(PC); PC += 2;      break; //LD HL,D16   3 12   ----
                case 0x22: mmu.WriteByte(HL++, A);              break; //LD (HL+),A  1 8    ----
                case 0x23: HL += 1;                             break; //INC HL      1 8    ----
                case 0x24: H = Increment(H);                    break; //INC H       1 8    Z0H-
                case 0x25: H = Decrement(H);                    break; //DEC H       1 8    Z1H-
                case 0x26: H = mmu.ReadByte(PC); PC += 1; ;     break; //LD H,D8     2 8    ----

                case 0x27: //DAA    1 4 Z-0C
                    if (FlagN)
                    { // sub
                        if (FlagC) { A -= 0x60; }
                        if (FlagH) { A -= 0x6; }
                    }
                    else
                    { // add
                        if (FlagC || (A > 0x99)) { A += 0x60; FlagC = true; }
                        if (FlagH || (A & 0xF) > 0x9) { A += 0x6; }
                    }
                    SetFlagZ(A);
                    FlagH = false;
                    break;

                case 0x28: JumpRelative(FlagZ);                 break; //JR Z R8    2 12/8  ----
                case 0x29: DAD(HL);                             break; //ADD HL,HL  1 8     -0HC
                case 0x2A: A = mmu.ReadByte(HL++);              break; //LD A (HL+) 1 8     ----
                case 0x2B: HL -= 1;                             break; //DEC HL     1 4     ----
                case 0x2C: L = Increment(L);                    break; //INC L      1 4     Z0H-
                case 0x2D: L = Decrement(L);                    break; //DEC L      1 4     Z1H-
                case 0x2E: L = mmu.ReadByte(PC); PC += 1; ;     break; //LD L,D8    2 8     ----
                case 0x2F: A = (byte)~A; FlagN = true; FlagH = true; break; //CPL	       1 4     -11-
                #endregion

                #region 30-3F
                case 0x30: JumpRelative(!FlagC);                            break; //JR NC R8   2 12/8  ----
                case 0x31: SP = mmu.ReadWord(PC); PC += 2; ;                break; //LD SP,D16  3 12    ----
                case 0x32: mmu.WriteByte(HL--, A);                          break; //LD (HL-),A 1 8     ----
                case 0x33: SP += 1;                                         break; //INC SP     1 8     ----
                case 0x34: mmu.WriteByte(HL, Increment(mmu.ReadByte(HL)));  break; //INC (HL)   1 12    Z0H-
                case 0x35: mmu.WriteByte(HL, Decrement(mmu.ReadByte(HL)));  break; //DEC (HL)   1 12    Z1H-
                case 0x36: mmu.WriteByte(HL, mmu.ReadByte(PC)); PC += 1;    break; //LD (HL),D8 2 12    ----
                case 0x37: FlagC = true; FlagN = false; FlagH = false;      break; //SCF	       1 4     -001

                case 0x38: JumpRelative(FlagC);                             break; //JR C R8    2 12/8  ----
                case 0x39: DAD(SP);                                         break; //ADD HL,SP  1 8     -0HC
                case 0x3A: A = mmu.ReadByte(HL--);                          break; //LD A (HL-) 1 8     ----
                case 0x3B: SP -= 1;                                         break; //DEC SP     1 8     ----
                case 0x3C: A = Increment(A);                                break; //INC A      1 4     Z0H-
                case 0x3D: A = Decrement(A);                                break; //DEC (HL)   1 4     Z1H-
                case 0x3E: A = mmu.ReadByte(PC); PC += 1;                   break; //LD A,D8    2 8     ----
                case 0x3F: FlagC = !FlagC; FlagN = false; FlagH = false;    break; //CCF        1 4     -00C
                #endregion

                #region 40-4F
                case 0x40: DebugOpCodeHit?.Invoke(); /*B = B;*/ break; //LD B,B	    1 4    ----
                case 0x41: B = C;                   break; //LD B,C	    1 4	   ----
                case 0x42: B = D;                   break; //LD B,D	    1 4	   ----
                case 0x43: B = E;                   break; //LD B,E	    1 4	   ----
                case 0x44: B = H;                   break; //LD B,H	    1 4	   ----
                case 0x45: B = L;                   break; //LD B,L	    1 4	   ----
                case 0x46: B = mmu.ReadByte(HL);    break; //LD B,(HL)	1 8	   ----
                case 0x47: B = A;                   break; //LD B,A	    1 4	   ----

                case 0x48: C = B;                   break; //LD C,B	    1 4    ----
                case 0x49: /*C = C;*/               break; //LD C,C	    1 4    ----
                case 0x4A: C = D;                   break; //LD C,D   	1 4    ----
                case 0x4B: C = E;                   break; //LD C,E   	1 4    ----
                case 0x4C: C = H;                   break; //LD C,H   	1 4    ----
                case 0x4D: C = L;                   break; //LD C,L   	1 4    ----
                case 0x4E: C = mmu.ReadByte(HL);    break; //LD C,(HL)	1 8    ----
                case 0x4F: C = A;                   break; //LD C,A   	1 4    ----
                #endregion

                #region 50-5f
                case 0x50: D = B;               break; //LD D,B	    1 4    ----
                case 0x51: D = C;               break; //LD D,C	    1 4    ----
                case 0x52: /*D = D;*/           break; //LD D,D	    1 4    ----
                case 0x53: D = E;               break; //LD D,E	    1 4    ----
                case 0x54: D = H;               break; //LD D,H	    1 4    ----
                case 0x55: D = L;               break; //LD D,L	    1 4    ----
                case 0x56: D = mmu.ReadByte(HL); break; //LD D,(HL)    1 8    ---- 
                case 0x57: D = A;               break; //LD D,A	    1 4    ----

                case 0x58: E = B;               break; //LD E,B   	1 4    ----
                case 0x59: E = C;               break; //LD E,C   	1 4    ----
                case 0x5A: E = D;               break; //LD E,D   	1 4    ----
                case 0x5B: /*E = E;*/           break; //LD E,E   	1 4    ----
                case 0x5C: E = H;               break; //LD E,H   	1 4    ----
                case 0x5D: E = L;               break; //LD E,L   	1 4    ----
                case 0x5E: E = mmu.ReadByte(HL); break; //LD E,(HL)    1 8    ----
                case 0x5F: E = A;               break; //LD E,A	    1 4    ----
                #endregion

                #region 60-6F
                case 0x60: H = B;               break; //LD H,B   	1 4    ----
                case 0x61: H = C;               break; //LD H,C   	1 4    ----
                case 0x62: H = D;               break; //LD H,D   	1 4    ----
                case 0x63: H = E;               break; //LD H,E   	1 4    ----
                case 0x64: /*H = H;*/           break; //LD H,H   	1 4    ----
                case 0x65: H = L;               break; //LD H,L   	1 4    ----
                case 0x66: H = mmu.ReadByte(HL); break; //LD H,(HL)    1 8    ----
                case 0x67: H = A;               break; //LD H,A	    1 4    ----

                case 0x68: L = B;               break; //LD L,B   	1 4    ----
                case 0x69: L = C;               break; //LD L,C   	1 4    ----
                case 0x6A: L = D;               break; //LD L,D   	1 4    ----
                case 0x6B: L = E;               break; //LD L,E   	1 4    ----
                case 0x6C: L = H;               break; //LD L,H   	1 4    ----
                case 0x6D: /*L = L;*/           break; //LD L,L	    1 4    ----
                case 0x6E: L = mmu.ReadByte(HL); break; //LD L,(HL)	1 8    ----
                case 0x6F: L = A;               break; //LD L,A	    1 4    ----
                #endregion

                #region 70-7F
                case 0x70: mmu.WriteByte(HL, B);    break; //LD (HL),B	1 8    ----
                case 0x71: mmu.WriteByte(HL, C);    break; //LD (HL),C	1 8	   ----
                case 0x72: mmu.WriteByte(HL, D);    break; //LD (HL),D	1 8	   ----
                case 0x73: mmu.WriteByte(HL, E);    break; //LD (HL),E	1 8	   ----
                case 0x74: mmu.WriteByte(HL, H);    break; //LD (HL),H	1 8	   ----
                case 0x75: mmu.WriteByte(HL, L);    break; //LD (HL),L	1 8	   ----
                case 0x76: Halt();                  break; //HLT	        1 4    ----
                case 0x77: mmu.WriteByte(HL, A);    break; //LD (HL),A	1 8    ----

                case 0x78: A = B;                   break; //LD A,B	    1 4    ----
                case 0x79: A = C;                   break; //LD A,C	    1 4	   ----
                case 0x7A: A = D;                   break; //LD A,D	    1 4	   ----
                case 0x7B: A = E;                   break; //LD A,E	    1 4	   ----
                case 0x7C: A = H;                   break; //LD A,H	    1 4	   ----
                case 0x7D: A = L;                   break; //LD A,L	    1 4	   ----
                case 0x7E: A = mmu.ReadByte(HL);    break; //LD A,(HL)    1 8    ----
                case 0x7F: /*A = A;*/               break; //LD A,A	    1 4    ----
                #endregion

                #region 80-8F
                case 0x80: Add(B); break;               //ADD B	    1 4    Z0HC	
                case 0x81: Add(C); break;               //ADD C	    1 4    Z0HC	
                case 0x82: Add(D); break;               //ADD D	    1 4    Z0HC	
                case 0x83: Add(E); break;               //ADD E	    1 4    Z0HC	
                case 0x84: Add(H); break;               //ADD H	    1 4    Z0HC	
                case 0x85: Add(L); break;               //ADD L	    1 4    Z0HC	
                case 0x86: Add(mmu.ReadByte(HL)); break; //ADD M	    1 8    Z0HC	
                case 0x87: Add(A); break;               //ADD A	    1 4    Z0HC	

                case 0x88: AddWithCarry(B); break;      //ADC B	    1 4    Z0HC	
                case 0x89: AddWithCarry(C); break;      //ADC C	    1 4    Z0HC	
                case 0x8A: AddWithCarry(D); break;      //ADC D	    1 4    Z0HC	
                case 0x8B: AddWithCarry(E); break;      //ADC E	    1 4    Z0HC	
                case 0x8C: AddWithCarry(H); break;      //ADC H	    1 4    Z0HC	
                case 0x8D: AddWithCarry(L); break;      //ADC L	    1 4    Z0HC	
                case 0x8E: AddWithCarry(mmu.ReadByte(HL)); break; //ADC M	    1 8    Z0HC	
                case 0x8F: AddWithCarry(A); break;      //ADC A	    1 4    Z0HC	
                #endregion

                #region 90-9F
                case 0x90: Subtract(B); break;                  //SUB B	    1 4    Z1HC
                case 0x91: Subtract(C); break;                  //SUB C	    1 4    Z1HC
                case 0x92: Subtract(D); break;                  //SUB D	    1 4    Z1HC
                case 0x93: Subtract(E); break;                  //SUB E	    1 4    Z1HC
                case 0x94: Subtract(H); break;                  //SUB H	    1 4    Z1HC
                case 0x95: Subtract(L); break;                  //SUB L	    1 4    Z1HC
                case 0x96: Subtract(mmu.ReadByte(HL)); break;   //SUB M	    1 8    Z1HC
                case 0x97: Subtract(A); break;                  //SUB A	    1 4    Z1HC

                case 0x98: SubtractWithCarry(B); break;         //SBC B	    1 4    Z1HC
                case 0x99: SubtractWithCarry(C); break;         //SBC C	    1 4    Z1HC
                case 0x9A: SubtractWithCarry(D); break;         //SBC D	    1 4    Z1HC
                case 0x9B: SubtractWithCarry(E); break;         //SBC E	    1 4    Z1HC
                case 0x9C: SubtractWithCarry(H); break;         //SBC H	    1 4    Z1HC
                case 0x9D: SubtractWithCarry(L); break;         //SBC L	    1 4    Z1HC
                case 0x9E: SubtractWithCarry(mmu.ReadByte(HL)); break; //SBC M	    1 8    Z1HC
                case 0x9F: SubtractWithCarry(A); break;         //SBC A	    1 4    Z1HC
                #endregion

                #region A0-AF
                case 0xA0: And(B); break;               //AND B	    1 4    Z010
                case 0xA1: And(C); break;               //AND C	    1 4    Z010
                case 0xA2: And(D); break;               //AND D	    1 4    Z010
                case 0xA3: And(E); break;               //AND E	    1 4    Z010
                case 0xA4: And(H); break;               //AND H	    1 4    Z010
                case 0xA5: And(L); break;               //AND L	    1 4    Z010
                case 0xA6: And(mmu.ReadByte(HL)); break; //AND M	    1 8    Z010
                case 0xA7: And(A); break;               //AND A	    1 4    Z010

                case 0xA8: Xor(B); break;               //XOR B	    1 4    Z000
                case 0xA9: Xor(C); break;               //XOR C	    1 4    Z000
                case 0xAA: Xor(D); break;               //XOR D	    1 4    Z000
                case 0xAB: Xor(E); break;               //XOR E	    1 4    Z000
                case 0xAC: Xor(H); break;               //XOR H	    1 4    Z000
                case 0xAD: Xor(L); break;               //XOR L	    1 4    Z000
                case 0xAE: Xor(mmu.ReadByte(HL)); break; //XOR M	    1 8    Z000
                case 0xAF: Xor(A); break;               //XOR A	    1 4    Z000
                #endregion

                #region B0-BF
                case 0xB0: Or(B); break;                //OR B     	1 4    Z000
                case 0xB1: Or(C); break;                //OR C     	1 4    Z000
                case 0xB2: Or(D); break;                //OR D     	1 4    Z000
                case 0xB3: Or(E); break;                //OR E     	1 4    Z000
                case 0xB4: Or(H); break;                //OR H     	1 4    Z000
                case 0xB5: Or(L); break;                //OR L     	1 4    Z000
                case 0xB6: Or(mmu.ReadByte(HL)); break; //OR M     	1 8    Z000
                case 0xB7: Or(A); break;                //OR A     	1 4    Z000

                case 0xB8: CP(B); break;                //CP B     	1 4    Z1HC
                case 0xB9: CP(C); break;                //CP C     	1 4    Z1HC
                case 0xBA: CP(D); break;                //CP D     	1 4    Z1HC
                case 0xBB: CP(E); break;                //CP E     	1 4    Z1HC
                case 0xBC: CP(H); break;                //CP H     	1 4    Z1HC
                case 0xBD: CP(L); break;                //CP L     	1 4    Z1HC
                case 0xBE: CP(mmu.ReadByte(HL)); break; //CP M     	1 8    Z1HC
                case 0xBF: CP(A); break;                //CP A     	1 4    Z1HC
                #endregion

                #region C0-CF
                case 0xC0: ReturnFromSubroutine(!FlagZ); break; //RET NZ	     1 20/8  ----
                case 0xC1: BC = PopStack(); break;              //POP BC      1 12    ----
                case 0xC2: Jump(!FlagZ); break;                 //JP NZ,A16   3 16/12 ----
                case 0xC3: Jump(true); break;                   //JP A16      3 16    ----
                case 0xC4: CallSubroutine(!FlagZ); break;       //CALL NZ A16 3 24/12 ----
                case 0xC5: PushStack(BC); break;                //PUSH BC     1 16    ----
                case 0xC6: Add(mmu.ReadByte(PC)); PC += 1; break; //ADD A,D8    2 8     Z0HC
                case 0xC7: RestartAt(0x0); break;               //RST 0       1 16    ----

                case 0xC8: ReturnFromSubroutine(FlagZ); break;  //RET Z       1 20/8  ----
                case 0xC9: ReturnFromSubroutine(true); break;   //RET         1 16    ----
                case 0xCA: Jump(FlagZ); break;                  //JP Z,A16    3 16/12 ----
                case 0xCB: Execute16BitOperation(mmu.ReadByte(PC++)); break; //PREFIX CB OPCODE TABLE
                case 0xCC: CallSubroutine(FlagZ); break;        //CALL Z,A16  3 24/12 ----
                case 0xCD: CallSubroutine(true); break;         //CALL A16    3 24    ----
                case 0xCE: AddWithCarry(mmu.ReadByte(PC)); PC += 1; break; //ADC A,D8    2 8     ----
                case 0xCF: RestartAt(0x8); break;               //RST 1 08    1 16    ----
                #endregion

                #region D0-DF
                case 0xD0: ReturnFromSubroutine(!FlagC); break;         //RET NC      1 20/8  ----
                case 0xD1: DE = PopStack(); break;                      //POP DE      1 12    ----
                case 0xD2: Jump(!FlagC); break;                         //JP NC,A16   3 16/12 ----
                //case 0xD3: break;                                     //Illegal Opcode
                case 0xD4: CallSubroutine(!FlagC); break;               //CALL NC,A16 3 24/12 ----
                case 0xD5: PushStack(DE); break;                        //PUSH DE     1 16    ----
                case 0xD6: Subtract(mmu.ReadByte(PC)); PC += 1; break;  //SUB D8      2 8     ----
                case 0xD7: RestartAt(0x10); break;                      //RST 2 10    1 16    ----

                case 0xD8: ReturnFromSubroutine(FlagC); break;          //RET C       1 20/8  ----
                case 0xD9: ReturnFromSubroutine(true); InterruptMasterEnable = true; break; //RETI        1 16    ----
                case 0xDA: Jump(FlagC); break;                          //JP C,A16    3 16/12 ----
                //case 0xDB: break;                                     //Illegal Opcode
                case 0xDC: CallSubroutine(FlagC); break;                //Call C,A16  3 24/12 ----
                //case 0xDD: break;                                     //Illegal Opcode
                case 0xDE: SubtractWithCarry(mmu.ReadByte(PC)); PC += 1; break; //SBC A,A8    2 8     Z1HC
                case 0xDF: RestartAt(0x18); break;                      //RST 3 18    1 16    ----
                #endregion

                #region E0-EF
                case 0xE0: mmu.WriteByte((ushort)(0xFF00 + mmu.ReadByte(PC)), A); PC += 1; break; //LDH (A8),A 2 12 ----
                case 0xE1: HL = PopStack(); break;                          //POP HL      1 12    ----
                case 0xE2: mmu.WriteByte((ushort)(0xFF00 + C), A); break;   //LD (C),A   1 8  ----
                //case 0xE3: break;                                         //Illegal Opcode
                //case 0xE4: break;                                         //Illegal Opcode
                case 0xE5: PushStack(HL); break;                            //PUSH HL     1 16    ----
                case 0xE6: And(mmu.ReadByte(PC)); PC += 1; break;           //AND D8      2 8     Z010
                case 0xE7: RestartAt(0x20); break;                          //RST 4 20    1 16    ----

                case 0xE8: SP = DADr8(SP); break;                           //ADD SP,R8   2 16    00HC
                case 0xE9: PC = HL; break;                                  //JP (HL)     1 4     ----
                case 0xEA: mmu.WriteByte(mmu.ReadWord(PC), A); PC += 2; break; //LD (A16),A 3 16 ----
                //case 0xEB: break;                                         //Illegal Opcode
                //case 0xEC: break;                                         //Illegal Opcode
                //case 0xED: break;                                         //Illegal Opcode
                case 0xEE: Xor(mmu.ReadByte(PC)); PC += 1; break;           //XOR D8      2 8     Z000
                case 0xEF: RestartAt(0x28); break;                          //RST 5 28    1 16    ----
                #endregion

                #region F0-FF
                case 0xF0: A = mmu.ReadByte((ushort)(0xFF00 + mmu.ReadByte(PC))); PC += 1; break; //LDH A,(A8)  2 12    ----
                case 0xF1: AF = PopStack(); break;                          //POP AF      1 12    ZNHC
                case 0xF2: A = mmu.ReadByte((ushort)(0xFF00 + C)); break;   //LD A,(C)    1 8     ----
                case 0xF3: InterruptMasterEnable = false; break;            //DI          1 4     ----
                //case 0xF4: break;                                         //Illegal Opcode
                case 0xF5: PushStack(AF); break;                            //PUSH AF     1 16    ----
                case 0xF6: Or(mmu.ReadByte(PC)); PC += 1; break;            //OR D8       2 8     Z000
                case 0xF7: RestartAt(0x30); break;                          //RST 6 30    1 16    ----

                case 0xF8: HL = DADr8(SP); break;                           //LD HL,SP+R8 2 12    00HC
                case 0xF9: SP = HL; break;                                  //LD SP,HL    1 8     ----
                case 0xFA: A = mmu.ReadByte(mmu.ReadWord(PC)); PC += 2; break; //LD A,(A16)  3 16    ----
                case 0xFB: IMEEnabler = true; break;                        //IE          1 4     ----
                //case 0xFC: break;                                         //Illegal Opcode
                //case 0xFD: break;                                         //Illegal Opcode
                case 0xFE: CP(mmu.ReadByte(PC)); PC += 1; break;            //CP D8       2 8     Z1HC
                case 0xFF: RestartAt(0x38); break;                          //RST 7 38    1 16    ----
                #endregion

                default: WarnUnsupportedOpcode(opcode); break;
            }
        }

        /// <summary>
        /// Runs a 16-bit operation (prefixed with OP Code 0xCB)
        /// </summary>
        /// <param name="opcode">The operation code to run.</param>
        public void Execute16BitOperation(byte opcode)
        {
            switch (opcode)
            {
                #region 00-0F
                case 0x00: B = RotateLeftCircular(B); break;                //RLC B    2   8   Z00C
                case 0x01: C = RotateLeftCircular(C); break;                //RLC C    2   8   Z00C
                case 0x02: D = RotateLeftCircular(D); break;                //RLC D    2   8   Z00C
                case 0x03: E = RotateLeftCircular(E); break;                //RLC E    2   8   Z00C
                case 0x04: H = RotateLeftCircular(H); break;                //RLC H    2   8   Z00C
                case 0x05: L = RotateLeftCircular(L); break;                //RLC L    2   8   Z00C
                case 0x06: mmu.WriteByte(HL, RotateLeftCircular(mmu.ReadByte(HL))); break; //RLC (HL) 2   8   Z00C
                case 0x07: A = RotateLeftCircular(A); break;                //RLC B    2   8   Z00C
                
                case 0x08: B = RotateRightCircular(B); break;               //RRC B    2   8   Z00C
                case 0x09: C = RotateRightCircular(C); break;               //RRC C    2   8   Z00C
                case 0x0A: D = RotateRightCircular(D); break;               //RRC D    2   8   Z00C
                case 0x0B: E = RotateRightCircular(E); break;               //RRC E    2   8   Z00C
                case 0x0C: H = RotateRightCircular(H); break;               //RRC H    2   8   Z00C
                case 0x0D: L = RotateRightCircular(L); break;               //RRC L    2   8   Z00C
                case 0x0E: mmu.WriteByte(HL, RotateRightCircular(mmu.ReadByte(HL))); break; //RRC (HL) 2   8   Z00C
                case 0x0F: A = RotateRightCircular(A); break;               //RRC B    2   8   Z00C
                #endregion

                #region 10-1F
                case 0x10: B = RotateLeft(B); break; //RL B     2   8   Z00C
                case 0x11: C = RotateLeft(C); break; //RL C     2   8   Z00C
                case 0x12: D = RotateLeft(D); break; //RL D     2   8   Z00C
                case 0x13: E = RotateLeft(E); break; //RL E     2   8   Z00C
                case 0x14: H = RotateLeft(H); break; //RL H     2   8   Z00C
                case 0x15: L = RotateLeft(L); break; //RL L     2   8   Z00C
                case 0x16: mmu.WriteByte(HL, RotateLeft(mmu.ReadByte(HL))); break; //RL (HL)  2   8   Z00C
                case 0x17: A = RotateLeft(A); break; //RL B     2   8   Z00C
                
                case 0x18: B = RotateRight(B); break; //RR B     2   8   Z00C
                case 0x19: C = RotateRight(C); break; //RR C     2   8   Z00C
                case 0x1A: D = RotateRight(D); break; //RR D     2   8   Z00C
                case 0x1B: E = RotateRight(E); break; //RR E     2   8   Z00C
                case 0x1C: H = RotateRight(H); break; //RR H     2   8   Z00C
                case 0x1D: L = RotateRight(L); break; //RR L     2   8   Z00C
                case 0x1E: mmu.WriteByte(HL, RotateRight(mmu.ReadByte(HL))); break; //RR (HL)  2   8   Z00C
                case 0x1F: A = RotateRight(A); break; //RR B     2   8   Z00C
                #endregion

                #region 20-2F
                case 0x20: B = ShiftLeftArithmetic(B); break; //SLA B    2   8   Z00C
                case 0x21: C = ShiftLeftArithmetic(C); break; //SLA C    2   8   Z00C
                case 0x22: D = ShiftLeftArithmetic(D); break; //SLA D    2   8   Z00C
                case 0x23: E = ShiftLeftArithmetic(E); break; //SLA E    2   8   Z00C
                case 0x24: H = ShiftLeftArithmetic(H); break; //SLA H    2   8   Z00C
                case 0x25: L = ShiftLeftArithmetic(L); break; //SLA L    2   8   Z00C
                case 0x26: mmu.WriteByte(HL, ShiftLeftArithmetic(mmu.ReadByte(HL))); break; //SLA (HL) 2   8   Z00C
                case 0x27: A = ShiftLeftArithmetic(A); break; //SLA B    2   8   Z00C
                
                case 0x28: B = ShiftRightArithmetic(B); break; //SRA B    2   8   Z00C
                case 0x29: C = ShiftRightArithmetic(C); break; //SRA C    2   8   Z00C
                case 0x2A: D = ShiftRightArithmetic(D); break; //SRA D    2   8   Z00C
                case 0x2B: E = ShiftRightArithmetic(E); break; //SRA E    2   8   Z00C
                case 0x2C: H = ShiftRightArithmetic(H); break; //SRA H    2   8   Z00C
                case 0x2D: L = ShiftRightArithmetic(L); break; //SRA L    2   8   Z00C
                case 0x2E: mmu.WriteByte(HL, ShiftRightArithmetic(mmu.ReadByte(HL))); break; //SRA (HL) 2   8   Z00C
                case 0x2F: A = ShiftRightArithmetic(A); break; //SRA B    2   8   Z00C
                #endregion

                #region 30-3F
                case 0x30: B = SwapNibbles(B); break; //SWAP B    2   8   Z00C
                case 0x31: C = SwapNibbles(C); break; //SWAP C    2   8   Z00C
                case 0x32: D = SwapNibbles(D); break; //SWAP D    2   8   Z00C
                case 0x33: E = SwapNibbles(E); break; //SWAP E    2   8   Z00C
                case 0x34: H = SwapNibbles(H); break; //SWAP H    2   8   Z00C
                case 0x35: L = SwapNibbles(L); break; //SWAP L    2   8   Z00C
                case 0x36: mmu.WriteByte(HL, SwapNibbles(mmu.ReadByte(HL))); break; //SWAP (HL) 2   8   Z00C
                case 0x37: A = SwapNibbles(A); break; //SWAP B    2   8   Z00C
                
                case 0x38: B = ShiftRightLogical(B); break; //SRL B    2   8   Z000
                case 0x39: C = ShiftRightLogical(C); break; //SRL C    2   8   Z000
                case 0x3A: D = ShiftRightLogical(D); break; //SRL D    2   8   Z000
                case 0x3B: E = ShiftRightLogical(E); break; //SRL E    2   8   Z000
                case 0x3C: H = ShiftRightLogical(H); break; //SRL H    2   8   Z000
                case 0x3D: L = ShiftRightLogical(L); break; //SRL L    2   8   Z000
                case 0x3E: mmu.WriteByte(HL, ShiftRightLogical(mmu.ReadByte(HL))); break; //SRL (HL) 2   8   Z000
                case 0x3F: A = ShiftRightLogical(A); break; //SRL B    2   8   Z000
                #endregion

                #region 40-4F
                case 0x40: TestBit(0x1, B); break; //BIT B    2   8   Z01-
                case 0x41: TestBit(0x1, C); break; //BIT C    2   8   Z01-
                case 0x42: TestBit(0x1, D); break; //BIT D    2   8   Z01-
                case 0x43: TestBit(0x1, E); break; //BIT E    2   8   Z01-
                case 0x44: TestBit(0x1, H); break; //BIT H    2   8   Z01-
                case 0x45: TestBit(0x1, L); break; //BIT L    2   8   Z01-
                case 0x46: TestBit(0x1, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x47: TestBit(0x1, A); break; //BIT B    2   8   Z01-
                case 0x48: TestBit(0x2, B); break; //BIT B    2   8   Z01-
                case 0x49: TestBit(0x2, C); break; //BIT C    2   8   Z01-
                case 0x4A: TestBit(0x2, D); break; //BIT D    2   8   Z01-
                case 0x4B: TestBit(0x2, E); break; //BIT E    2   8   Z01-
                case 0x4C: TestBit(0x2, H); break; //BIT H    2   8   Z01-
                case 0x4D: TestBit(0x2, L); break; //BIT L    2   8   Z01-
                case 0x4E: TestBit(0x2, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x4F: TestBit(0x2, A); break; //BIT B    2   8   Z01-
                #endregion

                #region 50-5F
                case 0x50: TestBit(0x4, B); break; //BIT B    2   8   Z01-
                case 0x51: TestBit(0x4, C); break; //BIT C    2   8   Z01-
                case 0x52: TestBit(0x4, D); break; //BIT D    2   8   Z01-
                case 0x53: TestBit(0x4, E); break; //BIT E    2   8   Z01-
                case 0x54: TestBit(0x4, H); break; //BIT H    2   8   Z01-
                case 0x55: TestBit(0x4, L); break; //BIT L    2   8   Z01-
                case 0x56: TestBit(0x4, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x57: TestBit(0x4, A); break; //BIT B    2   8   Z01-
                case 0x58: TestBit(0x8, B); break; //BIT B    2   8   Z01-
                case 0x59: TestBit(0x8, C); break; //BIT C    2   8   Z01-
                case 0x5A: TestBit(0x8, D); break; //BIT D    2   8   Z01-
                case 0x5B: TestBit(0x8, E); break; //BIT E    2   8   Z01-
                case 0x5C: TestBit(0x8, H); break; //BIT H    2   8   Z01-
                case 0x5D: TestBit(0x8, L); break; //BIT L    2   8   Z01-
                case 0x5E: TestBit(0x8, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x5F: TestBit(0x8, A); break; //BIT B    2   8   Z01-
                #endregion

                #region 60-6F
                case 0x60: TestBit(0x10, B); break; //BIT B    2   8   Z01-
                case 0x61: TestBit(0x10, C); break; //BIT C    2   8   Z01-
                case 0x62: TestBit(0x10, D); break; //BIT D    2   8   Z01-
                case 0x63: TestBit(0x10, E); break; //BIT E    2   8   Z01-
                case 0x64: TestBit(0x10, H); break; //BIT H    2   8   Z01-
                case 0x65: TestBit(0x10, L); break; //BIT L    2   8   Z01-
                case 0x66: TestBit(0x10, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x67: TestBit(0x10, A); break; //BIT B    2   8   Z01-

                case 0x68: TestBit(0x20, B); break; //BIT B    2   8   Z01-
                case 0x69: TestBit(0x20, C); break; //BIT C    2   8   Z01-
                case 0x6A: TestBit(0x20, D); break; //BIT D    2   8   Z01-
                case 0x6B: TestBit(0x20, E); break; //BIT E    2   8   Z01-
                case 0x6C: TestBit(0x20, H); break; //BIT H    2   8   Z01-
                case 0x6D: TestBit(0x20, L); break; //BIT L    2   8   Z01-
                case 0x6E: TestBit(0x20, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x6F: TestBit(0x20, A); break; //BIT B    2   8   Z01-
                #endregion

                #region 70-7F
                case 0x70: TestBit(0x40, B); break; //BIT B    2   8   Z01-
                case 0x71: TestBit(0x40, C); break; //BIT C    2   8   Z01-
                case 0x72: TestBit(0x40, D); break; //BIT D    2   8   Z01-
                case 0x73: TestBit(0x40, E); break; //BIT E    2   8   Z01-
                case 0x74: TestBit(0x40, H); break; //BIT H    2   8   Z01-
                case 0x75: TestBit(0x40, L); break; //BIT L    2   8   Z01-
                case 0x76: TestBit(0x40, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x77: TestBit(0x40, A); break; //BIT B    2   8   Z01-
                case 0x78: TestBit(0x80, B); break; //BIT B    2   8   Z01-
                case 0x79: TestBit(0x80, C); break; //BIT C    2   8   Z01-
                case 0x7A: TestBit(0x80, D); break; //BIT D    2   8   Z01-
                case 0x7B: TestBit(0x80, E); break; //BIT E    2   8   Z01-
                case 0x7C: TestBit(0x80, H); break; //BIT H    2   8   Z01-
                case 0x7D: TestBit(0x80, L); break; //BIT L    2   8   Z01-
                case 0x7E: TestBit(0x80, mmu.ReadByte(HL)); break; //BIT (HL) 2   8   Z01-
                case 0x7F: TestBit(0x80, A); break; //BIT B    2   8   Z01-
                #endregion

                #region 80-8F
                case 0x80: B = ResetBit(0x1, B); break; //RES B    2   8   ----
                case 0x81: C = ResetBit(0x1, C); break; //RES C    2   8   ----
                case 0x82: D = ResetBit(0x1, D); break; //RES D    2   8   ----
                case 0x83: E = ResetBit(0x1, E); break; //RES E    2   8   ----
                case 0x84: H = ResetBit(0x1, H); break; //RES H    2   8   ----
                case 0x85: L = ResetBit(0x1, L); break; //RES L    2   8   ----
                case 0x86: mmu.WriteByte(HL, ResetBit(0x1, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0x87: A = ResetBit(0x1, A); break; //RES B    2   8   ----
                case 0x88: B = ResetBit(0x2, B); break; //RES B    2   8   ----
                case 0x89: C = ResetBit(0x2, C); break; //RES C    2   8   ----
                case 0x8A: D = ResetBit(0x2, D); break; //RES D    2   8   ----
                case 0x8B: E = ResetBit(0x2, E); break; //RES E    2   8   ----
                case 0x8C: H = ResetBit(0x2, H); break; //RES H    2   8   ----
                case 0x8D: L = ResetBit(0x2, L); break; //RES L    2   8   ----
                case 0x8E: mmu.WriteByte(HL, ResetBit(0x2, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0x8F: A = ResetBit(0x2, A); break; //RES B    2   8   ----
                #endregion

                #region 90-9F
                case 0x90: B = ResetBit(0x4, B); break; //RES B    2   8   ----
                case 0x91: C = ResetBit(0x4, C); break; //RES C    2   8   ----
                case 0x92: D = ResetBit(0x4, D); break; //RES D    2   8   ----
                case 0x93: E = ResetBit(0x4, E); break; //RES E    2   8   ----
                case 0x94: H = ResetBit(0x4, H); break; //RES H    2   8   ----
                case 0x95: L = ResetBit(0x4, L); break; //RES L    2   8   ----
                case 0x96: mmu.WriteByte(HL, ResetBit(0x4, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0x97: A = ResetBit(0x4, A); break; //RES B    2   8   ----
                case 0x98: B = ResetBit(0x8, B); break; //RES B    2   8   ----
                case 0x99: C = ResetBit(0x8, C); break; //RES C    2   8   ----
                case 0x9A: D = ResetBit(0x8, D); break; //RES D    2   8   ----
                case 0x9B: E = ResetBit(0x8, E); break; //RES E    2   8   ----
                case 0x9C: H = ResetBit(0x8, H); break; //RES H    2   8   ----
                case 0x9D: L = ResetBit(0x8, L); break; //RES L    2   8   ----
                case 0x9E: mmu.WriteByte(HL, ResetBit(0x8, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0x9F: A = ResetBit(0x8, A); break; //RES B    2   8   ----
                #endregion

                #region A0-AF
                case 0xA0: B = ResetBit(0x10, B); break; //RES B    2   8   ----
                case 0xA1: C = ResetBit(0x10, C); break; //RES C    2   8   ----
                case 0xA2: D = ResetBit(0x10, D); break; //RES D    2   8   ----
                case 0xA3: E = ResetBit(0x10, E); break; //RES E    2   8   ----
                case 0xA4: H = ResetBit(0x10, H); break; //RES H    2   8   ----
                case 0xA5: L = ResetBit(0x10, L); break; //RES L    2   8   ----
                case 0xA6: mmu.WriteByte(HL, ResetBit(0x10, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0xA7: A = ResetBit(0x10, A); break; //RES B    2   8   ----

                case 0xA8: B = ResetBit(0x20, B); break; //RES B    2   8   ----
                case 0xA9: C = ResetBit(0x20, C); break; //RES C    2   8   ----
                case 0xAA: D = ResetBit(0x20, D); break; //RES D    2   8   ----
                case 0xAB: E = ResetBit(0x20, E); break; //RES E    2   8   ----
                case 0xAC: H = ResetBit(0x20, H); break; //RES H    2   8   ----
                case 0xAD: L = ResetBit(0x20, L); break; //RES L    2   8   ----
                case 0xAE: mmu.WriteByte(HL, ResetBit(0x20, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0xAF: A = ResetBit(0x20, A); break; //RES B    2   8   ----
                #endregion

                #region B0-BF
                case 0xB0: B = ResetBit(0x40, B); break; //RES B    2   8   ----
                case 0xB1: C = ResetBit(0x40, C); break; //RES C    2   8   ----
                case 0xB2: D = ResetBit(0x40, D); break; //RES D    2   8   ----
                case 0xB3: E = ResetBit(0x40, E); break; //RES E    2   8   ----
                case 0xB4: H = ResetBit(0x40, H); break; //RES H    2   8   ----
                case 0xB5: L = ResetBit(0x40, L); break; //RES L    2   8   ----
                case 0xB6: mmu.WriteByte(HL, ResetBit(0x40, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0xB7: A = ResetBit(0x40, A); break; //RES B    2   8   ----

                case 0xB8: B = ResetBit(0x80, B); break; //RES B    2   8   ----
                case 0xB9: C = ResetBit(0x80, C); break; //RES C    2   8   ----
                case 0xBA: D = ResetBit(0x80, D); break; //RES D    2   8   ----
                case 0xBB: E = ResetBit(0x80, E); break; //RES E    2   8   ----
                case 0xBC: H = ResetBit(0x80, H); break; //RES H    2   8   ----
                case 0xBD: L = ResetBit(0x80, L); break; //RES L    2   8   ----
                case 0xBE: mmu.WriteByte(HL, ResetBit(0x80, mmu.ReadByte(HL))); break; //RES (HL) 2   8   ----
                case 0xBF: A = ResetBit(0x80, A); break; //RES B    2   8   ----
                #endregion

                #region C0-CF
                case 0xC0: B = SetBit(0x1, B); break; //SET B    2   8   ----
                case 0xC1: C = SetBit(0x1, C); break; //SET C    2   8   ----
                case 0xC2: D = SetBit(0x1, D); break; //SET D    2   8   ----
                case 0xC3: E = SetBit(0x1, E); break; //SET E    2   8   ----
                case 0xC4: H = SetBit(0x1, H); break; //SET H    2   8   ----
                case 0xC5: L = SetBit(0x1, L); break; //SET L    2   8   ----
                case 0xC6: mmu.WriteByte(HL, SetBit(0x1, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xC7: A = SetBit(0x1, A); break; //SET B    2   8   ----

                case 0xC8: B = SetBit(0x2, B); break; //SET B    2   8   ----
                case 0xC9: C = SetBit(0x2, C); break; //SET C    2   8   ----
                case 0xCA: D = SetBit(0x2, D); break; //SET D    2   8   ----
                case 0xCB: E = SetBit(0x2, E); break; //SET E    2   8   ----
                case 0xCC: H = SetBit(0x2, H); break; //SET H    2   8   ----
                case 0xCD: L = SetBit(0x2, L); break; //SET L    2   8   ----
                case 0xCE: mmu.WriteByte(HL, SetBit(0x2, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xCF: A = SetBit(0x2, A); break; //SET B    2   8   ----
                #endregion

                #region D0-DF
                case 0xD0: B = SetBit(0x4, B); break; //SET B    2   8   ----
                case 0xD1: C = SetBit(0x4, C); break; //SET C    2   8   ----
                case 0xD2: D = SetBit(0x4, D); break; //SET D    2   8   ----
                case 0xD3: E = SetBit(0x4, E); break; //SET E    2   8   ----
                case 0xD4: H = SetBit(0x4, H); break; //SET H    2   8   ----
                case 0xD5: L = SetBit(0x4, L); break; //SET L    2   8   ----
                case 0xD6: mmu.WriteByte(HL, SetBit(0x4, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xD7: A = SetBit(0x4, A); break; //SET B    2   8   ----

                case 0xD8: B = SetBit(0x8, B); break; //SET B    2   8   ----
                case 0xD9: C = SetBit(0x8, C); break; //SET C    2   8   ----
                case 0xDA: D = SetBit(0x8, D); break; //SET D    2   8   ----
                case 0xDB: E = SetBit(0x8, E); break; //SET E    2   8   ----
                case 0xDC: H = SetBit(0x8, H); break; //SET H    2   8   ----
                case 0xDD: L = SetBit(0x8, L); break; //SET L    2   8   ----
                case 0xDE: mmu.WriteByte(HL, SetBit(0x8, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xDF: A = SetBit(0x8, A); break; //SET B    2   8   ----
                #endregion

                #region E0-EF
                case 0xE0: B = SetBit(0x10, B); break; //SET B    2   8   ----
                case 0xE1: C = SetBit(0x10, C); break; //SET C    2   8   ----
                case 0xE2: D = SetBit(0x10, D); break; //SET D    2   8   ----
                case 0xE3: E = SetBit(0x10, E); break; //SET E    2   8   ----
                case 0xE4: H = SetBit(0x10, H); break; //SET H    2   8   ----
                case 0xE5: L = SetBit(0x10, L); break; //SET L    2   8   ----
                case 0xE6: mmu.WriteByte(HL, SetBit(0x10, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xE7: A = SetBit(0x10, A); break; //SET B    2   8   ----

                case 0xE8: B = SetBit(0x20, B); break; //SET B    2   8   ----
                case 0xE9: C = SetBit(0x20, C); break; //SET C    2   8   ----
                case 0xEA: D = SetBit(0x20, D); break; //SET D    2   8   ----
                case 0xEB: E = SetBit(0x20, E); break; //SET E    2   8   ----
                case 0xEC: H = SetBit(0x20, H); break; //SET H    2   8   ----
                case 0xED: L = SetBit(0x20, L); break; //SET L    2   8   ----
                case 0xEE: mmu.WriteByte(HL, SetBit(0x20, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xEF: A = SetBit(0x20, A); break; //SET B    2   8   ----
                #endregion

                #region F0-FF
                case 0xF0: B = SetBit(0x40, B); break; //SET B    2   8   ----
                case 0xF1: C = SetBit(0x40, C); break; //SET C    2   8   ----
                case 0xF2: D = SetBit(0x40, D); break; //SET D    2   8   ----
                case 0xF3: E = SetBit(0x40, E); break; //SET E    2   8   ----
                case 0xF4: H = SetBit(0x40, H); break; //SET H    2   8   ----
                case 0xF5: L = SetBit(0x40, L); break; //SET L    2   8   ----
                case 0xF6: mmu.WriteByte(HL, SetBit(0x40, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xF7: A = SetBit(0x40, A); break; //SET B    2   8   ----

                case 0xF8: B = SetBit(0x80, B); break; //SET B    2   8   ----
                case 0xF9: C = SetBit(0x80, C); break; //SET C    2   8   ----
                case 0xFA: D = SetBit(0x80, D); break; //SET D    2   8   ----
                case 0xFB: E = SetBit(0x80, E); break; //SET E    2   8   ----
                case 0xFC: H = SetBit(0x80, H); break; //SET H    2   8   ----
                case 0xFD: L = SetBit(0x80, L); break; //SET L    2   8   ----
                case 0xFE: mmu.WriteByte(HL, SetBit(0x80, mmu.ReadByte(HL))); break; //SET (HL) 2   8   ----
                case 0xFF: A = SetBit(0x80, A); break; //SET B    2   8   ----
                #endregion
            }
        }

        /// <summary>
        /// Uses a bitwise OR to set the flags on the register.
        /// </summary>
        /// <param name="flags">The byte containing the flagged bits to set.</param>
        /// <param name="register">The register to set the bits on.</param>
        /// <returns>A new byte with the flag set.</returns>
        private byte SetBit(byte flags, byte register)
        {
            return (byte)(register | flags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        private byte ResetBit(int flags, byte register)
        {
            return (byte)(register & ~flags);
        }

        /// <summary>
        /// Tests to see if the bit is set. Sets the Zero flag to 1 if true.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="register"></param>
        private void TestBit(byte flag, byte register)
        {
            FlagZ = (register & flag) == 0;
            FlagN = false;
            FlagH = true;
        }

        private byte ShiftRightLogical(byte b)
        {
            byte result = (byte)(b >> 1);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte SwapNibbles(byte b)
        {
            byte result = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            return result;
        }

        private byte ShiftRightArithmetic(byte b)
        {
            byte result = (byte)((b >> 1) | (b & 0x80));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte ShiftLeftArithmetic(byte b)
        {
            byte result = (byte)(b << 1);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private byte RotateRight(byte b)
        {
            bool prevC = FlagC;
            byte result = (byte)((b >> 1) | (prevC ? 0x80 : 0));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte RotateLeft(byte b)
        {
            bool prevC = FlagC;
            byte result = (byte)((b << 1) | (prevC ? 1 : 0));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private byte RotateRightCircular(byte b)
        {
            byte result = (byte)((b >> 1) | (b << 7));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x1) != 0;
            return result;
        }

        private byte RotateLeftCircular(byte b)
        {
            byte result = (byte)((b << 1) | (b >> 7));
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = (b & 0x80) != 0;
            return result;
        }

        private ushort DADr8(ushort w)
        {   // warning r8 is signed!
            byte b = mmu.ReadByte(PC++);
            FlagZ = false;
            FlagN = false;
            SetFlagH((byte)w, b);
            SetFlagC((byte)w + b);
            return (ushort)(w + (sbyte)b);
        }

        private void JumpRelative(bool flag)
        {
            if (flag)
            {
                sbyte sb = (sbyte)mmu.ReadByte(PC);
                PC = (ushort)(PC + sb);
                PC += 1;
                cycles += Cycles.JUMP_RELATIVE_TRUE;
            }
            else
            {
                PC += 1;
                cycles += Cycles.JUMP_RELATIVE_FALSE;
            }
        }

        private void STOP()
        {
            //throw new NotImplementedException();
        }

        private byte Increment(byte b)
        {
            int result = b + 1;
            SetFlagZ(result);
            FlagN = false;
            SetFlagH(b, 1);
            return (byte)result;
        }

        private byte Decrement(byte b)
        {
            int result = b - 1;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(b, 1);
            return (byte)result;
        }

        private void Add(byte b)
        {
            int result = A + b;
            SetFlagZ(result);
            FlagN = false;
            SetFlagH(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void AddWithCarry(byte b)
        {
            int carry = FlagC ? 1 : 0;
            int result = A + b + carry;
            SetFlagZ(result);
            FlagN = false;
            if (FlagC)
                SetFlagHCarry(A, b);
            else SetFlagH(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void Subtract(byte b)
        {
            int result = A - b;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void SubtractWithCarry(byte b)
        {
            int carry = FlagC ? 1 : 0;
            int result = A - b - carry;
            SetFlagZ(result);
            FlagN = true;
            if (FlagC)
                SetFlagHSubCarry(A, b);
            else SetFlagHSub(A, b);
            SetFlagC(result);
            A = (byte)result;
        }

        private void And(byte b)
        {
            byte result = (byte)(A & b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = true;
            FlagC = false;
            A = result;
        }

        private void Xor(byte b)
        {
            byte result = (byte)(A ^ b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            A = result;
        }

        private void Or(byte b)
        {
            byte result = (byte)(A | b);
            SetFlagZ(result);
            FlagN = false;
            FlagH = false;
            FlagC = false;
            A = result;
        }

        private void CP(byte b)
        {//Z1HC
            int result = A - b;
            SetFlagZ(result);
            FlagN = true;
            SetFlagHSub(A, b);
            SetFlagC(result);
        }

        private void DAD(ushort w)
        { //-0HC
            int result = HL + w;
            FlagN = false;
            SetFlagH(HL, w); //Special Flag H with word
            FlagC = result >> 16 != 0; //Special Flag C as short value involved
            HL = (ushort)result;
        }

        private void ReturnFromSubroutine(bool flag)
        {
            if (flag)
            {
                PC = PopStack();
                cycles += Cycles.RETURN_TRUE;
            }
            else
            {
                cycles += Cycles.RETURN_FALSE;
            }
        }

        private void CallSubroutine(bool flag)
        {
            if (flag)
            {
                PushStack((ushort)(PC + 2));
                PC = mmu.ReadWord(PC);
                cycles += Cycles.CALL_TRUE;
            }
            else
            {
                PC += 2;
                cycles += Cycles.CALL_FALSE;
            }
        }

        private void Jump(bool flag)
        {
            if (flag)
            {
                PC = mmu.ReadWord(PC);
                cycles += Cycles.JUMP_TRUE;
            }
            else
            {
                PC += 2;
                cycles += Cycles.JUMP_FALSE;
            }
        }

        private void RestartAt(byte b)
        {
            PushStack(PC);
            PC = b;
        }

        private void Halt()
        {
            if (!InterruptMasterEnable)
            {
                if ((mmu.InterruptEnable & mmu.InterruptFlags & 0x1F) == 0)
                {
                    HALTED = true;
                    PC--;
                }
                else
                {
                    HALT_BUG = true;
                }
            }
        }

        public void UpdateIME()
        {
            InterruptMasterEnable |= IMEEnabler;
            IMEEnabler = false;
        }

        public void HandleInterrupts()
        {
            byte IE = mmu.InterruptEnable;
            byte IF = mmu.InterruptFlags;
            for (int i = 0; i < 5; i++)
            {
                if ((((IE & IF) >> i) & 0x1) == 1)
                {
                    ExecuteInterrupt(i);
                }
            }

            UpdateIME();
        }

        public void ExecuteInterrupt(int b)
        {
            if (HALTED)
            {
                PC++;
                HALTED = false;
            }
            if (InterruptMasterEnable)
            {
                PushStack(PC);
                PC = (ushort)(0x40 + (8 * b));
                InterruptMasterEnable = false;
                mmu.InterruptFlags = BitClear(b, mmu.InterruptFlags);
            }
        }

        private void PushStack(ushort w)
        {// (SP - 1) < -PC.hi; (SP - 2) < -PC.lo
            SP -= 2;
            mmu.WriteWord(SP, w);
        }

        private ushort PopStack()
        {
            ushort ret = mmu.ReadWord(SP);
            SP += 2;
            //byte l = mmu.readByte(++SP);
            //byte h = mmu.readByte(++SP);
            //ushort ret = (ushort)(h << 8 | l);
            //Console.WriteLine("stack POP = " + ret.ToString("x4") + " SP = " + SP.ToString("x4") + " reading: " + mmu.readWord(SP).ToString("x4") + "ret = " /*+ ((ushort)(h << 8 | l)).ToString("x4")*/);

            return ret;
        }

        private void SetFlagZ(int b)
        {
            FlagZ = (byte)b == 0;
        }

        private void SetFlagC(int i)
        {
            FlagC = (i >> 8) != 0;
        }

        private void SetFlagH(byte b1, byte b2)
        {
            FlagH = ((b1 & 0xF) + (b2 & 0xF)) > 0xF;
        }

        private void SetFlagH(ushort w1, ushort w2)
        {
            FlagH = ((w1 & 0xFFF) + (w2 & 0xFFF)) > 0xFFF;
        }

        private void SetFlagHCarry(byte b1, byte b2)
        {
            FlagH = ((b1 & 0xF) + (b2 & 0xF)) >= 0xF;
        }

        private void SetFlagHSub(byte b1, byte b2)
        {
            FlagH = (b1 & 0xF) < (b2 & 0xF);
        }

        private void SetFlagHSubCarry(byte b1, byte b2)
        {
            int carry = FlagC ? 1 : 0;
            FlagH = (b1 & 0xF) < ((b2 & 0xF) + carry);
        }

        private void WarnUnsupportedOpcode(byte opcode)
        {
            Console.WriteLine((PC - 1).ToString("x4") + " Unsupported operation " + opcode.ToString("x2"));
        }

        private int dev;
        private void debug(byte opcode)
        {
            dev += cycles;
            if (dev >= 23440108 /*&& PC == 0x35A*/) //0x100 23440108
                Console.WriteLine("Cycle " + dev + " PC " + (PC - 1).ToString("x4") + " Stack: " + SP.ToString("x4") + " AF: " + A.ToString("x2") + "" + F.ToString("x2")
                    + " BC: " + B.ToString("x2") + "" + C.ToString("x2") + " DE: " + D.ToString("x2") + "" + E.ToString("x2") + " HL: " + H.ToString("x2") + "" + L.ToString("x2")
                    + " op " + opcode.ToString("x2") + " D16 " + mmu.ReadWord(PC).ToString("x4") + " LY: " + mmu.LcdY.ToString("x2"));
        }
    }
}
