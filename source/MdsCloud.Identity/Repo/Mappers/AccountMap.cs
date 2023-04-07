using FluentNHibernate.Mapping;
using MdsCloud.Common.NHibernate.Types;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Repo.Mappers;

public class AccountMap : ClassMap<Account>
{
    public AccountMap()
    {
        Table("account");
        Id(a => a.Id).Column("id").GeneratedBy.Sequence("account_pk_seq");
        Map(a => a.Name).Column("name");
        HasMany<User>(a => a.Users);
        Map(a => a.IsActive).Column("is_active");
        Map(a => a.Created).Column("created").CustomType(typeof(DateTimeAsLong));
        Map(a => a.LastActivity).Column("last_activity").CustomType(typeof(DateTimeAsLong));
    }
}
