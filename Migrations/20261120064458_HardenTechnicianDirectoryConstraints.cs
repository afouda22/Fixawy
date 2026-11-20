using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixawyy.Migrations
{
    /// <inheritdoc />
    public partial class HardenTechnicianDirectoryConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TechnicianSchedules_TechnicianProfileId",
                table: "TechnicianSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSchedules_TechnicianProfileId_DayOfWeek",
                table: "TechnicianSchedules",
                columns: new[] { "TechnicianProfileId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_Name",
                table: "Specialties",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TechnicianSchedules_TechnicianProfileId_DayOfWeek",
                table: "TechnicianSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Specialties_Name",
                table: "Specialties");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSchedules_TechnicianProfileId",
                table: "TechnicianSchedules",
                column: "TechnicianProfileId");
        }
    }
}
