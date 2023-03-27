using MdsCloud.CLI.Utils;

namespace MdsCloud.CLI.Stack.Builders;

public class ElkBuilder : BaseBuilder
{
    public ElkBuilder(string sourceDirectory, string baseStackConfigDirectory)
        : base(sourceDirectory, baseStackConfigDirectory) { }

    public override void BuildDockerImage()
    {
        // We do not build ELK images directly.
    }

    public override void WriteConfigs(IWriteConfigsArgs args)
    {
        DirectoryTools.EnsurePathsExists(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "setup"),
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "elasticsearch"),
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "logstash"),
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "kibana")
        );

        WriteSetupConfigs();
        WriteElasticSearchConfigs();
        WriteLogstashConfigs();
        WriteKibanaConfigs();
    }

    private void SetExecutable(string path)
    {
        // Not sure about the below approach and how it would shake out when used on a non linux system.
        // Until some more thought can be put into this we'll just call shell for now and this can be adjusted
        // in the future.
        // https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core

        var child = new ChildProcess(
            $"chmod +x {path}",
            workingDirectory: BaseStackConfigDirectory
        );
        child.Start();
        child.WaitForExit();
    }

    private void WriteSetupConfigs()
    {
        var template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Setup.Dockerfile.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "setup", "Dockerfile"),
            template.Render(new { })
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Setup.entrypoint-sh.scriban"
        );
        var entryPointPath = Path.Join(
            BaseStackConfigDirectory,
            "configs",
            "elk",
            "setup",
            "entrypoint.sh"
        );
        File.WriteAllText(entryPointPath, template.Render(new { }));

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Setup.lib-sh.scriban"
        );
        var libPath = Path.Join(BaseStackConfigDirectory, "configs", "elk", "setup", "lib.sh");
        File.WriteAllText(libPath, template.Render(new { }));

        SetExecutable(entryPointPath);
        SetExecutable(libPath);
    }

    private void WriteElasticSearchConfigs()
    {
        var template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.ElasticSearch.Dockerfile.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "elasticsearch", "Dockerfile"),
            template.Render(new { })
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.ElasticSearch.elasticsearch.yml.scriban"
        );
        File.WriteAllText(
            Path.Join(
                BaseStackConfigDirectory,
                "configs",
                "elk",
                "elasticsearch",
                "elasticsearch.yml"
            ),
            template.Render(new { })
        );
    }

    private void WriteLogstashConfigs()
    {
        var template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Logstash.Dockerfile.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "logstash", "Dockerfile"),
            template.Render(new { })
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Logstash.logstash.yml.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "logstash", "logstash.yml"),
            template.Render(new { })
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Logstash.logstash.conf.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "logstash", "logstash.conf"),
            template.Render(new { })
        );
    }

    private void WriteKibanaConfigs()
    {
        var template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Kibana.Dockerfile.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "kibana", "Dockerfile"),
            template.Render(new { })
        );

        template = GetTemplateFromEmbeddedResource(
            "MdsCloud.CLI.Templates.ELK.Kibana.kibana.yml.scriban"
        );
        File.WriteAllText(
            Path.Join(BaseStackConfigDirectory, "configs", "elk", "kibana", "kibana.yml"),
            template.Render(new { })
        );
    }
}
