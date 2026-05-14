using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class ExpectimaxSolver : ISolver {
    private readonly int _maxDepth;
    public string Name => $"Expectimax-{_maxDepth}";

    public ExpectimaxSolver(int maxDepth = 3) {
        _maxDepth = maxDepth;
    }

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;
        var maxValue = GetMaxValue(game);

        while (!game.IsSolved) {
            var bestIndex = FindBest(game, size, maxValue);
            if (bestIndex < 0) break;

            var sphere = game.Reveal(bestIndex);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }

    private int FindBest(IGame game, int size, int maxValue) {
        var candidates = GetCandidatesByEV(game, size);
        if (candidates.Count == 0) return -1;

        var bestIndex = -1;
        var alpha = double.MinValue;

        foreach (var (index, _, maxOutcome) in candidates) {
            var remaining = game.MaxClicks - game.RevealedCount;
            var upperBound = maxOutcome + (remaining - 1) * maxValue;
            if (upperBound <= alpha) continue;

            var expected = ExpectedValue(game, index, _maxDepth, maxValue);
            if (expected > alpha) {
                alpha = expected;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static double ExpectedValue(IGame game, int index, int depth, int maxValue) {
        var outcomes = game.GetPossibleSpheres(index);
        var expected = 0.0;

        foreach (var (sphere, prob) in outcomes) {
            var value = (double) game.ValueConverter.GetValue(sphere);
            if (depth > 1) {
                var fork = game.Fork();
                fork.ApplyHypothetical(index, sphere);
                value += BestExpected(fork, depth - 1, maxValue);
            }
            expected += prob * value;
        }

        return expected;
    }

    private static double BestExpected(IGame game, int depth, int maxValue) {
        if (game.IsSolved || depth <= 0) return 0;

        var size = game.Rows * game.Columns;
        var candidates = GetCandidatesByEV(game, size);
        if (candidates.Count == 0) return 0;

        var best = double.MinValue;

        foreach (var (index, _, maxOutcome) in candidates) {
            var remaining = game.MaxClicks - game.RevealedCount;
            var upperBound = maxOutcome + (remaining - 1) * maxValue;
            if (upperBound <= best) continue;

            var expected = ExpectedValue(game, index, depth, maxValue);
            if (expected > best) best = expected;
        }

        return best == double.MinValue ? 0 : best;
    }

    private static int GetMaxValue(IGame game) {
        var max = 0;
        foreach (var s in game.ValueConverter.UsedSpheres) {
            var v = game.ValueConverter.GetValue(s);
            if (v > max) max = v;
        }
        return max;
    }

    private static List<(int Index, double EV, int MaxOutcome)> GetCandidatesByEV(IGame game, int size) {
        var candidates = new List<(int, double, int)>();
        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;

            var ev = 0.0;
            var maxOutcome = 0;
            foreach (var (s, prob) in possible) {
                var val = game.ValueConverter.GetValue(s);
                ev += prob * val;
                if (val > maxOutcome) maxOutcome = val;
            }

            candidates.Add((i, ev, maxOutcome));
        }

        candidates.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        return candidates;
    }
}
