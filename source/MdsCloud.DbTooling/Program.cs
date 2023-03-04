// See https://aka.ms/new-console-template for more information

// https://commanddotnet.bilal-fazlani.com/gettingstarted/getting-started-0/

using CommandDotNet;

namespace MdsCloud.DbTooling;

public class Program
{
    static int Main(string[] args)
    {
        return new AppRunner<Program>().Run(args);
    }

    [Subcommand(RenameAs = "migrations")]
    public Commands.Migrations? Migrations { get; set; }
}
