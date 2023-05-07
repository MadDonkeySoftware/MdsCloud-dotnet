using MdsCloud.Identity.Core.DTOs;

namespace MdsCloud.Identity.Core.Interfaces;

public interface IConfigurationService
{
    public Configuration GetConfiguration(ArgsWithTrace<string> requestArgs);
    public void SaveConfigurations(ArgsWithTrace<List<Tuple<string, SaveConfigurationArgs>>> args);
    public string GetPublicSignature();
}
