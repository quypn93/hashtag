BEGIN TRANSACTION;
GO

ALTER TABLE [CrawlLogs] DROP CONSTRAINT [FK_CrawlLogs_HashtagSources_SourceId];
GO

ALTER TABLE [HashtagCategories] DROP CONSTRAINT [FK_HashtagCategories_HashtagCategories_ParentCategoryId];
GO

ALTER TABLE [HashtagHistory] DROP CONSTRAINT [FK_HashtagHistory_HashtagSources_SourceId];
GO

ALTER TABLE [HashtagRelations] DROP CONSTRAINT [FK_HashtagRelations_Hashtags_HashtagId1];
GO

ALTER TABLE [HashtagRelations] DROP CONSTRAINT [FK_HashtagRelations_Hashtags_HashtagId2];
GO

DELETE FROM [HashtagSources]
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 3;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 4;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 5;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 6;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 7;
SELECT @@ROWCOUNT;

GO

DELETE FROM [HashtagSources]
WHERE [Id] = 8;
SELECT @@ROWCOUNT;

GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Hashtags]') AND [c].[name] = N'TotalAppearances');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Hashtags] DROP CONSTRAINT [' + @var0 + '];');
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HashtagRelations]') AND [c].[name] = N'UpdatedAt');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [HashtagRelations] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [HashtagRelations] ADD DEFAULT ((getutcdate())) FOR [UpdatedAt];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HashtagRelations]') AND [c].[name] = N'CreatedAt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [HashtagRelations] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [HashtagRelations] ADD DEFAULT ((getutcdate())) FOR [CreatedAt];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HashtagMetrics]') AND [c].[name] = N'CreatedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [HashtagMetrics] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [HashtagMetrics] ADD DEFAULT ((getutcdate())) FOR [CreatedAt];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HashtagKeywords]') AND [c].[name] = N'CreatedAt');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [HashtagKeywords] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [HashtagKeywords] ADD DEFAULT ((getutcdate())) FOR [CreatedAt];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[HashtagHistory]') AND [c].[name] = N'CreatedAt');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [HashtagHistory] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [HashtagHistory] ADD DEFAULT ((getutcdate())) FOR [CreatedAt];
GO

ALTER TABLE [HashtagHistory] ADD [IsViral] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [HashtagHistory] ADD [RankDiff] int NULL;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CrawlLogs]') AND [c].[name] = N'Success');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [CrawlLogs] DROP CONSTRAINT [' + @var6 + '];');
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CrawlLogs]') AND [c].[name] = N'HashtagsCollected');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [CrawlLogs] DROP CONSTRAINT [' + @var7 + '];');
GO

ALTER TABLE [CrawlLogs] ADD CONSTRAINT [FK_CrawlLogs_HashtagSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [HashtagSources] ([Id]);
GO

ALTER TABLE [HashtagCategories] ADD CONSTRAINT [FK_HashtagCategories_HashtagCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [HashtagCategories] ([Id]);
GO

ALTER TABLE [HashtagHistory] ADD CONSTRAINT [FK_HashtagHistory_HashtagSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [HashtagSources] ([Id]);
GO

ALTER TABLE [HashtagRelations] ADD CONSTRAINT [FK_HashtagRelations_Hashtags_HashtagId1] FOREIGN KEY ([HashtagId1]) REFERENCES [Hashtags] ([Id]);
GO

ALTER TABLE [HashtagRelations] ADD CONSTRAINT [FK_HashtagRelations_Hashtags_HashtagId2] FOREIGN KEY ([HashtagId2]) REFERENCES [Hashtags] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251229062311_AddRankDiffIsViralSimple', N'8.0.0');
GO

COMMIT;
GO

