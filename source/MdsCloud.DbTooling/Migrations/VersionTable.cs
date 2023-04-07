using FluentMigrator.Runner.VersionTableInfo;

namespace MdsCloud.DbTooling.Migrations;

[VersionTableMetaData]
public class VersionTable : DefaultVersionTableMetaData
{
    // lowercase and underscore separate to make life easier with postgres
    public override string TableName => "version_info";
    public override string ColumnName => "version";
    public override string UniqueIndexName => "uc_version";
    public override string AppliedOnColumnName => "applied_on";
    public override string DescriptionColumnName => "description";
}
