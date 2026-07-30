using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace team_hub_auth.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNameSurnameIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_Name",
                table: "users",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_users_Surname",
                table: "users",
                column: "Surname");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Name",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Surname",
                table: "users");
        }
    }
}
