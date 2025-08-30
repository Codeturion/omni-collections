using System.Runtime.CompilerServices;

namespace Omni.Collections.Core;

/// <summary>
/// Mathematical and utility functions shared across Omni.Collections data structures.
/// </summary>
public static class CollectionUtils
{
    public static readonly int[] Primes =
    [
        17, 37, 79, 163, 331, 673, 1361, 2729, 5471, 10949, 21911, 43853, 87719, 175447, 350899,
        701819, 1403641, 2807303, 5614657, 11229331, 22458671, 44917381, 89834777, 179669557
    ];
    
    /// <summary>
    /// Gets the smallest prime number greater than or equal to the specified minimum value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPrime(int min)
    {
        foreach (var prime in Primes)
        {
            if (prime >= min)
                return prime;
        }
        return min | 1;
    }

    /// <summary>
    /// Calculates the next power of two greater than or equal to the specified value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNextPowerOfTwo(int n)
    {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
}