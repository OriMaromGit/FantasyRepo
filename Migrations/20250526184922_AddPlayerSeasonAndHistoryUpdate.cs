using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyNBA.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerSeasonAndHistoryUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredAt",
                table: "PlayerTeamHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalApiDataJson",
                table: "Players",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Season",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Player_UniquePerSourceSeason",
                table: "Players",
                columns: new[] { "ExternalApiDataJson", "Season", "DataSourceApi" },
                unique: true,
                filter: "[ExternalApiDataJson] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Player_UniquePerSourceSeason",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AcquiredAt",
                table: "PlayerTeamHistories");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalApiDataJson",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
