using System;
using System.Text;
using System.Collections.Generic;

using static Opcode;

public static unsafe class SLVM {
    public static byte[]            Stack;
    public static uint              StackCurrent;

    private static ErrorStream Err;

    private const int  StackSize = 1024 * 1024 * 8; // 8mb stack
    private const uint FrameHeader = 16;

    public static void Init() {
        Stack  = null;
        Stack  = new byte[StackSize];
    }

    public static int Run(CodeUnit exe, ErrorStream err) {
        Err        = err;
        var  bytes = exe.Bytes;
        var  count = exe.Count;
        uint pc    = 0;
        Readu32(bytes, ref pc);

        if (bytes[0] != 0x80 ||
            bytes[1] != 0x00 ||
            bytes[2] != 0x00 ||
            bytes[3] != 0x8A) {

            err.Push("Incorrent executable. Wrong mask");
            return 1;
        }

        uint main = Readu32(bytes, ref pc);

        if (main < 8) {
            err.Push("Incorrent executable. Main function in wrong position");
            return 2;
        }

            pc = main; // program counter
        var fp = main; // frame pointer

        while (pc < count) {
            var opcode = ReadCode(bytes, ref pc);

            switch (opcode) {
                case func : {
                } break;
                case call : {
                    var newPc     = Readu32(bytes, ref pc);
                    var oldPc     = pc;
                    pc = newPc;
                    var argsCount = Readu32(bytes, ref pc);
                    var retSize   = Readu32(bytes, ref pc);
                    StackPush(oldPc);
                    StackPush(fp);
                    StackPush(retSize);
                    StackPush(argsCount);
                    fp = StackCurrent;

                    for (var i = 0; i < argsCount; ++i) {
                        StackPush(Readu32(bytes, ref pc));
                    }
                } break;
                case add_s32 : {
                    var a = StackPops32();
                    var b = StackPops32();
                    StackPush(a + b);
                } break;
                case ret : {
                    if (fp == main) {
                        return StackPops32();
                    }

                    var oldPc     = ReadOldPc(fp);
                    var oldFp     = ReadOldFp(fp);
                    var retSize   = ReadRetSize(fp);
                    var argsCount = ReadArgsCount(fp);
                    uint back     = FrameHeader;
                    for (var i = 0; i < argsCount; ++i) {
                        back += ReadArgSize(i, fp);
                    }
                    StackCurrent = fp - back;
                    StackPush(fp + argsCount * 4, retSize);

                    fp = oldFp;
                    pc = oldPc;
                } break;
                case push_s32 : {
                    StackPush(Reads32(bytes, ref pc));
                } break;
                case pop_s32 : {
                    StackPops32();
                    break;
                }
                case larg_s32 : {
                    var index = Reads32(bytes, ref pc);
                    StackPush(ReadArgs32(index, fp));
                    break;
                }
                default : {
                    err.Push($"Unknown opcode at {pc}");
                    return 3;
                }
            }
        }

        return StackPops32();
    }

    public static uint ReadArgsCount(uint fp) {
        var s = (uint)(Stack[fp - 4]       |
                       Stack[fp - 3] << 8  |
                       Stack[fp - 2] << 16 |
                       Stack[fp - 1] << 24);

        return s;
    }

    public static uint ReadRetSize(uint fp) {
        var s = (uint)(Stack[fp - 8]       |
                       Stack[fp - 7] << 8  |
                       Stack[fp - 6] << 16 |
                       Stack[fp - 5] << 24);

        return s;
    }

    public static uint ReadOldFp(uint fp) {
        var s = (uint)(Stack[fp - 12]       |
                       Stack[fp - 11] << 8  |
                       Stack[fp - 10] << 16 |
                       Stack[fp - 9] << 24);

        return s;
    }

    public static uint ReadOldPc(uint fp) {
        var s = (uint)(Stack[fp - 16]       |
                       Stack[fp - 15] << 8  |
                       Stack[fp - 14] << 16 |
                       Stack[fp - 13] << 24);

        return s;
    }

    public static Opcode ReadCode(byte[] bytes, ref uint ptr) {
        var opcode = (Opcode)Readu16(bytes, ref ptr);
        return opcode;
    }

    public static long Reads64(byte[] bytes, ref uint ptr) {
        var i = (long)(bytes[ptr]           |
                       bytes[ptr + 1] << 8  |
                       bytes[ptr + 2] << 16 |
                       bytes[ptr + 3] << 24 |
                       bytes[ptr + 4] << 32 |
                       bytes[ptr + 5] << 40 |
                       bytes[ptr + 6] << 48 |
                       bytes[ptr + 7] << 56);

        ptr += 8;

        return i;
    }

    public static ulong Readu64(byte[] bytes, ref uint ptr) {
        var i = (ulong)(bytes[ptr]           |
                        bytes[ptr + 1] << 8  |
                        bytes[ptr + 2] << 16 |
                        bytes[ptr + 3] << 24 |
                        bytes[ptr + 4] << 32 |
                        bytes[ptr + 5] << 40 |
                        bytes[ptr + 6] << 48 |
                        bytes[ptr + 7] << 56);

        ptr += 8;

        return i;
    }

    public static int Reads32(byte[] bytes, ref uint ptr) {
        var i = bytes[ptr]           |
                bytes[ptr + 1] << 8  |
                bytes[ptr + 2] << 16 |
                bytes[ptr + 3] << 24;

        ptr += 4;

        return i;
    }

    public static uint Readu32(byte[] bytes, ref uint ptr) {
        var i = (uint)(bytes[ptr]           |
                       bytes[ptr + 1] << 8  |
                       bytes[ptr + 2] << 16 |
                       bytes[ptr + 3] << 24);

        ptr += 4;

        return i;
    }

    public static short Reads16(byte[] bytes, ref uint ptr) {
        var i = (short)(bytes[ptr] |
                        bytes[ptr + 1] << 8);

        ptr += 2;

        return i;
    }

    public static ushort Readu16(byte[] bytes, ref uint ptr) {
        var i = (ushort)(bytes[ptr] |
                         bytes[ptr + 1] << 8);

        ptr += 2;

        return i;
    }

    public static sbyte Reads8(byte[] bytes, ref uint ptr) {
        var i = (sbyte)(bytes[ptr++]);

        return i;
    }

    public static byte Readu8(byte[] bytes, ref uint ptr) {
        var i = bytes[ptr++];

        return i;
    }

    public static float Readfloat(byte[] bytes, ref uint ptr) {
        var i = Readu32(bytes, ref ptr);
        return *(float*)&i;
    }

    public static double Readdouble(byte[] bytes, ref uint ptr) {
        var i = Readu64(bytes, ref ptr);
        return *(double*)&i;
    }

    public static void StackPush(float f) {
        if (StackCurrent + 4 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        uint *intPtr = (uint*)&f;
        uint intVal = *intPtr;

        Stack[StackCurrent]     = (byte)( intVal        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((intVal >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((intVal >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((intVal >> 24) & 0xFF);

        StackCurrent += 4;
    }

    public static void StackPush(double d) {
        if (StackCurrent + 8 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        ulong *intPtr = (ulong*)&d;
        ulong intVal = *intPtr;

        Stack[StackCurrent]     = (byte)( intVal        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((intVal >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((intVal >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((intVal >> 24) & 0xFF);
        Stack[StackCurrent + 4] = (byte)((intVal >> 32) & 0xFF);
        Stack[StackCurrent + 5] = (byte)((intVal >> 40) & 0xFF);
        Stack[StackCurrent + 6] = (byte)((intVal >> 48) & 0xFF);
        Stack[StackCurrent + 7] = (byte)((intVal >> 56) & 0xFF);

        StackCurrent += 8;
    }

    public static void StackPush(int a) {
        if (StackCurrent + 4 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((a >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((a >> 24) & 0xFF);

        StackCurrent += 4;
    }

    public static void StackPush(uint a) {
        if (StackCurrent + 4 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((a >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((a >> 24) & 0xFF);

        StackCurrent += 4;
    }

    public static void StackPush(short a) {
        if (StackCurrent + 2 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);

        StackCurrent += 2;
    }

    public static void StackPush(ushort a) {
        if (StackCurrent + 2 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);

        StackCurrent += 2;
    }

    public static void StackPush(long a) {
        if (StackCurrent + 8 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((a >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((a >> 24) & 0xFF);
        Stack[StackCurrent + 4] = (byte)((a >> 32) & 0xFF);
        Stack[StackCurrent + 5] = (byte)((a >> 40) & 0xFF);
        Stack[StackCurrent + 6] = (byte)((a >> 48) & 0xFF);
        Stack[StackCurrent + 7] = (byte)((a >> 56) & 0xFF);

        StackCurrent += 8;
    }

    public static void StackPush(ulong a) {
        if (StackCurrent + 8 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent]     = (byte)( a        & 0xFF);
        Stack[StackCurrent + 1] = (byte)((a >> 8)  & 0xFF);
        Stack[StackCurrent + 2] = (byte)((a >> 16) & 0xFF);
        Stack[StackCurrent + 3] = (byte)((a >> 24) & 0xFF);
        Stack[StackCurrent + 4] = (byte)((a >> 32) & 0xFF);
        Stack[StackCurrent + 5] = (byte)((a >> 40) & 0xFF);
        Stack[StackCurrent + 6] = (byte)((a >> 48) & 0xFF);
        Stack[StackCurrent + 7] = (byte)((a >> 56) & 0xFF);

        StackCurrent += 8;
    }

    public static void StackPush(byte b) {
        if (StackCurrent + 1 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent++] = b;
    }

    public static void StackPush(sbyte b) {
        if (StackCurrent + 1 >= StackSize) {
            Err.Push("Stack overflow");
            return;
        }

        Stack[StackCurrent++] = (byte)b;
    }

    public static long StackPops64() {
        var i = (long)(Stack[StackCurrent - 8]       |
                       Stack[StackCurrent - 7] << 8  |
                       Stack[StackCurrent - 6] << 16 |
                       Stack[StackCurrent - 5] << 24 |
                       Stack[StackCurrent - 4] << 32 |
                       Stack[StackCurrent - 3] << 40 |
                       Stack[StackCurrent - 2] << 48 |
                       Stack[StackCurrent - 1] << 56);

        StackCurrent -= 8;

        return i;
    }

    public static ulong StackPopu64() {
        var i = (ulong)(Stack[StackCurrent - 8]       |
                        Stack[StackCurrent - 7] << 8  |
                        Stack[StackCurrent - 6] << 16 |
                        Stack[StackCurrent - 5] << 24 |
                        Stack[StackCurrent - 4] << 32 |
                        Stack[StackCurrent - 3] << 40 |
                        Stack[StackCurrent - 2] << 48 |
                        Stack[StackCurrent - 1] << 56);

        StackCurrent -= 8;

        return i;
    }

    public static int StackPops32() {
        var i = Stack[StackCurrent - 4]       |
                Stack[StackCurrent - 3] << 8  |
                Stack[StackCurrent - 2] << 16 |
                Stack[StackCurrent - 1] << 24;

        StackCurrent -= 4;

        return i;
    }

    public static uint StackPopu32() {
        var i = (uint)(Stack[StackCurrent - 4]       |
                       Stack[StackCurrent - 3] << 8  |
                       Stack[StackCurrent - 2] << 16 |
                       Stack[StackCurrent - 1] << 24);

        StackCurrent -= 4;

        return i;
    }

    public static short StackPops16() {
        var i = (short)(Stack[StackCurrent - 2] |
                        Stack[StackCurrent - 1] << 8);

        StackCurrent -= 2;

        return i;
    }

    public static ushort StackPopu16() {
        var i = (ushort)(Stack[StackCurrent - 2] |
                         Stack[StackCurrent - 1] << 8);

        StackCurrent -= 2;

        return i;
    }

    public static sbyte StackPops8() {
        var i = (sbyte)(Stack[--StackCurrent]);

        return i;
    }

    public static byte StackPopu8() {
        var i = Stack[--StackCurrent];

        return i;
    }

    public static float StackPopfloat() {
        var i = StackPopu32();
        return *(float*)&i;
    }

    public static double StackPopdouble() {
        var i = StackPopu64();
        return *(double*)&i;
    }

    public static int ReadArgs32(int index, uint fp) {
        var start = (fp - FrameHeader);

        for (int i = index; i >= 0; --i) {
            var argSize = (uint)(Stack[fp]           |
                                 Stack[fp + 1] << 8  |
                                 Stack[fp + 2] << 16 |
                                 Stack[fp + 3] << 24);

            start -= argSize;
            fp    += 4;
        }

        var s = Stack[start + 0]       |
                Stack[start + 1] << 8  |
                Stack[start + 2] << 16 |
                Stack[start + 3] << 24;

        return s;
    }

    public static uint ReadArgSize(int index, uint fp) {
        var start = fp + index * 4;

        var s = (uint)(Stack[start + 0]       |
                       Stack[start + 1] << 8  |
                       Stack[start + 2] << 16 |
                       Stack[start + 3] << 24);

        return s;
    }

    public static void StackCopy(uint from, uint to, uint size) {
        for (uint i = 0; i < size; ++i) {
            Stack[to + i] = Stack[from + i];
        }
    }

    public static void StackPush(uint from, uint size) {
        for (uint i = 0; i < size; ++i) {
            Stack[StackCurrent + i] = Stack[from + i];
        }

        StackCurrent += size;
    }

    public static string BytecodeToString(byte[] bytes, int count) {
        var  sb = new StringBuilder();
        uint pc = 8;

        while (pc < count) {
            var opcode = ReadCode(bytes, ref pc);

            switch (opcode) {
                case func : {
                    sb.Append(".func");
                    var argCount = Readu32(bytes, ref pc);
                    sb.Append(' ');
                    sb.Append(argCount.ToString());
                    var retSize = Readu32(bytes, ref pc);
                    sb.Append(' ');
                    sb.Append(retSize.ToString());

                    for (int i = 0; i < argCount; ++i) {
                        sb.Append(' ');
                        sb.Append(Readu32(bytes, ref pc));
                    }
                } break;
                case call : {
                    sb.Append("call ");
                    var index = Reads32(bytes, ref pc);
                    sb.Append(index.ToString());
                } break;
                case add_s32 : {
                    sb.Append(opcode.ToString());
                } break;
                case ret : {
                    sb.Append(opcode.ToString());
                } break;
                case push_s32 : {
                    sb.Append(opcode.ToString());
                    sb.Append(' ');
                    var val = Reads32(bytes, ref pc);
                    sb.Append(val.ToString());
                } break;
                case larg_s32 : {
                    sb.Append(opcode.ToString());
                    sb.Append(' ');
                    var index = Readu32(bytes, ref pc);
                    sb.Append(index.ToString());
                } break;
                default : {
                    sb.Append("Unknown operation");
                    break;
                }
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }
}