using HashTag.Data;
using HashTag.Filters;
using HashTag.Options;
using HashTag.Repositories;
using HashTag.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// Add Response Compression for better performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/json",
        "text/css",
        "text/javascript",
        "application/javascript"
    });
});

// Configure compression levels
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// Add Localization services for multi-language support
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// Add Response Caching for better performance
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Add Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "TrendTag.Auth";
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add Data Protection with persistent keys to survive app restarts
// This prevents "key not found" errors when decrypting session cookies after restart
var dataProtectionPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("TrendTag");

// Add Session support for authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<TrendTagDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository Layer
builder.Services.AddScoped<IHashtagRepository, HashtagRepository>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();

// Register Services
// ⚡ PERFORMANCE: ADO.NET service for stored procedures (5-10x faster than EF Core)
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();

builder.Services.AddScoped<ICrawlerService, CrawlerService>();
builder.Services.AddScoped<IHashtagAnalyticsService, HashtagAnalyticsService>();
builder.Services.AddScoped<IHashtagMetricsService, HashtagMetricsService>();
builder.Services.AddSingleton<ITikTokLiveSearchService, TikTokLiveSearchService>();
builder.Services.AddScoped<IHashtagGeneratorService, HashtagGeneratorService>();
builder.Services.AddScoped<IGrowthAnalysisService, GrowthAnalysisService>();
builder.Services.AddScoped<BlogSeeder>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IBlogAutoGeneratorService, BlogAutoGeneratorService>();

// Configure OpenAI and Hashtag Generator options
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<HashtagGeneratorOptions>(builder.Configuration.GetSection("HashtagGenerator"));

// Add HttpClient for OpenAI API calls
builder.Services.AddHttpClient();

// Register Filters
builder.Services.AddScoped<AdminAuthFilter>();

// Register Background Services
builder.Services.AddHostedService<HashtagCrawlerHostedService>();
builder.Services.AddHostedService<HashtagMetricsHostedService>();
builder.Services.AddHostedService<BlogGeneratorHostedService>();

var app = builder.Build();

// Enable response compression FIRST in pipeline
app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Handle 404 and other status codes with custom pages
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// Remove security headers that expose server info
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

// Redirect www to non-www for canonical URLs
app.Use(async (context, next) =>
{
    var host = context.Request.Host;
    if (host.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
    {
        var newHost = new HostString(host.Host.Substring(4), host.Port ?? 443);
        var newUrl = $"{context.Request.Scheme}://{newHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(newUrl, permanent: true);
        return;
    }
    await next();
});

app.UseHttpsRedirection();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 30 days
        const int durationInSeconds = 60 * 60 * 24 * 30;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
    }
});

app.UseStaticFiles();

// Serve robots.txt từ project root
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/robots.txt")
    {
        var robotsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robots.txt");
        if (System.IO.File.Exists(robotsPath))
        {
            context.Response.ContentType = "text/plain";
            await context.Response.SendFileAsync(robotsPath);
            return;
        }
    }
    await next();
});

app.UseRouting();

// Configure Request Localization for multi-language support
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("vi")
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});

// Enable Response Caching
app.UseResponseCaching();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Blog routes (must be BEFORE default route)
app.MapControllerRoute(
    name: "blog-tag",
    pattern: "blog/tag/{slug}",
    defaults: new { controller = "Blog", action = "Tag" });

app.MapControllerRoute(
    name: "blog-category",
    pattern: "blog/category/{slug}",
    defaults: new { controller = "Blog", action = "Category" });

app.MapControllerRoute(
    name: "blog-details",
    pattern: "blog/{slug}",
    defaults: new { controller = "Blog", action = "Details" });

app.MapControllerRoute(
    name: "blog-index",
    pattern: "blog",
    defaults: new { controller = "Blog", action = "Index" });

// SEO-friendly category routes (Vietnamese and English)
app.MapControllerRoute(
    name: "category-vi",
    pattern: "chu-de/{slug}",
    defaults: new { controller = "Home", action = "Category" });
app.MapControllerRoute(
    name: "category-en",
    pattern: "category/{slug}",
    defaults: new { controller = "Home", action = "Category" });

// SEO-friendly privacy policy routes (Vietnamese and English)
app.MapControllerRoute(
    name: "privacy-vi",
    pattern: "chinh-sach-bao-mat",
    defaults: new { controller = "Home", action = "Privacy" });
app.MapControllerRoute(
    name: "privacy-en",
    pattern: "privacy-policy",
    defaults: new { controller = "Home", action = "Privacy" });

// SEO-friendly terms of service routes (Vietnamese and English)
app.MapControllerRoute(
    name: "terms-vi",
    pattern: "dieu-khoan-su-dung",
    defaults: new { controller = "Home", action = "Terms" });
app.MapControllerRoute(
    name: "terms-en",
    pattern: "terms-of-service",
    defaults: new { controller = "Home", action = "Terms" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed blog posts on startup
// try
// {
//     using (var scope = app.Services.CreateScope())
//     {
//         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
//         logger.LogInformation("Application startup: Running BlogSeeder...");

//         var seeder = scope.ServiceProvider.GetRequiredService<BlogSeeder>();
//         await seeder.SeedBlogPostsAsync();

//         logger.LogInformation("Application startup: BlogSeeder completed");
//     }
// }
// catch (Exception ex)
// {
//     var logger = app.Services.GetRequiredService<ILogger<Program>>();
//     logger.LogError(ex, "Application startup: Failed to run BlogSeeder: {Message}", ex.Message);
// }

app.Run();
