using Shiron.OuroLab.Chest;
using Shiron.OuroLab.Cli;
using Shiron.OuroLab.Cli.Commands;
using Shiron.OuroLab.Solvers;
using Spectre.Console.Cli;

Registry.RegisterGame("ouro-chest", () => new OuroChestGame(), "greedy-ev", "goal-hunter", "info-gain", "random", "expectimax");
Registry.RegisterSolver("greedy-ev", () => new GreedyEVSolver());
Registry.RegisterSolver("goal-hunter", () => new GoalHunterSolver());
Registry.RegisterSolver("info-gain", () => new InfoGainSolver());
Registry.RegisterSolver("random", () => new RandomSolver());
Registry.RegisterSolver("expectimax", () => new ExpectimaxSolver());

var app = new CommandApp();
app.Configure(c => {
    c.SetApplicationName("ourolab");
    c.SetApplicationVersion("0.0.0");

    c.AddCommand<BenchmarkCommand>("benchmark");
    c.AddCommand<GenerateCommand>("generate");
});

await app.RunAsync(args);
