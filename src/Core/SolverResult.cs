namespace Shiron.OuroLab.Core;

public sealed record SolverResult(
    int Score,
    int Reveals,
    bool Solved,
    TimeSpan Elapsed = default
);
