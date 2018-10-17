using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class columnrename2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentPlaybackId",
                table: "PlaybackSessions");

            migrationBuilder.RenameColumn(
                name: "CurrentPlaybackId",
                table: "PlaybackSessions",
                newName: "CurrentSongPlaybackId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaybackSessions_CurrentPlaybackId",
                table: "PlaybackSessions",
                newName: "IX_PlaybackSessions_CurrentSongPlaybackId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions",
                column: "CurrentSongPlaybackId",
                principalTable: "PlaybackTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions");

            migrationBuilder.RenameColumn(
                name: "CurrentSongPlaybackId",
                table: "PlaybackSessions",
                newName: "CurrentPlaybackId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaybackSessions_CurrentSongPlaybackId",
                table: "PlaybackSessions",
                newName: "IX_PlaybackSessions_CurrentPlaybackId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentPlaybackId",
                table: "PlaybackSessions",
                column: "CurrentPlaybackId",
                principalTable: "PlaybackTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
