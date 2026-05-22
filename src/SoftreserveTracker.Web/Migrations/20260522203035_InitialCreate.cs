using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftreserveTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rosters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccessToken = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rosters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RosterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Rosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "Rosters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RosterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidWeeks_Rosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "Rosters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlusOneBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlusOneBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlusOneBalances_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaidWeekId = table.Column<int>(type: "INTEGER", nullable: false),
                    RaidType = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SoftresId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidSessions_RaidWeeks_RaidWeekId",
                        column: x => x.RaidWeekId,
                        principalTable: "RaidWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LootAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaidSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    AwardedToRaw = table.Column<string>(type: "TEXT", nullable: false),
                    SoftReserveWin = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDisenchanted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SoftresId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootAwards_Players_WinnerPlayerId",
                        column: x => x.WinnerPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LootAwards_RaidSessions_RaidSessionId",
                        column: x => x.RaidSessionId,
                        principalTable: "RaidSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionReservationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaidSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemDropped = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayerReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlusOneDelta = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedToPlayerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionReservationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionReservationResults_Players_AwardedToPlayerId",
                        column: x => x.AwardedToPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionReservationResults_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionReservationResults_RaidSessions_RaidSessionId",
                        column: x => x.RaidSessionId,
                        principalTable: "RaidSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftReserves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaidSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    BossSource = table.Column<string>(type: "TEXT", nullable: true),
                    PlayerClass = table.Column<string>(type: "TEXT", nullable: true),
                    Spec = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    ReservedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftReserves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftReserves_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoftReserves_RaidSessions_RaidSessionId",
                        column: x => x.RaidSessionId,
                        principalTable: "RaidSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RaidSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_RaidSessions_RaidSessionId",
                        column: x => x.RaidSessionId,
                        principalTable: "RaidSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LootAwards_RaidSessionId",
                table: "LootAwards",
                column: "RaidSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LootAwards_WinnerPlayerId",
                table: "LootAwards",
                column: "WinnerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RosterId_NormalizedName",
                table: "Players",
                columns: new[] { "RosterId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlusOneBalances_PlayerId_ItemId",
                table: "PlusOneBalances",
                columns: new[] { "PlayerId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidSessions_RaidWeekId_RaidType_SessionDate_SoftresId",
                table: "RaidSessions",
                columns: new[] { "RaidWeekId", "RaidType", "SessionDate", "SoftresId" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidWeeks_RosterId_PeriodStart",
                table: "RaidWeeks",
                columns: new[] { "RosterId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidWeeks_RosterId_WeekNumber",
                table: "RaidWeeks",
                columns: new[] { "RosterId", "WeekNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rosters_AccessToken",
                table: "Rosters",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionReservationResults_AwardedToPlayerId",
                table: "SessionReservationResults",
                column: "AwardedToPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionReservationResults_PlayerId",
                table: "SessionReservationResults",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionReservationResults_RaidSessionId_PlayerId_ItemId",
                table: "SessionReservationResults",
                columns: new[] { "RaidSessionId", "PlayerId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_PlayerId",
                table: "SoftReserves",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftReserves_RaidSessionId_PlayerId_ItemId",
                table: "SoftReserves",
                columns: new[] { "RaidSessionId", "PlayerId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_RaidSessionId",
                table: "UploadedFiles",
                column: "RaidSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LootAwards");

            migrationBuilder.DropTable(
                name: "PlusOneBalances");

            migrationBuilder.DropTable(
                name: "SessionReservationResults");

            migrationBuilder.DropTable(
                name: "SoftReserves");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "RaidSessions");

            migrationBuilder.DropTable(
                name: "RaidWeeks");

            migrationBuilder.DropTable(
                name: "Rosters");
        }
    }
}
