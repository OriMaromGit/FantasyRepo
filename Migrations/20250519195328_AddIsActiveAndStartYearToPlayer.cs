using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyNBA.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveAndStartYearToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NbaStartYear",
                table: "Players",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NbaStartYear",
                table: "Players");
        }
    }
}
