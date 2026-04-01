using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vdlcrm.Services.Migrations
{
    /// <inheritdoc />
    public partial class MakeRoleIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "RoleSequenceId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "RoleSequenceId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "RoleSequenceId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "RoleSequenceId",
                keyValue: 4);

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleId",
                table: "roles",
                column: "RoleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roles_RoleId",
                table: "roles");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "RoleSequenceId", "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, 1, "Admin" },
                    { 2, 2, "Internal User" },
                    { 3, 3, "External User" },
                    { 4, 4, "Student" }
                });
        }
    }
}
