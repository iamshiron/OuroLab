using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class GoalHunterSolver : ISolver {
    public string Name => "Goal Hunter";

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;

        while (!game.IsSolved) {
            var goalSphere = game.GoalSphere;
            var hunting = goalSphere.HasValue && !game.GoalAchieved;
            var bestIndex = FindBest(game, size, hunting ? goalSphere!.Value : null);

            if (bestIndex < 0 && hunting)
                bestIndex = FindBest(game, size, null);

            if (bestIndex < 0) break;

            var sphere = game.Reveal(bestIndex);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }

    private static int FindBest(IGame game, int size, Sphere? requireSphere) {
        var bestIndex = -1;
        var bestEV = double.MinValue;

        for (var i = 0; i < size; i++) {
            if (game.IsRevealed(i)) continue;

            var possible = game.GetPossibleSpheres(i);
            if (possible.Count == 0) continue;
            if (requireSphere.HasValue && !possible.ContainsKey(requireSphere.Value)) continue;

            var ev = 0.0;
            foreach (var (sphere, prob) in possible)
                ev += prob * game.ValueConverter.GetValue(sphere);

            if (ev > bestEV) {
                bestEV = ev;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
