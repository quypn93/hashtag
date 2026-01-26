-- Thêm các nguồn hashtag mới vào database
-- Chạy script này trong SQL Server hoặc qua migration

-- Instagram Top Hashtags
INSERT INTO HashtagSources (Name, Url, IsActive)
VALUES ('Instagram', 'https://top-hashtags.com/instagram/', 1);

-- All Hashtag
INSERT INTO HashtagSources (Name, Url, IsActive)
VALUES ('AllHashtag', 'https://www.all-hashtag.com/hashtag-generator.php', 1);

-- RiteTag
INSERT INTO HashtagSources (Name, Url, IsActive)
VALUES ('RiteTag', 'https://ritetag.com/best-hashtags-for/tiktok', 1);

-- Inflact (formerly InfluencerMarketingHub)
INSERT INTO HashtagSources (Name, Url, IsActive)
VALUES ('Inflact', 'https://inflact.com/tools/hashtag-generator/', 1);
