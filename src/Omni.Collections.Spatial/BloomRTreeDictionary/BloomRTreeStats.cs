namespace Omni.Collections.Spatial.BloomRTreeDictionary;

public readonly struct BloomRTreeStats
{
    public int TotalEntries { get; init; }

    public long SpatialQueries { get; init; }

    public long BloomFilterHits { get; init; }

    public double BloomFilterEffectiveness { get; init; }

    public long DictionaryLookups { get; init; }

    public int TreeHeight { get; init; }

    public override string ToString() =>
        $"Entries: {TotalEntries}, Queries: {SpatialQueries}, " +
        $"Bloom Effectiveness: {BloomFilterEffectiveness:P2}, Height: {TreeHeight}";
}