using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coinecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedAssetsTypeToCbor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_NftsByAddress",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "TxHash",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "OutputIndex",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "PolicyId",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "AssetName",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "UtxoStatus",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.AddColumn<byte[]>(
                name: "AssetsCbor",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NftsByAddress",
                schema: "coinecta",
                table: "NftsByAddress",
                columns: new[] { "Address", "Slot" });

            migrationBuilder.CreateIndex(
                name: "IX_StakeRequestByAddresses_Address",
                schema: "coinecta",
                table: "StakeRequestByAddresses",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_StakeRequestByAddresses_Slot",
                schema: "coinecta",
                table: "StakeRequestByAddresses",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_StakeRequestByAddresses_TxHash",
                schema: "coinecta",
                table: "StakeRequestByAddresses",
                column: "TxHash");

            migrationBuilder.CreateIndex(
                name: "IX_StakeRequestByAddresses_TxIndex",
                schema: "coinecta",
                table: "StakeRequestByAddresses",
                column: "TxIndex");

            migrationBuilder.CreateIndex(
                name: "IX_StakePositionByStakeKeys_Slot",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_StakePositionByStakeKeys_StakeKey",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                column: "StakeKey");

            migrationBuilder.CreateIndex(
                name: "IX_StakePositionByStakeKeys_TxHash",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                column: "TxHash");

            migrationBuilder.CreateIndex(
                name: "IX_StakePositionByStakeKeys_TxIndex",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                column: "TxIndex");

            migrationBuilder.CreateIndex(
                name: "IX_StakePositionByStakeKeys_UtxoStatus",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                column: "UtxoStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StakePoolByAddresses_Address",
                schema: "coinecta",
                table: "StakePoolByAddresses",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_StakePoolByAddresses_Slot",
                schema: "coinecta",
                table: "StakePoolByAddresses",
                column: "Slot");

            migrationBuilder.CreateIndex(
                name: "IX_StakePoolByAddresses_TxHash",
                schema: "coinecta",
                table: "StakePoolByAddresses",
                column: "TxHash");

            migrationBuilder.CreateIndex(
                name: "IX_StakePoolByAddresses_TxIndex",
                schema: "coinecta",
                table: "StakePoolByAddresses",
                column: "TxIndex");

            migrationBuilder.CreateIndex(
                name: "IX_StakePoolByAddresses_UtxoStatus",
                schema: "coinecta",
                table: "StakePoolByAddresses",
                column: "UtxoStatus");

            migrationBuilder.CreateIndex(
                name: "IX_NftsByAddress_Address",
                schema: "coinecta",
                table: "NftsByAddress",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_NftsByAddress_Slot",
                schema: "coinecta",
                table: "NftsByAddress",
                column: "Slot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StakeRequestByAddresses_Address",
                schema: "coinecta",
                table: "StakeRequestByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakeRequestByAddresses_Slot",
                schema: "coinecta",
                table: "StakeRequestByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakeRequestByAddresses_TxHash",
                schema: "coinecta",
                table: "StakeRequestByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakeRequestByAddresses_TxIndex",
                schema: "coinecta",
                table: "StakeRequestByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakePositionByStakeKeys_Slot",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropIndex(
                name: "IX_StakePositionByStakeKeys_StakeKey",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropIndex(
                name: "IX_StakePositionByStakeKeys_TxHash",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropIndex(
                name: "IX_StakePositionByStakeKeys_TxIndex",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropIndex(
                name: "IX_StakePositionByStakeKeys_UtxoStatus",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropIndex(
                name: "IX_StakePoolByAddresses_Address",
                schema: "coinecta",
                table: "StakePoolByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakePoolByAddresses_Slot",
                schema: "coinecta",
                table: "StakePoolByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakePoolByAddresses_TxHash",
                schema: "coinecta",
                table: "StakePoolByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakePoolByAddresses_TxIndex",
                schema: "coinecta",
                table: "StakePoolByAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StakePoolByAddresses_UtxoStatus",
                schema: "coinecta",
                table: "StakePoolByAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NftsByAddress",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropIndex(
                name: "IX_NftsByAddress_Address",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropIndex(
                name: "IX_NftsByAddress_Slot",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.DropColumn(
                name: "AssetsCbor",
                schema: "coinecta",
                table: "NftsByAddress");

            migrationBuilder.AddColumn<string>(
                name: "TxHash",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "OutputIndex",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PolicyId",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssetName",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UtxoStatus",
                schema: "coinecta",
                table: "NftsByAddress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_NftsByAddress",
                schema: "coinecta",
                table: "NftsByAddress",
                columns: new[] { "TxHash", "OutputIndex", "Slot", "PolicyId", "AssetName", "UtxoStatus" });
        }
    }
}
