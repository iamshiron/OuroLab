using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class GreedyEVLookaheadSolver : ISolver {
    private readonly int _lookahead;
    public string Name => _lookahead == 0 ? "Greedy EV" : $"Greedy EV-{_lookahead}";

    public GreedyEVLookaheadSolver(int lookahead) {
        _lookahead = lookahead;
    }

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;

        while (!game.IsSolved) {
            var bestIndex = FindBest(game, size);
            if (bestIndex < 0) break;

            var sphere = game.Reveal(bestIndex);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }

    private int FindBest(IGame game, int size) {
        var bestIndex = -1;
        var bestEV = double.MinValue;

        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;

            var ev = ComputeLookaheadEV(game, i, _lookahead);
            if (ev > bestEV) {
                bestEV = ev;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private double ComputeLookaheadEV(IGame game, int index, int depth) {
        var outcomes = game.GetPossibleSpheres(index);
        var expected = 0.0;

        foreach (var (sphere, prob) in outcomes) {
            var value = (double) game.ValueConverter.GetValue(sphere);

            if (depth > 0) {
                var fork = game.Fork();
                fork.ApplyHypothetical(index, sphere);
                if (!fork.IsSolved) {
                    value += BestGreedyEV(fork, depth - 1);
                }
            }

            expected += prob * value;
        }

        return expected;
    }

    private static double BestGreedyEV(IGame game, int depth) {
        var size = game.Rows * game.Columns;
        var best = double.MinValue;

        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;

            var ev = 0.0;
            foreach (var (s, prob) in possible) {
                var value = (double) game.ValueConverter.GetValue(s);

                if (depth > 0) {
                    var fork = game.Fork();
                    fork.ApplyHypothetical(i, s);
                    if (!fork.IsSolved) {
                        value += BestGreedyEV(fork, depth - 1);
                    }
                }

                ev += prob * value;
            }

            if (ev > best) best = ev;
        }

        return best == double.MinValue ? 0 : best;
    }
}
