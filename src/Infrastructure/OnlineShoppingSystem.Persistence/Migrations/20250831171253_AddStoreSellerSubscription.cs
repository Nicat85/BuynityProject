using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreSellerSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreSellerSubscriptions_AspNetUsers_UserId",
                table: "StoreSellerSubscriptions");

            migrationBuilder.CreateTable(
                name: "SupportChatThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportChatThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportChatThreads_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportChatThreads_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInternalNote = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportChatMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportChatMessages_SupportChatThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "SupportChatThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatMessages_SenderId",
                table: "SupportChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatMessages_ThreadId_CreatedAt",
                table: "SupportChatMessages",
                columns: new[] { "ThreadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatThreads_AssignedToId",
                table: "SupportChatThreads",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatThreads_CustomerId",
                table: "SupportChatThreads",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatThreads_Status_LastMessageAt",
                table: "SupportChatThreads",
                columns: new[] { "Status", "LastMessageAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_StoreSellerSubscriptions_AspNetUsers_UserId",
                table: "StoreSellerSubscriptions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreSellerSubscriptions_AspNetUsers_UserId",
                table: "StoreSellerSubscriptions");

            migrationBuilder.DropTable(
                name: "SupportChatMessages");

            migrationBuilder.DropTable(
                name: "SupportChatThreads");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreSellerSubscriptions_AspNetUsers_UserId",
                table: "StoreSellerSubscriptions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
