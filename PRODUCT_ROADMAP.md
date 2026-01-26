# TrendTag - Product Roadmap & Feature Planning

**Last Updated:** 30/12/2025
**Status:** Phase 0 Complete ✅ | Phase 1 Planning 📋

---

## 📊 Market Analysis Summary

### Vietnam TikTok Market (2025)
- **Total Users:** 65.6 million
- **Users 18+:** 40.9 million
- **Active Creators:** 3-6 million (estimated)
- **Growth Rate:** 20% YoY in APAC region
- **Market Position:** Top in APAC for engagement

### Target Audience
- **Primary:** 3-6 million Vietnamese creators
- **Secondary:** 65.6 million users seeking engagement
- **Tertiary:** Brands/agencies doing influencer marketing

### Market Opportunity
- **Global Social Media Analytics Market:** $14B (2024) → $69.15B (2032)
- **CAGR:** 22-25% (2024-2032)
- **Vietnam Potential:** $50-200M USD (freemium model)
- **Potential MAU:** 30K-300K users (1-5% creator penetration)
- **Revenue Potential:** $36K-60K MRR (2% premium conversion @ $5-10/month)

### Key Insights
- **Pain Points:**
  - Hard to find trending hashtags suitable for niche
  - Don't know which hashtags have low competition
  - Lack of real-time Vietnamese hashtag analysis tools
  - TikTok Ads reach declining, need better hashtag optimization

- **Content Trends (Vietnam 2025):**
  - DIY, Beauty, Food (most popular)
  - #EduTokVN (explosive growth)
  - Authentic storytelling > staged content
  - 3-5 hashtags per video (optimal strategy)

- **Competitive Advantage:**
  - Most tools are global, not optimized for Vietnam
  - Lack of real-time trending hashtag updates
  - No competition difficulty analysis
  - English interface, not friendly for Vietnamese creators

---

## 🎯 Product Vision

**Mission:** Empower Vietnamese TikTok creators with data-driven hashtag insights to maximize their reach and engagement.

**Core Value Proposition:**
1. Real-time trending hashtags (updated every 6 hours)
2. Competition difficulty analysis (help new creators go viral)
3. Multi-source data (TikTok Creative Center, Google Trends, Buffer)
4. 100% free core features (competitive advantage)
5. Vietnamese interface & localized content

---

## ✅ Phase 0: Foundation (COMPLETE)

### Completed Features
- [x] Hashtag search & autocomplete
- [x] Trending hashtags display (top 10 by category)
- [x] 16+ category filtering
- [x] Hashtag details page with metrics
- [x] Competition difficulty indicator
- [x] Historical trending data (50 recent updates)
- [x] SEO optimization (meta tags, structured data, sitemap)
- [x] Blog system (categories, tags, posts)
- [x] Responsive design (mobile-first)
- [x] Vietnamese timezone support
- [x] Database schema & crawler infrastructure

### Technical Stack
- **Backend:** ASP.NET Core 8 MVC
- **Database:** SQL Server LocalDB
- **Frontend:** Bootstrap 5 + Razor Views
- **Crawler:** Background hosted services (6-hour intervals)
- **Data Sources:** TikTok Creative Center, Google Trends, Buffer

---

## 🚀 Phase 1: Core Enhancement (PRIORITY - Next 2-3 Months)

### 1. Hashtag Generator (AI-Powered) ⭐⭐⭐⭐⭐
**Priority:** CRITICAL
**Effort:** Medium (2-3 weeks)
**Impact:** Very High
**Monetization:** Premium feature ($5-10/month unlimited)

**Features:**
- Input: Video description (e.g., "nấu phở bò tại Hà Nội")
- Output: 5-8 recommended hashtags:
  - 2-3 trending hashtags (high reach, high competition)
  - 2-3 niche hashtags (targeted, medium competition)
  - 1-2 location-based hashtags (geo-targeted)
- Display: Competition level + expected reach for each tag
- Copy all hashtags button

**Technical Implementation:**
- **Option 1 (MVP):** Rule-based algorithm
  - Use existing trending data + category mappings
  - Keyword matching from description
  - Difficulty balancing algorithm
  - Cost: $0, Time: 2 weeks

- **Option 2 (Advanced):** GPT/AI Integration
  - OpenAI GPT-4 API for semantic understanding
  - Training data from successful videos
  - Cost: ~$100-500/month API, Time: 3 weeks

**Start with Option 1, upgrade to Option 2 later**

**Database Schema:**
```sql
CREATE TABLE HashtagRecommendations (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NULL, -- NULL for anonymous users
    InputDescription NVARCHAR(500),
    RecommendedHashtags NVARCHAR(MAX), -- JSON array
    GeneratedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

**UI Mockup:**
```
┌─────────────────────────────────────────┐
│ 🤖 Hashtag Generator                    │
├─────────────────────────────────────────┤
│ Mô tả video của bạn:                    │
│ ┌─────────────────────────────────────┐ │
│ │ Nấu phở bò Hà Nội tại nhà đơn giản │ │
│ └─────────────────────────────────────┘ │
│              [Tạo Hashtag]              │
├─────────────────────────────────────────┤
│ ✨ Gợi Ý Hashtag:                        │
│                                         │
│ 🔥 Trending (Broad Reach):              │
│ ☐ #amthuc (Cao, 45M views)             │
│ ☐ #nauanvietnam (Cao, 23M views)       │
│                                         │
│ 🎯 Niche (Targeted):                    │
│ ☐ #phobotainha (Thấp, 156K views)     │
│ ☐ #nauandonngian (TB, 2.3M views)     │
│                                         │
│ 📍 Location:                            │
│ ☐ #hanoifood (TB, 890K views)         │
│                                         │
│        [Copy Tất Cả] [Lưu Lại]         │
└─────────────────────────────────────────┘
```

**Success Metrics:**
- 50%+ users use this feature
- 30%+ conversion to premium (if freemium)
- Average 4+ hashtags selected per generation

---

### 2. Hashtag Performance Tracker ⭐⭐⭐⭐⭐
**Priority:** CRITICAL
**Effort:** Medium (2-3 weeks)
**Impact:** Very High (retention feature)
**Monetization:** Freemium (free basic, premium advanced)

**Features:**
- User adds hashtags they used in videos
- Track performance: views, likes, shares, comments
- Compare performance across hashtags
- Weekly/monthly performance reports
- Top performing hashtags dashboard
- Export reports (CSV/PDF) - Premium

**Technical Implementation:**
- Requires user authentication (login/register)
- Store user's hashtag usage + video performance
- Cron job: weekly summary email
- Charts: Chart.js or ApexCharts

**Database Schema:**
```sql
CREATE TABLE UserHashtagTracking (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    HashtagId INT FOREIGN KEY REFERENCES Hashtags(Id),
    VideoUrl NVARCHAR(500),
    Views BIGINT,
    Likes INT,
    Shares INT,
    Comments INT,
    PostedAt DATETIME2,
    TrackedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) UNIQUE,
    PasswordHash NVARCHAR(255),
    DisplayName NVARCHAR(100),
    IsPremium BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

**UI Mockup:**
```
┌─────────────────────────────────────────┐
│ 📊 Hashtag Performance (7 ngày qua)     │
├─────────────────────────────────────────┤
│ #nauandonngian                          │
│ ⭐⭐⭐⭐⭐ Avg 45K views                  │
│ ▰▰▰▰▰▰▰▰▰▱ 90% performance             │
│                                         │
│ #amthucvietnam                          │
│ ⭐⭐⭐⭐ Avg 32K views                    │
│ ▰▰▰▰▰▰▰▱▱▱ 70% performance             │
│                                         │
│ #foodhacks                              │
│ ⭐⭐⭐ Avg 18K views                      │
│ ▰▰▰▰▰▱▱▱▱▱ 50% performance             │
│                                         │
│        [Thêm Video Mới] [Xem Chi Tiết] │
└─────────────────────────────────────────┘
```

**Success Metrics:**
- 40%+ users track at least 1 hashtag
- 60%+ users return weekly to check stats
- 20%+ convert to premium for export feature

---

### 3. Competitor Hashtag Analysis ⭐⭐⭐⭐
**Priority:** HIGH
**Effort:** High (3-4 weeks)
**Impact:** High (unique differentiator)
**Monetization:** Premium feature ($10-15/month)

**Features:**
- Input: TikTok username of competitor
- Crawl top 10-20 recent videos
- Analyze hashtags used
- Display top hashtags + frequency
- Show rising/declining hashtag trends
- Compare with your own hashtag strategy

**Technical Implementation:**
- **Crawling Strategy:**
  - Option 1: Unofficial TikTok API (TikTokApi library)
  - Option 2: Web scraping (Playwright/Selenium)
  - Rate limiting: Max 10 competitors/day per user
  - Cache results: Update daily

- **Legal Considerations:**
  - Comply with TikTok Terms of Service
  - Public data only (no private profiles)
  - Add disclaimer about data usage

**Database Schema:**
```sql
CREATE TABLE CompetitorAnalysis (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    CompetitorUsername NVARCHAR(100),
    TopHashtags NVARCHAR(MAX), -- JSON array
    VideoCount INT,
    AnalyzedAt DATETIME2 DEFAULT GETUTCDATE(),
    CachedUntil DATETIME2 -- Cache for 24 hours
);
```

**UI Mockup:**
```
┌─────────────────────────────────────────┐
│ 🔍 Phân Tích Competitor                 │
├─────────────────────────────────────────┤
│ TikTok Username:                        │
│ ┌─────────────────────────────────────┐ │
│ │ @competitor_name                    │ │
│ └─────────────────────────────────────┘ │
│              [Phân Tích]                │
├─────────────────────────────────────────┤
│ 📊 Kết Quả: @competitor_name            │
│ (Phân tích 20 videos gần nhất)         │
│                                         │
│ Top Hashtags (30 ngày):                 │
│ #foodhacksvietnam (95%) - Signature    │
│ #nauangiare (80%)                       │
│ #amthucvietnam (75%)                    │
│                                         │
│ Rising Hashtags:                        │
│ #tiktokfood ↗️ (+150% usage)           │
│ #viralrecipes ↗️ (+80% usage)          │
│                                         │
│    [Lưu Phân Tích] [So Sánh Với Tôi]  │
└─────────────────────────────────────────┘
```

**Success Metrics:**
- 25%+ premium users use this monthly
- Average 3-5 competitors analyzed per user
- 15%+ users adjust strategy based on insights

---

## 📅 Phase 2: Growth Features (Month 3-4)

### 4. Trending Content Ideas
**Priority:** MEDIUM
**Effort:** Medium (2 weeks)
**Impact:** Medium-High

**Features:**
- Display trending topics per category
- Suggest content ideas + hashtag combos
- Show viral video examples
- User can save/favorite ideas

**Implementation:**
- Aggregate data from trending hashtags
- Manual curation + AI suggestions
- Update weekly

---

### 5. Best Time to Post
**Priority:** MEDIUM
**Effort:** Low-Medium (1-2 weeks)
**Impact:** Medium

**Features:**
- Analyze peak engagement hours per category
- Recommend best posting times
- Calendar view for content planning
- Timezone: Vietnam (GMT+7)

**Implementation:**
- Historical data analysis
- Category-specific patterns
- User can set reminders (push notifications later)

---

### 6. Hashtag Combination Optimizer
**Priority:** MEDIUM
**Effort:** Medium (2-3 weeks)
**Impact:** Medium

**Features:**
- Input: 10-15 hashtags user wants to use
- AI analyzes compatibility
- Output: Top 3 optimized combinations
- Shows expected reach for each combo

**Implementation:**
- ML model trained on successful video data
- Combination scoring algorithm
- A/B testing recommendations

---

## 🔮 Phase 3: Advanced Features (Month 5-6)

### 7. Video Caption Generator
- AI generates captions from hashtags + description
- Optimize with SEO keywords
- Multi-language support (VN + EN)

### 8. Hashtag Calendar
- Show trending hashtags by events/holidays
- Examples: Tết, Valentine, Black Friday
- Auto-suggest seasonal content

### 9. Influencer Discovery
- Find micro-influencers in niche
- Contact info + collaboration suggestions
- Estimated campaign costs

---

## 🌟 Phase 4: Scale Features (Month 7+)

### 10. TikTok Shop Integration
- Hashtag suggestions for product videos
- Affiliate marketing hashtag recommendations
- E-commerce trend analysis

### 11. Multi-Platform Support
- Instagram Reels hashtags
- YouTube Shorts hashtags
- Cross-platform optimization

### 12. Team Collaboration
- Share hashtag lists with team
- Content calendar for agencies
- Multi-user accounts

---

## 💰 Monetization Strategy

### Freemium Model

**Free Tier (Forever Free):**
- ✅ Search & view trending hashtags
- ✅ Category filtering
- ✅ Basic competition analysis
- ✅ 5 hashtag generations/day
- ✅ Track 3 hashtags
- ✅ View top 10 trending per category

**Premium Tier ($5-10/month):**
- ✅ Unlimited hashtag generations
- ✅ Hashtag performance tracker (unlimited)
- ✅ Competitor analysis (10/day)
- ✅ Export reports (CSV/PDF)
- ✅ Historical data (6 months)
- ✅ Best time to post analytics
- ✅ Priority support

**Pro Tier ($15-25/month) - For Agencies:**
- ✅ All Premium features
- ✅ Competitor analysis (unlimited)
- ✅ Team collaboration (5 users)
- ✅ Content calendar
- ✅ API access
- ✅ White-label reports

### Revenue Projections

**Conservative (1% creator penetration):**
- MAU: 30,000 users
- Conversion: 2% → 600 paid users
- ARPU: $7/month
- MRR: $4,200
- ARR: $50,400

**Moderate (3% creator penetration):**
- MAU: 100,000 users
- Conversion: 3% → 3,000 paid users
- ARPU: $8/month
- MRR: $24,000
- ARR: $288,000

**Optimistic (5% creator penetration):**
- MAU: 300,000 users
- Conversion: 5% → 15,000 paid users
- ARPU: $10/month
- MRR: $150,000
- ARR: $1,800,000

---

## 🎨 UI/UX Improvements (Parallel Work)

### User Dashboard
- [x] Clean, modern design ✅
- [ ] User login/register system
- [ ] My Saved Hashtags section
- [ ] My Performance Stats
- [ ] Quick access to recent searches
- [ ] Notification center

### Mobile Optimization
- [x] Responsive navbar ✅
- [x] Mobile-friendly search ✅
- [ ] PWA (Progressive Web App)
- [ ] Native app (iOS/Android) - Phase 4

### Chrome Extension
- [ ] Analyze hashtags directly on TikTok.com
- [ ] Quick copy hashtag combo
- [ ] Real-time competition indicator
- [ ] One-click add to tracker

---

## 📈 Growth & Marketing Strategy

### SEO (Organic Growth)
- [x] On-page SEO optimization ✅
- [x] Structured data (Schema.org) ✅
- [x] Blog system for content marketing ✅
- [ ] Target keywords:
  - "hashtag tiktok trending" (~10K searches/month)
  - "hashtag viral tiktok" (~5K searches/month)
  - "công cụ tìm hashtag" (~3K searches/month)
- [ ] Backlink building
- [ ] Guest posting on marketing blogs

### Content Marketing
- [ ] Write 20+ blog posts about TikTok growth
- [ ] Video tutorials on YouTube
- [ ] TikTok account sharing tips (meta marketing)
- [ ] Weekly trending hashtag newsletter

### Community Building
- [ ] Facebook group for Vietnamese TikTok creators
- [ ] Discord server for real-time hashtag discussions
- [ ] User success stories & case studies

### Paid Acquisition
- [ ] Google Ads (search campaigns)
- [ ] Facebook/Instagram Ads (creator targeting)
- [ ] TikTok Ads (reach creators on platform)
- [ ] Influencer partnerships (micro-influencers)

---

## 🛠 Technical Debt & Infrastructure

### Performance Optimization
- [ ] Database indexing optimization
- [ ] Redis caching layer
- [ ] CDN for static assets
- [ ] Image optimization (lazy loading)

### Scalability
- [ ] Migrate from LocalDB to Azure SQL/AWS RDS
- [ ] Containerization (Docker)
- [ ] Load balancing (multiple app instances)
- [ ] Queue system for crawler jobs (RabbitMQ/Azure Service Bus)

### Monitoring & Analytics
- [ ] Application Insights / Google Analytics
- [ ] Error tracking (Sentry)
- [ ] User behavior analytics (Mixpanel/Amplitude)
- [ ] Performance monitoring (New Relic)

### Security
- [ ] HTTPS enforcement ✅
- [ ] Rate limiting for API endpoints
- [ ] SQL injection prevention ✅
- [ ] XSS protection ✅
- [ ] GDPR compliance
- [ ] Data encryption at rest

---

## 📋 Implementation Checklist - Phase 1

### Hashtag Generator (Week 1-3)
- [ ] Week 1: Database schema + backend logic
  - [ ] Create HashtagRecommendations table
  - [ ] Implement rule-based algorithm
  - [ ] Add category keyword mappings
  - [ ] Create HashtagGeneratorService
- [ ] Week 2: Frontend UI
  - [ ] Design generator interface
  - [ ] Add input form with validation
  - [ ] Display recommendations with metrics
  - [ ] Copy all hashtags button
- [ ] Week 3: Testing & refinement
  - [ ] A/B test algorithm accuracy
  - [ ] User feedback collection
  - [ ] Performance optimization

### User Authentication System (Week 2-4)
- [ ] Week 2-3: Backend implementation
  - [ ] Create Users table
  - [ ] Implement registration/login
  - [ ] Email verification
  - [ ] Password reset flow
  - [ ] JWT/Cookie authentication
- [ ] Week 3-4: Frontend UI
  - [ ] Login/Register pages
  - [ ] User profile page
  - [ ] Settings page
  - [ ] Premium upgrade flow

### Hashtag Performance Tracker (Week 4-7)
- [ ] Week 4-5: Database + backend
  - [ ] Create UserHashtagTracking table
  - [ ] API endpoints for CRUD operations
  - [ ] Performance calculation logic
  - [ ] Weekly summary email job
- [ ] Week 6: Frontend dashboard
  - [ ] Performance charts (Chart.js)
  - [ ] Add video form
  - [ ] Comparison view
  - [ ] Export functionality (Premium)
- [ ] Week 7: Testing & polish
  - [ ] Edge case handling
  - [ ] Mobile responsiveness
  - [ ] Performance optimization

### Competitor Analysis (Week 8-11)
- [ ] Week 8-9: Crawler implementation
  - [ ] Research TikTok scraping libraries
  - [ ] Implement rate limiting
  - [ ] Cache mechanism (24h)
  - [ ] Error handling
- [ ] Week 10: Analysis logic
  - [ ] Hashtag frequency calculator
  - [ ] Rising/declining trend detector
  - [ ] Comparison algorithm
- [ ] Week 11: Frontend + testing
  - [ ] Analysis UI
  - [ ] Results visualization
  - [ ] Save/export features
  - [ ] Premium gating

---

## 🎯 Success Metrics (KPIs)

### Product Metrics
- **MAU (Monthly Active Users):** 30K → 100K → 300K
- **DAU/MAU Ratio:** >30% (healthy engagement)
- **User Retention:**
  - Day 1: >40%
  - Day 7: >20%
  - Day 30: >10%
- **Premium Conversion:** 2-5%
- **Churn Rate:** <5% monthly

### Feature Adoption
- **Hashtag Generator:** 50%+ MAU use monthly
- **Performance Tracker:** 40%+ MAU use weekly
- **Competitor Analysis:** 25%+ premium users use monthly

### Business Metrics
- **MRR (Monthly Recurring Revenue):** $4K → $24K → $150K
- **ARPU (Average Revenue Per User):** $7-10/month
- **CAC (Customer Acquisition Cost):** <$10 (organic), <$20 (paid)
- **LTV (Lifetime Value):** >$100 (>10 months retention)
- **LTV/CAC Ratio:** >3

### Growth Metrics
- **Organic Traffic:** 50K-100K visits/month
- **Email List:** 10K-50K subscribers
- **Social Following:** 20K+ (Facebook/TikTok/Instagram combined)
- **Blog Traffic:** 20K visits/month

---

## 🚧 Risks & Mitigation

### Technical Risks
**Risk:** TikTok API changes/restrictions
**Mitigation:**
- Diversify data sources
- Build fallback scrapers
- Cache historical data

**Risk:** Database performance degradation at scale
**Mitigation:**
- Implement caching early (Redis)
- Database sharding strategy
- Monitor query performance

### Business Risks
**Risk:** Low conversion to premium
**Mitigation:**
- A/B test pricing ($5 vs $10)
- Add more premium features
- Freemium limits adjustment

**Risk:** High competition from established tools
**Mitigation:**
- Focus on Vietnam market (niche)
- Better UX/localization
- Community building
- Faster feature iteration

### Legal Risks
**Risk:** TikTok terms of service violations
**Mitigation:**
- Only use public data
- Add proper disclaimers
- Legal consultation if needed

---

## 📚 Resources & References

### Market Research
- [TikTok Statistics 2025](https://www.bizreport.com/business/tiktok-statistics)
- [Vietnam TikTok Usage](https://www.vietnam.vn/en/nguoi-dung-tiktok-o-viet-nam-tang-truong-ra-sao)
- [TikTok Trends Vietnam 2025](https://tiksnap.net/blogs/tiktok-trends-in-vietnam-2025/)
- [Social Media Analytics Market](https://www.gminsights.com/industry-analysis/social-media-analytics-market)

### Technical Documentation
- ASP.NET Core MVC
- Entity Framework Core
- TikTok API (unofficial)
- Bootstrap 5 Components

### Competitor Analysis
- TikTok Creative Center
- Hootsuite
- Later
- Sprout Social
- Local Vietnam tools (if any)

---

## 📝 Notes

### Current Status (30/12/2025)
- Phase 0 complete ✅
- Planning Phase 1 implementation
- Need to prioritize: Hashtag Generator (highest ROI)
- User authentication required for Phase 1 features
- Consider hiring 1-2 developers for faster execution

### Next Steps
1. Design detailed database schema for Phase 1
2. Set up project management board (Trello/Notion)
3. Start development: Hashtag Generator (Week 1)
4. Implement user authentication (Week 2-3)
5. Launch MVP of Phase 1 features (Month 2)

### Decision Log
- **2025-12-30:** Decided to start with rule-based hashtag generator before AI integration (faster MVP)
- **2025-12-30:** Freemium model chosen over fully paid (better user acquisition)
- **2025-12-30:** Focus on Vietnam market first before expanding to SEA

---

**END OF ROADMAP**

*This is a living document. Update regularly as features are completed and priorities change.*
