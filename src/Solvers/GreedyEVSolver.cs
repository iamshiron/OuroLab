using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class GreedyEVSolver : ISolver {
    public string Name => "Greedy EV";

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;

        while (!game.IsSolved) {
            var bestIndex = -1;
            var bestEV = double.MinValue;

            for (var i = 0; i < size; i++) {
                if (game.IsRevealed(i)) continue;

                var possible = game.GetPossibleSpheres(i);
                if (possible.Count == 0) continue;

                var ev = 0.0;
                foreach (var s in possible)
                    ev += game.ValueConverter.GetValue(s);
                ev /= possible.Count;

                if (ev > bestEV) {
                    bestEV = ev;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0) break;

            var sphere = game.Reveal(bestIndex);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }
}
