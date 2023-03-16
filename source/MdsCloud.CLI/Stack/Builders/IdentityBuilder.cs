using MdsCloud.CLI.Domain;
using MdsCloud.CLI.Utils;

namespace MdsCloud.CLI.Stack.Builders;

public class IdentityBuilder : BaseBuilder
{
    public IdentityBuilder(string sourceDirectory, string baseStackConfigDirectory)
        : base(sourceDirectory, baseStackConfigDirectory) { }

    public override void BuildDockerImage()
    {
        // TODO: Figure out how to fail the process if a command fails (like the docker build)
        using var dockerBuildIdentityProcess = new ChildProcess(
            $"docker build -t local/{Constants.DockerImageNames.Identity}:latest -f MdsCloud.Identity.Dockerfile .",
            SourceDirectory,
            Path.Join(BaseStackConfigDirectory, "logs", "identityDockerBuild.log")
        );

        dockerBuildIdentityProcess.Start();
        EmitMilestone("Building Identity Service docker image...");
        dockerBuildIdentityProcess.OnDataOutput += (_, message) =>
        {
            if (!string.IsNullOrEmpty(message))
                EmitStatusUpdate(message);
        };
        dockerBuildIdentityProcess.WaitForExit();

        if (dockerBuildIdentityProcess.ExitCode != 0)
        {
            // TODO: Custom Exception
            EmitMilestone("Identity Service docker image failed to build.");
            throw new Exception("");
        }

        EmitMilestone("Identity Service docker image built.");
    }

    public override void WriteConfigs(IWriteConfigsArgs args)
    {
        DirectoryTools.EnsurePathsExists(
            Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys"),
            Path.Join(BaseStackConfigDirectory, "configs", "identity", "proxy")
        );

        EmitStatusUpdate("Generating SSH Keys for identity service (pass)");
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys", "pass"),
            args.IdentityPrivateKeyPassword
        );

        EmitStatusUpdate("Generating SSH Keys for identity service (private ssh key)");
        using (
            var privateKeyGenProcess = new ChildProcess(
                $"ssh-keygen -f ./key -t rsa -b 4096 -m PKCS8 -n {args.IdentityPrivateKeyPassword} -N {args.IdentityPrivateKeyPassword}",
                Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys")
            )
        )
        {
            privateKeyGenProcess.Start();
            privateKeyGenProcess.WaitForExit();
        }

        EmitStatusUpdate("Generating SSH Keys for identity service (public ssh key pem)");
        using (
            var publicKeyGenProcess = new ChildProcess(
                "ssh-keygen -f ./key.pub -e -m pem",
                Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys"),
                Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys", "key.pub.pem")
            )
        )
        {
            publicKeyGenProcess.Start();
            publicKeyGenProcess.WaitForExit();
        }

        EmitStatusUpdate("Generating SSH Keys for identity service (openssl certs)");
        using (
            var openSslProcess = new ChildProcess(
                "openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout nginx-selfsigned.key -out nginx-selfsigned.crt -batch -subj /",
                Path.Join(BaseStackConfigDirectory, "configs", "identity", "proxy")
            )
        )
        {
            openSslProcess.Start();
            openSslProcess.WaitForExit();
        }

        EmitStatusUpdate("Generating appsettings.json");
        var template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.Identity.Appsettings.Json.scriban"
        );

        var isLocalDev = args.IsLocalDev[Constants.StackConfiguration.Keys.Identity];

        var identityAppSettingsJson = template.Render(
            new
            {
                KeyPrivatePath = isLocalDev
                    ? Path.Join(BaseStackConfigDirectory, "configs", "identity", "keys", "key")
                    : Path.Join("keys", "key"),
                KeyPassword = args.IdentityPrivateKeyPassword,
                KeyPublicPath = isLocalDev
                    ? Path.Join(
                        BaseStackConfigDirectory,
                        "configs",
                        "identity",
                        "keys",
                        "key.pub.pem"
                    )
                    : Path.Join("keys", "key.pub.pem"),
                DevSettingShowSql = isLocalDev,
                ConnectionString = args.DbConnectionStings[
                    Constants.StackConfiguration.Keys.Identity
                ]
            }
        );

        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "identity", "appsettings.json"),
            identityAppSettingsJson
        );

        if (isLocalDev)
        {
            if (
                !File.Exists(
                    Path.Join(
                        args.LocalSourceRootDirectory,
                        "source",
                        "MdsCloud.Identity",
                        "appsettings.json"
                    )
                )
            )
            {
                throw new Exception(
                    "Could not find project appsettings.json. Did the source configuration change?"
                );
            }

            File.WriteAllText(
                Path.Join(
                    args.LocalSourceRootDirectory,
                    "source",
                    "MdsCloud.Identity",
                    "appsettings.Development.json"
                ),
                identityAppSettingsJson
            );
        }

        EmitMilestone("Identity Service support files configured.");
    }
}
