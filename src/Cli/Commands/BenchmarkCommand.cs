using Spectre.Console.Cli;

namespace Shiron.OuroLab.Cli.Commands;

internal sealed class BenchmarkCommand : AsyncCommand<BenchmarkCommand.Settings> {
    internal sealed class Settings : CommandSettings {
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken) {
        return Task.FromResult(0);
    }
}
