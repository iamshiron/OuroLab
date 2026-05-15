using Shiron.OuroLab.Core;
using Shiron.OuroLab.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shiron.OuroLab.Cli.Commands;

internal sealed class PlayCommand : AsyncCommand<PlayCommand.Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandOption("-g|--game")]
        public string? Game { get; set; }
    }

    private static readonly Dictionary<Sphere, string> SphereColors = new()
    {
        { Sphere.Purple, "mediumorchid" },
        { Sphere.Blue, "blue" },
        { Sphere.Teal, "darkcyan" },
        { Sphere.Green, "green" },
        { Sphere.Yellow, "yellow" },
        { Sphere.Orange, "darkorange" },
        { Sphere.Red, "red" },
        { Sphere.Black, "black" },
        { Sphere.White, "white" },
        { Sphere.Rainbow, "magenta" },
    };

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken) {
        var gameName = settings.Game ?? PromptGame();
        if (gameName is null) return Task.FromResult(1);

        var game = Registry.Games[gameName]();
        game.NewGame();

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold]OuroLab — Play Mode[/]");
        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to quit.[/]");
        AnsiConsole.WriteLine();

        if (game.GoalDescription is not null)
            AnsiConsole.MarkupLine($"[bold]Goal[/]: {game.GoalDescription}");

        while (!game.IsSolved) {
            RenderBoard(game);
            RenderStatus(game);

            var index = PromptCell(game);
            if (index < 0) break;

            var sphere = game.Reveal(index);
            var value = game.ValueConverter.GetValue(sphere);
            var color = SphereColors.GetValueOrDefault(sphere, "white");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Revealed: [{color}]{sphere}[/] [grey]({value}pts)[/]");
            AnsiConsole.WriteLine();
        }

        RenderBoard(game);
        RenderFinalStatus(game);

        return Task.FromResult(0);
    }

    private static string? PromptGame() {
        if (Registry.Games.Count == 0) {
            AnsiConsole.MarkupLine("[red]No games registered.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]game[/]:")
                .AddChoices(Registry.Games.Keys)
        );
    }

    private static int PromptCell(IGame game) {
        var row = AnsiConsole.Prompt(
            new TextPrompt<int>($"Enter [green]row[/] (0-{game.Rows - 1}):")
                .Validate(r => r >= 0 && r < game.Rows,
                    $"Row must be between 0 and {game.Rows - 1}.")
        );

        var col = AnsiConsole.Prompt(
            new TextPrompt<int>($"Enter [green]column[/] (0-{game.Columns - 1}):")
                .Validate(c => c >= 0 && c < game.Columns,
                    $"Column must be between 0 and {game.Columns - 1}.")
        );

        var index = row * game.Columns + col;

        if (game.IsRevealed(index)) {
            AnsiConsole.MarkupLine("[red]That cell is already revealed. Pick another.[/]");
            return PromptCell(game);
        }

        return index;
    }

    private static void RenderBoard(IGame game) {
        var grid = new Grid();
        for (var col = 0; col <= game.Columns; col++)
            grid.AddColumn();

        var header = new List<string> { "" };
        for (var col = 0; col < game.Columns; col++)
            header.Add($"[bold grey]{col}[/]");
        grid.AddRow(header.ToArray());

        for (var row = 0; row < game.Rows; row++) {
            var cells = new List<string> { $"[bold grey]{row}[/]" };
            for (var col = 0; col < game.Columns; col++) {
                var index = row * game.Columns + col;
                if (game.IsVisible(index)) {
                    var sphere = game.GetRevealedSphere(index);
                    var value = game.ValueConverter.GetValue(sphere);
                    var color = SphereColors.GetValueOrDefault(sphere, "white");
                    if (game.IsRevealed(index)) {
                        cells.Add($"[{color} bold]{sphere}[/]\n[grey]{value}pts[/]");
                    } else {
                        cells.Add($"[{color} bold]{sphere}[/]\n[grey]{value}pts ★[/]");
                    }
                } else {
                    cells.Add("[grey dim]???[/]");
                }
            }

            grid.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(new Panel(grid).Header("[bold]Board[/]"));
        AnsiConsole.WriteLine();
    }

    private static void RenderStatus(IGame game) {
        var remaining = game.MaxClicks - game.RevealedCount;
        var efficiency = game.TheoreticalMaxScore > 0
            ? (double) game.Score / game.TheoreticalMaxScore * 100.0
            : 0;

        var status = new Grid();
        status.AddColumn();
        status.AddColumn();
        status.AddColumn();

        status.AddRow(
            $"[bold]Score[/]: {game.Score} / {game.TheoreticalMaxScore}",
            $"[bold]Remaining[/]: {remaining} click{(remaining != 1 ? "s" : "")}",
            $"[bold]Efficiency[/]: {efficiency:F1}%"
        );

        if (game.GoalAchieved)
            status.AddRow("[bold green]Goal achieved![/]", "", "");

        AnsiConsole.Write(status);
        AnsiConsole.WriteLine();
    }

    private static void RenderFinalStatus(IGame game) {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Game Over[/]").Centered());
        AnsiConsole.WriteLine();

        var efficiency = game.TheoreticalMaxScore > 0
            ? (double) game.Score / game.TheoreticalMaxScore * 100.0
            : 0;

        AnsiConsole.MarkupLine($"  [bold]Final Score[/]:      {game.Score} / {game.TheoreticalMaxScore}");
        AnsiConsole.MarkupLine($"  [bold]Efficiency[/]:        {efficiency:F1}%");

        if (game.GoalDescription is not null)
            AnsiConsole.MarkupLine($"  [bold]Goal[/]:              {(game.GoalAchieved ? "[green]Achieved[/]" : "[red]Missed[/]")}");

        AnsiConsole.WriteLine();
    }
}
