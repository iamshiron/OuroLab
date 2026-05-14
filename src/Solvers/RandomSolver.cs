using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Solvers;

public sealed class RandomSolver : ISolver {
    private readonly Random _random;
    public string Name => "Random";

    public RandomSolver(int? seed = null) {
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    public SolverResult Solve(IGame game) {
        var size = game.Rows * game.Columns;
        var score = 0;
        var reveals = 0;
        var unrevealed = Enumerable.Range(0, size).ToList();

        while (!game.IsSolved) {
            if (unrevealed.Count == 0) break;

            var pick = _random.Next(unrevealed.Count);
            var index = unrevealed[pick];
            unrevealed.RemoveAt(pick);

            var sphere = game.Reveal(index);
            score += game.ValueConverter.GetValue(sphere);
            reveals++;
        }

        return new SolverResult(score, reveals, game.IsSolved);
    }
}
