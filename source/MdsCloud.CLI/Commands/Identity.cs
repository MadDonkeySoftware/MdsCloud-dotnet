using CommandDotNet;
using MdsCloud.CLI.Utilities;
using MdsCloud.SdkDotNet;
using MdsCloud.SdkDotNet.DTOs.Identity;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

// register, token, update
public class Identity : BaseMdsCommand
{
    public Identity(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console) { }

    private void CollectPasswordUpdateInfo(ref UpdateUserRequest payload)
    {
        var oldPass = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your old password:").Secret(null)
        );
        var newPass = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your desired password:").Secret(null)
        );
        var newPass2 = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Confirm your desired password:").Secret(null)
        );

        if (newPass != newPass2)
        {
            if (
                this.AnsiConsole.Confirm(
                    "Desired password and confirmation do not appear to match. Try again?"
                )
            )
            {
                CollectPasswordUpdateInfo(ref payload);
            }
        }
        else
        {
            payload.OldPassword = oldPass;
            payload.NewPassword = newPass;
        }
    }

    [Command("register")]
    public void Register() { }

    [Command("token")]
    public async void Token([Option("env")] string? environment = null)
    {
        var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
        MdsSdk.Initialize(env);

        var client = MdsSdk.GetIdentityServiceClient();
        var token = await client.Authenticate();
        this.AnsiConsole.WriteLine(token);
    }

    [Command("update")]
    public async void Update(
        [Option('p', "password")] bool? password = null,
        [Option('e', "email")] string? email = null,
        [Option('n', "name")] string? name = null,
        [Option("env")] string? environment = null
    )
    {
        var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
        MdsSdk.Initialize(env);
        var payload = new UpdateUserRequest();
        var client = MdsSdk.GetIdentityServiceClient();

        try
        {
            await client.GetPublicSignature();
        }
        catch (Exception ex)
        {
            // TODO: implement
        }

        if (password.HasValue && password.Value)
        {
            CollectPasswordUpdateInfo(ref payload);
        }

        if (email != null)
        {
            payload.Email = email;
        }

        if (name != null)
        {
            payload.FriendlyName = name;
        }

        if (payload.HasValue())
        {
            await client.UpdateUser(payload);
        }
    }
    /*
    */
}
