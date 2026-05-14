using Shiron.OuroLab.Core;

namespace Shiron.OuroLab.Cli;

public static class Registry {
    private static readonly Dictionary<string, Func<IGame>> _games = new();
    private static readonly Dictionary<string, Func<ISolver>> _solvers = new();
    private static readonly Dictionary<string, HashSet<string>> _compatibility = new();

    public static IReadOnlyDictionary<string, Func<IGame>> Games => _games;
    public static IReadOnlyDictionary<string, Func<ISolver>> Solvers => _solvers;

    public static void RegisterGame(string name, Func<IGame> factory, params string[] compatibleSolvers) {
        _games[name] = factory;
        _compatibility[name] = new HashSet<string>(compatibleSolvers);
    }

    public static void RegisterSolver(string name, Func<ISolver> factory) {
        _solvers[name] = factory;
    }

    public static bool IsCompatible(string game, string solver)
        => _compatibility.TryGetValue(game, out var solvers) && solvers.Contains(solver);

    public static IEnumerable<string> GetCompatibleSolvers(IEnumerable<string> gameNames)
        => gameNames.SelectMany(gn => _compatibility.GetValueOrDefault(gn) ?? []).Distinct();
}
