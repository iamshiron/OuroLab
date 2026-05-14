using Shiron.OuroLab.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shiron.OuroLab.Cli.Commands;

internal sealed class BenchmarkCommand : AsyncCommand<BenchmarkCommand.Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandOption("-g|--game")]
        public string[]? Games { get; set; }

        [CommandOption("-s|--solver")]
        public string[]? Solvers { get; set; }

        [CommandOption("-n|--iterations")]
        public int Iterations { get; set; } = 100;

        [CommandOption("-t|--threads")]
        public int Threads { get; set; } = 8;
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken) {
        if (settings.Iterations <= 0) {
            AnsiConsole.MarkupLine("[red]Iterations must be positive.[/]");
            return Task.FromResult(1);
        }

        if (settings.Threads <= 0) {
            AnsiConsole.MarkupLine("[red]Threads must be positive.[/]");
            return Task.FromResult(1);
        }

        var selectedGames = ResolveGames(settings.Games);
        if (selectedGames is null) return Task.FromResult(1);

        var selectedSolvers = ResolveSolvers(settings.Solvers, selectedGames);
        if (selectedSolvers is null) return Task.FromResult(1);

        if (selectedGames.Length == 0) {
            AnsiConsole.MarkupLine("[red]No games selected.[/]");
            return Task.FromResult(1);
        }

        if (selectedSolvers.Length == 0) {
            AnsiConsole.MarkupLine("[red]No solvers selected.[/]");
            return Task.FromResult(1);
        }

        var results = RunBenchmarks(selectedGames, selectedSolvers, settings.Iterations, settings.Threads);
        DisplayResults(results);

        return Task.FromResult(0);
    }

    private static string[]? ResolveGames(string[]? games) {
        if (games is not null) {
            var invalid = games.Where(g => !Registry.Games.ContainsKey(g)).ToList();
            if (invalid.Count > 0) {
                AnsiConsole.MarkupLine($"[red]Unknown game(s): {string.Join(", ", invalid)}[/]");
                AnsiConsole.MarkupLine($"[grey]Available: {string.Join(", ", Registry.Games.Keys)}[/]");
                return null;
            }

            return games;
        }

        return AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]game(s)[/] to benchmark:")
                .NotRequired()
                .InstructionsText("[grey](Press <space> to select, <enter> to confirm)[/]")
                .AddChoices(Registry.Games.Keys)
        ).ToArray();
    }

    private static string[]? ResolveSolvers(string[]? solvers, string[] games) {
        if (solvers is not null) {
            var invalid = solvers.Where(s => !Registry.Solvers.ContainsKey(s)).ToList();
            if (invalid.Count > 0) {
                AnsiConsole.MarkupLine($"[red]Unknown solver(s): {string.Join(", ", invalid)}[/]");
                AnsiConsole.MarkupLine($"[grey]Available: {string.Join(", ", Registry.Solvers.Keys)}[/]");
                return null;
            }

            var incompatible = solvers
                .Where(s => !games.Any(g => Registry.IsCompatible(g, s)))
                .ToList();
            if (incompatible.Count > 0) {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning: solver(s) incompatible with all selected games: {string.Join(", ", incompatible)}[/]");
            }

            return solvers;
        }

        var compatible = Registry.GetCompatibleSolvers(games).ToList();
        if (compatible.Count == 0) {
            AnsiConsole.MarkupLine("[red]No compatible solvers for the selected game(s).[/]");
            return [];
        }

        return AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [yellow]solver(s)[/] to benchmark:")
                .NotRequired()
                .InstructionsText("[grey](Press <space> to select, <enter> to confirm)[/]")
                .AddChoices(compatible)
        ).ToArray();
    }

    private static List<(string Game, string Solver, BenchmarkResult Result)> RunBenchmarks(
        string[] games, string[] solvers, int iterations, int threads) {
        var results = new List<(string, string, BenchmarkResult)>();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Running benchmarks...", ctx => {
                foreach (var gameName in games) {
                    foreach (var solverName in solvers) {
                        if (!Registry.IsCompatible(gameName, solverName)) continue;

                        ctx.Status($"Running [green]{gameName}[/] + [yellow]{solverName}[/] ([grey]{iterations} iterations, {threads} thread(s)[/])...");

                        var benchmark = new Benchmark {
                            GameFactory = Registry.Games[gameName],
                            SolverFactory = Registry.Solvers[solverName],
                            Iterations = iterations,
                            MaxDegreeOfParallelism = threads,
                        };

                        results.Add((gameName, solverName, benchmark.Run()));
                    }
                }
            });

        return results;
    }

    private static void DisplayResults(List<(string Game, string Solver, BenchmarkResult Result)> results) {
        if (results.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No benchmarks were run.[/]");
            return;
        }

        var table = new Table()
            .Title("Benchmark Results")
            .Border(TableBorder.Rounded)
            .AddColumn("Game")
            .AddColumn("Solver")
            .AddColumn("Iterations", c => c.RightAligned())
            .AddColumn("Avg Score", c => c.RightAligned())
            .AddColumn("Avg Eff.", c => c.RightAligned())
            .AddColumn("Best Score", c => c.RightAligned())
            .AddColumn("Best Eff.", c => c.RightAligned())
            .AddColumn("Goal Hit Rate", c => c.RightAligned())
            .AddColumn("Avg Time", c => c.RightAligned())
            .AddColumn("Total Time", c => c.RightAligned());

        foreach (var (game, solver, result) in results) {
            table.AddRow(
                game,
                solver,
                result.Iterations.ToString(),
                $"{result.AverageScore:F2}",
                $"{result.AverageEfficiency:F1}%",
                result.BestScore.ToString(),
                $"{result.BestEfficiency:F1}%",
                result.GoalHitRate.HasValue ? $"{result.GoalHitRate.Value:F1}%" : "N/A",
                $"{result.AverageTime.TotalMilliseconds:F2}ms",
                $"{result.TotalTime.TotalMilliseconds:F2}ms"
            );
        }

        AnsiConsole.Write(table);
    }
}
