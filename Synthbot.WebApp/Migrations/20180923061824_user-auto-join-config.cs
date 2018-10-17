using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class userautojoinconfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoJoin",
                table: "SynthbotUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultSpotifyDevice",
                table: "SynthbotUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoJoin",
                table: "SynthbotUsers");

            migrationBuilder.DropColumn(
                name: "DefaultSpotifyDevice",
                table: "SynthbotUsers");
        }
    }
}
