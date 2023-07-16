namespace GameBoySharp.Emu.Core
{
    public enum CpuOperation : byte
    {
        ///
        /// NOP
        ///
        NOP = 0x00,
        ///
        /// LD BC,nn
        ///
        LD_BC_nn = 0x01,
        ///
        /// LD (BC),A
        ///
        LD_BC_A = 0x02,
        ///
        /// INC BC
        ///
        INC_BC = 0x03,
        ///
        /// INC B
        ///
        INC_B = 0x04,
        ///
        /// DEC B
        ///
        DEC_B = 0x05,
        ///
        /// LD B,n
        ///
        LD_B_n = 0x06,
        ///
        /// RLCA
        ///
        RLCA = 0x07,
        ///
        /// LD (A16),SP
        ///
        LD_A16_SP = 0x08,
        ///
        /// ADD HL,BC
        ///
        ADD_HL_BC = 0x09,
        ///
        /// LD A,(BC)
        ///
        LD_A_BC = 0x0A,
        ///
        /// DEC BC
        ///
        DEC_BC = 0x0B,
        ///
        /// INC C
        ///
        INC_C = 0x0C,
        ///
        /// DEC C
        ///
        DEC_C = 0x0D,
        ///
        /// LD C,n
        ///
        LD_C_n = 0x0E,
        ///
        /// RRCA
        ///
        RRCA = 0x0F,

        ///
        /// STOP
        ///
        STOP = 0x10,
        ///
        /// LD DE,nn
        ///
        LD_DE_nn = 0x11,
        ///
        /// LD (DE),A
        ///
        LD_DE_A = 0x12,
        ///
        /// INC DE
        ///
        INC_DE = 0x13,
        ///
        /// INC D
        ///
        INC_D = 0x14,
        ///
        /// DEC D
        ///
        DEC_D = 0x15,
        ///
        /// LD D,n
        ///
        LD_D_n = 0x16,
        ///
        /// RLA
        ///
        RLA = 0x17,
        ///
        /// JR n
        ///
        JR_n = 0x18,
        ///
        /// ADD HL,DE
        ///
        ADD_HL_DE = 0x19,
        ///
        /// LD A,(DE)
        ///
        LD_A_DE = 0x1A,
        ///
        /// DEC DE
        ///
        DEC_DE = 0x1B,
        ///
        /// INC E
        ///
        INC_E = 0x1C,
        ///
        /// DEC E
        ///
        DEC_E = 0x1D,
        ///
        /// LD E,n
        ///
        LD_E_n = 0x1E,
        ///
        /// RRA
        ///
        RRA = 0x1F,

        ///
        /// JR NZ,n
        ///
        JR_NZ_n = 0x20,
        ///
        /// LD HL,nn
        ///
        LD_HL_nn = 0x21,
        ///
        /// LDI (HL),A
        ///
        LDI_HL_A = 0x22,
        ///
        /// INC HL
        ///
        INC_HL = 0x23,
        ///
        /// INC H
        ///
        INC_H = 0x24,
        ///
        /// DEC H
        ///
        DEC_H = 0x25,
        ///
        /// LD H,n
        ///
        LD_H_n = 0x26,
        ///
        /// DAA
        ///
        DAA = 0x27,
        ///
        /// JR Z,n
        ///
        JR_Z_n = 0x28,
        ///
        /// ADD HL,HL
        ///
        ADD_HL_HL = 0x29,
        ///
        /// LDI A,(HL)
        ///
        LDI_A_HL = 0x2A,
        ///
        /// DEC HL
        ///
        DEC_HL = 0x2B,
        ///
        /// INC L
        ///
        INC_L = 0x2C,
        ///
        /// DEC L
        ///
        DEC_L = 0x2D,
        ///
        /// LD L,n
        ///
        LD_L_n = 0x2E,
        ///
        /// CPL
        ///
        CPL = 0x2F,

        ///
        /// JR NC,n
        ///
        JR_NC_n = 0x30,
        ///
        /// LD SP,nn
        ///
        LD_SP_nn = 0x31,
        ///
        /// LDD (HL),A
        ///
        LDD_HL_A = 0x32,
        ///
        /// INC SP
        ///
        INC_SP = 0x33,
        ///
        /// INC (HL) Adr
        ///
        INC_HL_Adr = 0x34,
        ///
        /// DEC (HL) Adr
        ///
        DEC_HL_Adr = 0x35,
        ///
        /// LD (HL),n
        ///
        LD_HL_n = 0x36,
        ///
        /// SCF
        ///
        SCF = 0x37,
        ///
        /// JR C,n
        ///
        JR_C_n = 0x38,
        ///
        /// ADD HL,SP
        ///
        ADD_HL_SP = 0x39,
        ///
        /// LDD A,(HL)
        ///
        LDD_A_HL = 0x3A,
        ///
        /// DEC SP
        ///
        DEC_SP = 0x3B,
        ///
        /// INC A
        ///
        INC_A = 0x3C,
        ///
        /// DEC A
        ///
        DEC_A = 0x3D,
        ///
        /// LD A,n
        ///
        LD_A_n = 0x3E,
        ///
        /// CCF
        ///
        CCF = 0x3F,

        ///
        /// LD B,B
        ///
        LD_B_B = 0x40,
        ///
        /// LD B,C
        ///
        LD_B_C = 0x41,
        ///
        /// LD B,D
        ///
        LD_B_D = 0x42,
        ///
        /// LD B,E
        ///
        LD_B_E = 0x43,
        ///
        /// LD B,H
        ///
        LD_B_H = 0x44,
        ///
        /// LD B,L
        ///
        LD_B_L = 0x45,
        ///
        /// LD B,(HL)
        ///
        LD_B_HL = 0x46,
        ///
        /// LD B,A
        ///
        LD_B_A = 0x47,
        ///
        /// LD C,B
        ///
        LD_C_B = 0x48,
        ///
        /// LD C,C
        ///
        LD_C_C = 0x49,
        ///
        /// LD C,D
        ///
        LD_C_D = 0x4A,
        ///
        /// LD C,E
        ///
        LD_C_E = 0x4B,
        ///
        /// LD C,H
        ///
        LD_C_H = 0x4C,
        ///
        /// LD C,L
        ///
        LD_C_L = 0x4D,
        ///
        /// LD C,(HL)
        ///
        LD_C_HL = 0x4E,
        ///
        /// LD C,A
        ///
        LD_C_A = 0x4F,

        ///
        /// LD D,B
        ///
        LD_D_B = 0x50,
        ///
        /// LD D,C
        ///
        LD_D_C = 0x51,
        ///
        /// LD D,D
        ///
        LD_D_D = 0x52,
        ///
        /// LD D,E
        ///
        LD_D_E = 0x53,
        ///
        /// LD D,H
        ///
        LD_D_H = 0x54,
        ///
        /// LD D,L
        ///
        LD_D_L = 0x55,
        ///
        /// LD D,(HL)
        ///
        LD_D_HL = 0x56,
        ///
        /// LD D,A
        ///
        LD_D_A = 0x57,
        ///
        /// LD E,B
        ///
        LD_E_B = 0x58,
        ///
        /// LD E,C
        ///
        LD_E_C = 0x59,
        ///
        /// LD E,D
        ///
        LD_E_D = 0x5A,
        ///
        /// LD E,E
        ///
        LD_E_E = 0x5B,
        ///
        /// LD E,H
        ///
        LD_E_H = 0x5C,
        ///
        /// LD E,L
        ///
        LD_E_L = 0x5D,
        ///
        /// LD E,(HL)
        ///
        LD_E_HL = 0x5E,
        ///
        /// LD E,A
        ///
        LD_E_A = 0x5F,

        ///
        /// LD H,B
        ///
        LD_H_B = 0x60,
        ///
        /// LD H,C
        ///
        LD_H_C = 0x61,
        ///
        /// LD H,D
        ///
        LD_H_D = 0x62,
        ///
        /// LD H,E
        ///
        LD_H_E = 0x63,
        ///
        /// LD H,H
        ///
        LD_H_H = 0x64,
        ///
        /// LD H,L
        ///
        LD_H_L = 0x65,
        ///
        /// LD H,(HL)
        ///
        LD_H_HL = 0x66,
        ///
        /// LD H,A
        ///
        LD_H_A = 0x67,
        ///
        /// LD L,B
        ///
        LD_L_B = 0x68,
        ///
        /// LD L,C
        ///
        LD_L_C = 0x69,
        ///
        /// LD L,D
        ///
        LD_L_D = 0x6A,
        ///
        /// LD L,E
        ///
        LD_L_E = 0x6B,
        ///
        /// LD L,H
        ///
        LD_L_H = 0x6C,
        ///
        /// LD L,L
        ///
        LD_L_L = 0x6D,
        ///
        /// LD L,(HL)
        ///
        LD_L_HL = 0x6E,
        ///
        /// LD L,A
        ///
        LD_L_A = 0x6F,

        ///
        /// LD (HL),B
        ///
        LD_HL_B = 0x70,
        ///
        /// LD (HL),C
        ///
        LD_HL_C = 0x71,
        ///
        /// LD (HL),D
        ///
        LD_HL_D = 0x72,
        ///
        /// LD (HL),E
        ///
        LD_HL_E = 0x73,
        ///
        /// LD (HL),H
        ///
        LD_HL_H = 0x74,
        ///
        /// LD (HL),L
        ///
        LD_HL_L = 0x75,
        ///
        /// HALT
        ///
        HALT = 0x76,
        ///
        /// LD (HL),A
        ///
        LD_HL_A = 0x77,
        ///
        /// LD A,B
        ///
        LD_A_B = 0x78,
        ///
        /// LD A,C
        ///
        LD_A_C = 0x79,
        ///
        /// LD A,D
        ///
        LD_A_D = 0x7A,
        ///
        /// LD A,E
        ///
        LD_A_E = 0x7B,
        ///
        /// LD A,H
        ///
        LD_A_H = 0x7C,
        ///
        /// LD A,L
        ///
        LD_A_L = 0x7D,
        ///
        /// LD A,(HL)
        ///
        LD_A_HL = 0x7E,
        ///
        /// LD A,A
        ///
        LD_A_A = 0x7F,

        ///
        /// ADD A,B
        ///
        ADD_A_B = 0x80,
        ///
        /// ADD A,C
        ///
        ADD_A_C = 0x81,
        ///
        /// ADD A,D
        ///
        ADD_A_D = 0x82,
        ///
        /// ADD A,E
        ///
        ADD_A_E = 0x83,
        ///
        /// ADD A,H
        ///
        ADD_A_H = 0x84,
        ///
        /// ADD A,L
        ///
        ADD_A_L = 0x85,
        ///
        /// ADD A,(HL)
        ///
        ADD_A_HL = 0x86,
        ///
        /// ADD A,A
        ///
        ADD_A_A = 0x87,
        ///
        /// ADC A,B
        ///
        ADC_A_B = 0x88,
        ///
        /// ADC A,C
        ///
        ADC_A_C = 0x89,
        ///
        /// ADC A,D
        ///
        ADC_A_D = 0x8A,
        ///
        /// ADC A,E
        ///
        ADC_A_E = 0x8B,
        ///
        /// ADC A,H
        ///
        ADC_A_H = 0x8C,
        ///
        /// ADC A,L
        ///
        ADC_A_L = 0x8D,
        ///
        /// ADC A,(HL)
        ///
        ADC_A_HL = 0x8E,
        ///
        /// ADC A,A
        ///
        ADC_A_A = 0x8F,

        ///
        /// SUB A,B
        ///
        SUB_A_B = 0x90,
        ///
        /// SUB A,C
        ///
        SUB_A_C = 0x91,
        ///
        /// SUB A,D
        ///
        SUB_A_D = 0x92,
        ///
        /// SUB A,E
        ///
        SUB_A_E = 0x93,
        ///
        /// SUB A,H
        ///
        SUB_A_H = 0x94,
        ///
        /// SUB A,L
        ///
        SUB_A_L = 0x95,
        ///
        /// SUB A,(HL)
        ///
        SUB_A_HL = 0x96,
        ///
        /// SUB A,A
        ///
        SUB_A_A = 0x97,
        ///
        /// SBC A,B
        ///
        SBC_A_B = 0x98,
        ///
        /// SBC A,C
        ///
        SBC_A_C = 0x99,
        ///
        /// SBC A,D
        ///
        SBC_A_D = 0x9A,
        ///
        /// SBC A,E
        ///
        SBC_A_E = 0x9B,
        ///
        /// SBC A,H
        ///
        SBC_A_H = 0x9C,
        ///
        /// SBC A,L
        ///
        SBC_A_L = 0x9D,
        ///
        /// SBC A,(HL)
        ///
        SBC_A_HL = 0x9E,
        ///
        /// SBC A,A
        ///
        SBC_A_A = 0x9F,

        ///
        /// AND B
        ///
        AND_B = 0xA0,
        ///
        /// AND C
        ///
        AND_C = 0xA1,
        ///
        /// AND D
        ///
        AND_D = 0xA2,
        ///
        /// AND E
        ///
        AND_E = 0xA3,
        ///
        /// AND H
        ///
        AND_H = 0xA4,
        ///
        /// AND L
        ///
        AND_L = 0xA5,
        ///
        /// AND (HL)
        ///
        AND_HL = 0xA6,
        ///
        /// AND A
        ///
        AND_A = 0xA7,
        ///
        /// XOR B
        ///
        XOR_B = 0xA8,
        ///
        /// XOR C
        ///
        XOR_C = 0xA9,
        ///
        /// XOR D
        ///
        XOR_D = 0xAA,
        ///
        /// XOR E
        ///
        XOR_E = 0xAB,
        ///
        /// XOR H
        ///
        XOR_H = 0xAC,
        ///
        /// XOR L
        ///
        XOR_L = 0xAD,
        ///
        /// XOR (HL)
        ///
        XOR_HL = 0xAE,
        ///
        /// XOR A
        ///
        XOR_A = 0xAF,

        ///
        /// OR B
        ///
        OR_B = 0xB0,
        ///
        /// OR C
        ///
        OR_C = 0xB1,
        ///
        /// OR D
        ///
        OR_D = 0xB2,
        ///
        /// OR E
        ///
        OR_E = 0xB3,
        ///
        /// OR H
        ///
        OR_H = 0xB4,
        ///
        /// OR L
        ///
        OR_L = 0xB5,
        ///
        /// OR (HL)
        ///
        OR_HL = 0xB6,
        ///
        /// OR A
        ///
        OR_A = 0xB7,
        ///
        /// CMP B
        ///
        CMP_B = 0xB8,
        ///
        /// CMP C
        ///
        CMP_C = 0xB9,
        ///
        /// CMP D
        ///
        CMP_D = 0xBA,
        ///
        /// CMP E
        ///
        CMP_E = 0xBB,
        ///
        /// CMP H
        ///
        CMP_H = 0xBC,
        ///
        /// CMP L
        ///
        CMP_L = 0xBD,
        ///
        /// CMP (HL)
        ///
        CMP_HL = 0xBE,
        ///
        /// CMP A
        ///
        CMP_A = 0xBF,

        ///
        /// RET !FZ
        ///
        RET_Not_FZ = 0xC0,
        ///
        /// POP BC
        ///
        POP_BC = 0xC1,
        ///
        /// JP !FZ,nn
        ///
        JP_Not_FZ_nn = 0xC2,
        ///
        /// JP nn)
        ///
        JP_nn = 0xC3,
        ///
        /// CALL !FZ,nn
        ///
        CALL_Not_FZ_nn = 0xC4,
        ///
        /// PUSH BC
        ///
        PUSH_BC = 0xC5,
        ///
        /// ADD,n
        ///
        ADD_n = 0xC6,
        ///
        /// RST 0
        ///
        RST_0 = 0xC7,
        ///
        /// RET FZ
        ///
        RET_FZ = 0xC8,
        ///
        /// RET
        ///
        RET = 0xC9,
        ///
        /// JP FZ,nn
        ///
        JP_FZ_nn = 0xCA,
        ///
        /// PREFIX CB
        ///
        PREFIX_CB = 0xCB,
        ///
        /// CALL FZ,nn
        ///
        CALL_FZ_nn = 0xCC,
        ///
        /// CALL nn
        ///
        CALL_nn = 0xCD,
        ///
        /// ADC A,n
        ///
        ADC_A_n = 0xCE,
        ///
        /// RST 0x8
        ///
        RST_0x8 = 0xCF,

        ///
        /// RET !FC
        ///
        RET_Not_FC = 0xD0,
        ///
        /// POP DE
        ///
        POP_DE = 0xD1,
        ///
        /// JP !FC,nn
        ///
        JP_Not_FC_nn = 0xD2,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_D3 = 0xD3,
        ///
        /// CALL !FC,nn
        ///
        CALL_Not_FC_nn = 0xD4,
        ///
        /// PUSH DE
        ///
        PUSH_DE = 0xD5,
        ///
        /// SUB A,n
        ///
        SUB_A_n = 0xD6,
        ///
        /// RST 0x10
        ///
        RST_0x10 = 0xD7,
        ///
        /// RET FC
        ///
        RET_FC = 0xD8,
        ///
        /// RETI
        ///
        RETI = 0xD9,
        ///
        /// JP FC,nn
        ///
        JP_FC_nn = 0xDA,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_DB = 0xDB,
        ///
        /// CALL FC,nn
        ///
        CALL_FC_nn = 0xDC,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_DD = 0xDD,
        ///
        /// SBC A,n
        ///
        SBC_A_n = 0xDE,
        ///
        /// RST 0x18
        ///
        RST_0x18 = 0xDF,

        ///
        /// LDH (n),A
        ///
        LDH_n_A = 0xE0,
        ///
        /// POP HL
        ///
        POP_HL = 0xE1,
        ///
        /// LDH (C),A
        ///
        LDH_C_A = 0xE2,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_E3 = 0xE3,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_E4 = 0xE4,
        ///
        /// PUSH HL
        ///
        PUSH_HL = 0xE5,
        ///
        /// AND n
        ///
        AND_n = 0xE6,
        ///
        /// RST 0x20
        ///
        RST_0x20 = 0xE7,
        ///
        /// ADD SP,n
        ///
        ADD_SP_n = 0xE8,
        ///
        /// JP, (HL)
        ///
        JP__HL = 0xE9,
        ///
        /// LD n,A
        ///
        LD_n_A = 0xEA,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_EB = 0xEB,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_EC = 0xEC,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_ED = 0xED,
        ///
        /// XOR nn
        ///
        XOR_nn = 0xEE,
        ///
        /// RST 0x28
        ///
        RST_0x28 = 0xEF,

        ///
        /// LDH A,(n)
        ///
        LDH_A_n = 0xF0,
        ///
        /// POP AF
        ///
        POP_AF = 0xF1,
        ///
        /// LD A,(0xFF00+C)
        ///
        LD_A_0xFF00C = 0xF2,
        ///
        /// DI
        ///
        DI = 0xF3,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_F4 = 0xF4,
        ///
        /// PUSH AF
        ///
        PUSH_AF = 0xF5,
        ///
        /// OR n
        ///
        OR_n = 0xF6,
        ///
        /// RST 0x30
        ///
        RST_0x30 = 0xF7,
        ///
        /// LDHL SP,n
        ///
        LDHL_SP_n = 0xF8,
        ///
        /// LD SP,HL
        ///
        LD_SP_HL = 0xF9,
        ///
        /// LD A,(nn)
        ///
        LD_A_nn = 0xFA,
        ///
        /// EI
        ///
        EI = 0xFB,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_FC = 0xFC,
        ///
        /// ILLEGAL
        ///
        ILLEGAL_FD = 0xFD,
        ///
        /// CMP n
        ///
        CMP_n = 0xFE,
        ///
        /// RST 0x38
        ///
        RST_0x38 = 0xFF,
    }
}
