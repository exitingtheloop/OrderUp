using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderUp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAddonJunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductAddons",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AddonId = table.Column<int>(type: "int", nullable: false),
                    MaxPerItemOverride = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAddons", x => new { x.ProductId, x.AddonId });
                    table.ForeignKey(
                        name: "FK_ProductAddons_Addons_AddonId",
                        column: x => x.AddonId,
                        principalTable: "Addons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAddons_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAddons_AddonId",
                table: "ProductAddons",
                column: "AddonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductAddons");
        }
    }
}
