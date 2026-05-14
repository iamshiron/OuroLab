namespace Shiron.OuroLab.Core;

public sealed record SolverResult(
    int Score,
    int Reveals,
    bool Solved,
    bool? GoalHit = null,
    int? TheoreticalMaxScore = null,
    TimeSpan Elapsed = default
);
