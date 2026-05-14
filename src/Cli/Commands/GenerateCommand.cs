using Shiron.OuroLab.Core;
using Shiron.OuroLab.Chest;
using Shiron.OuroLab.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shiron.OuroLab.Cli.Commands;

internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandOption("-g|--game")]
        public string? Game { get; set; }

        [CommandOption("-s|--seed")]
        public int? Seed { get; set; }
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings,
        CancellationToken cancellationToken) {
        var gameName = settings.Game ?? PromptGame();
        if (gameName is null) return Task.FromResult(1);

        var game = Registry.Games[gameName]();
        if (settings.Seed.HasValue && game is OuroChestGame)
            game = new OuroChestGame(seed: settings.Seed.Value);

        game.NewGame();
        RenderBoard(game);

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

    private static void RenderBoard(IGame game) {
        var chest = (OuroChestGame) game;
        var converter = game.ValueConverter;

        var panel = new Panel("".ToString());
        var grid = new Grid();

        for (var col = 0; col <= game.Columns; col++)
            grid.AddColumn();

        var header = new List<string> { "" };
        for (var col = 0; col < game.Columns; col++)
            header.Add($"[grey]{col}[/]");
        grid.AddRow(header.ToArray());

        for (var row = 0; row < game.Rows; row++) {
            var cells = new List<string> { $"[grey]{row}[/]" };
            for (var col = 0; col < game.Columns; col++) {
                var sphere = chest.PeekSphere(row, col);
                var name = sphere.ToString();
                var color = SphereColors.GetValueOrDefault(sphere, "white");
                var value = converter.UsedSpheres.Contains(sphere) ? converter.GetValue(sphere) : 0;
                cells.Add($"[{color}]{name}[/] [grey]({value}pts)[/]");
            }

            grid.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Theoretical Max Score[/]: {game.TheoreticalMaxScore}");
        if (game.GoalDescription is not null)
            AnsiConsole.MarkupLine($"[bold]Goal[/]: {game.GoalDescription}");
    }
}
