-- =====================================================
-- Hashtag Generator Feature - Database Schema
-- =====================================================
-- Creates table to store hashtag generation history
-- Supports caching and analytics
-- =====================================================

USE [TrendTagDb];
GO

-- Table: HashtagGenerations
-- Stores all hashtag generation requests and results
CREATE TABLE HashtagGenerations (
    Id INT PRIMARY KEY IDENTITY(1,1),

    -- User input
    InputDescription NVARCHAR(1000) NOT NULL,
    InputDescriptionHash NVARCHAR(64) NULL, -- Will be computed in application code

    -- AI/System response
    RecommendedHashtags NVARCHAR(MAX) NOT NULL, -- JSON array
    GenerationMethod NVARCHAR(50) DEFAULT 'AI', -- 'AI' or 'RuleBased'

    -- User interaction
    UserId INT NULL, -- NULL for anonymous users
    WasCopied BIT DEFAULT 0,
    WasSaved BIT DEFAULT 0,
    UserFeedback NVARCHAR(20) NULL, -- 'positive', 'negative', 'neutral'

    -- Metadata
    GeneratedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CachedUntil DATETIME2 NULL, -- For 7-day caching
    TokensUsed INT NULL, -- Track OpenAI API usage

    -- Indexes
    INDEX IX_HashtagGenerations_InputHash (InputDescriptionHash),
    INDEX IX_HashtagGenerations_GeneratedAt (GeneratedAt),
    INDEX IX_HashtagGenerations_UserId (UserId)
);
GO

-- Table: GenerationFeedback
-- Tracks which specific hashtags were selected/used by users
CREATE TABLE GenerationHashtagSelection (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GenerationId INT NOT NULL FOREIGN KEY REFERENCES HashtagGenerations(Id),
    HashtagId INT NULL FOREIGN KEY REFERENCES Hashtags(Id), -- Can be NULL if hashtag not in DB
    HashtagText NVARCHAR(100) NOT NULL,
    WasSelected BIT DEFAULT 0,
    PerformanceViews BIGINT NULL, -- Track actual performance if user reports back

    INDEX IX_GenerationHashtagSelection_GenerationId (GenerationId),
    INDEX IX_GenerationHashtagSelection_HashtagId (HashtagId)
);
GO

-- Table: GenerationRateLimits
-- Track rate limiting per user/IP
CREATE TABLE GenerationRateLimits (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NULL,
    IpAddress NVARCHAR(50) NULL,
    GenerationCount INT DEFAULT 0,
    WindowStartTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IX_GenerationRateLimits_UserId (UserId),
    INDEX IX_GenerationRateLimits_IpAddress (IpAddress)
);
GO

PRINT 'Hashtag Generator tables created successfully!';
GO

-- Sample data verification query
SELECT
    'HashtagGenerations' AS TableName,
    COUNT(*) AS RecordCount
FROM HashtagGenerations
UNION ALL
SELECT
    'GenerationHashtagSelection',
    COUNT(*)
FROM GenerationHashtagSelection;
GO
