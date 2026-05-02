using System;

namespace Omni.Collections.Core.Hashing;

/// <summary>
/// Allocation-free 64-bit hash function for probabilistic data structures.
/// Implementations must be deterministic per <c>(value, seed)</c> pair and well-distributed across the 64-bit output space.
/// </summary>
/// <remarks>
/// <para>
/// Use this in place of <see cref="object.GetHashCode"/> for any structure whose correctness depends on hash quality —
/// Bloom filters, HyperLogLog, CountMinSketch, etc. <c>GetHashCode</c> is 32-bit, may be process-randomized for strings,
/// and provides no seed parameter, which makes it unsuitable for the math underlying probabilistic structures.
/// </para>
/// <para>
/// The <c>seed</c> parameter on <see cref="Hash"/> lets callers derive multiple independent hash families from one hasher
/// (per-row seeds in CountMinSketch, defense against adversarial inputs in Bloom families). Implementations must mix
/// the seed thoroughly enough that distinct seeds yield uncorrelated outputs.
/// </para>
/// <para>
/// Use <see cref="Hashers.Default{T}"/> for the recommended XxHash3-based hasher of common types
/// (<c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>, <c>string</c>, <c>Guid</c>, <c>byte[]</c>,
/// <c>ReadOnlyMemory&lt;byte&gt;</c>). For custom types, implement this interface directly.
/// </para>
/// </remarks>
public interface IHasher<T>
{
    /// <summary>
    /// Computes a 64-bit hash of <paramref name="value"/> using the supplied <paramref name="seed"/>.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="seed">A 64-bit seed. Distinct seeds must yield uncorrelated hashes.</param>
    /// <returns>A well-distributed 64-bit hash.</returns>
    ulong Hash(in T value, ulong seed);
}
