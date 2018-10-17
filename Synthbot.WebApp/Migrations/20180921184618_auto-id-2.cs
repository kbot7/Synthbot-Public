using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class autoid2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PlaybackSessions",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(string));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PlaybackTrackers",
                nullable: false,
                oldClrType: typeof(string),
                oldDefaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PlaybackSessions",
                nullable: false,
                oldClrType: typeof(string),
                oldDefaultValueSql: "NEWID()");
        }
    }
}
