#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.41.0"

using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

Console.WriteLine("=== TikTok UI Inspector ===\n");

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false, // Show browser to see what's happening
    SlowMo = 1000 // Slow down by 1 second
});

var page = await browser.NewPageAsync(new BrowserNewPageOptions
{
    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
});

try
{
    Console.WriteLine("1. Navigating to TikTok Creative Center...");
    await page.GotoAsync(
        "https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?countryCode=VN&period=7",
        new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 }
    );

    Console.WriteLine("2. Waiting for page to load...");
    await Task.Delay(5000);

    Console.WriteLine("\n3. Taking screenshot of full page...");
    await page.ScreenshotAsync(new PageScreenshotOptions
    {
        Path = "tiktok_full_page.png",
        FullPage = true
    });
    Console.WriteLine("   Screenshot saved: tiktok_full_page.png");

    Console.WriteLine("\n4. Searching for Industry filter...");

    // Try to find elements with "Industry" text
    var industryElements = await page.Locator("text=Industry").AllAsync();
    Console.WriteLine($"   Found {industryElements.Count} elements with 'Industry' text");

    if (industryElements.Count > 0)
    {
        Console.WriteLine("\n5. Inspecting first Industry element...");
        var firstIndustry = industryElements[0];

        // Get parent element
        var parent = await firstIndustry.Locator("xpath=..").First.ElementHandleAsync();
        if (parent != null)
        {
            var parentTag = await parent.EvaluateAsync<string>("el => el.tagName");
            var parentClass = await parent.EvaluateAsync<string>("el => el.className");
            Console.WriteLine($"   Parent: <{parentTag}> class=\"{parentClass}\"");
        }

        // Take screenshot of the area
        var box = await firstIndustry.BoundingBoxAsync();
        if (box != null)
        {
            Console.WriteLine($"   Position: x={box.X}, y={box.Y}, w={box.Width}, h={box.Height}");

            // Screenshot with some padding
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = "tiktok_industry_filter.png",
                Clip = new Clip
                {
                    X = Math.Max(0, box.X - 50),
                    Y = Math.Max(0, box.Y - 50),
                    Width = box.Width + 100,
                    Height = box.Height + 100
                }
            });
            Console.WriteLine("   Screenshot saved: tiktok_industry_filter.png");
        }
    }

    Console.WriteLine("\n6. Looking for filter/dropdown components...");

    // Common dropdown selectors
    var dropdownSelectors = new[]
    {
        "[class*='select']",
        "[class*='Select']",
        "[class*='dropdown']",
        "[class*='Dropdown']",
        "[role='combobox']",
        "[role='button']",
        "button",
    };

    foreach (var selector in dropdownSelectors)
    {
        var count = await page.Locator(selector).CountAsync();
        Console.WriteLine($"   {selector}: {count} elements");
    }

    Console.WriteLine("\n7. Checking page HTML structure...");
    var htmlContent = await page.ContentAsync();

    // Save HTML for inspection
    await File.WriteAllTextAsync("tiktok_page.html", htmlContent);
    Console.WriteLine("   HTML saved: tiktok_page.html");

    Console.WriteLine("\n8. Press ENTER to close browser and exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    await browser.CloseAsync();
    await playwright.DisposeAsync();
}

Console.WriteLine("\n=== Inspection Complete ===");
