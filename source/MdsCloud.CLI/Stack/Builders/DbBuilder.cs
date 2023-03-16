using MdsCloud.CLI.Utils;

namespace MdsCloud.CLI.Stack.Builders;

public class DbBuilder : BaseBuilder
{
    public DbBuilder(string sourceDirectory, string baseStackConfigDirectory)
        : base(sourceDirectory, baseStackConfigDirectory) { }

    public override void BuildDockerImage()
    {
        // We do not build a DB image.
    }

    public override void WriteConfigs(IWriteConfigsArgs args)
    {
        DirectoryTools.EnsurePathsExists(
            Path.Join(BaseStackConfigDirectory, "configs", "postgres")
        );

        File.WriteAllText( // TODO: Refactor this
            Path.Join(BaseStackConfigDirectory, "configs", "postgres", "db-init.sql"),
            "CREATE DATABASE identity;"
        );
    }
}
