using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class AddTrendDataPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrendDataJson",
                table: "HashtagHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrendMomentum",
                table: "HashtagHistory",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrendDataJson",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "TrendMomentum",
                table: "HashtagHistory");
        }
    }
}
