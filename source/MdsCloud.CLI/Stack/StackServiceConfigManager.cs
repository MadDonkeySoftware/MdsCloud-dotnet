using MdsCloud.CLI.Domain;
using MdsCloud.CLI.Stack.Builders;
using MdsCloud.CLI.Utils;
using Scriban;

namespace MdsCloud.CLI.Stack;

public class StackServiceConfigManager : IStackServiceConfigManager
{
    public event EventHandler<string>? OnStatusUpdate;
    public event EventHandler<string>? OnMilestoneAchieved;

    private const string LocalDockerImageNameTemplate = "local/{0}:latest";
    private const string StableDockerImageNameTemplate = "mdscloud/{0}:stable";

    protected string BaseStackConfigDirectory { get; }

    public StackServiceConfigManager(string baseStackConfigDirectory)
    {
        BaseStackConfigDirectory = baseStackConfigDirectory;
    }

    public void Configure(MdsStackSettings settings, MdsStackConfiguration config)
    {
        var shouldLocalBuild = new List<string> { Constants.StackConfiguration.Values.Local, };

        var identityBuilder = new IdentityBuilder(
            settings.SourceDirectory,
            BaseStackConfigDirectory
        );
        var builderLookup = new Dictionary<string, BaseBuilder>
        {
            { Constants.StackConfiguration.Keys.Identity, identityBuilder }
        };

        if (Directory.Exists(Path.Join(BaseStackConfigDirectory, "configs")))
        {
            Directory.Delete(Path.Join(BaseStackConfigDirectory, "configs"), true);
        }

        if (File.Exists(Path.Join(BaseStackConfigDirectory, "docker-compose.yml")))
        {
            File.Delete(Path.Join(BaseStackConfigDirectory, "docker-compose.yml"));
        }

        var args = new WriteConfigsArgs
        {
            LocalSourceRootDirectory = settings.SourceDirectory,
            IdentityPrivateKeyPassword = RandomStringGenerator.GenerateString(12),
            // NOTE: DbConnectionStings is populated elsewhere
        };

        void Dispatch(BaseBuilder builder, bool isLocalDev = false)
        {
            builder.OnStatusUpdate += (_, message) => EmitStatusUpdate(message);
            builder.OnMilestoneAchieved += (_, message) => EmitMilestone(message);

            if (isLocalDev)
            {
                builder.WriteConfigs(args);
            }
            else
            {
                builder.Build(args);
            }
        }

        var dbBuilder = new DbBuilder(settings.SourceDirectory, BaseStackConfigDirectory);
        var dbToolingBuilder = new DbToolingBuilder(
            settings.SourceDirectory,
            BaseStackConfigDirectory
        );
        Dispatch(dbBuilder);
        Dispatch(dbToolingBuilder);

        foreach (var service in builderLookup.Keys)
        {
            var builder = builderLookup[service];
            var isLocalDev = !shouldLocalBuild.Contains(config.GetStackConfigurationValue(service));
            args.DbConnectionStings[service] = BuildConnectionString(isLocalDev, service);
            args.IsLocalDev[service] = isLocalDev;
            Dispatch(builder, isLocalDev);
        }

        EmitStatusUpdate("Generating docker compose configuration");

        var identityIsLocal = args.IsLocalDev[Constants.StackConfiguration.Keys.Identity];
        var templateKey = identityIsLocal
            ? "MdsCloud.CLI.Templates.Identity.Compose.LocalDev.scriban"
            : "MdsCloud.CLI.Templates.Identity.Compose.InContainer.scriban";
        var template = GetTemplateFromEmbeddedResource(templateKey);
        var identityRendered = template.Render(
            new
            {
                BaseConfigDir = Path.Join(BaseStackConfigDirectory, "configs"),
                MdsIdentityImage = string.Format(
                    LocalDockerImageNameTemplate,
                    Constants.DockerImageNames.Identity
                ),
                MdsDbToolingImage = string.Format(
                    LocalDockerImageNameTemplate,
                    Constants.DockerImageNames.DbTooling
                ),
                MdsSysPassword = settings.DefaultAdminPassword,
            }
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.DockerComposeBase.scriban"
        );
        var dockerComposeRendered = template.Render(
            new
            {
                BaseConfigDir = Path.Join(BaseStackConfigDirectory, "configs"),
                Identity = identityRendered
            }
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "docker-compose.yml"),
            dockerComposeRendered
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.Identity.Nginx.Config.scriban"
        );
        var nginxConfRendered = template.Render(
            new
            {
                Servers = new List<string>
                {
                    identityIsLocal ? "host.docker.internal:8888" : "mds-identity:8888"
                }
            }
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "identity", "proxy", "nginx.conf"),
            nginxConfRendered
        );
    }

    protected void EmitStatusUpdate(string message)
    {
        OnStatusUpdate?.Invoke(this, message);
    }

    protected void EmitMilestone(string message)
    {
        OnMilestoneAchieved?.Invoke(this, message);
    }

    protected Template GetTemplateFromEmbeddedResource(string id)
    {
        return Template.Parse(EmbeddedResources.ReadEmbeddedResource(id));
    }

    private string BuildConnectionString(bool isLocalDev, string dbName)
    {
        // TODO: Make server, port, user and pass dynamic.
        var server = isLocalDev ? "localhost" : "postgres";
        return $"Server={server};Port=5432;Database={dbName};User Id=postgres;Password=pwd4postgres;";
    }
}
