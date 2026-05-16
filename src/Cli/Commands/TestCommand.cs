using System.Diagnostics;
using Shiron.OuroLab.Chest;
using Shiron.OuroLab.Core;
using Shiron.OuroLab.Quest;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shiron.OuroLab.Cli.Commands;

internal sealed class TestCommand : AsyncCommand<TestCommand.Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandOption("-g|--game")]
        public string? Game { get; set; }

        [CommandOption("-s|--solver")]
        public string[]? Solvers { get; set; }
    }

    private static readonly Dictionary<Sphere, string> Colors = new() {
        { Sphere.Purple, "mediumorchid" },
        { Sphere.Blue, "cornflowerblue" },
        { Sphere.Teal, "darkcyan" },
        { Sphere.Green, "green" },
        { Sphere.Yellow, "yellow" },
        { Sphere.Orange, "darkorange" },
        { Sphere.Red, "red" },
        { Sphere.Black, "white on black" },
        { Sphere.White, "white" },
        { Sphere.Rainbow, "magenta" },
    };

    private static readonly Sphere[] ChestSpheres =
        [Sphere.Blue, Sphere.Teal, Sphere.Green, Sphere.Yellow, Sphere.Orange, Sphere.Red];

    private static readonly Sphere[] QuestSpheres =
        [Sphere.Purple, Sphere.Blue, Sphere.Teal, Sphere.Green, Sphere.Yellow, Sphere.Orange];

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken) {
        var gameName = ResolveGame(settings.Game);
        if (gameName is null) return Task.FromResult(1);

        var prototype = Registry.Games[gameName]();

        AnsiConsole.Clear();

        var grid = gameName switch {
            "ouro-quest" => EditQuestGrid(prototype),
            _ => EditChestGrid(prototype),
        };

        if (grid is null) return Task.FromResult(1);

        var game = BuildGame(gameName, grid, prototype.MaxClicks);
        if (game is null) return Task.FromResult(1);

        var solverNames = ResolveSolvers(settings.Solvers, gameName);
        if (solverNames is null || solverNames.Length == 0) return Task.FromResult(1);

        var results = RunTests(game, solverNames);
        DisplayResults(game, results, gameName, grid);

        return Task.FromResult(0);
    }

    private static string? ResolveGame(string? game) {
        if (game is not null) {
            if (!Registry.Games.ContainsKey(game)) {
                AnsiConsole.MarkupLine($"[red]Unknown game: {game}[/]");
                AnsiConsole.MarkupLine($"[grey]Available: {string.Join(", ", Registry.Games.Keys)}[/]");
                return null;
            }

            return game;
        }

        if (Registry.Games.Count == 0) {
            AnsiConsole.MarkupLine("[red]No games registered.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]game[/] to test:")
                .AddChoices(Registry.Games.Keys)
        );
    }

    private static string[]? ResolveSolvers(string[]? solvers, string gameName) {
        if (solvers is not null) {
            var invalid = solvers.Where(s => !Registry.Solvers.ContainsKey(s)).ToList();
            if (invalid.Count > 0) {
                AnsiConsole.MarkupLine($"[red]Unknown solver(s): {string.Join(", ", invalid)}[/]");
                return null;
            }

            return solvers.Where(s => Registry.IsCompatible(gameName, s)).ToArray();
        }

        var compatible = Registry.GetCompatibleSolvers([gameName]).ToList();
        if (compatible.Count == 0) {
            AnsiConsole.MarkupLine("[red]No compatible solvers.[/]");
            return [];
        }

        return AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [yellow]solver(s)[/] to test:")
                .InstructionsText("[grey](Press <space> to select, <enter> to confirm)[/]")
                .AddChoices(compatible)
        ).ToArray();
    }

    private static Sphere[,]? EditChestGrid(IGame prototype) {
        var rows = prototype.Rows;
        var cols = prototype.Columns;
        var grid = new Sphere[rows, cols];
        var converter = prototype.ValueConverter;

        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                grid[r, c] = Sphere.Blue;

        AnsiConsole.MarkupLine("[bold]Ouro Chest — Grid Editor[/]");
        AnsiConsole.MarkupLine(
            "[grey]Enter row,col for each cell. Type the sphere name after (e.g. \"2,3 red\").[/]");
        AnsiConsole.MarkupLine("[grey]Type \"done\" when finished, \"show\" to display the grid.[/]");
        AnsiConsole.MarkupLine(
            $"[grey]Spheres: {string.Join(", ", ChestSpheres.Select(s => s.ToString()))}[/]");
        AnsiConsole.MarkupLine($"[grey]Grid: {rows}×{cols}[/]");
        AnsiConsole.WriteLine();

        while (true) {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]>[/]")
                    .AllowEmpty()
            ).Trim();

            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("done", StringComparison.OrdinalIgnoreCase)) {
                if (ValidateChestGrid(grid, rows, cols))
                    return grid;
                continue;
            }

            if (input.Equals("show", StringComparison.OrdinalIgnoreCase)) {
                RenderGrid(grid, rows, cols, converter);
                continue;
            }

            var parts = input.Split(' ', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2) {
                AnsiConsole.MarkupLine("[red]Format: row,col sphere[/]");
                continue;
            }

            var coords = parts[0].Split(',');
            if (coords.Length != 2 || !int.TryParse(coords[0], out var r) || !int.TryParse(coords[1], out var c)) {
                AnsiConsole.MarkupLine("[red]Invalid coordinates. Format: row,col[/]");
                continue;
            }

            if (r < 0 || r >= rows || c < 0 || c >= cols) {
                AnsiConsole.MarkupLine($"[red]Out of bounds. Grid is {rows}×{cols}.[/]");
                continue;
            }

            if (!Enum.TryParse<Sphere>(parts[1], true, out var sphere) ||
                !ChestSpheres.Contains(sphere)) {
                AnsiConsole.MarkupLine(
                    $"[red]Unknown sphere. Available: {string.Join(", ", ChestSpheres.Select(s => s.ToString()))}[/]");
                continue;
            }

            grid[r, c] = sphere;
            var color = Colors[sphere];
            AnsiConsole.MarkupLine($"  Set [{color}]{sphere}[/] at ({r},{c})");
        }
    }

    private static bool ValidateChestGrid(Sphere[,] grid, int rows, int cols) {
        var hasRed = false;
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                if (grid[r, c] == Sphere.Red)
                    hasRed = true;

        if (!hasRed) {
            AnsiConsole.MarkupLine("[red]Grid must contain at least one Red sphere.[/]");
            return false;
        }

        return true;
    }

    private static Sphere[,]? EditQuestGrid(IGame prototype) {
        var rows = prototype.Rows;
        var cols = prototype.Columns;
        var grid = new Sphere[rows, cols];
        var converter = prototype.ValueConverter;
        var purples = new HashSet<(int, int)>();

        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                grid[r, c] = Sphere.Blue;

        AnsiConsole.MarkupLine("[bold]Ouro Quest — Purple Placement[/]");
        AnsiConsole.MarkupLine("[grey]Enter row,col to place a purple sphere. Enter again to remove.[/]");
        AnsiConsole.MarkupLine("[grey]Need exactly 4 purples. Type \"done\" when finished.[/]");
        AnsiConsole.MarkupLine($"[grey]Grid: {rows}×{cols}[/]");
        AnsiConsole.WriteLine();

        while (true) {
            RenderQuestGrid(grid, rows, cols, converter, purples);
            AnsiConsole.MarkupLine($"[bold]Purples placed[/]: {purples.Count}/4");
            AnsiConsole.WriteLine();

            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]>[/]")
                    .AllowEmpty()
            ).Trim();

            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("done", StringComparison.OrdinalIgnoreCase)) {
                if (purples.Count == 4)
                    return grid;
                AnsiConsole.MarkupLine($"[red]Need exactly 4 purples (have {purples.Count}).[/]");
                continue;
            }

            var coords = input.Split(',');
            if (coords.Length != 2 || !int.TryParse(coords[0], out var r) ||
                !int.TryParse(coords[1], out var c)) {
                AnsiConsole.MarkupLine("[red]Format: row,col[/]");
                continue;
            }

            if (r < 0 || r >= rows || c < 0 || c >= cols) {
                AnsiConsole.MarkupLine($"[red]Out of bounds. Grid is {rows}×{cols}.[/]");
                continue;
            }

            if (purples.Contains((r, c))) {
                purples.Remove((r, c));
                grid[r, c] = Sphere.Blue;
                AnsiConsole.MarkupLine($"  Removed purple at ({r},{c})");
            } else {
                if (purples.Count >= 4) {
                    AnsiConsole.MarkupLine("[red]Already have 4 purples. Remove one first.[/]");
                    continue;
                }

                purples.Add((r, c));
                grid[r, c] = Sphere.Purple;
                AnsiConsole.MarkupLine($"  Placed [mediumorchid]Purple[/] at ({r},{c})");
            }
        }
    }

    private static void RenderGrid(Sphere[,] grid, int rows, int cols, IValueConverter converter) {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("");
        for (var c = 0; c < cols; c++)
            table.AddColumn($"[bold grey]{c}[/]");

        for (var r = 0; r < rows; r++) {
            var cells = new List<string> { $"[bold grey]{r}[/]" };
            for (var c = 0; c < cols; c++) {
                var s = grid[r, c];
                var color = Colors.GetValueOrDefault(s, "white");
                var value = converter.UsedSpheres.Contains(s) ? converter.GetValue(s) : 0;
                cells.Add($"[{color} bold]{s}[/]\n[grey]{value}pts[/]");
            }

            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static void RenderQuestGrid(Sphere[,] grid, int rows, int cols, IValueConverter converter,
        HashSet<(int, int)> purples) {
        var board = new Board(rows, cols);
        var purpleSet = new HashSet<int>();
        foreach (var (r, c) in purples) {
            board[r, c] = Sphere.Purple;
            purpleSet.Add(r * cols + c);
        }

        for (var i = 0; i < rows * cols; i++) {
            if (purpleSet.Contains(i)) continue;
            var (row, col) = board.ToPosition(i);
            var count = CountPurpleNeighbors(row, col, rows, cols, purpleSet);
            board[i] = CountToSphere(count);
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("");
        for (var c = 0; c < cols; c++)
            table.AddColumn($"[bold grey]{c}[/]");

        for (var r = 0; r < rows; r++) {
            var cells = new List<string> { $"[bold grey]{r}[/]" };
            for (var c = 0; c < cols; c++) {
                var s = board[r, c];
                var color = Colors.GetValueOrDefault(s, "white");
                var value = converter.UsedSpheres.Contains(s) ? converter.GetValue(s) : 0;
                var marker = purples.Contains((r, c)) ? " ★" : "";
                cells.Add($"[{color} bold]{s}[/]{marker}\n[grey]{value}pts[/]");
            }

            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static int CountPurpleNeighbors(int row, int col, int rows, int cols, HashSet<int> purpleSet) {
        var count = 0;
        for (var dr = -1; dr <= 1; dr++) {
            for (var dc = -1; dc <= 1; dc++) {
                if (dr == 0 && dc == 0) continue;
                var nr = row + dr;
                var nc = col + dc;
                if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                    if (purpleSet.Contains(nr * cols + nc))
                        count++;
            }
        }

        return count;
    }

    private static Sphere CountToSphere(int count) => count switch {
        0 => Sphere.Blue,
        1 => Sphere.Teal,
        2 => Sphere.Green,
        3 => Sphere.Yellow,
        _ => Sphere.Orange,
    };

    private static IGame? BuildGame(string gameName, Sphere[,] grid, int maxClicks) {
        var rows = grid.GetLength(0);
        var cols = grid.GetLength(1);

        return gameName switch {
            "ouro-chest" => BuildChestGame(grid, rows, cols, maxClicks),
            "ouro-quest" => BuildQuestGame(grid, rows, cols, maxClicks),
            _ => null,
        };
    }

    private static IGame BuildChestGame(Sphere[,] grid, int rows, int cols, int maxClicks) {
        var cells = new Sphere[rows * cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                cells[r * cols + c] = grid[r, c];

        var board = Board.FromArray(cells, rows, cols);
        return new OuroChestGame(board, maxClicks);
    }

    private static IGame? BuildQuestGame(Sphere[,] grid, int rows, int cols, int maxClicks) {
        var purpleIndices = new HashSet<int>();

        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                if (grid[r, c] == Sphere.Purple)
                    purpleIndices.Add(r * cols + c);

        if (purpleIndices.Count < 2) {
            AnsiConsole.MarkupLine("[red]Need at least 2 Purple spheres.[/]");
            return null;
        }

        var cells = new Sphere[rows * cols];
        foreach (var idx in purpleIndices)
            cells[idx] = Sphere.Purple;

        for (var i = 0; i < rows * cols; i++) {
            if (purpleIndices.Contains(i)) continue;
            var row = i / cols;
            var col = i % cols;
            cells[i] = CountToSphere(CountPurpleNeighbors(row, col, rows, cols, purpleIndices));
        }

        var board = Board.FromArray(cells, rows, cols);
        return new OuroQuestGame(board, maxClicks, purpleIndices);
    }

    private static List<(string Solver, SolverResult Result, TimeSpan Elapsed, bool GoalHit, int MaxScore)>
        RunTests(IGame game, string[] solverNames) {
        var results = new List<(string, SolverResult, TimeSpan, bool, int)>();

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Testing solvers...", ctx => {
                foreach (var solverName in solverNames) {
                    ctx.Status($"Testing [yellow]{solverName}[/]...");
                    var solver = Registry.Solvers[solverName]();
                    var testGame = game.Fork();
                    var maxScore = testGame.TheoreticalMaxScore;

                    var sw = Stopwatch.StartNew();
                    var result = solver.Solve(testGame);
                    sw.Stop();

                    var goalHit = testGame.GoalDescription is not null && testGame.GoalAchieved;
                    results.Add((solverName, result, sw.Elapsed, goalHit, maxScore));
                }
            });

        return results;
    }

    private static void DisplayResults(IGame game,
        List<(string Solver, SolverResult Result, TimeSpan Elapsed, bool GoalHit, int MaxScore)> results,
        string gameName, Sphere[,] grid) {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Target Grid[/]").Centered());
        AnsiConsole.WriteLine();

        if (gameName == "ouro-quest") {
            var purples = new HashSet<(int, int)>();
            for (var r = 0; r < grid.GetLength(0); r++)
                for (var c = 0; c < grid.GetLength(1); c++)
                    if (grid[r, c] == Sphere.Purple)
                        purples.Add((r, c));
            RenderQuestGrid(grid, grid.GetLength(0), grid.GetLength(1), game.ValueConverter, purples);
        } else {
            RenderGrid(grid, grid.GetLength(0), grid.GetLength(1), game.ValueConverter);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Game[/]: {gameName}");
        AnsiConsole.MarkupLine($"[bold]Theoretical Max Score[/]: {game.TheoreticalMaxScore}");
        if (game.GoalDescription is not null)
            AnsiConsole.MarkupLine($"[bold]Goal[/]: {game.GoalDescription}");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Test Results[/]").Centered());
        AnsiConsole.WriteLine();

        if (results.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No solvers tested.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Solver")
            .AddColumn("Score", c => c.RightAligned())
            .AddColumn("Max", c => c.RightAligned())
            .AddColumn("Efficiency", c => c.RightAligned())
            .AddColumn("Goal", c => c.Centered())
            .AddColumn("Verdict", c => c.Centered())
            .AddColumn("Time", c => c.RightAligned());

        foreach (var (solver, result, elapsed, goalHit, maxScore) in results) {
            var efficiency = maxScore > 0 ? (double) result.Score / maxScore * 100 : 0;
            var verdict = efficiency >= 80 ? "[green]PASS[/]"
                : efficiency >= 50 ? "[yellow]OKAY[/]"
                : "[red]FAIL[/]";
            var goalStr = game.GoalDescription is not null
                ? goalHit ? "[green]✓[/]" : "[red]✗[/]"
                : "[grey]—[/]";

            table.AddRow(
                solver,
                result.Score.ToString(),
                maxScore.ToString(),
                $"{efficiency:F1}%",
                goalStr,
                verdict,
                $"{elapsed.TotalMilliseconds:F1}ms"
            );
        }

        AnsiConsole.Write(table);
    }
}
