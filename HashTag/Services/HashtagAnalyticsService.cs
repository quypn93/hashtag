using HashTag.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Services;

public class HashtagAnalyticsService : IHashtagAnalyticsService
{
    private readonly IHashtagRepository _repository;
    private readonly ILogger<HashtagAnalyticsService> _logger;

    public HashtagAnalyticsService(IHashtagRepository repository, ILogger<HashtagAnalyticsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TrendingChartData> GetTrendingChartDataAsync(int days = 7)
    {
        var result = new TrendingChartData();

        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days + 1);

            // Get top 10 trending hashtags in this period
            var topHashtags = await _repository.GetTopHashtagsInPeriodAsync(startDate, endDate, 10);

            if (!topHashtags.Any())
            {
                _logger.LogWarning("No hashtags found for trending chart");
                return result;
            }

            // Generate date labels
            for (int i = 0; i < days; i++)
            {
                result.Labels.Add(startDate.AddDays(i).ToString("MM/dd"));
            }

            // Get rank history for each top hashtag
            foreach (var hashtag in topHashtags)
            {
                var series = new TrendingHashtagSeries
                {
                    HashtagName = hashtag.TagDisplay
                };

                var history = await _repository.GetHashtagHistoryInPeriodAsync(hashtag.Id, startDate, endDate);

                // Fill ranks for each day
                for (int i = 0; i < days; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayHistory = history.Where(h => h.CollectedDate.Date == date).ToList();

                    // Get best rank for this day across all sources
                    var bestRank = dayHistory.Any() ? dayHistory.Min(h => h.Rank) : (int?)null;
                    series.Ranks.Add(bestRank);
                }

                result.Series.Add(series);
            }

            _logger.LogInformation("Generated trending chart data for {Days} days with {Count} hashtags",
                days, result.Series.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating trending chart data");
        }

        return result;
    }

    public async Task<SourceComparisonData> GetSourceComparisonDataAsync()
    {
        var result = new SourceComparisonData();

        try
        {
            var sources = await _repository.GetAllSourcesAsync();
            var colors = GenerateColors(sources.Count);

            foreach (var (source, index) in sources.Select((s, i) => (s, i)))
            {
                result.SourceNames.Add(source.Name);

                // Count unique hashtags from this source
                var count = await _repository.GetUniqueHashtagCountBySourceAsync(source.Id);
                result.HashtagCounts.Add(count);

                result.Colors.Add(colors[index]);
            }

            _logger.LogInformation("Generated source comparison data for {Count} sources", sources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating source comparison data");
        }

        return result;
    }

    public async Task<HashtagHistoryData> GetHashtagHistoryDataAsync(int hashtagId, int days = 30)
    {
        var result = new HashtagHistoryData();

        try
        {
            var hashtag = await _repository.GetHashtagByIdAsync(hashtagId);
            if (hashtag == null)
            {
                _logger.LogWarning("Hashtag {Id} not found", hashtagId);
                return result;
            }

            result.HashtagName = hashtag.TagDisplay;

            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days + 1);

            var history = await _repository.GetHashtagHistoryInPeriodAsync(hashtagId, startDate, endDate);
            var sources = await _repository.GetAllSourcesAsync();

            // Generate date labels
            for (int i = 0; i < days; i++)
            {
                result.Dates.Add(startDate.AddDays(i).ToString("MM/dd"));
            }

            // Overall best rank per day
            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var dayHistory = history.Where(h => h.CollectedDate.Date == date).ToList();
                var bestRank = dayHistory.Any() ? dayHistory.Min(h => h.Rank) : 999;
                result.Ranks.Add(bestRank);
            }

            // Rank per source
            foreach (var source in sources)
            {
                var sourceRanks = new List<int?>();

                for (int i = 0; i < days; i++)
                {
                    var date = startDate.AddDays(i);
                    var dayHistory = history.FirstOrDefault(h =>
                        h.CollectedDate.Date == date && h.SourceId == source.Id);

                    sourceRanks.Add(dayHistory?.Rank);
                }

                result.SourceRanks[source.Name] = sourceRanks;
            }

            _logger.LogInformation("Generated history data for hashtag {Id} over {Days} days",
                hashtagId, days);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtag history data for {Id}", hashtagId);
        }

        return result;
    }

    public async Task<DailyActivityData> GetDailyActivityDataAsync(int days = 30)
    {
        var result = new DailyActivityData();

        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days + 1);

            var allHistory = await _repository.GetAllHistoryInPeriodAsync(startDate, endDate);

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                result.Dates.Add(date.ToString("MM/dd"));

                var dayHistory = allHistory.Where(h => h.CollectedDate.Date == date).ToList();

                result.TotalHashtags.Add(dayHistory.Count);
                result.UniqueHashtags.Add(dayHistory.Select(h => h.HashtagId).Distinct().Count());
            }

            _logger.LogInformation("Generated daily activity data for {Days} days", days);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily activity data");
        }

        return result;
    }

    private List<string> GenerateColors(int count)
    {
        var baseColors = new[]
        {
            "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF",
            "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF6384"
        };

        var colors = new List<string>();
        for (int i = 0; i < count; i++)
        {
            colors.Add(baseColors[i % baseColors.Length]);
        }

        return colors;
    }
}
