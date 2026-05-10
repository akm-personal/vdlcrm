using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vdlcrm.Services.Migrations
{
    /// <inheritdoc />
    public partial class MakeVdlIdAlternateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
   
    migrationBuilder.AddUniqueConstraint(
        name: "AK_student_details_VdlId",
        table: "student_details",
        column: "VdlId");

}


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
