using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vdlcrm.Services.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelsUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fee_records_student_details_StudentId",
                table: "fee_records");

            migrationBuilder.DropIndex(
                name: "IX_fee_records_StudentId",
                table: "fee_records");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "fee_records");

            migrationBuilder.AddColumn<string>(
                name: "VdlId",
                table: "fee_records",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_student_details_VdlId",
                table: "student_details",
                column: "VdlId");

            migrationBuilder.CreateIndex(
                name: "IX_fee_records_VdlId",
                table: "fee_records",
                column: "VdlId");

            migrationBuilder.AddForeignKey(
                name: "FK_fee_records_student_details_VdlId",
                table: "fee_records",
                column: "VdlId",
                principalTable: "student_details",
                principalColumn: "VdlId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fee_records_student_details_VdlId",
                table: "fee_records");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_student_details_VdlId",
                table: "student_details");

            migrationBuilder.DropIndex(
                name: "IX_fee_records_VdlId",
                table: "fee_records");

            migrationBuilder.DropColumn(
                name: "VdlId",
                table: "fee_records");

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "fee_records",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_fee_records_StudentId",
                table: "fee_records",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_fee_records_student_details_StudentId",
                table: "fee_records",
                column: "StudentId",
                principalTable: "student_details",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
