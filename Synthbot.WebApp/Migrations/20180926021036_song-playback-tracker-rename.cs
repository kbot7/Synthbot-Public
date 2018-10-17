using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class songplaybacktrackerrename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackTrackers_PlaybackSessions_PlaybackSessionId",
                table: "PlaybackTrackers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlaybackTrackers",
                table: "PlaybackTrackers");

            migrationBuilder.RenameTable(
                name: "PlaybackTrackers",
                newName: "SongPlaybackTrackers");

            migrationBuilder.RenameIndex(
                name: "IX_PlaybackTrackers_PlaybackSessionId",
                table: "SongPlaybackTrackers",
                newName: "IX_SongPlaybackTrackers_PlaybackSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SongPlaybackTrackers",
                table: "SongPlaybackTrackers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_SongPlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions",
                column: "CurrentSongPlaybackId",
                principalTable: "SongPlaybackTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SongPlaybackTrackers_PlaybackSessions_PlaybackSessionId",
                table: "SongPlaybackTrackers",
                column: "PlaybackSessionId",
                principalTable: "PlaybackSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_SongPlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_SongPlaybackTrackers_PlaybackSessions_PlaybackSessionId",
                table: "SongPlaybackTrackers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SongPlaybackTrackers",
                table: "SongPlaybackTrackers");

            migrationBuilder.RenameTable(
                name: "SongPlaybackTrackers",
                newName: "PlaybackTrackers");

            migrationBuilder.RenameIndex(
                name: "IX_SongPlaybackTrackers_PlaybackSessionId",
                table: "PlaybackTrackers",
                newName: "IX_PlaybackTrackers_PlaybackSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlaybackTrackers",
                table: "PlaybackTrackers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentSongPlaybackId",
                table: "PlaybackSessions",
                column: "CurrentSongPlaybackId",
                principalTable: "PlaybackTrackers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaybackTrackers_PlaybackSessions_PlaybackSessionId",
                table: "PlaybackTrackers",
                column: "PlaybackSessionId",
                principalTable: "PlaybackSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
