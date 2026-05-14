namespace Shiron.OuroLab.Core;

public sealed record BenchmarkResult(
    string SolverName,
    string GameName,
    int Iterations,
    double AverageScore,
    double AverageEfficiency,
    int BestScore,
    double BestEfficiency,
    double AverageReveals,
    double? GoalHitRate,
    TimeSpan AverageTime,
    TimeSpan TotalTime,
    IReadOnlyList<SolverResult> Results
);
