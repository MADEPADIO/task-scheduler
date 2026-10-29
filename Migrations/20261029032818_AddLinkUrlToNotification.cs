using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSchedulerApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkUrlToNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "Notifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "Notifications");
        }
    }
}
