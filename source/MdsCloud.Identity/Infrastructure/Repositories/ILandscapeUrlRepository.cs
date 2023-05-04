using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Infrastructure.Repositories;

public interface ILandscapeUrlRepository
{
    IList<LandscapeUrl> GetLandscapeUrlsForScope(string scope);
    void SaveLandscapeUrl(LandscapeUrl item);
}
