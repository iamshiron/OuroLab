using Shiron.OuroLab.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(c => {
    c.SetApplicationName("ourolab");
    c.SetApplicationVersion("0.0.0");

    c.AddCommand<BenchmarkCommand>("benchmark");
});

await app.RunAsync(args);
