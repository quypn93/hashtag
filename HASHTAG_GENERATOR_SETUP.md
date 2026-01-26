# Hashtag Generator - Setup Guide

**Feature:** AI-Powered Hashtag Generator using GPT-4
**Status:** Implementation in progress
**Date:** 30/12/2025

---

## 📦 Step 1: Install Required NuGet Packages

Run these commands in Package Manager Console or Terminal:

```bash
cd HashTag

# Option A: OpenAI Official SDK (Recommended)
dotnet add package Azure.AI.OpenAI --version 1.0.0-beta.12

# Option B: Community SDK (Alternative)
dotnet add package Betalgo.OpenAI --version 7.4.3

# For JSON handling
dotnet add package System.Text.Json --version 8.0.0
```

---

## 🔑 Step 2: Get OpenAI API Key

### Option A: OpenAI API (Easier, mais dùng thử)

1. Go to https://platform.openAI.com
2. Sign up / Login
3. Go to **API Keys** section
4. Click **Create new secret key**
5. Copy key (starts with `sk-...`)

**Pricing:**
- GPT-4: $0.01 per 1K tokens
- GPT-3.5-Turbo: $0.001 per 1K tokens (10x cheaper)
- Free tier: $5 credit (enough for ~500-5000 generations)

### Option B: Azure Open AI (For Production)

1. Create Azure account
2. Create Azure OpenAI resource
3. Deploy GPT-4 model
4. Get endpoint + API key

**Pricing:**
- Similar to OpenAI
- Better reliability & SLA
- Vietnam region support

---

## ⚙️ Step 3: Configure AppSettings

Add to `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "gpt-4",
    "MaxTokens": 800,
    "Temperature": 0.7,
    "CacheDurationHours": 168
  },
  "HashtagGenerator": {
    "FreeUserDailyLimit": 5,
    "PremiumUserDailyLimit": 100,
    "EnableCaching": true,
    "CacheDurationDays": 7
  }
}
```

Add to `appsettings.Development.json` (for testing):

```json
{
  "OpenAI": {
    "ApiKey": "sk-YOUR_DEVELOPMENT_KEY",
    "Model": "gpt-3.5-turbo"
  }
}
```

**⚠️ Security:**
- NEVER commit API keys to git
- Add to `.gitignore`: `appsettings.Development.json`
- Use User Secrets for development
- Use Azure Key Vault for production

---

## 🔐 Step 4: Setup User Secrets (Development)

```bash
cd HashTag

# Initialize user secrets
dotnet user-secrets init

# Add OpenAI API key
dotnet user-secrets set "OpenAI:ApiKey" "sk-YOUR_KEY_HERE"

# Verify
dotnet user-secrets list
```

---

## 🛠 Step 5: Register Service in Program.cs

Add this code to `Program.cs`:

```csharp
// Add OpenAI configuration
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.Configure<HashtagGeneratorOptions>(
    builder.Configuration.GetSection("HashtagGenerator"));

// Register Hashtag Generator Service
builder.Services.AddScoped<IHashtagGeneratorService, HashtagGeneratorService>();

// Register HTTP client for OpenAI (with retry policy)
builder.Services.AddHttpClient<IHashtagGeneratorService, HashtagGeneratorService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

---

## 📝 Step 6: Create Configuration Classes

Create `Options/OpenAIOptions.cs`:

```csharp
namespace HashTag.Options;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.7;
    public int CacheDurationHours { get; set; } = 168;
}

public class HashtagGeneratorOptions
{
    public int FreeUserDailyLimit { get; set; } = 5;
    public int PremiumUserDailyLimit { get; set; } = 100;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationDays { get; set; } = 7;
}
```

---

## 🧪 Step 7: Test API Connection

Create a test endpoint to verify OpenAI connection:

```csharp
[HttpGet("test-openai")]
public async Task<IActionResult> TestOpenAI()
{
    try
    {
        var response = await _generatorService.TestConnection();
        return Ok(new { success = true, message = "OpenAI connected!", response });
    }
    catch (Exception ex)
    {
        return Ok(new { success = false, error = ex.Message });
    }
}
```

Test in browser: `https://localhost:7125/api/hashtag/test-openai`

---

## 📊 Cost Estimation

### Per Generation Cost:

**Input (Prompt):**
- Description: ~50 tokens
- Trending hashtags list: ~400 tokens
- System prompt: ~200 tokens
- **Total Input:** ~650 tokens

**Output (Response):**
- JSON with 5-7 hashtags + reasoning
- **Total Output:** ~300 tokens

**Total per generation:** ~950 tokens

### Monthly Cost Scenarios:

**GPT-4 ($0.03 per 1K input + $0.06 per 1K output):**
- Cost per generation: ~$0.038
- 1,000 generations: $38
- 10,000 generations: $380

**GPT-3.5-Turbo ($0.0015 per 1K input + $0.002 per 1K output):**
- Cost per generation: ~$0.002
- 1,000 generations: $2
- 10,000 generations: $20

**Recommendation for MVP:**
- Start with GPT-3.5-Turbo (save 95% cost)
- Cache results (7 days) → reduce 80% API calls
- Upgrade to GPT-4 if quality isn't sufficient

---

## 💰 Cost Optimization Strategies

### 1. Aggressive Caching
```csharp
// Cache similar descriptions for 7 days
var cacheKey = ComputeDescriptionHash(description);
var cached = await GetCachedResult(cacheKey);
if (cached != null && cached.CachedUntil > DateTime.UtcNow)
{
    return cached; // No API call!
}
```

**Expected savings:** 70-80% (many users describe similar content)

### 2. Rate Limiting
```csharp
// Free users: 5 generations/day
// Premium users: 100 generations/day
```

**Expected savings:** Prevent abuse, control costs

### 3. Batch Processing (Future)
- Collect multiple requests
- Send to API in batch
- Reduce overhead

**Expected savings:** 20-30%

### 4. Fallback to Rule-Based
```csharp
if (apiCallFails || rateLimitExceeded)
{
    return await RuleBasedGenerator.Generate(description);
}
```

**Expected savings:** 100% for fallback cases

---

## 🚀 Deployment Checklist

### Development
- [ ] Install Azure.AI.OpenAI package
- [ ] Setup user secrets with API key
- [ ] Test API connection
- [ ] Implement HashtagGeneratorService
- [ ] Add caching logic
- [ ] Test with sample descriptions

### Staging
- [ ] Use Azure Key Vault for API key
- [ ] Enable Application Insights monitoring
- [ ] Set up cost alerts ($50, $100, $200)
- [ ] Test rate limiting
- [ ] Load test (100 concurrent users)

### Production
- [ ] Deploy to Azure App Service
- [ ] Configure auto-scaling
- [ ] Set up Redis cache (distributed)
- [ ] Monitor API costs daily
- [ ] A/B test GPT-3.5 vs GPT-4

---

## 📈 Monitoring & Analytics

### Metrics to Track:

**Usage:**
- Total generations per day/week/month
- Average generations per user
- Free vs Premium ratio

**Performance:**
- API response time (target: <3s)
- Cache hit rate (target: >70%)
- Error rate (target: <1%)

**Cost:**
- Daily API spend
- Cost per generation
- Cost per user

**Quality:**
- User satisfaction (thumbs up/down)
- Copy rate (% users who click "Copy All")
- Save rate (% users who save results)

### Alerts to Set:

- Daily cost > $20 → Email notification
- Daily cost > $50 → Pause non-premium generations
- Error rate > 5% → Switch to rule-based fallback
- Response time > 5s → Investigate

---

## 🔧 Troubleshooting

### Error: "Invalid API Key"
**Solution:** Check appsettings.json or user secrets

### Error: "Rate limit exceeded"
**Solution:** You hit OpenAI's rate limit (60 requests/minute for free tier). Upgrade plan or add retry logic.

### Error: "Model not found"
**Solution:** Check model name. Use "gpt-4" or "gpt-3.5-turbo"

### High API costs
**Solutions:**
1. Check cache hit rate (should be >70%)
2. Consider switching to GPT-3.5-Turbo
3. Implement stricter rate limiting
4. Add email verification for free users

---

## 📚 Resources

**Documentation:**
- [Azure OpenAI SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Best Practices for Prompts](https://platform.openai.com/docs/guides/prompt-engineering)

**Cost Calculators:**
- [OpenAI Pricing](https://openai.com/pricing)
- [Azure OpenAI Pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/)

---

**Next Steps:**
1. ✅ Install packages
2. ✅ Get API key
3. ✅ Configure appsettings
4. 🔨 Implement service (see HASHTAG_GENERATOR_SERVICE.md)
5. 🔨 Build frontend UI
6. 🧪 Test & refine prompts

---

**STATUS: Ready for implementation**
**Estimated Time: 3 days**
**Estimated Cost: $2-20/month (with caching)**
