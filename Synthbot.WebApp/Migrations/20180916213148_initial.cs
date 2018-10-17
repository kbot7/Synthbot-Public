using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SynthbotUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    DiscordUsername = table.Column<string>(nullable: true),
                    DiscordUserId = table.Column<string>(nullable: true),
                    DiscordEmailAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynthbotUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralTokenReceipts",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ReferralUserId = table.Column<string>(nullable: true),
                    ReceivedTS = table.Column<DateTime>(nullable: false),
                    Claimed = table.Column<bool>(nullable: false),
                    ClaimedTS = table.Column<DateTime>(nullable: false),
                    ReferrerSignalrUser = table.Column<string>(nullable: true),
                    ReplySent = table.Column<bool>(nullable: false),
                    ReplyError = table.Column<bool>(nullable: false),
                    SynthbotUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralTokenReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralTokenReceipts_SynthbotUsers_SynthbotUserId",
                        column: x => x.SynthbotUserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SynthbotUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    SynthbotUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynthbotUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SynthbotUserClaims_SynthbotUsers_SynthbotUserId",
                        column: x => x.SynthbotUserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SynthbotUserClaims_SynthbotUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SynthbotUserLoginProviders",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false),
                    SynthbotUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynthbotUserLoginProviders", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_SynthbotUserLoginProviders_SynthbotUsers_SynthbotUserId",
                        column: x => x.SynthbotUserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SynthbotUserLoginProviders_SynthbotUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SynthbotUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                    Name = table.Column<string>(maxLength: 128, nullable: false),
                    Value = table.Column<string>(nullable: true),
                    SynthbotUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynthbotUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_SynthbotUserTokens_SynthbotUsers_SynthbotUserId",
                        column: x => x.SynthbotUserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SynthbotUserTokens_SynthbotUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SynthbotUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceChannelActivePlaylists",
                columns: table => new
                {
                    SpotifyPlaylistId = table.Column<string>(nullable: false),
                    DiscordVoiceChannelId = table.Column<string>(nullable: false),
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
                name: "IX_ReferralTokenReceipts_SynthbotUserId",
                table: "ReferralTokenReceipts",
                column: "SynthbotUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUserClaims_SynthbotUserId",
                table: "SynthbotUserClaims",
                column: "SynthbotUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUserClaims_UserId",
                table: "SynthbotUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUserLoginProviders_SynthbotUserId",
                table: "SynthbotUserLoginProviders",
                column: "SynthbotUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUserLoginProviders_UserId",
                table: "SynthbotUserLoginProviders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "SynthbotUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "SynthbotUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SynthbotUserTokens_SynthbotUserId",
                table: "SynthbotUserTokens",
                column: "SynthbotUserId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralTokenReceipts");

            migrationBuilder.DropTable(
                name: "SynthbotUserClaims");

            migrationBuilder.DropTable(
                name: "SynthbotUserLoginProviders");

            migrationBuilder.DropTable(
                name: "SynthbotUserTokens");

            migrationBuilder.DropTable(
                name: "VoiceChannelActivePlaylists");

            migrationBuilder.DropTable(
                name: "SynthbotUsers");
        }
    }
}
