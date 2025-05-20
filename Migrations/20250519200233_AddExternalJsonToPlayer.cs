using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyNBA.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalJsonToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalApiDataJson",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalApiDataJson",
                table: "Players");
        }
    }
}
