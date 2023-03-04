using FluentMigrator;

namespace MdsCloud.DbTooling.Migrations.Identity;

[Tags("MdsCloud.Identity")]
[Tags("Production", "Development")]
[Migration(1676825127, "Initial implementation of the database schema")]
public class InitialCreate : Migration
{
    public override void Up()
    {
        // csharpier-ignore-start
        Create.Sequence("account_pk_seq");
        Create.Table("Account")
            .WithColumn("id").AsInt64().PrimaryKey()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("created").AsDateTime().NotNullable()
            .WithColumn("last_activity").AsDateTime().NotNullable();

        Create.Table("User")
            .WithColumn("id").AsString(255).PrimaryKey().NotNullable()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("account_id").AsInt64().NotNullable().ForeignKey("fk_account_id_to_account_id", "Account", "id")
            .WithColumn("friendly_name").AsString(255).NotNullable()
            .WithColumn("password").AsString(255).NotNullable()
            .WithColumn("is_primary").AsBoolean().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("activation_code").AsString(255).Nullable()
            .WithColumn("created").AsDateTime().NotNullable()
            .WithColumn("last_activity").AsDateTime().NotNullable()
            .WithColumn("last_modified").AsDateTime().NotNullable();

        Create.Table("LandscapeUrl")
            .WithColumn("scope").AsString(255).NotNullable()
            .WithColumn("key").AsString(255).NotNullable()
            .WithColumn("value").AsString(255).NotNullable();
        Create.PrimaryKey().OnTable("LandscapeUrl").Columns("scope", "key");

        Create.UniqueConstraint().OnTable("LandscapeUrl").Columns("scope", "key", "value");
        // csharpier-ignore-end
    }

    public override void Down()
    {
        // csharpier-ignore-start
        Delete.Table("LandscapeUrl");
        Delete.Table("User");
        Delete.Table("Account");
        Delete.Sequence("account_pk_seq");
        // csharpier-ignore-end
    }
}
