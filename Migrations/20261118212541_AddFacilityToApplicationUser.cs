using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixawyy.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Facility",
                table: "AspNetUsers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Facility",
                table: "AspNetUsers");
        }
    }
}
