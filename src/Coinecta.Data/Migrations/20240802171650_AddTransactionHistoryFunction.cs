using Microsoft.EntityFrameworkCore.Migrations;
﻿using Coinecta.Data.Utils;

#nullable disable

namespace Coinecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionHistoryFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET search_path TO coinecta");

            string sql = ResourceUtils.GetEmbeddedResourceSql("CreateGetTransactionHistoryByAddress.sql");
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SET search_path TO coinecta");

            string sql = ResourceUtils.GetEmbeddedResourceSql("DropGetTransactionHistoryByAddress.sql");
            migrationBuilder.Sql(sql);
        }
    }
}
