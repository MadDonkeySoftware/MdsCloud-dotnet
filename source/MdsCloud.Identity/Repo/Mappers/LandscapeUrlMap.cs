using FluentNHibernate.Mapping;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Repo.Mappers;

public class LandscapeUrlMap : ClassMap<LandscapeUrl>
{
    public LandscapeUrlMap()
    {
        Table("landscape_url");
        CompositeId().KeyProperty(x => x.Scope, "scope").KeyProperty(x => x.Key, "key");
        Map(a => a.Value).Column("value");
    }
}
