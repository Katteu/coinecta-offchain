using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coinecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStakePositionHistoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StakePositionHistory",
                schema: "coinecta",
                table: "StakePositionHistory");

            migrationBuilder.RenameTable(
                name: "StakePositionHistory",
                schema: "coinecta",
                newName: "StakePositionsHistory",
                newSchema: "coinecta");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StakePositionsHistory",
                schema: "coinecta",
                table: "StakePositionsHistory",
                columns: new[] { "StakeKey", "Slot", "TxHash", "TxIndex", "UtxoStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StakePositionsHistory",
                schema: "coinecta",
                table: "StakePositionsHistory");

            migrationBuilder.RenameTable(
                name: "StakePositionsHistory",
                schema: "coinecta",
                newName: "StakePositionHistory",
                newSchema: "coinecta");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StakePositionHistory",
                schema: "coinecta",
                table: "StakePositionHistory",
                columns: new[] { "StakeKey", "Slot", "TxHash", "TxIndex", "UtxoStatus" });
        }
    }
}
