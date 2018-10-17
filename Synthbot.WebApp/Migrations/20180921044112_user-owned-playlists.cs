using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class userownedplaylists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SynthbotUsers_PlaybackSessions_OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropIndex(
                name: "IX_SynthbotUsers_OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropColumn(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropColumn(
                name: "PlaylistOwnerId",
                table: "PlaybackSessions");

            migrationBuilder.CreateTable(
                name: "UserOwnedPlaylists",
                columns: table => new
                {
                    SpotifyPlaylistId = table.Column<string>(nullable: false),
                    SpotifyUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOwnedPlaylists", x => x.SpotifyPlaylistId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserOwnedPlaylists_SpotifyUserId",
                table: "UserOwnedPlaylists",
                column: "SpotifyUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOwnedPlaylists");

            migrationBuilder.AddColumn<string>(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaylistOwnerId",
                table: "PlaybackSessions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUsers_OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                column: "OwnedPlaybackSessionId",
                unique: true,
                filter: "[OwnedPlaybackSessionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SynthbotUsers_PlaybackSessions_OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                column: "OwnedPlaybackSessionId",
                principalTable: "PlaybackSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
