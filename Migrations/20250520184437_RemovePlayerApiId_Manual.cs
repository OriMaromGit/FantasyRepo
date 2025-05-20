using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyNBA.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlayerApiId_Manual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_PlayerApiId_DataSourceApi",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerApiId",
                table: "Players");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerApiId",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerApiId_DataSourceApi",
                table: "Players",
                columns: new[] { "PlayerApiId", "DataSourceApi" },
                unique: true);
        }
    }
}
