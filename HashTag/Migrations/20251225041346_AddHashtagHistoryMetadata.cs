using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class AddHashtagHistoryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "HashtagHistory",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturedCreatorsJson",
                table: "HashtagHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PostCount",
                table: "HashtagHistory",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RankChange",
                table: "HashtagHistory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrendScore",
                table: "HashtagHistory",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ViewCount",
                table: "HashtagHistory",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_Category",
                table: "HashtagHistory",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_ViewCount",
                table: "HashtagHistory",
                column: "ViewCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HashtagHistory_Category",
                table: "HashtagHistory");

            migrationBuilder.DropIndex(
                name: "IX_HashtagHistory_ViewCount",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "FeaturedCreatorsJson",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "PostCount",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "RankChange",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "TrendScore",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "HashtagHistory");
        }
    }
}
