#if !NET5_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System.Numerics;

internal static class BitOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong value)
    {
        ulong v = value;
        v -= (v >> 1) & 0x5555555555555555UL;
        v = (v & 0x3333333333333333UL) + ((v >> 2) & 0x3333333333333333UL);
        v = (v + (v >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        return (int)((v * 0x0101010101010101UL) >> 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeroCount(uint value)
    {
        if (value == 0) return 32;
        int count = 0;
        if ((value & 0xFFFF0000u) == 0) { count += 16; value <<= 16; }
        if ((value & 0xFF000000u) == 0) { count += 8; value <<= 8; }
        if ((value & 0xF0000000u) == 0) { count += 4; value <<= 4; }
        if ((value & 0xC0000000u) == 0) { count += 2; value <<= 2; }
        if ((value & 0x80000000u) == 0) { count += 1; }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeroCount(ulong value)
    {
        uint hi = (uint)(value >> 32);
        return hi != 0 ? LeadingZeroCount(hi) : 32 + LeadingZeroCount((uint)value);
    }
}
#endif
