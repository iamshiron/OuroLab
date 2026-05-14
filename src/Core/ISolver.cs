namespace Shiron.OuroLab.Core;

public interface ISolver {
    string Name { get; }
    SolverResult Solve(IGame game);
}
