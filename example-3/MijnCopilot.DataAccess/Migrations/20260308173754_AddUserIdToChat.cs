using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MijnCopilot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CHATS",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CHATS_UserId",
                table: "CHATS",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CHATS_UserId",
                table: "CHATS");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CHATS");
        }
    }
}
