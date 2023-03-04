using CommandDotNet;
using MdsCloud.CLI.Utilities;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

// register, token, update
public class Identity : BaseMdsCommand
{
    public Identity(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console) { }

    [Command("register")]
    public void Register() { }

    [Command("token")]
    public void Token([Option("env")] string? environment = null)
    {
        var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
    }

    [Command("update")]
    public void Update() { }
    /*
    */
}
