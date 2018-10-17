using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class playbacksessions2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_SynthbotUsers_PlaylistOwnerId",
                table: "PlaybackSessions");

            migrationBuilder.DropIndex(
                name: "IX_PlaybackSessions_PlaylistOwnerId",
                table: "PlaybackSessions");

            migrationBuilder.AlterColumn<string>(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistOwnerId",
                table: "PlaybackSessions",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SynthbotUsers_PlaybackSessions_OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropIndex(
                name: "IX_SynthbotUsers_OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.AlterColumn<string>(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistOwnerId",
                table: "PlaybackSessions",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackSessions_PlaylistOwnerId",
                table: "PlaybackSessions",
                column: "PlaylistOwnerId",
                unique: true,
                filter: "[PlaylistOwnerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_SynthbotUsers_PlaylistOwnerId",
                table: "PlaybackSessions",
                column: "PlaylistOwnerId",
                principalTable: "SynthbotUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
