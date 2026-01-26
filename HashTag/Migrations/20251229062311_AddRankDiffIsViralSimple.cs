using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class AddRankDiffIsViralSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to HashtagHistory table
            migrationBuilder.AddColumn<bool>(
                name: "IsViral",
                table: "HashtagHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RankDiff",
                table: "HashtagHistory",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsViral",
                table: "HashtagHistory");

            migrationBuilder.DropColumn(
                name: "RankDiff",
                table: "HashtagHistory");
        }
    }
}
