using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Omni.Collections.Benchmarks.Common;
using Omni.Collections.Hybrid.GraphDictionary;

namespace Omni.Collections.Benchmarks.Hybrid;

// GraphDictionary has no direct BCL analog (graph + dict in one structure).
// Benchmarks are absolute, not ratio-based. Categories used to keep grouping
// consistent with the rest of the suite.
[BenchmarkCategory(Categories.Hybrid)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
public class GraphDictionaryBenchmarks
{
    private const int OpsPerIteration = 32768;

    [Params(Sizes.Small, Sizes.Medium, Sizes.Large)]
    public int N;

    private string[] _keys = null!;
    private int[] _values = null!;
    private int[] _readIndices = null!;
    private const int ReadIndexMask = 1023;
    private int _readCounter;

    private GraphDictionary<string, int> _omniFilled = null!;
    private GraphDictionary<string, int> _omniMut = null!;
    private int _addCounter;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = RandomData.Strings(N + OpsPerIteration);
        _values = RandomData.Ints(N + OpsPerIteration);
        _readIndices = RandomData.IntsInRange(ReadIndexMask + 1, 0, N);

        _omniFilled = new GraphDictionary<string, int>();
        for (int i = 0; i < N; i++)
            _omniFilled.Add(_keys[i], _values[i]);
        // Build a sparse graph: each node connects to next 3 nodes.
        for (int i = 0; i < N; i++)
        {
            for (int j = 1; j <= 3 && i + j < N; j++)
                _omniFilled.AddEdge(_keys[i], _keys[i + j]);
        }
    }

    [IterationSetup(Targets = new[] { nameof(Omni_AddNode) })]
    public void ResetForAdd()
    {
        _omniMut = new GraphDictionary<string, int>();
        for (int i = 0; i < N; i++)
            _omniMut.Add(_keys[i], _values[i]);
        _addCounter = N;
    }

    /// Claim: GraphDictionary.Add (node only, no edges) is competitive with hash-table insertion.
    [Benchmark, BenchmarkCategory("Add"), InvocationCount(OpsPerIteration)]
    public bool Omni_AddNode()
    {
        _omniMut.Add(_keys[_addCounter], _values[_addCounter]);
        _addCounter++;
        return true;
    }

    /// Claim: GraphDictionary.GetNeighbors retrieves adjacency in O(degree).
    [Benchmark, BenchmarkCategory("Neighbors")]
    public int Omni_GetNeighbors()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        int count = 0;
        foreach (var _ in _omniFilled.GetNeighbors(k))
            count++;
        return count;
    }

    /// Claim: GraphDictionary.TryGetValue lookup matches Dictionary on cost.
    [Benchmark, BenchmarkCategory("Lookup")]
    public bool Omni_Lookup()
    {
        var k = _keys[_readIndices[_readCounter++ & ReadIndexMask]];
        return _omniFilled.TryGetValue(k, out _);
    }

    /// Claim: GraphDictionary fill with edges produces a graph; allocation reflects nodes + edge bookkeeping.
    [Benchmark, BenchmarkCategory("Fill"), InvocationCount(1)]
    public GraphDictionary<string, int> Omni_Fill()
    {
        var c = new GraphDictionary<string, int>();
        for (int i = 0; i < N; i++)
            c.Add(_keys[i], _values[i]);
        for (int i = 0; i < N; i++)
        {
            for (int j = 1; j <= 3 && i + j < N; j++)
                c.AddEdge(_keys[i], _keys[i + j]);
        }
        return c;
    }
}
