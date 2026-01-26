#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.41.0"

using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using System.Text.Json;

Console.WriteLine("=== TikTok Industry Crawler Debug ===\n");

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false, // Show browser
    SlowMo = 500
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
});

var page = await context.NewPageAsync();

// Load cookies from config
var cookiesJson = "[{\"name\":\"_ttp\",\"value\":\"2dxRzoDC5z1UNydFqBYi69CXWHd\",\"domain\":\".tiktok.com\",\"path\":\"/\",\"expires\":1798457071,\"httpOnly\":false,\"secure\":true,\"sameSite\":\"None\"}]";

try
{
    Console.WriteLine("1. Loading cookies...");
    var cookiesList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cookiesJson);
    if (cookiesList != null)
    {
        foreach (var cookieDict in cookiesList)
        {
            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = cookieDict["name"].ToString(),
                    Value = cookieDict["value"].ToString(),
                    Domain = cookieDict["domain"].ToString(),
                    Path = cookieDict["path"].ToString()
                }
            });
        }
    }
    Console.WriteLine("   Cookies loaded\n");

    Console.WriteLine("2. Navigating to TikTok Creative Center...");
    var baseUrl = "https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?country_code=VN&period=7";
    await page.GotoAsync(baseUrl, new PageGotoOptions
    {
        WaitUntil = WaitUntilState.NetworkIdle,
        Timeout = 60000
    });
    Console.WriteLine("   Page loaded\n");

    await Task.Delay(3000);

    // Take screenshot
    await page.ScreenshotAsync(new PageScreenshotOptions { Path = "step1_page_loaded.png", FullPage = true });
    Console.WriteLine("   Screenshot: step1_page_loaded.png\n");

    Console.WriteLine("3. Looking for Industry filter...");

    // Try multiple strategies to find the filter
    var strategies = new (string Name, string Selector)[]
    {
        ("Text 'Industry'", "text=Industry"),
        ("Has-text Industry", "*:has-text('Industry')"),
        ("Button with Industry", "button:has-text('Industry')"),
        ("Byted-select", "div[class*='byted-select']"),
        ("Select component", "[class*='Select']"),
        ("Dropdown", "[class*='dropdown']"),
        ("Role combobox", "[role='combobox']"),
        ("Aria-label Industry", "[aria-label*='Industry']"),
    };

    foreach (var (name, selector) in strategies)
    {
        try
        {
            var count = await page.Locator(selector).CountAsync();
            Console.WriteLine($"   {name} ({selector}): {count} found");

            if (count > 0)
            {
                var first = page.Locator(selector).First;
                var isVisible = await first.IsVisibleAsync();
                var text = await first.InnerTextAsync().ContinueWith(t => t.IsCompletedSuccessfully ? t.Result : "N/A");
                Console.WriteLine($"      First element - Visible: {isVisible}, Text: {text?.Substring(0, Math.Min(50, text?.Length ?? 0))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   {name}: Error - {ex.Message}");
        }
    }

    Console.WriteLine("\n4. Attempting to find and click Industry filter...");

    // Most likely approach based on code
    var industryLocator = page.Locator("text=Industry").First;
    var industryExists = await industryLocator.CountAsync() > 0;

    if (!industryExists)
    {
        Console.WriteLine("   ❌ No element with 'Industry' text found!");
        Console.WriteLine("   Saving page HTML for inspection...");
        var html = await page.ContentAsync();
        await File.WriteAllTextAsync("page_source.html", html);
        Console.WriteLine("   HTML saved to: page_source.html");
    }
    else
    {
        Console.WriteLine("   ✓ Found element with 'Industry' text");

        // Try to find clickable parent
        var parent = industryLocator.Locator("xpath=..");
        var parentTag = await parent.EvaluateAsync<string>("el => el.tagName");
        var parentClass = await parent.EvaluateAsync<string>("el => el.className");
        Console.WriteLine($"   Parent: <{parentTag}> class=\"{parentClass}\"");

        // Try to click
        try
        {
            Console.WriteLine("\n5. Clicking Industry filter...");
            await industryLocator.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
            Console.WriteLine("   ✓ Clicked successfully");

            await Task.Delay(2000);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "step2_dropdown_opened.png", FullPage = true });
            Console.WriteLine("   Screenshot: step2_dropdown_opened.png");

            // Look for options
            Console.WriteLine("\n6. Looking for industry options...");
            var optionSelectors = new[]
            {
                "text=Education",
                "div[data-type='select-option']",
                "[role='option']",
                "li:has-text('Education')"
            };

            foreach (var optSelector in optionSelectors)
            {
                var optCount = await page.Locator(optSelector).CountAsync();
                Console.WriteLine($"   {optSelector}: {optCount} found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Click failed: {ex.Message}");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "error_click_failed.png", FullPage = true });
        }
    }

    Console.WriteLine("\n7. Press ENTER to close...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    await browser.CloseAsync();
}

Console.WriteLine("\n=== Debug Complete ===");
