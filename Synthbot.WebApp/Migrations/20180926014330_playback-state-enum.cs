using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class playbackstateenum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Completed",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "Paused",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "Resumed",
                table: "PlaybackTrackers");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "PlaybackTrackers");

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Paused",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Resumed",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: false);
        }
    }
}
