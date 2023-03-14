// See https://aka.ms/new-console-template for more information

// https://commanddotnet.bilal-fazlani.com/gettingstarted/getting-started-0/

using Autofac;
using CommandDotNet;
using CommandDotNet.Help;
using CommandDotNet.IoC.Autofac;
using MadDonkeySoftware.SystemWrappers.Runtime;
using MdsCloud.CLI.Commands;
using MdsCloud.SdkDotNet.Utils;
using Spectre.Console;

namespace MdsCloud.CLI;

public class Program
{
    private static IContainer SetupIoc()
    {
        var ioc = new ContainerBuilder();

        // Commands / Sub-Commands
        ioc.RegisterType<Config>();
        ioc.RegisterType<Identity>();
        ioc.RegisterType<CloudInABox>();

        // Utilities
        ioc.RegisterType<ConfigUtilities>().As<IConfigUtilities>().SingleInstance();

        // Spectre.Console
        ioc.Register(
                ctx =>
                    AnsiConsole.Create(
                        new AnsiConsoleSettings
                        {
                            Ansi = AnsiSupport.Detect,
                            ColorSystem = ColorSystemSupport.Detect,
                            Out = new AnsiConsoleOutput(Console.Out),
                        }
                    )
            )
            .As<IAnsiConsole>();

        // SystemWrappers
        ioc.RegisterType<EnvironmentWrapper>()
            .As<MadDonkeySoftware.SystemWrappers.Runtime.IEnvironment>();

        return ioc.Build();
    }

    static int Main(string[] args)
    {
        var appRunner = new AppRunner<Program>(
            new AppSettings { Help = new AppHelpSettings { UsageAppName = "mds", } }
        );
        appRunner.UseDefaultMiddleware();

        appRunner.UseAutofac(SetupIoc());
        return appRunner.Run(args);
        // config
        // config inspect
        // config wizard
        // config write
        // decrypt
        // encrypt
        // env
        // env list
        // env print
        // env set
        // fs
        // fs containers
        // fs create
        // fs createPath
        // fs delete
        // fs download
        // fs list
        // fs upload
        // id
        // id register
        // id token
        // id update
        // ns
        // ns emit
        // ns watch
        // qs
        // qs create
        // qs delete
        // qs details
        // qs enqueueMessage
        // qs length
        // qs list
        // qs update
        // setup
        // sf
        // sf create
        // sf delete
        // sf details
        // sf invoke
        // sf list
        // sf update
        // sm
        // sm create
        // sm delete
        // sm details
        // sm execution
        // sm invoke
        // sm list
        // sm update
    }

    [Subcommand(RenameAs = "config")]
    public Commands.Config? Config { get; set; }

    [Subcommand(RenameAs = "id")]
    public Commands.Identity? Identity { get; set; }

    [Subcommand(RenameAs = "stack")]
    public Commands.CloudInABox? CloudInABox { get; set; }
}
