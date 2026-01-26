using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlLogMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogMessages",
                table: "CrawlLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogMessages",
                table: "CrawlLogs");
        }
    }
}
