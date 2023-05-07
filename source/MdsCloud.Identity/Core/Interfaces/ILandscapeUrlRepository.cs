using MdsCloud.Identity.Core.Model;

namespace MdsCloud.Identity.Core.Interfaces;

public interface ILandscapeUrlRepository
{
    IList<LandscapeUrl> GetLandscapeUrlsForScope(string scope);
    void SaveLandscapeUrl(LandscapeUrl item);
}
