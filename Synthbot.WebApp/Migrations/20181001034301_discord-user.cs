using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Synthbot.WebApp.Migrations
{
	public partial class discorduser : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "DiscordEmailAddress",
				table: "SynthbotUsers");

			migrationBuilder.DropColumn(
				name: "DiscordUsername",
				table: "SynthbotUsers");

			migrationBuilder.AlterColumn<string>(
				name: "DiscordUserId",
				table: "SynthbotUsers",
				nullable: false,
				oldClrType: typeof(string),
				oldNullable: true);

			migrationBuilder.CreateTable(
				name: "DiscordUsers",
				columns: table => new
				{
					DiscordUserId = table.Column<string>(nullable: false),
					DiscordUsername = table.Column<string>(nullable: true),
					DiscordEmailAddress = table.Column<string>(nullable: true),
					InvitedTs = table.Column<DateTime>(nullable: false),
					UserStatus = table.Column<string>(nullable: false, defaultValue: "NoResponse")
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_DiscordUsers", x => x.DiscordUserId);
				});

			migrationBuilder.CreateIndex(
				name: "IX_SynthbotUsers_DiscordUserId",
				table: "SynthbotUsers",
				column: "DiscordUserId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_DiscordUsers_DiscordUserId",
				table: "DiscordUsers",
				column: "DiscordUserId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_DiscordUsers_DiscordUsername",
				table: "DiscordUsers",
				column: "DiscordUsername");

			migrationBuilder.Sql(@"
			INSERT INTO [DiscordUsers] ([DiscordUserId], [UserStatus], [InvitedTs])
			SELECT [DiscordUserId], 'Registered', GETUTCDATE() FROM [SynthbotUsers]");

			migrationBuilder.AddForeignKey(
				name: "FK_SynthbotUsers_DiscordUsers_DiscordUserId",
				table: "SynthbotUsers",
				column: "DiscordUserId",
				principalTable: "DiscordUsers",
				principalColumn: "DiscordUserId",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_SynthbotUsers_DiscordUsers_DiscordUserId",
				table: "SynthbotUsers");

			migrationBuilder.DropTable(
				name: "DiscordUsers");

			migrationBuilder.DropIndex(
				name: "IX_SynthbotUsers_DiscordUserId",
				table: "SynthbotUsers");

			migrationBuilder.AlterColumn<string>(
				name: "DiscordUserId",
				table: "SynthbotUsers",
				nullable: true,
				oldClrType: typeof(string));

			migrationBuilder.AddColumn<string>(
				name: "DiscordEmailAddress",
				table: "SynthbotUsers",
				nullable: true);

			migrationBuilder.AddColumn<string>(
				name: "DiscordUsername",
				table: "SynthbotUsers",
				nullable: true);
		}
	}
}
