using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class playbacksessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceChannelActivePlaylists");

            migrationBuilder.AddColumn<string>(
                name: "ActivePlaybackSessionId",
                table: "SynthbotUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlaybackTrackers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    SpotifySongUri = table.Column<string>(nullable: false),
                    StartedUtc = table.Column<DateTime>(nullable: false),
                    Duration = table.Column<TimeSpan>(nullable: false),
                    Completed = table.Column<bool>(nullable: false),
                    PlaybackSessionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaybackSessions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    DiscordVoiceChannelId = table.Column<string>(nullable: false),
                    SpotifyPlaylistId = table.Column<string>(nullable: false),
                    PlaylistOwnerId = table.Column<string>(nullable: true),
                    CurrentPlaybackId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackSessions", x => x.Id);
                    table.UniqueConstraint("AK_PlaybackSessions_DiscordVoiceChannelId", x => x.DiscordVoiceChannelId);
                    table.ForeignKey(
                        name: "FK_PlaybackSessions_PlaybackTrackers_CurrentPlaybackId",
                        column: x => x.CurrentPlaybackId,
                        principalTable: "PlaybackTrackers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaybackSessions_SynthbotUsers_PlaylistOwnerId",
                        column: x => x.PlaylistOwnerId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUsers_ActivePlaybackSessionId",
                table: "SynthbotUsers",
                column: "ActivePlaybackSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackSessions_CurrentPlaybackId",
                table: "PlaybackSessions",
                column: "CurrentPlaybackId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackSessions_DiscordVoiceChannelId",
                table: "PlaybackSessions",
                column: "DiscordVoiceChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackSessions_PlaylistOwnerId",
                table: "PlaybackSessions",
                column: "PlaylistOwnerId",
                unique: true,
                filter: "[PlaylistOwnerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackTrackers_PlaybackSessionId",
                table: "PlaybackTrackers",
                column: "PlaybackSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SynthbotUsers_PlaybackSessions_ActivePlaybackSessionId",
                table: "SynthbotUsers",
                column: "ActivePlaybackSessionId",
                principalTable: "PlaybackSessions",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SynthbotUsers_PlaybackSessions_ActivePlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaybackSessions_PlaybackTrackers_CurrentPlaybackId",
                table: "PlaybackSessions");

            migrationBuilder.DropTable(
                name: "PlaybackTrackers");

            migrationBuilder.DropTable(
                name: "PlaybackSessions");

            migrationBuilder.DropIndex(
                name: "IX_SynthbotUsers_ActivePlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropColumn(
                name: "ActivePlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.DropColumn(
                name: "OwnedPlaybackSessionId",
                table: "SynthbotUsers");

            migrationBuilder.CreateTable(
                name: "VoiceChannelActivePlaylists",
                columns: table => new
                {
                    DiscordVoiceChannelId = table.Column<string>(nullable: false),
                    SpotifyPlaylistId = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceChannelActivePlaylists", x => new { x.DiscordVoiceChannelId, x.SpotifyPlaylistId });
                    table.ForeignKey(
                        name: "FK_VoiceChannelActivePlaylists_SynthbotUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelActivePlaylists_DiscordVoiceChannelId",
                table: "VoiceChannelActivePlaylists",
                column: "DiscordVoiceChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelActivePlaylists_SpotifyPlaylistId",
                table: "VoiceChannelActivePlaylists",
                column: "SpotifyPlaylistId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoiceChannelActivePlaylists_UserId",
                table: "VoiceChannelActivePlaylists",
                column: "UserId");
        }
    }
}
