using System;
using System.Collections.Generic;
using HashTag.Models;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Data;

public partial class TrendTagDbContext : DbContext
{
    public TrendTagDbContext()
    {
    }

    public TrendTagDbContext(DbContextOptions<TrendTagDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CrawlLog> CrawlLogs { get; set; }

    public virtual DbSet<Hashtag> Hashtags { get; set; }

    public virtual DbSet<HashtagCategory> HashtagCategories { get; set; }

    public virtual DbSet<HashtagHistory> HashtagHistories { get; set; }

    public virtual DbSet<HashtagKeyword> HashtagKeywords { get; set; }

    public virtual DbSet<HashtagMetrics> HashtagMetrics { get; set; }

    public virtual DbSet<HashtagRelation> HashtagRelations { get; set; }

    public virtual DbSet<HashtagSource> HashtagSources { get; set; }

    // Blog System
    public virtual DbSet<BlogPost> BlogPosts { get; set; }

    public virtual DbSet<BlogCategory> BlogCategories { get; set; }

    public virtual DbSet<BlogTag> BlogTags { get; set; }

    public virtual DbSet<BlogPostTag> BlogPostTags { get; set; }

    // Hashtag Generator
    public virtual DbSet<HashtagGeneration> HashtagGenerations { get; set; }

    public virtual DbSet<GenerationHashtagSelection> GenerationHashtagSelections { get; set; }

    public virtual DbSet<GenerationRateLimit> GenerationRateLimits { get; set; }

    // System Logs
    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CrawlLog>(entity =>
        {
            entity.HasIndex(e => new { e.SourceId, e.Success }, "IX_CrawlLogs_SourceId_Success");

            entity.HasIndex(e => e.StartedAt, "IX_CrawlLogs_StartedAt");

            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasOne(d => d.Source).WithMany(p => p.CrawlLogs)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Hashtag>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Hashtags_CategoryId");

            entity.HasIndex(e => e.DifficultyLevel, "IX_Hashtags_DifficultyLevel");

            entity.HasIndex(e => e.IsActive, "IX_Hashtags_IsActive");

            entity.HasIndex(e => e.LastSeen, "IX_Hashtags_LastSeen");

            // Unique constraint on Tag + CountryCode (same tag can exist in different regions)
            entity.HasIndex(e => new { e.Tag, e.CountryCode }, "IX_Hashtags_Tag_CountryCode").IsUnique();

            // Index on CountryCode for regional filtering
            entity.HasIndex(e => e.CountryCode, "IX_Hashtags_CountryCode");

            entity.Property(e => e.DifficultyLevel).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Tag).HasMaxLength(100);
            entity.Property(e => e.TagDisplay).HasMaxLength(101);
            entity.Property(e => e.CountryCode).HasMaxLength(5).HasDefaultValue("VN");

            entity.HasOne(d => d.Category).WithMany(p => p.Hashtags)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HashtagCategory>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_HashtagCategories_Name").IsUnique();

            entity.HasIndex(e => e.ParentCategoryId, "IX_HashtagCategories_ParentCategoryId");

            entity.Property(e => e.DisplayNameVi).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.SubCategories).HasForeignKey(d => d.ParentCategoryId);
        });

        modelBuilder.Entity<HashtagHistory>(entity =>
        {
            entity.ToTable("HashtagHistory");

            entity.HasIndex(e => e.Category, "IX_HashtagHistory_Category");

            entity.HasIndex(e => e.CollectedDate, "IX_HashtagHistory_CollectedDate");

            entity.HasIndex(e => new { e.HashtagId, e.SourceId, e.CollectedDate }, "IX_HashtagHistory_HashtagId_SourceId_CollectedDate");

            entity.HasIndex(e => e.Rank, "IX_HashtagHistory_Rank");

            entity.HasIndex(e => e.SourceId, "IX_HashtagHistory_SourceId");

            entity.HasIndex(e => e.ViewCount, "IX_HashtagHistory_ViewCount");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.RankChange).HasMaxLength(20);
            entity.Property(e => e.TrendScore).HasColumnType("decimal(5, 4)");

            entity.HasOne(d => d.Hashtag).WithMany(p => p.History).HasForeignKey(d => d.HashtagId);

            entity.HasOne(d => d.Source).WithMany(p => p.History)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HashtagKeyword>(entity =>
        {
            entity.HasIndex(e => e.HashtagId, "IX_HashtagKeywords_HashtagId");

            entity.HasIndex(e => e.Keyword, "IX_HashtagKeywords_Keyword");

            entity.HasIndex(e => new { e.Keyword, e.HashtagId }, "IX_HashtagKeywords_Keyword_HashtagId");

            entity.HasIndex(e => e.RelevanceScore, "IX_HashtagKeywords_RelevanceScore");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Keyword).HasMaxLength(200);
            entity.Property(e => e.KeywordVi).HasMaxLength(200);
            entity.Property(e => e.RelevanceScore)
                .HasDefaultValue(0.5m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasOne(d => d.Hashtag).WithMany(p => p.Keywords).HasForeignKey(d => d.HashtagId);
        });

        modelBuilder.Entity<HashtagMetrics>(entity =>
        {
            entity.HasIndex(e => e.Date, "IX_HashtagMetrics_Date");

            entity.HasIndex(e => e.DifficultyScore, "IX_HashtagMetrics_DifficultyScore");

            entity.HasIndex(e => new { e.HashtagId, e.Date }, "IX_HashtagMetrics_HashtagId_Date");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DifficultyScore).HasDefaultValue(50);
            entity.Property(e => e.EngagementRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GrowthRate30d).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GrowthRate7d).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Hashtag).WithMany(p => p.Metrics).HasForeignKey(d => d.HashtagId);
        });

        modelBuilder.Entity<HashtagRelation>(entity =>
        {
            entity.HasIndex(e => e.CorrelationScore, "IX_HashtagRelations_CorrelationScore");

            entity.HasIndex(e => new { e.HashtagId1, e.HashtagId2 }, "IX_HashtagRelations_HashtagId1_HashtagId2").IsUnique();

            entity.HasIndex(e => e.HashtagId2, "IX_HashtagRelations_HashtagId2");

            entity.Property(e => e.CoOccurrenceCount).HasDefaultValue(1);
            entity.Property(e => e.CorrelationScore).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Hashtag1).WithMany(p => p.RelationsAsHashtag1)
                .HasForeignKey(d => d.HashtagId1)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Hashtag2).WithMany(p => p.RelationsAsHashtag2)
                .HasForeignKey(d => d.HashtagId2)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HashtagSource>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_HashtagSources_Name").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastError).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Url).HasMaxLength(500);
        });

        // System Logs Configuration
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp, "IX_SystemLogs_Timestamp");
            entity.HasIndex(e => e.ServiceName, "IX_SystemLogs_ServiceName");

            entity.Property(e => e.ServiceName).HasMaxLength(100);
            entity.Property(e => e.EventType).HasMaxLength(50);
            entity.Property(e => e.Message).HasMaxLength(2000);
        });

        // Blog System Configuration
        modelBuilder.Entity<BlogPostTag>(entity =>
        {
            entity.HasKey(e => new { e.BlogPostId, e.BlogTagId });

            entity.HasOne(d => d.BlogPost)
                .WithMany(p => p.BlogPostTags)
                .HasForeignKey(d => d.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.BlogTag)
                .WithMany(p => p.BlogPostTags)
                .HasForeignKey(d => d.BlogTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
