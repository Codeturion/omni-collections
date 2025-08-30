namespace Omni.Collections.Hybrid.PredictiveDictionary;

public readonly struct PredictionStats
{
    public readonly int TotalPredictions;
    public readonly int SuccessfulPredictions;
    public readonly double HitRate;
    public readonly int PatternsLearned;
    public readonly double AverageConfidence;
    public readonly long MemoryUsageBytes;
    public PredictionStats(int total, int successful, int patterns, double avgConfidence, long memory)
    {
        TotalPredictions = total;
        SuccessfulPredictions = successful;
        HitRate = total > 0 ? (double)successful / total : 0.0;
        PatternsLearned = patterns;
        AverageConfidence = avgConfidence;
        MemoryUsageBytes = memory;
    }
}