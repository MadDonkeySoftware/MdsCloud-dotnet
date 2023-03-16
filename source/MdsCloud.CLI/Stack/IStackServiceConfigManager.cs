using MdsCloud.CLI.Domain;

namespace MdsCloud.CLI.Stack;

public interface IStackServiceConfigManager
{
    event EventHandler<string> OnStatusUpdate;
    event EventHandler<string> OnMilestoneAchieved;
    void Configure(MdsStackSettings settings, MdsStackConfiguration config);
}
