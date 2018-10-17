using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class removeuserownedplaylist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOwnedPlaylists");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
