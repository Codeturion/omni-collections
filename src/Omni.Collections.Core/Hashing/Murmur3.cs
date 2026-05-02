using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Core.Hashing;

/// <summary>
/// Murmur3 x64_128 algorithm core. Returns the low 64-bit lane of the 128-bit output, which is sufficient
/// for probabilistic-structure use cases that consume 64-bit hashes.
/// </summary>
internal static class Murmur3
{
    private const ulong C1 = 0x87c37b91114253d5UL;
    private const ulong C2 = 0x4cf5ad432745937fUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash64(ReadOnlySpan<byte> data, ulong seed)
    {
        ulong h1 = seed;
        ulong h2 = seed;
        int len = data.Length;
        int blockCount = len >> 4;

        for (int i = 0; i < blockCount; i++)
        {
            int offset = i << 4;
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset, 8));
            ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset + 8, 8));

            k1 *= C1; k1 = RotateLeft(k1, 31); k1 *= C2; h1 ^= k1;
            h1 = RotateLeft(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729UL;

            k2 *= C2; k2 = RotateLeft(k2, 33); k2 *= C1; h2 ^= k2;
            h2 = RotateLeft(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5UL;
        }

        int tail = len & 15;
        if (tail != 0)
        {
            int tailOffset = blockCount << 4;
            ulong t1 = 0;
            ulong t2 = 0;
            switch (tail)
            {
                case 15: t2 ^= (ulong)data[tailOffset + 14] << 48; goto case 14;
                case 14: t2 ^= (ulong)data[tailOffset + 13] << 40; goto case 13;
                case 13: t2 ^= (ulong)data[tailOffset + 12] << 32; goto case 12;
                case 12: t2 ^= (ulong)data[tailOffset + 11] << 24; goto case 11;
                case 11: t2 ^= (ulong)data[tailOffset + 10] << 16; goto case 10;
                case 10: t2 ^= (ulong)data[tailOffset + 9] << 8; goto case 9;
                case 9: t2 ^= (ulong)data[tailOffset + 8]; goto case 8;
                case 8: t1 ^= (ulong)data[tailOffset + 7] << 56; goto case 7;
                case 7: t1 ^= (ulong)data[tailOffset + 6] << 48; goto case 6;
                case 6: t1 ^= (ulong)data[tailOffset + 5] << 40; goto case 5;
                case 5: t1 ^= (ulong)data[tailOffset + 4] << 32; goto case 4;
                case 4: t1 ^= (ulong)data[tailOffset + 3] << 24; goto case 3;
                case 3: t1 ^= (ulong)data[tailOffset + 2] << 16; goto case 2;
                case 2: t1 ^= (ulong)data[tailOffset + 1] << 8; goto case 1;
                case 1: t1 ^= data[tailOffset]; break;
            }
            if (tail > 8)
            {
                t2 *= C2; t2 = RotateLeft(t2, 33); t2 *= C1; h2 ^= t2;
            }
            t1 *= C1; t1 = RotateLeft(t1, 31); t1 *= C2; h1 ^= t1;
        }

        h1 ^= (ulong)len;
        h2 ^= (ulong)len;
        h1 += h2;
        h2 += h1;
        h1 = FMix64(h1);
        h2 = FMix64(h2);
        h1 += h2;
        return h1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RotateLeft(ulong x, int n) => (x << n) | (x >> (64 - n));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FMix64(ulong k)
    {
        k ^= k >> 33;
        k *= 0xff51afd7ed558ccdUL;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53UL;
        k ^= k >> 33;
        return k;
    }
}
