using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HashTag.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedSchemaForSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HashtagCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    DisplayNameVi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HashtagCategories_HashtagCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "HashtagCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HashtagSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastCrawled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hashtags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TagDisplay = table.Column<string>(type: "nvarchar(101)", maxLength: 101, nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TotalAppearances = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LatestViewCount = table.Column<long>(type: "bigint", nullable: true),
                    LatestPostCount = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hashtags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hashtags_HashtagCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "HashtagCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CrawlLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    HashtagsCollected = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrawlLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrawlLogs_HashtagSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "HashtagSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HashtagHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HashtagId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CollectedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HashtagHistory_HashtagSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "HashtagSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HashtagHistory_Hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HashtagKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HashtagId = table.Column<int>(type: "int", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KeywordVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RelevanceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0.5m),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HashtagKeywords_Hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HashtagMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HashtagId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewCount = table.Column<long>(type: "bigint", nullable: true),
                    PostCount = table.Column<long>(type: "bigint", nullable: true),
                    EngagementRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DifficultyScore = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    GrowthRate7d = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrowthRate30d = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PredictedViewMin = table.Column<long>(type: "bigint", nullable: true),
                    PredictedViewMax = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HashtagMetrics_Hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HashtagRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HashtagId1 = table.Column<int>(type: "int", nullable: false),
                    HashtagId2 = table.Column<int>(type: "int", nullable: false),
                    CoOccurrenceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CorrelationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastSeenTogether = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashtagRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HashtagRelations_Hashtags_HashtagId1",
                        column: x => x.HashtagId1,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HashtagRelations_Hashtags_HashtagId2",
                        column: x => x.HashtagId2,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HashtagSources",
                columns: new[] { "Id", "IsActive", "LastCrawled", "LastError", "Name", "Url" },
                values: new object[,]
                {
                    { 1, true, null, null, "TikTok", "https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag" },
                    { 2, true, null, null, "GoogleTrends", "https://trends.google.com/trends/trendingsearches/daily?geo=VN" },
                    { 3, true, null, null, "Buffer", "https://buffer.com/resources/tiktok-hashtags/" },
                    { 4, true, null, null, "Trollishly", "https://www.trollishly.com/tiktok-trending-hashtags/" },
                    { 5, true, null, null, "CapCut", "https://www.capcut.com/resource/tiktok-hashtag-guide" },
                    { 6, true, null, null, "TokChart", "https://tokchart.com/hashtags" },
                    { 7, true, null, null, "Countik", "https://countik.com/popular/hashtags" }
                });

            migrationBuilder.InsertData(
                table: "HashtagSources",
                columns: new[] { "Id", "LastCrawled", "LastError", "Name", "Url" },
                values: new object[] { 8, null, null, "Picuki", "" });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlLogs_SourceId_Success",
                table: "CrawlLogs",
                columns: new[] { "SourceId", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_CrawlLogs_StartedAt",
                table: "CrawlLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagCategories_Name",
                table: "HashtagCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashtagCategories_ParentCategoryId",
                table: "HashtagCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_CollectedDate",
                table: "HashtagHistory",
                column: "CollectedDate");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_HashtagId_SourceId_CollectedDate",
                table: "HashtagHistory",
                columns: new[] { "HashtagId", "SourceId", "CollectedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_Rank",
                table: "HashtagHistory",
                column: "Rank");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagHistory_SourceId",
                table: "HashtagHistory",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagKeywords_HashtagId",
                table: "HashtagKeywords",
                column: "HashtagId");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagKeywords_Keyword",
                table: "HashtagKeywords",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagKeywords_Keyword_HashtagId",
                table: "HashtagKeywords",
                columns: new[] { "Keyword", "HashtagId" });

            migrationBuilder.CreateIndex(
                name: "IX_HashtagKeywords_RelevanceScore",
                table: "HashtagKeywords",
                column: "RelevanceScore");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagMetrics_Date",
                table: "HashtagMetrics",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagMetrics_DifficultyScore",
                table: "HashtagMetrics",
                column: "DifficultyScore");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagMetrics_HashtagId_Date",
                table: "HashtagMetrics",
                columns: new[] { "HashtagId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_HashtagRelations_CorrelationScore",
                table: "HashtagRelations",
                column: "CorrelationScore");

            migrationBuilder.CreateIndex(
                name: "IX_HashtagRelations_HashtagId1_HashtagId2",
                table: "HashtagRelations",
                columns: new[] { "HashtagId1", "HashtagId2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashtagRelations_HashtagId2",
                table: "HashtagRelations",
                column: "HashtagId2");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_CategoryId",
                table: "Hashtags",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_DifficultyLevel",
                table: "Hashtags",
                column: "DifficultyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_IsActive",
                table: "Hashtags",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_LastSeen",
                table: "Hashtags",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_Tag",
                table: "Hashtags",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashtagSources_Name",
                table: "HashtagSources",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrawlLogs");

            migrationBuilder.DropTable(
                name: "HashtagHistory");

            migrationBuilder.DropTable(
                name: "HashtagKeywords");

            migrationBuilder.DropTable(
                name: "HashtagMetrics");

            migrationBuilder.DropTable(
                name: "HashtagRelations");

            migrationBuilder.DropTable(
                name: "HashtagSources");

            migrationBuilder.DropTable(
                name: "Hashtags");

            migrationBuilder.DropTable(
                name: "HashtagCategories");
        }
    }
}
