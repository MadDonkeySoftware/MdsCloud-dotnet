using MdsCloud.CLI.Utilities;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

public abstract class BaseMdsCommand
{
    protected IConfigUtilities ConfigUtilities { get; }
    protected IAnsiConsole AnsiConsole { get; }

    protected BaseMdsCommand(IConfigUtilities configUtilities, IAnsiConsole console)
    {
        ConfigUtilities = configUtilities;
        AnsiConsole = console;
    }
}
