using System;
using Microsoft.EntityFrameworkCore.Migrations;
using team_hub_auth.Data;

#nullable disable

namespace team_hub_auth.Migrations
{
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260701112000_AddRefreshTokenSupport")]
    public partial class AddRefreshTokenSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefreshTokenExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_RefreshTokenHash",
                table: "users",
                column: "RefreshTokenHash",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_RefreshTokenHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "users");
        }
    }
}
