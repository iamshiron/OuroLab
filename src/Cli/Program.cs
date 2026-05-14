using Shiron.OuroLab.Chest;
using Shiron.OuroLab.Cli;
using Shiron.OuroLab.Cli.Commands;
using Shiron.OuroLab.Solvers;
using Spectre.Console.Cli;

Registry.RegisterGame("ouro-chest", () => new OuroChestGame(), "greedy-ev");
Registry.RegisterSolver("greedy-ev", () => new GreedyEVSolver());

var app = new CommandApp();
app.Configure(c => {
    c.SetApplicationName("ourolab");
    c.SetApplicationVersion("0.0.0");

    c.AddCommand<BenchmarkCommand>("benchmark");
});

await app.RunAsync(args);
