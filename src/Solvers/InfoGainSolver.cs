using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class InfoGainSolver : ISolver {
    public string Name => "Info Gain";

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;

        while (!game.IsSolved) {
            var goalSphere = game.GoalSphere;
            var hunting = goalSphere.HasValue && !game.GoalAchieved;

            var bestIndex = hunting
                ? FindBestInfoGain(game, size, goalSphere!.Value)
                : FindBestEV(game, size);

            if (bestIndex < 0) break;

            var sphere = game.Reveal(bestIndex);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }

    private static int FindBestInfoGain(IGame game, int size, Sphere goal) {
        var bestIndex = -1;
        var bestScore = double.MaxValue;

        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;

            var nonGoalSqSum = 0.0;
            foreach (var (s, prob) in possible) {
                if (s != goal)
                    nonGoalSqSum += prob * prob;
            }

            if (nonGoalSqSum < bestScore) {
                bestScore = nonGoalSqSum;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static int FindBestEV(IGame game, int size) {
        var bestIndex = -1;
        var bestEV = double.MinValue;

        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;

            var ev = 0.0;
            foreach (var (s, prob) in possible)
                ev += prob * game.ValueConverter.GetValue(s);

            if (ev > bestEV) {
                bestEV = ev;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
