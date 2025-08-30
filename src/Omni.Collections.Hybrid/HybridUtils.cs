using Omni.Collections.Core;
using System.Runtime.CompilerServices;

namespace Omni.Collections.Hybrid;

/// <summary>
/// Hybrid-specific utility functions that delegate to Core utilities.
/// </summary>
public static class HybridUtils
{
    /// <summary>
    /// Gets the smallest prime number greater than or equal to the specified minimum value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPrime(int min) => CollectionUtils.GetPrime(min);

    /// <summary>
    /// Calculates the next power of two greater than or equal to the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNextPowerOfTwo(int n) => CollectionUtils.GetNextPowerOfTwo(n);
}