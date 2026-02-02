-- Migration: MultiLanguageAndRegionSupport
-- Date: 2026-02-01
-- Description: Add English fields to BlogPost and CountryCode to Hashtags

-- ============================================
-- Part 1: Add English fields to BlogPosts table
-- ============================================

-- Check if TitleEn column exists before adding
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'TitleEn')
BEGIN
    ALTER TABLE BlogPosts ADD TitleEn NVARCHAR(200) NULL;
    PRINT 'Added column TitleEn to BlogPosts';
END
GO

-- Add SlugEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'SlugEn')
BEGIN
    ALTER TABLE BlogPosts ADD SlugEn NVARCHAR(250) NULL;
    PRINT 'Added column SlugEn to BlogPosts';
END
GO

-- Add ExcerptEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'ExcerptEn')
BEGIN
    ALTER TABLE BlogPosts ADD ExcerptEn NVARCHAR(500) NULL;
    PRINT 'Added column ExcerptEn to BlogPosts';
END
GO

-- Add ContentEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'ContentEn')
BEGIN
    ALTER TABLE BlogPosts ADD ContentEn NVARCHAR(MAX) NULL;
    PRINT 'Added column ContentEn to BlogPosts';
END
GO

-- Add MetaTitleEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'MetaTitleEn')
BEGIN
    ALTER TABLE BlogPosts ADD MetaTitleEn NVARCHAR(200) NULL;
    PRINT 'Added column MetaTitleEn to BlogPosts';
END
GO

-- Add MetaDescriptionEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'MetaDescriptionEn')
BEGIN
    ALTER TABLE BlogPosts ADD MetaDescriptionEn NVARCHAR(500) NULL;
    PRINT 'Added column MetaDescriptionEn to BlogPosts';
END
GO

-- Add MetaKeywordsEn column
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'BlogPosts' AND COLUMN_NAME = 'MetaKeywordsEn')
BEGIN
    ALTER TABLE BlogPosts ADD MetaKeywordsEn NVARCHAR(500) NULL;
    PRINT 'Added column MetaKeywordsEn to BlogPosts';
END
GO

-- Create index on SlugEn for faster lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BlogPosts_SlugEn' AND object_id = OBJECT_ID('BlogPosts'))
BEGIN
    CREATE INDEX IX_BlogPosts_SlugEn ON BlogPosts(SlugEn) WHERE SlugEn IS NOT NULL;
    PRINT 'Created index IX_BlogPosts_SlugEn';
END
GO

-- ============================================
-- Part 2: Add CountryCode to Hashtags table
-- ============================================

-- Add CountryCode column with default 'VN'
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Hashtags' AND COLUMN_NAME = 'CountryCode')
BEGIN
    ALTER TABLE Hashtags ADD CountryCode NVARCHAR(5) NOT NULL DEFAULT 'VN';
    PRINT 'Added column CountryCode to Hashtags';
END
GO

-- Update existing hashtags to have VN as country code (in case default wasn't applied)
UPDATE Hashtags SET CountryCode = 'VN' WHERE CountryCode IS NULL OR CountryCode = '';
GO

-- Create index for CountryCode for regional filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_CountryCode' AND object_id = OBJECT_ID('Hashtags'))
BEGIN
    CREATE INDEX IX_Hashtags_CountryCode ON Hashtags(CountryCode);
    PRINT 'Created index IX_Hashtags_CountryCode';
END
GO

-- Create composite index for Tag + CountryCode (unique per region)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_Tag_CountryCode' AND object_id = OBJECT_ID('Hashtags'))
BEGIN
    -- Drop the old unique constraint on Tag only
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Hashtags_Tag' AND object_id = OBJECT_ID('Hashtags'))
    BEGIN
        DROP INDEX IX_Hashtags_Tag ON Hashtags;
        PRINT 'Dropped old index IX_Hashtags_Tag';
    END

    -- Create new composite unique index
    CREATE UNIQUE INDEX IX_Hashtags_Tag_CountryCode ON Hashtags(Tag, CountryCode);
    PRINT 'Created unique index IX_Hashtags_Tag_CountryCode';
END
GO

-- ============================================
-- Part 3: Record migration in __EFMigrationsHistory
-- ============================================
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260201000000_MultiLanguageAndRegionSupport')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260201000000_MultiLanguageAndRegionSupport', '8.0.0');
    PRINT 'Recorded migration in __EFMigrationsHistory';
END
GO

PRINT 'Migration completed successfully!';
GO
