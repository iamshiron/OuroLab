namespace Shiron.OuroLab.Core;

public sealed record BenchmarkResult(
    string SolverName,
    string GameName,
    int Iterations,
    double AverageScore,
    double AverageReveals,
    TimeSpan AverageTime,
    TimeSpan TotalTime,
    IReadOnlyList<SolverResult> Results
);
