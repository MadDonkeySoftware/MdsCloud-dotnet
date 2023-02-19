using FluentNHibernate.Mapping;
using Identity.Domain;

namespace Identity.Repo.Mappers;

public class LandscapeUrlMap : ClassMap<LandscapeUrl>
{
    public LandscapeUrlMap()
    {
        CompositeId().KeyProperty(x => x.Scope, "scope").KeyProperty(x => x.Key, "key");
        // Map(a => a.Scope).Column("scope");
        // Map(a => a.Key).Column("key");
        Map(a => a.Value).Column("value");
    }
}
