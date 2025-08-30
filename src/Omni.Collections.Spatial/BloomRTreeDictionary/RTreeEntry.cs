namespace Omni.Collections.Spatial.BloomRTreeDictionary;

sealed class RTreeEntry<TKey, TValue>
    where TKey : notnull
{
    public TKey Key { get; set; } = default!;
    public TValue Value { get; set; } = default!;
    public BoundingRectangle Bounds { get; set; }

    public RTreeEntry() { }

    public RTreeEntry(TKey key, TValue value, BoundingRectangle bounds)
    {
        Key = key;
        Value = value;
        Bounds = bounds;
    }

    public override string ToString() => $"{Key}: {Bounds}";
}