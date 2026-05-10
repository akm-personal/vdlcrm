using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vdlcrm.Services.Migrations
{
    /// <inheritdoc />
    public partial class CompleteStringConversionFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fee_payments_users_CollectedBy",
                table: "fee_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_fee_records_users_CreatedBy",
                table: "fee_records");

            migrationBuilder.DropForeignKey(
                name: "FK_shifts_users_CreatedBy",
                table: "shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_shifts_users_UpdatedBy",
                table: "shifts");

            migrationBuilder.DropTable(
                name: "seat_assignments");

            migrationBuilder.DropTable(
                name: "seats");

            migrationBuilder.DropTable(
                name: "seat_rows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statuses",
                table: "statuses");

            migrationBuilder.DropIndex(
                name: "IX_shifts_CreatedBy",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_shifts_UpdatedBy",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "IX_fee_records_CreatedBy",
                table: "fee_records");

            migrationBuilder.DropIndex(
                name: "IX_fee_payments_CollectedBy",
                table: "fee_payments");

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "StatusId",
                keyValue: 9);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "student_details",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "statuses",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "statuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "shifts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "shifts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "roles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "fee_records",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CollectedBy",
                table: "fee_payments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "endpoint_permissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_statuses",
                table: "statuses",
                column: "Id");

            migrationBuilder.InsertData(
                table: "statuses",
                columns: new[] { "Id", "IsActive", "StatusId", "StatusName", "StatusType" },
                values: new object[,]
                {
                    { 1, true, 1, "Pending", "Fee" },
                    { 2, true, 2, "Partial", "Fee" },
                    { 3, true, 3, "Paid", "Fee" },
                    { 4, true, 4, "Active", "General" },
                    { 5, true, 5, "Not Active", "General" },
                    { 6, true, 6, "Active", "Student" },
                    { 7, true, 7, "Not Active", "Student" },
                    { 8, true, 8, "Dropped", "Student" },
                    { 9, true, 9, "Cancelled", "Student" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_statuses",
                table: "statuses");

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "statuses",
                keyColumn: "Id",
                keyColumnType: "INTEGER",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "student_details");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "statuses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "endpoint_permissions");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "statuses",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<int>(
                name: "UpdatedBy",
                table: "shifts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "shifts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "fee_records",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CollectedBy",
                table: "fee_payments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_statuses",
                table: "statuses",
                column: "StatusId");

            migrationBuilder.CreateTable(
                name: "seat_rows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RowName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RowOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_rows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeatRowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SeatLabel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SeatOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seats_seat_rows_SeatRowId",
                        column: x => x.SeatRowId,
                        principalTable: "seat_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seat_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeatId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RemovedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seat_assignments_seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_seat_assignments_shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_seat_assignments_student_details_StudentId",
                        column: x => x.StudentId,
                        principalTable: "student_details",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_shifts_CreatedBy",
                table: "shifts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_UpdatedBy",
                table: "shifts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_fee_records_CreatedBy",
                table: "fee_records",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_fee_payments_CollectedBy",
                table: "fee_payments",
                column: "CollectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_seat_assignments_SeatId_ShiftId",
                table: "seat_assignments",
                columns: new[] { "SeatId", "ShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seat_assignments_ShiftId",
                table: "seat_assignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_seat_assignments_StudentId",
                table: "seat_assignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_seats_SeatRowId_SeatLabel",
                table: "seats",
                columns: new[] { "SeatRowId", "SeatLabel" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fee_payments_users_CollectedBy",
                table: "fee_payments",
                column: "CollectedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fee_records_users_CreatedBy",
                table: "fee_records",
                column: "CreatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shifts_users_CreatedBy",
                table: "shifts",
                column: "CreatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shifts_users_UpdatedBy",
                table: "shifts",
                column: "UpdatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
