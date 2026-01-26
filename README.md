# TrendTag - Trending Hashtag Analytics System

Hệ thống thu thập và phân tích hashtag trending từ nhiều nguồn (TikTok, Google Trends, Buffer, v.v.)

## 🎯 Tính Năng

### Admin Dashboard
- **Manual Crawl**: Trigger crawl thủ công tất cả sources hoặc từng source riêng lẻ
- **Trending Hashtags**: Xem và filter hashtags trending
- **Crawl Logs**: Xem lịch sử crawl chi tiết
- **Source Management**: Theo dõi trạng thái các nguồn dữ liệu

### Auto Crawling
- Tự động crawl mỗi 6 giờ (có thể cấu hình)
- Retry logic với exponential backoff
- Duplicate prevention
- Comprehensive logging

### Hashtag Features
- 8 nguồn dữ liệu: TikTok, GoogleTrends, Buffer, Trollishly, CapCut, TokChart, Countik, Picuki
- Filter theo source, date range, rank
- Search với pagination
- View chi tiết hashtag history
- Related hashtags
- Categories & difficulty levels (sẵn sàng cho future)

## 🚀 Cài Đặt

### 1. Yêu Cầu
- .NET 8.0 SDK
- SQL Server LocalDB (đi kèm Visual Studio)
- PowerShell (để install Playwright browsers)

### 2. Setup Database

```bash
cd d:\Task\TrendTag\HashTag

# Restore dependencies
dotnet restore

# Apply migrations
dotnet ef database update
```

Database sẽ tự động tạo 9 tables và seed 8 sources.

### 3. Install Playwright Browsers

**Quan trọng**: Phải chạy lệnh này trước khi crawl lần đầu:

```powershell
# Trong PowerShell
cd d:\Task\TrendTag\HashTag\bin\Debug\net8.0
.\playwright.ps1 install chromium
```

Hoặc build rồi chạy:
```powershell
dotnet build
cd bin\Debug\net8.0
.\playwright.ps1 install chromium
```

### 4. Run Application

```bash
cd d:\Task\TrendTag\HashTag
dotnet run
```

Hoặc nhấn F5 trong Visual Studio.

## 📖 Sử Dụng

### Truy Cập Admin Dashboard

```
http://localhost:5000/Admin
```

hoặc

```
https://localhost:5001/Admin
```

### Menu Admin:
- **Dashboard**: `/Admin` - Tổng quan hệ thống
- **Manual Crawl**: `/Admin/Crawl` - Chạy crawl thủ công
- **Trending Hashtags**: `/Admin/Hashtags` - Xem hashtags
- **Crawl Logs**: `/Admin/CrawlLogs` - Xem logs

### Chạy Crawl Thủ Công

1. Vào `/Admin/Crawl`
2. Chọn:
   - **"Start Crawling All Sources"** - Crawl tất cả (khuyến nghị)
   - Hoặc click button "Crawl" từng source riêng lẻ
3. Đợi kết quả (mất ~2-3 phút cho all sources)
4. Xem kết quả tại `/Admin/CrawlResults`

### Xem Trending Hashtags

1. Vào `/Admin/Hashtags`
2. Sử dụng filters:
   - **Sources**: Chọn nguồn dữ liệu
   - **Date Range**: Lọc theo ngày
   - **Rank Range**: Lọc theo thứ hạng
   - **Sort By**: Sắp xếp theo rank, appearances, hoặc last seen
3. Click "Details" để xem chi tiết hashtag

## ⚙️ Cấu Hình

### appsettings.json

```json
{
  "CrawlerSettings": {
    "ScheduleIntervalHours": 6,          // Tần suất auto crawl (giờ)
    "RunOnStartup": false,               // Crawl ngay khi khởi động app
    "EnabledSources": [                  // Sources được bật
      "TikTok",
      "GoogleTrends",
      "Buffer",
      "Trollishly",
      "CapCut",
      "TokChart",
      "Countik"
    ],
    "MaxRetries": 3,                     // Số lần retry khi lỗi
    "TimeoutSeconds": 120                // Timeout cho mỗi request
  }
}
```

### Thay Đổi Tần Suất Auto Crawl

Sửa `ScheduleIntervalHours` trong appsettings.json:
- `6` = Mỗi 6 giờ (mặc định)
- `1` = Mỗi 1 giờ
- `24` = Mỗi ngày

### Tắt/Bật Sources

Thêm hoặc xóa tên source trong `EnabledSources`:
```json
"EnabledSources": ["TikTok", "GoogleTrends"]  // Chỉ crawl 2 nguồn này
```

## 📊 Database Schema

### Core Tables (Phase 1)
- **Hashtags**: Thông tin hashtag chính
- **HashtagSources**: Nguồn dữ liệu (8 sources)
- **HashtagHistory**: Lịch sử rank theo thời gian
- **CrawlLogs**: Log các lần crawl

### Extended Tables (Cho Search Features)
- **HashtagMetrics**: Metrics theo ngày (views, posts, difficulty, growth)
- **HashtagCategories**: Phân loại nội dung (Fashion, Tech, Beauty, etc.)
- **HashtagRelations**: Mối quan hệ giữa hashtags (co-occurrence)
- **HashtagKeywords**: Từ khóa mapping (intent-based search)

## 🔧 Troubleshooting

### "Playwright not installed" Error

```powershell
cd d:\Task\TrendTag\HashTag\bin\Debug\net8.0
.\playwright.ps1 install chromium
```

### Database Connection Error

Kiểm tra SQL Server LocalDB đang chạy:
```bash
sqllocaldb info mssqllocaldb
sqllocaldb start mssqllocaldb
```

### Crawl Failed

1. Kiểm tra logs tại `/Admin/CrawlLogs`
2. Xem error message
3. Thử crawl lại từng source riêng lẻ tại `/Admin/Crawl`
4. Một số sites có thể block crawler (bình thường)

### No Hashtags Showing

1. Chạy manual crawl lần đầu: `/Admin/Crawl`
2. Hoặc đợi auto crawler chạy (mỗi 6h)
3. Kiểm tra crawl logs xem có lỗi không

## 📁 Cấu Trúc Project

```
TrendTag/
├── HashTag/                    # Web application
│   ├── Controllers/
│   │   ├── AdminController.cs      # Admin features
│   │   └── HashtagController.cs    # Public hashtag views (unused)
│   ├── Services/
│   │   ├── CrawlerService.cs       # Crawling logic
│   │   └── HashtagCrawlerHostedService.cs  # Background service
│   ├── Repositories/
│   │   └── HashtagRepository.cs    # Data access
│   ├── Models/                     # Database entities (9 models)
│   ├── ViewModels/                 # View models
│   ├── Views/
│   │   ├── Admin/                  # Admin UI
│   │   └── Hashtag/                # Hashtag views
│   └── Data/
│       └── TrendTagDbContext.cs    # EF Core context
│
└── TrendTag.Crawler/           # Standalone crawler (for testing)
    └── Program.cs
```

## 🎯 Roadmap (Future Phases)

### Phase 6-7: Analytics & Search
- [ ] API endpoints cho charts
- [ ] Analytics service với calculations
- [ ] Intent-based keyword search
- [ ] Difficulty scoring algorithm

### Phase 8: Visualizations
- [ ] Chart.js integration
- [ ] Trending line charts
- [ ] Source comparison charts
- [ ] Growth rate visualization

### Phase 9: Polish
- [ ] Response caching
- [ ] Mobile responsive optimization
- [ ] Admin authentication
- [ ] Rate limiting

### Phase 10: Testing
- [ ] Unit tests
- [ ] Integration tests
- [ ] Performance testing

## 📝 Notes

- **Dashboard chỉ dành cho Admin**: Trending Hashtags hiện tại chỉ accessible qua Admin menu
- **Background Service**: Auto crawler chạy mỗi 6h, không cần manual trigger
- **Duplicate Prevention**: Hệ thống tự động bỏ qua hashtags đã thu thập trong cùng ngày
- **8 Sources**: Picuki không hoạt động (offline since 2023), 7 sources còn lại active

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server LocalDB (Entity Framework Core)
- **Crawler**: Microsoft.Playwright (Chromium headless)
- **UI**: Bootstrap 5 + Bootstrap Icons
- **Background Jobs**: IHostedService
- **Serialization**: Newtonsoft.Json

## 📞 Support

Nếu gặp vấn đề:
1. Check Crawl Logs: `/Admin/CrawlLogs`
2. Verify database: `dotnet ef database update`
3. Reinstall Playwright: `.\playwright.ps1 install chromium`
4. Check application logs trong console

---

**Version**: 1.0.0 (Phase 1-5 Complete)
**Last Updated**: 2025-12-25
