using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixawyy.Migrations
{
    /// <inheritdoc />
    public partial class AddB2BMaintenanceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HazardLevel",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpactScope",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationCriticality",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "MaintenanceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityScore",
                table: "MaintenanceTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "MaintenanceTickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MaintenanceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Building = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaintenanceTicketId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAttachments_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_DepartmentId",
                table: "MaintenanceTickets",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_LocationId",
                table: "MaintenanceTickets",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_SubmittedByUserId",
                table: "MaintenanceTickets",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAttachments_MaintenanceTicketId",
                table: "TicketAttachments",
                column: "MaintenanceTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_AspNetUsers_SubmittedByUserId",
                table: "MaintenanceTickets",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Departments_DepartmentId",
                table: "MaintenanceTickets",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTickets_Locations_LocationId",
                table: "MaintenanceTickets",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_AspNetUsers_SubmittedByUserId",
                table: "MaintenanceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Departments_DepartmentId",
                table: "MaintenanceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTickets_Locations_LocationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "TicketAttachments");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_DepartmentId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_LocationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTickets_SubmittedByUserId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "HazardLevel",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "ImpactScope",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "LocationCriticality",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "MaintenanceTickets");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MaintenanceTickets");
        }
    }
}
