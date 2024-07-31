using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coinecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStakePositionByStakeKeyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StakePositionByStakeKeys",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropColumn(
                name: "UtxoStatus",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.AddColumn<string>(
                name: "TxOutputRef",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StakePositionByStakeKeys",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                columns: new[] { "StakeKey", "Slot", "TxHash", "TxIndex" });

            migrationBuilder.CreateTable(
                name: "StakePositionHistory",
                schema: "coinecta",
                columns: table => new
                {
                    StakeKey = table.Column<string>(type: "text", nullable: false),
                    Slot = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TxHash = table.Column<string>(type: "text", nullable: false),
                    TxIndex = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UtxoStatus = table.Column<int>(type: "integer", nullable: false),
                    TxOutputRef = table.Column<string>(type: "text", nullable: false),
                    Amount_Coin = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Amount_MultiAssetJson = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    LockTime = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Interest_Numerator = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Interest_Denominator = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StakePositionJson = table.Column<JsonElement>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StakePositionHistory", x => new { x.StakeKey, x.Slot, x.TxHash, x.TxIndex, x.UtxoStatus });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StakePositionHistory",
                schema: "coinecta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StakePositionByStakeKeys",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropColumn(
                name: "TxOutputRef",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.AddColumn<int>(
                name: "UtxoStatus",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StakePositionByStakeKeys",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                columns: new[] { "StakeKey", "Slot", "TxHash", "TxIndex", "UtxoStatus" });
        }
    }
}
