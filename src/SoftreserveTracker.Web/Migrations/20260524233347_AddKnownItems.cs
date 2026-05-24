using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftreserveTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnownItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownItems", x => x.ItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnownItems");
        }
    }
}
