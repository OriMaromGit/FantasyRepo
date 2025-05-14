using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FantasyNBA.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoAndNicknameToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_TeamApiId_DataSourceApi",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DataSourceApi",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamApiId",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "ExternalApiDataJson",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalApiDataJson",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "DataSourceApi",
                table: "Teams",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TeamApiId",
                table: "Teams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamApiId_DataSourceApi",
                table: "Teams",
                columns: new[] { "TeamApiId", "DataSourceApi" },
                unique: true);
        }
    }
}
