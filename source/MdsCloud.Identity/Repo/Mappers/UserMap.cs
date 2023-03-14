using FluentNHibernate.Mapping;
using MdsCloud.Common.NHibernate.Types;
using MdsCloud.Identity.Domain;

namespace MdsCloud.Identity.Repo.Mappers;

public class UserMap : ClassMap<User>
{
    UserMap()
    {
        Id(u => u.Id).Column("id");
        Map(u => u.Email).Column("email");
        Map(u => u.FriendlyName).Column("friendly_name");
        Map(u => u.Password).Column("password");
        Map(u => u.IsPrimary).Column("is_primary");
        Map(u => u.IsActive).Column("is_active");
        Map(u => u.ActivationCode).Column("activation_code");
        Map(u => u.Created).Column("created").CustomType(typeof(DateTimeAsLong));
        Map(u => u.LastActivity).Column("last_activity").CustomType(typeof(DateTimeAsLong));
        Map(u => u.LastModified).Column("last_modified").CustomType(typeof(DateTimeAsLong));
        References<Account>(u => u.Account).Column("account_id");
    }
}
