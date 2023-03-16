using MdsCloud.CLI.Utils;
using Scriban;

namespace MdsCloud.CLI.Stack.Builders;

public abstract class BaseBuilder : IServiceBuilder
{
    public event EventHandler<string>? OnStatusUpdate;
    public event EventHandler<string>? OnMilestoneAchieved;

    protected string SourceDirectory { get; }
    protected string BaseStackConfigDirectory { get; }
    protected string AssociatedService { get; set; }

    protected BaseBuilder(string sourceDirectory, string baseStackConfigDirectory)
    {
        SourceDirectory = sourceDirectory;
        BaseStackConfigDirectory = baseStackConfigDirectory;
    }

    public abstract void BuildDockerImage();

    public virtual void Build(IWriteConfigsArgs args)
    {
        BuildDockerImage();
        WriteConfigs(args);
    }

    public abstract void WriteConfigs(IWriteConfigsArgs args);

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
}
