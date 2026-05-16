using System.Collections.Concurrent;
using System.Diagnostics;

namespace Shiron.OuroLab.Core;

public sealed class Benchmark {
    public required Func<IGame> GameFactory { get; init; }
    public required Func<ISolver> SolverFactory { get; init; }
    public required int Iterations { get; init; }
    public int MaxDegreeOfParallelism { get; init; } = 1;
    public Action<int>? OnProgress { get; init; }

    public BenchmarkResult Run() {
        var results = new SolverResult[Iterations];
        var gameNames = new ConcurrentBag<string>();
        var totalSw = Stopwatch.StartNew();
        var completed = 0;

        var options = new ParallelOptions {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
        };

        Parallel.For(0, Iterations, options, i => {
            var game = GameFactory();
            game.NewGame();
            gameNames.Add(game.Name);

            var sw = Stopwatch.StartNew();
            var result = SolverFactory().Solve(game);
            sw.Stop();

            var goalHit = game.GoalDescription is not null ? game.GoalAchieved : (bool?) null;
            var maxScore = game.TheoreticalMaxScore;
            results[i] = result with {
                Elapsed = sw.Elapsed,
                GoalHit = goalHit,
                TheoreticalMaxScore = maxScore,
            };

            if (OnProgress is not null) {
                var count = Interlocked.Increment(ref completed);
                OnProgress(count);
            }
        });

        totalSw.Stop();

        var hasGoal = results.Any(r => r.GoalHit.HasValue);
        var goalHitRate = hasGoal
            ? results.Count(r => r.GoalHit == true) / (double) results.Length * 100.0
            : (double?) null;

        var bestResult = results.Length > 0 ? results.MaxBy(r => r.Score) : null;
        var efficiencies = results
            .Where(r => r.TheoreticalMaxScore > 0)
            .Select(r => (double) r.Score / r.TheoreticalMaxScore!.Value * 100.0)
            .ToList();

        return new BenchmarkResult(
            SolverName: SolverFactory().Name,
            GameName: gameNames.FirstOrDefault() ?? "Unknown",
            Iterations: Iterations,
            AverageScore: results.Length > 0 ? results.Average(r => r.Score) : 0,
            AverageEfficiency: efficiencies.Count > 0 ? efficiencies.Average() : 0,
            BestScore: bestResult?.Score ?? 0,
            BestEfficiency: bestResult is not null && bestResult.TheoreticalMaxScore > 0
                ? (double) bestResult.Score / bestResult.TheoreticalMaxScore.Value * 100.0
                : 0,
            AverageReveals: results.Length > 0 ? results.Average(r => r.Reveals) : 0,
            GoalHitRate: goalHitRate,
            AverageTime: results.Length > 0
                ? TimeSpan.FromTicks((long) results.Average(r => (double) r.Elapsed.Ticks))
                : TimeSpan.Zero,
            TotalTime: totalSw.Elapsed,
            Results: results.AsReadOnly()
        );
    }
}
