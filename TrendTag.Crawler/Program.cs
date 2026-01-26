using Microsoft.Playwright;
using TrendTag.Crawler.Models;
using System.Net.Http;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
    Headless = true
});

var page = await browser.NewPageAsync(new BrowserNewPageOptions {
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
});

var hashtags = await CrawlTikTokHashtags(page);

foreach (var h in hashtags) {
    Console.WriteLine($"{h.Tag} - {h.Rank}");
}
Console.WriteLine($"Total: {hashtags.Count}");

var google = await CrawlGoogleTrendsDailyHashtags(page);
Console.WriteLine("Google Trends Daily (VN):");
foreach (var g in google) {
    Console.WriteLine($"{g.Tag} - {g.Rank}");
}
Console.WriteLine($"Total: {google.Count}");

var bufferTags = await CrawlBufferHashtags(page);
Console.WriteLine("Buffer – TikTok Hashtag Generator:");
foreach (var b in bufferTags) Console.WriteLine($"{b.Tag} - {b.Rank}");
Console.WriteLine($"Total: {bufferTags.Count}");

var troll = await CrawlTrollishlyHashtags(page);
Console.WriteLine("Trollishly – Trending TikTok Hashtags:");
foreach (var t in troll) Console.WriteLine($"{t.Tag} - {t.Rank}");
Console.WriteLine($"Total: {troll.Count}");

var capcut = await CrawlCapcutHashtags(page);
Console.WriteLine("CapCut / TikTokTrend Tools:");
foreach (var c in capcut) Console.WriteLine($"{c.Tag} - {c.Rank}");
Console.WriteLine($"Total: {capcut.Count}");

var picuki = await CrawlPicukiHashtags(page);
Console.WriteLine("Picuki – TikTok Trending Hashtags:");
foreach (var p in picuki) Console.WriteLine($"{p.Tag} - {p.Rank}");
Console.WriteLine($"Total: {picuki.Count}");

var tokchart = await CrawlTokChartHashtags(page);
Console.WriteLine("TokChart – Trending Hashtags:");
foreach (var t in tokchart) Console.WriteLine($"{t.Tag} - {t.Rank}");
Console.WriteLine($"Total: {tokchart.Count}");

var countik = await CrawlCountikHashtags(page);
Console.WriteLine("Countik – Popular Hashtags:");
foreach (var c in countik) Console.WriteLine($"{c.Tag} - {c.Rank}");
Console.WriteLine($"Total: {countik.Count}");

Console.ReadLine();


///////////// functions /////////////
static async Task<List<HashtagRaw>> CrawlTikTokHashtags(IPage page) {
    var result = new List<HashtagRaw>();

    await page.GotoAsync(
        "https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag",
        new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60000
        }
    );

    //wait React render
   await page.WaitForSelectorAsync(
       "a[data-testid^='cc_commonCom-trend_hashtag_item']"
   );

    var items = await page.QuerySelectorAllAsync(
        "a[data-testid^='cc_commonCom-trend_hashtag_item']"
    );

    int rank = 1;

    foreach (var item in items) {
        try {
            var tagEl = await item.QuerySelectorAsync(
                "span[class*='titleText']"
            );

            if (tagEl == null)
                continue;

            var tag = (await tagEl.InnerTextAsync()).Trim();

            if (string.IsNullOrWhiteSpace(tag))
                continue;

            result.Add(new HashtagRaw {
                Tag = tag.StartsWith("#") ? tag : $"#{tag}",
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        } catch {
            // ignore item-level error
        }
    }

    return result;
}

static async Task<List<HashtagRaw>> CrawlGoogleTrendsDailyHashtags(IPage page) {
    var result = new List<HashtagRaw>();

    try {
        await page.GotoAsync(
            "https://trends.google.com/trends/trendingsearches/daily?geo=VN",
            new PageGotoOptions {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60000
            }
        );

        // Wait until any of known selectors yield results (supports client-side rendering)
        await page.WaitForFunctionAsync(@"() => {
            const sels = ['#trend-table table tr', 'table[role=grid] tr', 'tr[jsname=OkdM2c]', 'div.mZ3Rlc'];
            for (const s of sels) {
                if (document.querySelectorAll(s).length > 0) return true;
            }
            return false;
        }", new PageWaitForFunctionOptions { Timeout = 20000 });

        // Extract titles using several selectors with a JS evaluation fallback to be robust against DOM changes
        var titles = await page.EvaluateAsync<string[]>(@"() => {
            const selectors = ['#trend-table table tr', 'table[role=grid] tr', 'tr[jsname=OkdM2c]', 'div.mZ3Rlc'];
            const seen = new Set();
            const out = [];
            for (const sel of selectors) {
                const nodes = Array.from(document.querySelectorAll(sel));
                for (const n of nodes) {
                    let text = '';
                    if (n.tagName && n.tagName.toLowerCase() === 'tr') {
                        const td = n.querySelector('td:nth-child(2)') || n.querySelector('td');
                        if (td) text = td.innerText;
                    } else {
                        text = n.innerText;
                    }
                    if (text) {
                        text = text.split('\n')[0].trim();
                        if (text && !seen.has(text)) {
                            seen.add(text);
                            out.push(text);
                        }
                    }
                }
                if (out.length) break;
            }
            return out;
        }");

        int rank = 1;

        foreach (var title in titles ?? Array.Empty<string>()) {
            try {
                var t = (title ?? "").Trim();
                if (string.IsNullOrWhiteSpace(t))
                    continue;

                // Normalize: keep letters/numbers, replace spaces with underscore
                var normalized = Regex.Replace(t, @"[^\p{L}\p{Nd}\s]", "");
                normalized = Regex.Replace(normalized, @"\s+", "_").Trim();

                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                var tag = normalized.StartsWith("#") ? normalized : $"#{normalized}";

                result.Add(new HashtagRaw {
                    Tag = tag,
                    Rank = rank++,
                    CollectedDate = DateTime.Today
                });
            } catch {
                // ignore per-item errors
            }
        }

    } catch (Exception ex) {
        Console.WriteLine($"Google Trends crawl error: {ex.Message}");
    }

    return result;
}

// Additional alternative sources - more reliable than Picuki
static async Task<List<HashtagRaw>> CrawlTokChartHashtags(IPage page) {
    var result = new List<HashtagRaw>();
    try {
        await page.GotoAsync("https://tokchart.com/hashtags",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        await page.WaitForSelectorAsync("a[href*='/hashtag/'], .hashtag, [class*='hashtag']",
            new PageWaitForSelectorOptions { Timeout = 20000 });

        var hashtags = await page.EvaluateAsync<string[]>(@"() => {
            const seen = new Set();
            const out = [];

            // Try multiple selector strategies
            const selectors = [
                'a[href*=\""/hashtag/""]',
                '[class*=""hashtag""]',
                'td, div, span, a'
            ];

            for (const sel of selectors) {
                const elements = document.querySelectorAll(sel);
                for (const el of elements) {
                    const text = (el.innerText || el.textContent || '').trim();
                    const hashtagRegex = /#[a-zA-Z0-9_]+/g;
                    const matches = text.match(hashtagRegex);
                    if (matches) {
                        for (const tag of matches) {
                            if (!seen.has(tag)) {
                                seen.add(tag);
                                out.push(tag);
                            }
                        }
                    }
                }
                if (out.length > 0) break;
            }
            return out;
        }");

        int rank = 1;
        foreach (var tag in hashtags ?? []) {
            result.Add(new HashtagRaw {
                Tag = tag.Trim(),
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        }
    } catch (Exception ex) {
        Console.WriteLine($"TokChart crawl error: {ex.Message}");
    }
    return result;
}

static async Task<List<HashtagRaw>> CrawlCountikHashtags(IPage page) {
    var result = new List<HashtagRaw>();
    try {
        await page.GotoAsync("https://countik.com/popular/hashtags",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        await page.WaitForSelectorAsync("a[href*='/hashtag/'], .hashtag-name, [class*='hashtag']",
            new PageWaitForSelectorOptions { Timeout = 20000 });

        var hashtags = await page.EvaluateAsync<string[]>(@"() => {
            const seen = new Set();
            const out = [];

            // Look for hashtag links or containers
            const links = document.querySelectorAll('a[href*=""/hashtag/""]');
            for (const link of links) {
                const text = (link.innerText || link.textContent || '').trim();
                if (text.startsWith('#') || text.startsWith('hashtag/')) {
                    const tag = text.startsWith('#') ? text : '#' + text.replace('hashtag/', '');
                    if (!seen.has(tag)) {
                        seen.add(tag);
                        out.push(tag);
                    }
                }
            }

            // Fallback: scan for any #hashtag patterns
            if (out.length === 0) {
                const allText = document.body.innerText || '';
                const hashtagRegex = /#[a-zA-Z0-9_]+/g;
                const matches = allText.match(hashtagRegex);
                if (matches) {
                    for (const tag of matches) {
                        if (!seen.has(tag)) {
                            seen.add(tag);
                            out.push(tag);
                        }
                    }
                }
            }
            return out;
        }");

        int rank = 1;
        foreach (var tag in hashtags ?? []) {
            result.Add(new HashtagRaw {
                Tag = tag.Trim(),
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        }
    } catch (Exception ex) {
        Console.WriteLine($"Countik crawl error: {ex.Message}");
    }
    return result;
}

// Site-specific crawlers with proper URLs and selectors
static async Task<List<HashtagRaw>> CrawlBufferHashtags(IPage page) {
    var result = new List<HashtagRaw>();
    try {
        await page.GotoAsync("https://buffer.com/resources/tiktok-hashtags/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        await page.WaitForSelectorAsync("p", new PageWaitForSelectorOptions { Timeout = 20000 });

        // Extract hashtags from paragraphs (format: "1. #hashtag")
        var hashtags = await page.EvaluateAsync<string[]>(@"() => {
            const paragraphs = Array.from(document.querySelectorAll('p'));
            const seen = new Set();
            const out = [];
            const hashtagRegex = /#[a-zA-Z0-9_]+/g;

            for (const p of paragraphs) {
                const text = p.innerText || '';
                const matches = text.match(hashtagRegex);
                if (matches) {
                    for (const tag of matches) {
                        if (!seen.has(tag)) {
                            seen.add(tag);
                            out.push(tag);
                        }
                    }
                }
            }
            return out;
        }");

        int rank = 1;
        foreach (var tag in hashtags ?? []) {
            result.Add(new HashtagRaw {
                Tag = tag.Trim(),
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        }
    } catch (Exception ex) {
        Console.WriteLine($"Buffer crawl error: {ex.Message}");
    }
    return result;
}

static async Task<List<HashtagRaw>> CrawlTrollishlyHashtags(IPage page) {
    var result = new List<HashtagRaw>();
    try {
        await page.GotoAsync("https://www.trollishly.com/tiktok-trending-hashtags/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        await page.WaitForSelectorAsync("#popular-hashtags-container, #top-hashtags-container, #trending-hashtags-container",
            new PageWaitForSelectorOptions { Timeout = 20000 });

        // Extract hashtags from the three main containers
        var hashtags = await page.EvaluateAsync<string[]>(@"() => {
            const containers = ['popular-hashtags-container', 'top-hashtags-container', 'trending-hashtags-container'];
            const seen = new Set();
            const out = [];

            for (const id of containers) {
                const container = document.getElementById(id);
                if (container) {
                    const text = container.innerText || '';
                    const hashtagRegex = /#[a-zA-Z0-9_]+/g;
                    const matches = text.match(hashtagRegex);
                    if (matches) {
                        for (const tag of matches) {
                            if (!seen.has(tag)) {
                                seen.add(tag);
                                out.push(tag);
                            }
                        }
                    }
                }
            }
            return out;
        }");

        int rank = 1;
        foreach (var tag in hashtags ?? []) {
            result.Add(new HashtagRaw {
                Tag = tag.Trim(),
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        }
    } catch (Exception ex) {
        Console.WriteLine($"Trollishly crawl error: {ex.Message}");
    }
    return result;
}

static async Task<List<HashtagRaw>> CrawlCapcutHashtags(IPage page) {
    var result = new List<HashtagRaw>();
    try {
        await page.GotoAsync("https://www.capcut.com/resource/tiktok-hashtag-guide",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        await page.WaitForSelectorAsync("p", new PageWaitForSelectorOptions { Timeout = 20000 });

        // Extract hashtags from paragraphs
        var hashtags = await page.EvaluateAsync<string[]>(@"() => {
            const paragraphs = Array.from(document.querySelectorAll('p'));
            const seen = new Set();
            const out = [];
            const hashtagRegex = /#[a-zA-Z0-9_]+/g;

            for (const p of paragraphs) {
                const text = p.innerText || '';
                const matches = text.match(hashtagRegex);
                if (matches) {
                    for (const tag of matches) {
                        if (!seen.has(tag)) {
                            seen.add(tag);
                            out.push(tag);
                        }
                    }
                }
            }
            return out;
        }");

        int rank = 1;
        foreach (var tag in hashtags ?? []) {
            result.Add(new HashtagRaw {
                Tag = tag.Trim(),
                Rank = rank++,
                CollectedDate = DateTime.Today
            });
        }
    } catch (Exception ex) {
        Console.WriteLine($"CapCut crawl error: {ex.Message}");
    }
    return result;
}

static Task<List<HashtagRaw>> CrawlPicukiHashtags(IPage _) {
    // Note: Picuki.com went offline in mid-2023. Using alternative approach.
    // Recommend replacing with TokChart or Countik instead
    var result = new List<HashtagRaw>();
    Console.WriteLine("Warning: Picuki.com is no longer available. Consider using TokChart or Countik instead.");
    return Task.FromResult(result);
}