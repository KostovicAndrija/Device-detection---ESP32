namespace Application.Localization;

/// <summary>Each signal type contributes at most one presence per time bucket.</summary>
public static class DeskRiskCalculator
{
    public static (string? SignalType, int ActiveBuckets, double Frequency, int Score, string Level) Calculate(
        IEnumerable<(string SignalType, long Bucket)> hits, int totalBuckets, DeskRiskOptions criteria)
    {
        var strongest = hits.GroupBy(x => x.SignalType, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Type = g.Key, Count = g.Select(x => x.Bucket).Distinct().Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Type, StringComparer.Ordinal).FirstOrDefault();
        var active = strongest?.Count ?? 0;
        var frequency = Math.Min(1, active / (double)Math.Max(1, totalBuckets));
        var score = (int)Math.Round(100 * frequency);
        // Compare the unrounded value so rounding cannot promote a desk across a threshold.
        var level = 100 * frequency >= criteria.CriticalThreshold ? "critical" :
            100 * frequency >= criteria.WarningThreshold ? "warning" : "normal";
        return (strongest?.Type, active, frequency, score, level);
    }
}
