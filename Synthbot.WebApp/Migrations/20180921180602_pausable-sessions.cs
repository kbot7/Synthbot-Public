using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
    public partial class pausablesessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "PlaybackTrackers",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Paused",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PausedAtMs",
                table: "PlaybackTrackers",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedUtc",
                table: "PlaybackTrackers",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Resumed",
                table: "PlaybackTrackers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumedUtc",
                table: "PlaybackTrackers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobId",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "Paused",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "PausedAtMs",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "PausedUtc",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "Resumed",
                table: "PlaybackTrackers");

            migrationBuilder.DropColumn(
                name: "ResumedUtc",
                table: "PlaybackTrackers");
        }
    }
}
