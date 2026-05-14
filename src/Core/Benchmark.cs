using System.Diagnostics;

namespace Shiron.OuroLab.Core;

public sealed class Benchmark {
    public required Func<IGame> GameFactory { get; init; }
    public required ISolver Solver { get; init; }
    public required int Iterations { get; init; }

    public BenchmarkResult Run() {
        var results = new SolverResult[Iterations];
        var timings = new TimeSpan[Iterations];
        var totalSw = Stopwatch.StartNew();

        string? gameName = null;

        for (var i = 0; i < Iterations; i++) {
            var game = GameFactory();
            game.NewGame();
            gameName ??= game.Name;

            var sw = Stopwatch.StartNew();
            var result = Solver.Solve(game);
            sw.Stop();

            results[i] = result with { Elapsed = sw.Elapsed };
        }

        totalSw.Stop();

        return new BenchmarkResult(
            SolverName: Solver.Name,
            GameName: gameName ?? "Unknown",
            Iterations: Iterations,
            AverageScore: results.Length > 0 ? results.Average(r => r.Score) : 0,
            AverageReveals: results.Length > 0 ? results.Average(r => r.Reveals) : 0,
            AverageTime: timings.Length > 0
                ? TimeSpan.FromTicks((long) results.Average(r => r.Elapsed.Ticks))
                : TimeSpan.Zero,
            TotalTime: totalSw.Elapsed,
            Results: results.AsReadOnly()
        );
    }
}
