namespace Omni.Collections.Benchmarks.Core;

/// <summary>
/// Benchmark operation categories for result grouping
/// </summary>
public enum BenchmarkCategory
{
    Core,           // Add, Get, Remove, Contains, Enumerate, Clear
    Bulk,           // AddRange, RemoveRange, GetRange
    Specialized,    // Implementation-specific operations
    Memory,         // Memory usage and allocation patterns
    Scaling         // Big-O validation across different data sizes
}