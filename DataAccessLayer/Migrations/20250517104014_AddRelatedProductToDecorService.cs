using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedProductToDecorService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RelatedProductId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSeasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSeasons_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSeasons_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelatedProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalItem = table.Column<int>(type: "int", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatedProducts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelatedProducts_DecorServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "DecorServices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RelatedProductItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelatedProductId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedProductItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelatedProductItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RelatedProductItems_RelatedProducts_RelatedProductId",
                        column: x => x.RelatedProductId,
                        principalTable: "RelatedProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RelatedProductId",
                table: "Bookings",
                column: "RelatedProductId",
                unique: true,
                filter: "[RelatedProductId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSeasons_ProductId",
                table: "ProductSeasons",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSeasons_SeasonId",
                table: "ProductSeasons",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedProductItems_ProductId",
                table: "RelatedProductItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedProductItems_RelatedProductId",
                table: "RelatedProductItems",
                column: "RelatedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedProducts_AccountId",
                table: "RelatedProducts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RelatedProducts_ServiceId",
                table: "RelatedProducts",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_RelatedProducts_RelatedProductId",
                table: "Bookings",
                column: "RelatedProductId",
                principalTable: "RelatedProducts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_RelatedProducts_RelatedProductId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "ProductSeasons");

            migrationBuilder.DropTable(
                name: "RelatedProductItems");

            migrationBuilder.DropTable(
                name: "RelatedProducts");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RelatedProductId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RelatedProductId",
                table: "Bookings");
        }
    }
}
