namespace MdsCloud.CLI.Stack.Builders;

public interface IServiceBuilder
{
    public event EventHandler<string> OnStatusUpdate;
    public event EventHandler<string> OnMilestoneAchieved;

    void Build(IWriteConfigsArgs args);
}
