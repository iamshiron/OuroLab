using Shiron.OuroLab.Chest;
using Shiron.OuroLab.Cli;
using Shiron.OuroLab.Cli.Commands;
using Shiron.OuroLab.Quest;
using Shiron.OuroLab.Solvers;
using Spectre.Console.Cli;

Registry.RegisterGame("ouro-chest", () => new OuroChestGame(), "greedy-ev", "greedy-ev-1", "greedy-ev-2", "goal-hunter", "info-gain", "random", "expectimax", "cached-expectimax");
Registry.RegisterGame("ouro-quest", () => new OuroQuestGame(), "greedy-ev", "greedy-ev-1", "greedy-ev-2", "goal-hunter", "info-gain", "random", "expectimax", "cached-expectimax");
Registry.RegisterSolver("greedy-ev", () => new GreedyEVLookaheadSolver(0));
Registry.RegisterSolver("greedy-ev-1", () => new GreedyEVLookaheadSolver(1));
Registry.RegisterSolver("greedy-ev-2", () => new GreedyEVLookaheadSolver(2));
Registry.RegisterSolver("goal-hunter", () => new GoalHunterSolver());
Registry.RegisterSolver("info-gain", () => new InfoGainSolver());
Registry.RegisterSolver("random", () => new RandomSolver());
Registry.RegisterSolver("expectimax", () => new ExpectimaxSolver());
Registry.RegisterSolver("cached-expectimax", () => new CachedExpectimaxSolver());

var app = new CommandApp();
app.Configure(c => {
    c.SetApplicationName("ourolab");
    c.SetApplicationVersion("0.0.0");

    c.AddCommand<BenchmarkCommand>("benchmark");
    c.AddCommand<GenerateCommand>("generate");
    c.AddCommand<PlayCommand>("play");
    c.AddCommand<TestCommand>("test");
});

await app.RunAsync(args);
