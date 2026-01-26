-- Cleanup HashtagRelations table to improve performance
-- This table was causing metrics calculation to be very slow
-- Relations feature has been disabled in HashtagMetricsService

USE TrendTagDb;
GO

-- Check current row count
SELECT
    'Before cleanup' AS Status,
    COUNT(*) AS TotalRows,
    COUNT(*) * 8 / 1024 AS ApproxSizeMB
FROM HashtagRelations;
GO

-- Option 1: Delete all relations (Recommended)
-- Relations feature is disabled, so this data is no longer needed
TRUNCATE TABLE HashtagRelations;
GO

-- Option 2: Keep only top relations by CorrelationScore (Alternative)
-- Uncomment below if you want to keep some data
/*
DELETE FROM HashtagRelations
WHERE Id NOT IN (
    SELECT TOP 1000 Id
    FROM HashtagRelations
    ORDER BY CorrelationScore DESC
);
GO
*/

-- Check row count after cleanup
SELECT
    'After cleanup' AS Status,
    COUNT(*) AS TotalRows,
    COUNT(*) * 8 / 1024 AS ApproxSizeMB
FROM HashtagRelations;
GO

-- Rebuild indexes to reclaim space
ALTER INDEX ALL ON HashtagRelations REBUILD;
GO

PRINT 'HashtagRelations cleanup completed!';
GO
