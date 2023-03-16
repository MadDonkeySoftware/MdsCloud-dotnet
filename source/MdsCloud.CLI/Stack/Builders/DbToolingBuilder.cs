using MdsCloud.CLI.Attributes;
using MdsCloud.CLI.Domain;
using MdsCloud.CLI.Utils;

namespace MdsCloud.CLI.Stack.Builders;

[StackBuilderMetadata(Service = Constants.Services.Identity)]
public class DbToolingBuilder : BaseBuilder
{
    public DbToolingBuilder(string sourceDirectory, string baseStackConfigDirectory)
        : base(sourceDirectory, baseStackConfigDirectory) { }

    public override void BuildDockerImage()
    {
        using var dockerBuildProcess = new ChildProcess(
            $"docker build -t local/{Constants.DockerImageNames.DbTooling}:latest -f MdsCloud.DbTooling.Dockerfile .",
            SourceDirectory,
            Path.Join(BaseStackConfigDirectory, "logs", "dbToolingDockerBuild.log")
        );
        dockerBuildProcess.OnDataOutput += (_, message) =>
        {
            if (!string.IsNullOrEmpty(message))
                EmitStatusUpdate(message);
        };

        dockerBuildProcess.Start();
        EmitMilestone("Building Db Tooling docker image...");
        dockerBuildProcess.WaitForExit();

        if (dockerBuildProcess.ExitCode != 0)
        {
            // TODO: Custom Exception
            EmitMilestone("Db Tooling docker image failed to build.");
            throw new Exception("");
        }

        EmitMilestone("Db Tooling docker image built.");
    }

    public override void WriteConfigs(IWriteConfigsArgs args)
    {
        DirectoryTools.EnsurePathsExists(
            Path.Join(BaseStackConfigDirectory, "configs", "dbTooling")
        );

        const string templateKey = "MdsCloud.CLI.Templates.DbTooling.Appsettings.Json.scriban";
        var template = GetTemplateFromEmbeddedResource(templateKey);
        var dbToolingAppSettingsJson = template.Render(new { });

        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "dbTooling", "appsettings.json"),
            dbToolingAppSettingsJson
        );
    }
}
