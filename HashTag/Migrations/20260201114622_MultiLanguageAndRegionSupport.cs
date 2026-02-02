using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class MultiLanguageAndRegionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old unique index on Tag only (if it exists)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_Tag' AND object_id = OBJECT_ID('Hashtags'))
                BEGIN
                    DROP INDEX [IX_Hashtags_Tag] ON [Hashtags];
                END
            ");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Hashtags",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "VN");

            migrationBuilder.AddColumn<string>(
                name: "ContentEn",
                table: "BlogPosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcerptEn",
                table: "BlogPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescriptionEn",
                table: "BlogPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywordsEn",
                table: "BlogPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitleEn",
                table: "BlogPosts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlugEn",
                table: "BlogPosts",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleEn",
                table: "BlogPosts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Create CountryCode index if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_CountryCode' AND object_id = OBJECT_ID('Hashtags'))
                BEGIN
                    CREATE INDEX [IX_Hashtags_CountryCode] ON [Hashtags] ([CountryCode]);
                END
            ");

            // Trim any leading/trailing spaces from Tag
            migrationBuilder.Sql("UPDATE Hashtags SET Tag = LTRIM(RTRIM(Tag)) WHERE Tag <> LTRIM(RTRIM(Tag));");

            // Update HashtagHistory to point to survivor
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           FIRST_VALUE(Id) OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS SurvivorId,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                UPDATE hh
                SET hh.HashtagId = d.SurvivorId
                FROM HashtagHistory hh
                INNER JOIN Duplicates d ON hh.HashtagId = d.Id
                WHERE d.RowNum > 1;
            ");

            // Update HashtagMetrics to point to survivor
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           FIRST_VALUE(Id) OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS SurvivorId,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                UPDATE hm
                SET hm.HashtagId = d.SurvivorId
                FROM HashtagMetrics hm
                INNER JOIN Duplicates d ON hm.HashtagId = d.Id
                WHERE d.RowNum > 1;
            ");

            // Update HashtagKeywords to point to survivor
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           FIRST_VALUE(Id) OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS SurvivorId,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                UPDATE hk
                SET hk.HashtagId = d.SurvivorId
                FROM HashtagKeywords hk
                INNER JOIN Duplicates d ON hk.HashtagId = d.Id
                WHERE d.RowNum > 1;
            ");

            // Update HashtagRelations (HashtagId1)
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           FIRST_VALUE(Id) OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS SurvivorId,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                UPDATE hr
                SET hr.HashtagId1 = d.SurvivorId
                FROM HashtagRelations hr
                INNER JOIN Duplicates d ON hr.HashtagId1 = d.Id
                WHERE d.RowNum > 1;
            ");

            // Update HashtagRelations (HashtagId2)
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           FIRST_VALUE(Id) OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS SurvivorId,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                UPDATE hr
                SET hr.HashtagId2 = d.SurvivorId
                FROM HashtagRelations hr
                INNER JOIN Duplicates d ON hr.HashtagId2 = d.Id
                WHERE d.RowNum > 1;
            ");

            // Delete duplicate hashtags (keep the one with most recent LastSeen)
            migrationBuilder.Sql(@"
                ;WITH Duplicates AS (
                    SELECT Id, Tag, CountryCode,
                           ROW_NUMBER() OVER (PARTITION BY Tag, CountryCode ORDER BY LastSeen DESC, TotalAppearances DESC, Id ASC) AS RowNum
                    FROM Hashtags
                )
                DELETE FROM Duplicates WHERE RowNum > 1;
            ");

            // Create unique index on Tag + CountryCode
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_Tag_CountryCode' AND object_id = OBJECT_ID('Hashtags'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_Hashtags_Tag_CountryCode] ON [Hashtags] ([Tag], [CountryCode]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hashtags_CountryCode",
                table: "Hashtags");

            migrationBuilder.DropIndex(
                name: "IX_Hashtags_Tag_CountryCode",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "ContentEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "ExcerptEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "MetaDescriptionEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "MetaKeywordsEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "MetaTitleEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "SlugEn",
                table: "BlogPosts");

            migrationBuilder.DropColumn(
                name: "TitleEn",
                table: "BlogPosts");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_Tag",
                table: "Hashtags",
                column: "Tag",
                unique: true);
        }
    }
}
