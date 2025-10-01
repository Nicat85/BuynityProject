using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineShoppingSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Update_AppUser_FinCode_Check : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FinCode",
                table: "AspNetUsers",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FinCode",
                table: "AspNetUsers",
                column: "FinCode",
                unique: true,
                filter: "[FinCode] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppUser_FinCode_Format",
                table: "AspNetUsers",
                sql: "[FinCode] IS NULL OR [FinCode] LIKE '[A-Z][A-Z][A-Z][A-Z][A-Z][A-Z][A-Z]'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_FinCode",
                table: "AspNetUsers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppUser_FinCode_Format",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FinCode",
                table: "AspNetUsers");
        }
    }
}
