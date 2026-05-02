using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Omni.Collections.Probabilistic;

namespace Omni.Collections.Tests.Probabilistic;

/// <summary>
/// Property-based tests verifying that the IHasher-based probabilistic types satisfy their
/// theoretical accuracy guarantees. Each test runs with multiple FsCheck-generated seeds.
/// </summary>
public class ProbabilisticPropertyTests
{
    private const double DesignFpr = 0.01;
    private const int InsertCount = 5000;
    private const int NegativeProbeCount = 5000;

    [Property(MaxTest = 50)]
    public Property BloomFilter_FalsePositiveRate_StaysWithinTheoreticalBound()
    {
        return Prop.ForAll(Arb.From<int>(), seed =>
        {
            var rng = new System.Random(seed);
            var bloom = new BloomFilter<long>(InsertCount, DesignFpr);
            var inserted = new HashSet<long>();
            while (inserted.Count < InsertCount)
            {
                long x = NextLong(rng);
                if (inserted.Add(x))
                {
                    bloom.Add(x);
                }
            }

            int falsePositives = 0;
            int probes = 0;
            while (probes < NegativeProbeCount)
            {
                long x = NextLong(rng);
                if (inserted.Contains(x)) continue;
                if (bloom.Contains(x)) falsePositives++;
                probes++;
            }

            double empiricalFpr = (double)falsePositives / NegativeProbeCount;
            // Bound = 2 × design FPR. Probes=5000 at p=0.01: μ=50, σ≈7, 2× ≈ +7σ.
            // Per-trial false-fail probability < 1e-6; safe at MaxTest=50.
            return (empiricalFpr <= DesignFpr * 2.0)
                .Label($"seed={seed} empiricalFpr={empiricalFpr:F4} designFpr={DesignFpr}");
        });
    }

    [Property(MaxTest = 50)]
    public Property HyperLogLog_CardinalityEstimate_WithinFiveStandardErrors()
    {
        return Prop.ForAll(Arb.From<int>(), seed =>
        {
            const int distinctItems = 50_000;
            var rng = new System.Random(seed);
            var hll = new HyperLogLog<long>(bucketBits: 12);
            var seen = new HashSet<long>();
            while (seen.Count < distinctItems)
            {
                long x = NextLong(rng);
                if (seen.Add(x))
                {
                    hll.Add(x);
                }
            }

            long estimate = hll.EstimateCardinality();
            // Standard error for HLL with 2^12 = 4096 buckets is ~1.625%.
            // 5 SE ≈ 8.1% — comfortably above any honest run.
            double maxError = distinctItems * (5.0 * hll.StandardError);
            double actualError = Math.Abs(estimate - distinctItems);
            return (actualError <= maxError)
                .Label($"seed={seed} estimate={estimate} actual={distinctItems} error={actualError:F0} bound={maxError:F0}");
        });
    }

    [Property(MaxTest = 50)]
    public Property CountMinSketch_EstimateNeverUnderCounts()
    {
        return Prop.ForAll(Arb.From<int>(), seed =>
        {
            const int distinctItems = 1000;
            const uint perItemCount = 5;
            var rng = new System.Random(seed);
            var cms = new CountMinSketch<long>(width: 4096, depth: 5);
            var trueCounts = new Dictionary<long, uint>();
            for (int i = 0; i < distinctItems; i++)
            {
                long x = NextLong(rng);
                cms.Add(x, perItemCount);
                trueCounts[x] = trueCounts.TryGetValue(x, out var c) ? c + perItemCount : perItemCount;
            }

            // CMS is biased upward — estimate ≥ true count for every item, by construction.
            foreach (var (item, trueCount) in trueCounts)
            {
                uint estimate = cms.EstimateCount(item);
                if (estimate < trueCount)
                {
                    return Prop.OfTestable(false)
                        .Label($"seed={seed} item={item} trueCount={trueCount} estimate={estimate}");
                }
            }
            return Prop.OfTestable(true);
        });
    }

    [Property(MaxTest = 30)]
    public Property CountMinSketch_DifferentSeeds_ProduceDifferentTables()
    {
        // Defends against the old Random(42) bug: two CMS instances with different base seeds
        // must allocate distinct hash families, so per-item estimates diverge on collision-noise probes.
        return Prop.ForAll(Arb.From<int>(), entropy =>
        {
            var rng = new System.Random(entropy);
            var cmsA = new CountMinSketch<long>(width: 256, depth: 3, hasher: Omni.Collections.Core.Hashing.Hashers.Default<long>(), seed: 1UL);
            var cmsB = new CountMinSketch<long>(width: 256, depth: 3, hasher: Omni.Collections.Core.Hashing.Hashers.Default<long>(), seed: 2UL);
            var items = Enumerable.Range(0, 200).Select(_ => NextLong(rng)).ToArray();
            foreach (var x in items)
            {
                cmsA.Add(x);
                cmsB.Add(x);
            }
            // Probe items NOT inserted — their estimates reflect collision noise, which must differ across seeds.
            int diverged = 0;
            for (int i = 0; i < 200; i++)
            {
                long probe = NextLong(rng);
                if (cmsA.EstimateCount(probe) != cmsB.EstimateCount(probe)) diverged++;
            }
            // Width=256, depth=3, 200 inserts → ~75% collision rate. With distinct seeds,
            // ≥30/200 probes should diverge. Threshold catches a regression that collapses
            // divergence (e.g., the previous Random(42) shared-seed bug).
            return (diverged >= 30).Label($"entropy={entropy} diverged={diverged}/200");
        });
    }

    private static long NextLong(System.Random rng)
    {
        Span<byte> buf = stackalloc byte[8];
        rng.NextBytes(buf);
        return BitConverter.ToInt64(buf);
    }
}
