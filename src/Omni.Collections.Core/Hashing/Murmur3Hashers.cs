using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Omni.Collections.Core.Hashing;

internal sealed class Murmur3Int : IHasher<int>
{
    public static readonly Murmur3Int Instance = new();
    private Murmur3Int() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in int value, ulong seed)
    {
        int v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return Murmur3.Hash64(bytes, seed);
    }
}

internal sealed class Murmur3UInt : IHasher<uint>
{
    public static readonly Murmur3UInt Instance = new();
    private Murmur3UInt() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in uint value, ulong seed)
    {
        uint v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return Murmur3.Hash64(bytes, seed);
    }
}

internal sealed class Murmur3Long : IHasher<long>
{
    public static readonly Murmur3Long Instance = new();
    private Murmur3Long() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in long value, ulong seed)
    {
        long v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return Murmur3.Hash64(bytes, seed);
    }
}

internal sealed class Murmur3ULong : IHasher<ulong>
{
    public static readonly Murmur3ULong Instance = new();
    private Murmur3ULong() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in ulong value, ulong seed)
    {
        ulong v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return Murmur3.Hash64(bytes, seed);
    }
}

internal sealed class Murmur3String : IHasher<string>
{
    public static readonly Murmur3String Instance = new();
    private Murmur3String() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in string value, ulong seed)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value.AsSpan());
        return Murmur3.Hash64(bytes, seed);
    }
}

internal sealed class Murmur3Guid : IHasher<Guid>
{
    public static readonly Murmur3Guid Instance = new();
    private Murmur3Guid() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in Guid value, ulong seed)
    {
        Span<byte> buf = stackalloc byte[16];
        value.TryWriteBytes(buf);
        return Murmur3.Hash64(buf, seed);
    }
}

internal sealed class Murmur3ByteArray : IHasher<byte[]>
{
    public static readonly Murmur3ByteArray Instance = new();
    private Murmur3ByteArray() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in byte[] value, ulong seed)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return Murmur3.Hash64(value, seed);
    }
}

internal sealed class Murmur3ReadOnlyMemoryByte : IHasher<ReadOnlyMemory<byte>>
{
    public static readonly Murmur3ReadOnlyMemoryByte Instance = new();
    private Murmur3ReadOnlyMemoryByte() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in ReadOnlyMemory<byte> value, ulong seed)
    {
        return Murmur3.Hash64(value.Span, seed);
    }
}
