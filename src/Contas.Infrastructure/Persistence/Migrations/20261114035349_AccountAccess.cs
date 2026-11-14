using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagerSubject",
                table: "Accounts",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerSubject",
                table: "Accounts",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ManagerSubject",
                table: "Accounts",
                column: "ManagerSubject");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_OwnerSubject",
                table: "Accounts",
                column: "OwnerSubject",
                unique: true,
                filter: "NOT \"Deleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_ManagerSubject",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_OwnerSubject",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ManagerSubject",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OwnerSubject",
                table: "Accounts");
        }
    }
}
