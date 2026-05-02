using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Omni.Collections.Core.Hashing;

internal sealed class XxHash3Int : IHasher<int>
{
    public static readonly XxHash3Int Instance = new();
    private XxHash3Int() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in int value, ulong seed)
    {
        int v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}

internal sealed class XxHash3UInt : IHasher<uint>
{
    public static readonly XxHash3UInt Instance = new();
    private XxHash3UInt() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in uint value, ulong seed)
    {
        uint v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}

internal sealed class XxHash3Long : IHasher<long>
{
    public static readonly XxHash3Long Instance = new();
    private XxHash3Long() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in long value, ulong seed)
    {
        long v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}

internal sealed class XxHash3ULong : IHasher<ulong>
{
    public static readonly XxHash3ULong Instance = new();
    private XxHash3ULong() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in ulong value, ulong seed)
    {
        ulong v = value;
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref v, 1));
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}

internal sealed class XxHash3String : IHasher<string>
{
    public static readonly XxHash3String Instance = new();
    private XxHash3String() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in string value, ulong seed)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value.AsSpan());
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}

internal sealed class XxHash3Guid : IHasher<Guid>
{
    public static readonly XxHash3Guid Instance = new();
    private XxHash3Guid() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in Guid value, ulong seed)
    {
        Span<byte> buf = stackalloc byte[16];
        value.TryWriteBytes(buf);
        return XxHash3.HashToUInt64(buf, unchecked((long)seed));
    }
}

internal sealed class XxHash3ByteArray : IHasher<byte[]>
{
    public static readonly XxHash3ByteArray Instance = new();
    private XxHash3ByteArray() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in byte[] value, ulong seed)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return XxHash3.HashToUInt64(value, unchecked((long)seed));
    }
}

internal sealed class XxHash3ReadOnlyMemoryByte : IHasher<ReadOnlyMemory<byte>>
{
    public static readonly XxHash3ReadOnlyMemoryByte Instance = new();
    private XxHash3ReadOnlyMemoryByte() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in ReadOnlyMemory<byte> value, ulong seed)
    {
        return XxHash3.HashToUInt64(value.Span, unchecked((long)seed));
    }
}
