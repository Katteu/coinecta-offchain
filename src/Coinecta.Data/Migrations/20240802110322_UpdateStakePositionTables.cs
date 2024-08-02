using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coinecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStakePositionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Interest_Denominator",
                schema: "coinecta",
                table: "StakePositionsHistory");

            migrationBuilder.DropColumn(
                name: "Interest_Numerator",
                schema: "coinecta",
                table: "StakePositionsHistory");

            migrationBuilder.DropColumn(
                name: "Interest_Denominator",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.DropColumn(
                name: "Interest_Numerator",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.AddColumn<byte[]>(
                name: "InterestCbor",
                schema: "coinecta",
                table: "StakePositionsHistory",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "InterestCbor",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterestCbor",
                schema: "coinecta",
                table: "StakePositionsHistory");

            migrationBuilder.DropColumn(
                name: "InterestCbor",
                schema: "coinecta",
                table: "StakePositionByStakeKeys");

            migrationBuilder.AddColumn<decimal>(
                name: "Interest_Denominator",
                schema: "coinecta",
                table: "StakePositionsHistory",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Interest_Numerator",
                schema: "coinecta",
                table: "StakePositionsHistory",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Interest_Denominator",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Interest_Numerator",
                schema: "coinecta",
                table: "StakePositionByStakeKeys",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
