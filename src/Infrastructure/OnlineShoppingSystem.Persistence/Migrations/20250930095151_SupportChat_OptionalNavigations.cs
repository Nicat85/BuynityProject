using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportChat_OptionalNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupportChatThreads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupportChatThreads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SupportChatThreads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "SupportChatMessages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupportChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupportChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SupportChatMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupportChatThreads");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupportChatThreads");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupportChatThreads");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupportChatMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupportChatMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SupportChatMessages");

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "SupportChatMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
