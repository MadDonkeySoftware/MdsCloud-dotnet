using CommandDotNet;
using MdsCloud.SdkDotNet;
using MdsCloud.SdkDotNet.DTOs.Identity;
using MdsCloud.SdkDotNet.Utils;
using Spectre.Console;

namespace MdsCloud.CLI.Commands;

public class Identity : BaseMdsCommand
{
    public Identity(IConfigUtilities configUtilities, IAnsiConsole console)
        : base(configUtilities, console) { }

    private string? CollectNewPassword()
    {
        var newPass = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your desired password:").Secret(null)
        );
        var newPass2 = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Confirm your desired password:").Secret(null)
        );

        if (newPass == newPass2)
            return newPass;

        return this.AnsiConsole.Confirm(
            "Desired password and confirmation do not appear to match. Try again?"
        )
            ? CollectNewPassword()
            : null;
    }

    private void CollectPasswordUpdateInfo(ref UpdateUserRequest payload)
    {
        var oldPass = this.AnsiConsole.Prompt(
            new TextPrompt<string>("Enter your old password:").Secret(null)
        );

        var newPass = CollectNewPassword();

        payload.OldPassword = newPass == null ? null : oldPass;
        payload.NewPassword = newPass;
    }

    private void CollectRegistrationInfo(ref RegisterRequest payload)
    {
        string StringPrompt(string queryPrompt, string failurePrompt) =>
            this.AnsiConsole
                .Prompt(
                    new TextPrompt<string>(queryPrompt)
                        .ValidationErrorMessage(failurePrompt)
                        .Validate(
                            value =>
                                string.IsNullOrEmpty(value.Trim())
                                    ? ValidationResult.Error()
                                    : ValidationResult.Success()
                        )
                )
                .Trim();

        var friendlyName = StringPrompt(
            "What should I call you?",
            "Friendly name appears to be blank."
        );
        var userId = StringPrompt(
            "Enter your desired user name:",
            "Authentication user name appears to be blank."
        );
        var email = StringPrompt(
            "Enter your account recovery email address:",
            "Recovery email address appears to be blank."
        );
        var password = CollectNewPassword();
        var accountName = StringPrompt(
            "Enter a friendly name for your account",
            "Account name appears to be blank."
        );

        payload.FriendlyName = friendlyName;
        payload.UserId = userId;
        payload.Email = email;
        payload.Password = password;
        payload.AccountName = accountName;
    }

    [Command("register")]
    public async void Register([Option("env")] string? environment = null)
    {
        var env = environment ?? this.ConfigUtilities.GetDefaultEnvironment();
        MdsSdk.Initialize(env);

        var client = MdsSdk.GetIdentityServiceClient();

        var args = new RegisterRequest();
        CollectRegistrationInfo(ref args);

        var table = new Table();
        table.AddColumns("", "");
        table.Border(TableBorder.None);
        table.ShowHeaders = false;

        foreach (var propertyInfo in typeof(RegisterRequest).GetProperties())
        {
            var displayPrompt = propertyInfo.Name;
            string displayValue;
            if (propertyInfo.Name != "Password")
            {
                displayValue = propertyInfo.GetValue(args)?.ToString() ?? "";
            }
            else
            {
                displayValue = new string('*', ((string)propertyInfo.GetValue(args)!).Length);
            }

            table.AddRow(displayPrompt, displayValue);
        }

        this.AnsiConsole.Write(table);
        if (
            this.AnsiConsole.Confirm("Would you like to continue with attempting the registration?")
        )
        {
            var result = await client.Register(args);
            this.AnsiConsole.WriteLine(
                $"Registration successful. Your account id is: {result?.AccountId}"
            );
        }
    }

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
            Console.WriteLine(ex.Message);
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
}
