using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftreserveTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLootRolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LootRolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LootAwardId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlayerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RollAmount = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerClass = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Classification = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: true),
                    PlusOneState = table.Column<int>(type: "INTEGER", nullable: true),
                    RolledAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootRolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootRolls_LootAwards_LootAwardId",
                        column: x => x.LootAwardId,
                        principalTable: "LootAwards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LootRolls_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LootRolls_LootAwardId_PlayerId",
                table: "LootRolls",
                columns: new[] { "LootAwardId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_LootRolls_PlayerId",
                table: "LootRolls",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LootRolls");
        }
    }
}
