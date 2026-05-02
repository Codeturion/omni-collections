using System;

namespace Omni.Collections.Benchmarks.Common;

/// <summary>
/// Deterministic seeded random data, generated once and reused across iterations.
///
/// Critical: do NOT call Random.Next inside a [Benchmark] method. The atomic
/// operation under Random.Shared and the range-check overhead dwarf the cost
/// of any sub-microsecond operation being measured. Generate the data in
/// [GlobalSetup], cycle through it via a precomputed array.
/// </summary>
internal static class RandomData
{
    public const int Seed = 0x0FF1CE;

    public static int[] Ints(int count, int seed = Seed)
    {
        var rng = new Random(seed);
        var arr = new int[count];
        for (int i = 0; i < count; i++)
            arr[i] = rng.Next();
        return arr;
    }

    public static int[] IntsInRange(int count, int min, int max, int seed = Seed)
    {
        var rng = new Random(seed);
        var arr = new int[count];
        for (int i = 0; i < count; i++)
            arr[i] = rng.Next(min, max);
        return arr;
    }

    public static string[] Strings(int count, int length = 16, int seed = Seed)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rng = new Random(seed);
        var arr = new string[count];
        var buf = new char[length];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < length; j++)
                buf[j] = alphabet[rng.Next(alphabet.Length)];
            arr[i] = new string(buf);
        }
        return arr;
    }

    public static (float x, float y)[] Points2D(int count, float min = -1000f, float max = 1000f, int seed = Seed)
    {
        var rng = new Random(seed);
        var arr = new (float, float)[count];
        var range = max - min;
        for (int i = 0; i < count; i++)
        {
            arr[i] = (
                (float)(rng.NextDouble() * range + min),
                (float)(rng.NextDouble() * range + min));
        }
        return arr;
    }

    public static (float x, float y, float z)[] Points3D(int count, float min = -1000f, float max = 1000f, int seed = Seed)
    {
        var rng = new Random(seed);
        var arr = new (float, float, float)[count];
        var range = max - min;
        for (int i = 0; i < count; i++)
        {
            arr[i] = (
                (float)(rng.NextDouble() * range + min),
                (float)(rng.NextDouble() * range + min),
                (float)(rng.NextDouble() * range + min));
        }
        return arr;
    }
}
