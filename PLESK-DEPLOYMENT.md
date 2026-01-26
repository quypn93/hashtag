# TrendTag Deployment on Plesk Hosting

## ⚠️ Lưu ý quan trọng

**Plesk shared hosting KHÔNG hỗ trợ Playwright** vì:
- Không có quyền root để cài system dependencies
- Không thể chạy Chromium headless browser
- Giới hạn về memory và process

## ✅ Giải pháp

### Option 1: Tắt Crawler (Khuyến nghị cho Plesk)

Crawler đã được **TẮT** trong file `appsettings.json`:

```json
"CrawlerSettings": {
  "ScheduleIntervalHours": 999999,  // Không tự động chạy
  "RunOnStartup": false,
  "EnabledSources": [],  // Không có source nào được enable
}
```

**Kết quả:**
- ✅ Website vẫn hoạt động bình thường
- ✅ Hashtag generator vẫn hoạt động (dùng data có sẵn trong DB)
- ✅ Blog, sitemap, tất cả features khác vẫn hoạt động
- ❌ Không tự động cập nhật hashtag mới từ TikTok

### Option 2: Chạy Crawler từ máy local

**Cách 1: Chạy crawler trên máy local rồi đồng bộ DB**

1. Chạy crawler trên máy Windows/Mac của bạn:
```bash
cd D:\Task\TrendTag
dotnet run --project HashTag
```

2. Vào Admin panel: `http://localhost:7125/Admin/Crawler`

3. Click "Run Crawler Now"

4. Database sẽ tự động đồng bộ (vì dùng chung connection string)

**Cách 2: Schedule task trên máy local**

Tạo Windows Task Scheduler để tự động chạy crawler mỗi 6 giờ:

```powershell
# Task definition
$action = New-ScheduledTaskAction -Execute "dotnet" -Argument "run --project D:\Task\TrendTag\HashTag"
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Hours 6)
Register-ScheduledTask -TaskName "TrendTag Crawler" -Action $action -Trigger $trigger
```

### Option 3: Nâng cấp lên VPS/Cloud (Tốt nhất)

Để crawler hoạt động tốt nhất, nên nâng cấp lên:

**VPS/Cloud Providers:**
- DigitalOcean Droplet ($12/tháng)
- Vultr Cloud Compute ($10/tháng)
- AWS EC2 t3.small ($15/tháng)
- Azure App Service ($13/tháng)

**Lợi ích:**
- ✅ Chạy Playwright được
- ✅ Auto crawler 24/7
- ✅ Nhiều RAM hơn
- ✅ Không bị giới hạn process

## 📤 Deploy lên Plesk

### 1. Upload files

Trong Plesk File Manager, upload các files vào `httpdocs`:

```
httpdocs/
├── HashTag.dll
├── HashTag.deps.json
├── HashTag.runtimeconfig.json
├── appsettings.json
├── appsettings.Production.json (optional)
├── wwwroot/
└── ... (other DLLs)
```

### 2. Cấu hình Database

Đảm bảo connection string trong `appsettings.json` đúng:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=112.78.2.36; Initial Catalog=vir62982_tag; User ID=vir62982_user; Password=*1MAbonR?hu7saa7; App=hashtagapp; Timeout=180; Encrypt=false;"
  }
}
```

### 3. Cấu hình IIS/Plesk

**Plesk → Domains → trendtag.com → Hosting Settings:**

- Document root: `httpdocs`
- Application pool: `.NET Core`
- .NET CLR version: No Managed Code (cho .NET 8)

**web.config** (Plesk tự tạo, hoặc tạo thủ công):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\HashTag.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### 4. Restart Application

Trong Plesk:
- **Tools & Settings → Restart App Pool**
- Hoặc vào domain → **Restart Application**

### 5. Kiểm tra

Truy cập:
- Homepage: `https://trendtag.com`
- Admin: `https://trendtag.com/Admin/Login`
- Blog: `https://trendtag.com/blog`
- Sitemap: `https://trendtag.com/sitemap.xml`

## 🔍 Troubleshooting trên Plesk

### Lỗi: 500 Internal Server Error

**Nguyên nhân:** .NET Runtime chưa được cài

**Giải pháp:**
1. Plesk → Extensions → .NET Core
2. Cài đặt .NET 8.0 Runtime
3. Restart application

### Lỗi: Database connection failed

**Nguyên nhân:** Connection string sai hoặc firewall block

**Giải pháp:**
1. Kiểm tra IP database server có đúng không
2. Kiểm tra firewall có allow IP của Plesk server không
3. Test connection trong Plesk → Databases → Remote Access

### Lỗi: Crawler fails (Expected)

**Nguyên nhân:** Playwright không hoạt động trên Plesk

**Giải pháp:** Đã tắt crawler rồi, không cần làm gì

## 📊 Features hoạt động trên Plesk

| Feature | Status | Note |
|---------|--------|------|
| Homepage | ✅ Hoạt động | Hiển thị hashtag từ DB |
| Hashtag Generator | ✅ Hoạt động | Dùng AI + data có sẵn |
| Blog System | ✅ Hoạt động | Đầy đủ chức năng |
| Sitemap | ✅ Hoạt động | Tự động generate |
| Admin Panel | ✅ Hoạt động | Quản lý hashtag, blog |
| Growth Tracker | ✅ Hoạt động | Phân tích dữ liệu |
| Auto Crawler | ❌ Tắt | Cần VPS hoặc chạy local |

## 💡 Khuyến nghị

Để TrendTag hoạt động tốt nhất với auto-crawler:

1. **Ngắn hạn:** Chạy crawler trên máy local, schedule mỗi 6 giờ
2. **Dài hạn:** Nâng cấp lên VPS ($10-15/tháng) để crawler chạy 24/7

Hiện tại với Plesk, website vẫn hoạt động hoàn toàn bình thường, chỉ thiếu tính năng tự động cập nhật hashtag mới.
