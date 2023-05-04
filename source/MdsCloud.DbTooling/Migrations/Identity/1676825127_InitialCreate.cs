using FluentMigrator;

namespace MdsCloud.DbTooling.Migrations.Identity;

[Tags("Identity")]
[Tags("Production", "Development")]
[Migration(1676825127, "Initial implementation of the database schema")]
public class InitialCreate : Migration
{
    public override void Up()
    {
        // csharpier-ignore-start
        Create.Sequence("account_pk_seq");

        Create.Table("account")
            .WithColumn("id").AsInt64().PrimaryKey()
            .WithColumn("name").AsString(255).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("created").AsDateTimeOffset().NotNullable()
            .WithColumn("last_activity").AsDateTimeOffset().Nullable();

        Create.Table("user")
            .WithColumn("id").AsString(255).PrimaryKey().NotNullable()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("account_id").AsInt64().NotNullable().ForeignKey("fk_account_id_to_account_id", "account", "id")
            .WithColumn("friendly_name").AsString(255).NotNullable()
            .WithColumn("password").AsString(255).NotNullable()
            .WithColumn("is_primary").AsBoolean().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable()
            .WithColumn("activation_code").AsString(255).Nullable()
            .WithColumn("created").AsDateTimeOffset().NotNullable()
            .WithColumn("last_activity").AsDateTimeOffset().Nullable()
            .WithColumn("last_modified").AsDateTimeOffset().Nullable();

        Create.Table("landscape_url")
            .WithColumn("scope").AsString(255).NotNullable()
            .WithColumn("key").AsString(255).NotNullable()
            .WithColumn("value").AsString(255).NotNullable();
        Create.PrimaryKey().OnTable("landscape_url").Columns("scope", "key");

        Create.UniqueConstraint().OnTable("landscape_url").Columns("scope", "key", "value");
        // csharpier-ignore-end
    }

    public override void Down()
    {
        // csharpier-ignore-start
        Delete.Table("landscape_url");
        Delete.Table("user");
        Delete.Table("account");
        Delete.Sequence("account_pk_seq");
        // csharpier-ignore-end
    }
}
