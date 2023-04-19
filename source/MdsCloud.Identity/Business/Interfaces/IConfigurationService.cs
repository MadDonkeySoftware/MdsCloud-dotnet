using MdsCloud.Identity.Business.DTOs;

namespace MdsCloud.Identity.Business.Interfaces;

public interface IConfigurationService
{
    public Configuration GetConfiguration(ArgWithTrace<string> requestArgs);
    public void SaveConfigurations(ArgsWithTrace<List<Tuple<string, SaveConfigurationArgs>>> args);
    public string GetPublicSignature();
}
