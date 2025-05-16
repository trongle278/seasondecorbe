using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccessObject.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalizationFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFilterEnabled",
                table: "Accounts",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountCategoryPreferences",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    DecorCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCategoryPreferences", x => new { x.AccountId, x.DecorCategoryId });
                    table.ForeignKey(
                        name: "FK_AccountCategoryPreferences_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountCategoryPreferences_DecorCategories_DecorCategoryId",
                        column: x => x.DecorCategoryId,
                        principalTable: "DecorCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountSeasonPreferences",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSeasonPreferences", x => new { x.AccountId, x.SeasonId });
                    table.ForeignKey(
                        name: "FK_AccountSeasonPreferences_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountSeasonPreferences_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountStylePreferences",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    DecorationStyleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountStylePreferences", x => new { x.AccountId, x.DecorationStyleId });
                    table.ForeignKey(
                        name: "FK_AccountStylePreferences_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountStylePreferences_DecorationStyles_DecorationStyleId",
                        column: x => x.DecorationStyleId,
                        principalTable: "DecorationStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountCategoryPreferences_DecorCategoryId",
                table: "AccountCategoryPreferences",
                column: "DecorCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSeasonPreferences_SeasonId",
                table: "AccountSeasonPreferences",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountStylePreferences_DecorationStyleId",
                table: "AccountStylePreferences",
                column: "DecorationStyleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCategoryPreferences");

            migrationBuilder.DropTable(
                name: "AccountSeasonPreferences");

            migrationBuilder.DropTable(
                name: "AccountStylePreferences");

            migrationBuilder.DropColumn(
                name: "IsFilterEnabled",
                table: "Accounts");
        }
    }
}
