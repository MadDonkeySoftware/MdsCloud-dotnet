using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Accounts",
                    columns: table =>
                        new
                        {
                            Id = table
                                .Column<ulong>(type: "bigint unsigned", nullable: false)
                                .Annotation(
                                    "MySql:ValueGenerationStrategy",
                                    MySqlValueGenerationStrategy.IdentityColumn
                                ),
                            Name = table
                                .Column<string>(type: "varchar(255)", nullable: false)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                            Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                            LastActivity = table.Column<DateTime>(
                                type: "datetime(6)",
                                nullable: false
                            )
                        },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Accounts", x => x.Id);
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder
                .CreateTable(
                    name: "Users",
                    columns: table =>
                        new
                        {
                            Id = table
                                .Column<string>(type: "varchar(255)", nullable: false)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            Email = table
                                .Column<string>(type: "longtext", nullable: false)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            FriendlyName = table
                                .Column<string>(type: "longtext", nullable: false)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            Password = table
                                .Column<string>(type: "longtext", nullable: false)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                            IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                            ActivationCode = table
                                .Column<string>(type: "longtext", nullable: true)
                                .Annotation("MySql:CharSet", "utf8mb4"),
                            Created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                            LastActivity = table.Column<DateTime>(
                                type: "datetime(6)",
                                nullable: false
                            ),
                            LastModified = table.Column<DateTime>(
                                type: "datetime(6)",
                                nullable: false
                            ),
                            AccountId = table.Column<ulong>(
                                type: "bigint unsigned",
                                nullable: false
                            )
                        },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Users", x => x.Id);
                        table.ForeignKey(
                            name: "FK_Users_Accounts_AccountId",
                            column: x => x.AccountId,
                            principalTable: "Accounts",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade
                        );
                    }
                )
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Name",
                table: "Accounts",
                column: "Name"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountId",
                table: "Users",
                column: "AccountId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "Accounts");
        }
    }
}
