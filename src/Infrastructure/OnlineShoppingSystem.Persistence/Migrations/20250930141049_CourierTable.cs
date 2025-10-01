using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSystem.Persistence.Migrations
{
    public partial class CourierTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Köhnə FK-ları müvəqqəti silirik (Restrict-ə keçməzdən əvvəl)
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            // 1) SellerId-i ƏVVƏLCƏ nullable əlavə et (DEFAULT YOXDUR!)
            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            // 2) courierAssignments cədvəlini yarat
            migrationBuilder.CreateTable(
                name: "courierAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courierAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_courierAssignments_AspNetUsers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_courierAssignments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3) Indexlər
            migrationBuilder.CreateIndex(
                name: "IX_Products_SellerId",
                table: "Products",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status_IsSecondHand_IsFromStore",
                table: "Products",
                columns: new[] { "Status", "IsSecondHand", "IsFromStore" });

            migrationBuilder.CreateIndex(
                name: "IX_courierAssignments_CourierId",
                table: "courierAssignments",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_courierAssignments_OrderId",
                table: "courierAssignments",
                column: "OrderId",
                unique: true);

            // 4) Legacy Seller user (yoxdursa) — sabit GUID
            var legacySellerId = "11111111-1111-1111-1111-111111111111";
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetUsers] WHERE [Id] = '{legacySellerId}')
BEGIN
    INSERT INTO [dbo].[AspNetUsers]
        ([Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],
         [EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],
         [PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
    VALUES
        ('{legacySellerId}',
         'legacy.seller',
         'LEGACY.SELLER',
         'legacy.seller@system.local',
         'LEGACY.SELLER@SYSTEM.LOCAL',
         0, NULL, NEWID(), NEWID(),
         0, 0, 0, 0)
END
");

            // 5) Backfill:
            // 5.1. NULL və 0-GUID olanları mövcud UserId-ə kopyala
            migrationBuilder.Sql(@"
UPDATE P
SET P.SellerId = P.UserId
FROM [dbo].[Products] AS P
INNER JOIN [dbo].[AspNetUsers] U ON U.Id = P.UserId
WHERE P.SellerId IS NULL
   OR P.SellerId = '00000000-0000-0000-0000-000000000000';
");

            // 5.2. Hələ də AspNetUsers-də olmayan SellerId-ləri Legacy Seller-ə yönəlt
            migrationBuilder.Sql($@"
UPDATE P
SET P.SellerId = '{legacySellerId}'
FROM [dbo].[Products] AS P
LEFT JOIN [dbo].[AspNetUsers] U ON U.Id = P.SellerId
WHERE U.Id IS NULL
  AND P.SellerId IS NOT NULL;
");

            // 6) SellerId-i NOT NULL et (data artıq təmizdir)
            migrationBuilder.AlterColumn<Guid>(
                name: "SellerId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 7) FK-ları Restrict ilə əlavə et
            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_SellerId",
                table: "Products",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_SellerId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "courierAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Products_SellerId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Status_IsSecondHand_IsFromStore",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Products");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
