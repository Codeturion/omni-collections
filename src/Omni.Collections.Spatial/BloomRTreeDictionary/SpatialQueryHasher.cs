using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Omni.Collections.Core.Hashing;

namespace Omni.Collections.Spatial.BloomRTreeDictionary;

/// <summary>
/// IHasher implementation for <see cref="SpatialQuery"/>. Hashes the struct's raw byte representation via XxHash3.
/// Internal because <see cref="SpatialQuery"/> is only used inside <see cref="BloomRTreeDictionary{TKey, TValue}"/>'s
/// negative-result cache.
/// </summary>
internal sealed class SpatialQueryHasher : IHasher<SpatialQuery>
{
    public static readonly SpatialQueryHasher Instance = new();

    private SpatialQueryHasher() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Hash(in SpatialQuery value, ulong seed)
    {
        ref SpatialQuery r = ref Unsafe.AsRef(in value);
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref r, 1));
        return XxHash3.HashToUInt64(bytes, unchecked((long)seed));
    }
}
