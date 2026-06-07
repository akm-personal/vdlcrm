using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vdlcrm.Services.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentStatusToCancelled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "statuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StatusType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StatusName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statuses", x => x.StatusId);
                });

            migrationBuilder.InsertData(
                table: "statuses",
                columns: new[] { "StatusId", "IsActive", "StatusName", "StatusType" },
                values: new object[,]
                {
                    { 1, true, "Pending", "Fee" },
                    { 2, true, "Partial", "Fee" },
                    { 3, true, "Paid", "Fee" },
                    { 4, true, "Active", "General" },
                    { 5, true, "Not Active", "General" },
                    { 6, true, "Active", "Student" },
                    { 7, true, "Not Active", "Student" },
                    { 8, true, "Dropped", "Student" },
                    { 9, true, "Cancelled", "Student" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_statuses_StatusType_StatusName",
                table: "statuses",
                columns: new[] { "StatusType", "StatusName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "statuses");
        }
    }
}
