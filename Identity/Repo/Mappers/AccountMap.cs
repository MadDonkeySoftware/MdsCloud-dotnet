using FluentNHibernate.Mapping;
using Identity.Domain;

namespace Identity.Repo.Mappers;

public class AccountMap : ClassMap<Account>
{
    public AccountMap()
    {
        Id(a => a.Id).Column("id").GeneratedBy.Sequence("Account_PK_seq");
        Map(a => a.Name).Column("name");
        HasMany<User>(a => a.Users);
        Map(a => a.IsActive).Column("is_active");
        Map(a => a.Created).Column("created");
        Map(a => a.LastActivity).Column("last_activity");
    }
}
