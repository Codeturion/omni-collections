using System;

namespace Omni.Collections.Core.Hashing;

/// <summary>
/// Factory for the built-in <see cref="IHasher{T}"/> implementations.
/// </summary>
/// <remarks>
/// Default hasher is XxHash3 (fast, well-distributed, not cryptographic). Murmur3-128 is offered as an
/// alternative for callers who specifically want a different hash family. Both produce 64-bit output suitable
/// for probabilistic structures' double-hashing schemes.
/// </remarks>
public static class Hashers
{
    /// <summary>
    /// Returns the recommended <see cref="IHasher{T}"/> for <typeparamref name="T"/>, backed by XxHash3.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// <typeparamref name="T"/> is not one of the built-in supported types
    /// (<c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>, <c>string</c>, <c>Guid</c>, <c>byte[]</c>,
    /// <c>ReadOnlyMemory&lt;byte&gt;</c>). Implement <see cref="IHasher{T}"/> directly for other types.
    /// </exception>
    public static IHasher<T> Default<T>() where T : notnull
    {
        if (typeof(T) == typeof(int)) return Cast<int, T>(XxHash3Int.Instance);
        if (typeof(T) == typeof(uint)) return Cast<uint, T>(XxHash3UInt.Instance);
        if (typeof(T) == typeof(long)) return Cast<long, T>(XxHash3Long.Instance);
        if (typeof(T) == typeof(ulong)) return Cast<ulong, T>(XxHash3ULong.Instance);
        if (typeof(T) == typeof(string)) return Cast<string, T>(XxHash3String.Instance);
        if (typeof(T) == typeof(Guid)) return Cast<Guid, T>(XxHash3Guid.Instance);
        if (typeof(T) == typeof(byte[])) return Cast<byte[], T>(XxHash3ByteArray.Instance);
        if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return Cast<ReadOnlyMemory<byte>, T>(XxHash3ReadOnlyMemoryByte.Instance);
        throw new NotSupportedException(BuildNotSupportedMessage(typeof(T)));
    }

    /// <summary>
    /// Returns a Murmur3-based <see cref="IHasher{T}"/> for <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// <typeparamref name="T"/> is not one of the built-in supported types. Implement <see cref="IHasher{T}"/> directly.
    /// </exception>
    public static IHasher<T> Murmur3<T>() where T : notnull
    {
        if (typeof(T) == typeof(int)) return Cast<int, T>(Murmur3Int.Instance);
        if (typeof(T) == typeof(uint)) return Cast<uint, T>(Murmur3UInt.Instance);
        if (typeof(T) == typeof(long)) return Cast<long, T>(Murmur3Long.Instance);
        if (typeof(T) == typeof(ulong)) return Cast<ulong, T>(Murmur3ULong.Instance);
        if (typeof(T) == typeof(string)) return Cast<string, T>(Murmur3String.Instance);
        if (typeof(T) == typeof(Guid)) return Cast<Guid, T>(Murmur3Guid.Instance);
        if (typeof(T) == typeof(byte[])) return Cast<byte[], T>(Murmur3ByteArray.Instance);
        if (typeof(T) == typeof(ReadOnlyMemory<byte>)) return Cast<ReadOnlyMemory<byte>, T>(Murmur3ReadOnlyMemoryByte.Instance);
        throw new NotSupportedException(BuildNotSupportedMessage(typeof(T)));
    }

    private static IHasher<TOut> Cast<TIn, TOut>(IHasher<TIn> hasher) => (IHasher<TOut>)(object)hasher;

    private static string BuildNotSupportedMessage(Type t) =>
        $"No built-in IHasher<{t.Name}>. Implement Omni.Collections.Core.Hashing.IHasher<{t.Name}> directly. " +
        "Built-in support: int, uint, long, ulong, string, Guid, byte[], ReadOnlyMemory<byte>.";
}
