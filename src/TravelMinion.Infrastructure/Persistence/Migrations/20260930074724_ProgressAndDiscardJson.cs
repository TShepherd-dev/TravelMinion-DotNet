using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelMinion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProgressAndDiscardJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchJobProgress");

            migrationBuilder.AddColumn<bool>(
                name: "Discarded",
                table: "Suggestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Progress",
                table: "ResearchJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discarded",
                table: "Suggestions");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "ResearchJobs");

            migrationBuilder.CreateTable(
                name: "ResearchJobProgress",
                columns: table => new
                {
                    ResearchJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SuggestionsFound = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchJobProgress", x => new { x.ResearchJobId, x.Id });
                    table.ForeignKey(
                        name: "FK_ResearchJobProgress_ResearchJobs_ResearchJobId",
                        column: x => x.ResearchJobId,
                        principalTable: "ResearchJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
