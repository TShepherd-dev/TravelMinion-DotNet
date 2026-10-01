using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelMinion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CountryBaseGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationStops");

            // Existing trips are dev-only and are not migrated to the new
            // geography (spec #5). Clear the old brief rows rather than leaving
            // half-migrated rows with blank arrival/departure bases.
            migrationBuilder.Sql("DELETE FROM [TripBriefs];");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "TripBriefs",
                newName: "Arrival_Date");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "TripBriefs",
                newName: "Departure_Date");

            migrationBuilder.AddColumn<string>(
                name: "Arrival_BaseName",
                table: "TripBriefs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Arrival_Time",
                table: "TripBriefs",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "Departure_BaseName",
                table: "TripBriefs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "Departure_Time",
                table: "TripBriefs",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    TripBriefTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SpanInDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => new { x.TripBriefTripId, x.Id });
                    table.ForeignKey(
                        name: "FK_Countries_TripBriefs_TripBriefTripId",
                        column: x => x.TripBriefTripId,
                        principalTable: "TripBriefs",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bases",
                columns: table => new
                {
                    CountryTripBriefTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Days = table.Column<int>(type: "int", nullable: false),
                    TransitFromPrevious = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bases", x => new { x.CountryTripBriefTripId, x.CountryId, x.Id });
                    table.ForeignKey(
                        name: "FK_Bases_Countries_CountryTripBriefTripId_CountryId",
                        columns: x => new { x.CountryTripBriefTripId, x.CountryId },
                        principalTable: "Countries",
                        principalColumns: new[] { "TripBriefTripId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bases");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropColumn(
                name: "Arrival_BaseName",
                table: "TripBriefs");

            migrationBuilder.DropColumn(
                name: "Arrival_Time",
                table: "TripBriefs");

            migrationBuilder.DropColumn(
                name: "Departure_BaseName",
                table: "TripBriefs");

            migrationBuilder.DropColumn(
                name: "Departure_Time",
                table: "TripBriefs");

            migrationBuilder.RenameColumn(
                name: "Arrival_Date",
                table: "TripBriefs",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "Departure_Date",
                table: "TripBriefs",
                newName: "EndDate");

            migrationBuilder.CreateTable(
                name: "DestinationStops",
                columns: table => new
                {
                    TripBriefTripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Days = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: true),
                    TransitFromPrevious = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationStops", x => new { x.TripBriefTripId, x.Id });
                    table.ForeignKey(
                        name: "FK_DestinationStops_TripBriefs_TripBriefTripId",
                        column: x => x.TripBriefTripId,
                        principalTable: "TripBriefs",
                        principalColumn: "TripId",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
