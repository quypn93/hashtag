# Hướng Dẫn Cài Đặt Groq API cho Hashtag Generator

## ✅ Groq API đã được tích hợp thành công!

Build Status: ✅ **SUCCESS** - Không có lỗi

---

## 🎯 Bước 1: Lấy Groq API Key (MIỄN PHÍ)

### 1.1. Truy cập Groq Console
- Mở trình duyệt và truy cập: **https://console.groq.com**
- Đăng nhập hoặc đăng ký tài khoản (miễn phí)

### 1.2. Tạo API Key
1. Sau khi đăng nhập, click vào **"API Keys"** ở menu bên trái
2. Click nút **"Create API Key"**
3. Đặt tên cho API key (ví dụ: "TrendTag Hashtag Generator")
4. Click **"Submit"**
5. **QUAN TRỌNG:** Copy API key ngay (chỉ hiển thị 1 lần!)
   - Format: `gsk_...` (bắt đầu bằng "gsk_")

### 1.3. Groq Free Tier Limits
- **30 requests/minute** - Rất cao cho nhu cầu cá nhân
- **Miễn phí hoàn toàn** - Không cần thẻ tín dụng
- **Model: Llama 3.1 70B** - Chất lượng tương đương GPT-3.5
- **Tốc độ:** Cực nhanh (~800 tokens/giây)

---

## 🔧 Bước 2: Cấu Hình API Key

### 2.1. Mở File appsettings.json
Đường dẫn: `d:\Task\TrendTag\HashTag\appsettings.json`

### 2.2. Thay Thế API Key
Tìm đến section `"OpenAI"` và thay **"YOUR_GROQ_API_KEY_HERE"** bằng API key vừa copy:

```json
"OpenAI": {
  "ApiKey": "gsk_...",  // <-- Dán API key vào đây
  "Model": "llama-3.1-70b-versatile",
  "MaxTokens": 800,
  "Temperature": 0.7,
  "ApiEndpoint": "https://api.groq.com/openai/v1/chat/completions",
  "Provider": "Groq"
}
```

### 2.3. Lưu File
- Nhấn `Ctrl + S` để lưu
- **KHÔNG** commit file này lên Git (đã có trong .gitignore)

---

## 🚀 Bước 3: Chạy Ứng Dụng

### 3.1. Build Project (đã build thành công)
```bash
cd HashTag
dotnet build
```

### 3.2. Chạy Application
```bash
dotnet run
```

### 3.3. Mở Trình Duyệt
- URL: **https://localhost:7125/Hashtag/Generator**
- Hoặc click vào **"Tạo Hashtag AI"** trên navbar

---

## 🧪 Bước 4: Test Hashtag Generator

### 4.1. Nhập Mô Tả Video
**Ví dụ 1 - Du lịch:**
```
Video hướng dẫn du lịch Đà Lạt tự túc với 2 triệu đồng,
khám phá những địa điểm check-in đẹp, ăn uống ngon,
phù hợp cho các bạn trẻ đi du lịch tiết kiệm
```

**Ví dụ 2 - Ẩm thực:**
```
Hướng dẫn làm bánh mì Việt Nam tại nhà,
công thức đơn giản, giòn rụm, thơm ngon,
nguyên liệu dễ tìm, phù hợp cho người mới
```

**Ví dụ 3 - Làm đẹp:**
```
Review skincare routine buổi tối cho da dầu mụn,
các sản phẩm giá rẻ từ 100-300k, hiệu quả sau 1 tháng,
phù hợp cho học sinh sinh viên
```

### 4.2. Click "Tạo Hashtag Ngay"
- Hệ thống sẽ gọi Groq API (Llama 3.1)
- Chờ 2-5 giây (rất nhanh)
- Hiển thị kết quả với 3 loại hashtag:
  - 🔥 **Trending Hashtags** (1-2 hashtag)
  - 🎯 **Niche Hashtags** (2-3 hashtag)
  - 💎 **Ultra-Niche Hashtags** (1-2 hashtag)

### 4.3. Kiểm Tra Kết Quả
Mỗi hashtag sẽ hiển thị:
- ✅ Tên hashtag (có thể click để chọn)
- 👁️ Lượt xem (view count)
- 📊 Mức độ cạnh tranh (Cao/Trung Bình/Thấp)
- 🎯 Phạm vi tiếp cận dự kiến
- 🔥 Xác suất viral (0-100%)
- 💡 Lý do AI đề xuất

### 4.4. Copy Hashtags
- Click vào từng hashtag để chọn/bỏ chọn
- Click nút **"Copy Tất Cả"** để copy vào clipboard
- Paste vào TikTok khi đăng video

---

## 📊 So Sánh: Groq vs OpenAI

| Tiêu chí | Groq (Llama 3.1) | OpenAI (GPT-3.5) |
|----------|------------------|------------------|
| **Giá** | ✅ MIỄN PHÍ | ❌ $0.002/request |
| **Tốc độ** | ✅ ~800 tokens/s | ⚠️ ~50 tokens/s |
| **Rate Limit** | ✅ 30 req/min | ❌ 3 req/min (free) |
| **Chất lượng** | ✅ Tương đương | ✅ Tốt |
| **Setup** | ✅ Không cần card | ❌ Cần payment method |
| **Phù hợp** | ✅ Personal projects | ❌ Enterprise |

**Kết luận:** Groq là lựa chọn **TỐT HƠN** cho TrendTag!

---

## 🔍 Cách Hoạt Động của Hệ Thống

### Workflow:
1. **User nhập mô tả** → Frontend gửi POST request
2. **Check rate limit** → 5 lượt/ngày cho free user
3. **Check cache** → Nếu mô tả giống nhau (SHA-256 hash)
4. **Lấy trending hashtags** → Top 50 hashtags từ database
5. **Gọi Groq API** → Llama 3.1 phân tích và đề xuất
6. **Parse JSON response** → Extract hashtag recommendations
7. **Lưu vào database** → Cache kết quả (7 ngày)
8. **Hiển thị cho user** → 3 categories với đầy đủ thông tin

### Fallback Strategy:
- Nếu Groq API fail → Tự động chuyển sang **Rule-Based Generator**
- Rule-Based: Dùng keyword matching + scoring algorithm
- Không cần AI, hoạt động 100% offline
- Chất lượng: Tốt, nhưng không bằng AI

---

## 🐛 Troubleshooting

### Lỗi: "Groq API error (401)"
**Nguyên nhân:** API key sai hoặc không hợp lệ

**Giải pháp:**
1. Kiểm tra API key có đúng format `gsk_...` không
2. Kiểm tra không có khoảng trắng thừa
3. Tạo lại API key mới từ Groq Console

### Lỗi: "Groq API error (429)"
**Nguyên nhân:** Vượt quá 30 requests/minute

**Giải pháp:**
1. Chờ 1 phút rồi thử lại
2. Hệ thống có caching nên hiếm khi xảy ra
3. Nếu cần nhiều hơn → Upgrade Groq plan (vẫn free)

### Lỗi: "Falling back to rule-based generator"
**Nguyên nhân:** Không kết nối được Groq API

**Giải pháp:**
1. Kiểm tra internet connection
2. Kiểm tra API key trong appsettings.json
3. Xem logs để biết chi tiết lỗi
4. Rule-based vẫn hoạt động tốt (không cần lo)

### Kết quả "không liên quan" đến mô tả
**Nguyên nhân:** Database chưa có hashtag liên quan

**Giải pháp:**
1. Chạy Crawler để cập nhật hashtags mới
2. AI sẽ chọn từ top 50 hashtags trong DB
3. Nếu DB ít hashtag → Kết quả sẽ kém chính xác

---

## 📈 Monitoring & Analytics

### Kiểm Tra Logs
Logs sẽ hiển thị:
```
info: HashTag.Services.HashtagGeneratorService[0]
      Calling Groq API at https://api.groq.com/openai/v1/chat/completions

info: HashTag.Services.HashtagGeneratorService[0]
      Groq API call successful
```

### Database Tracking
Mọi generation được lưu trong:
- **HashtagGenerations** - Kết quả AI và cache
- **GenerationHashtagSelections** - Hashtag nào user chọn
- **GenerationRateLimits** - Rate limit tracking

### Queries Hữu Ích:
```sql
-- Xem tất cả generations
SELECT TOP 10 * FROM HashtagGenerations ORDER BY CreatedAt DESC;

-- Xem rate limits
SELECT * FROM GenerationRateLimits WHERE LimitDate >= CAST(GETDATE() AS DATE);

-- Hashtags được chọn nhiều nhất
SELECT h.Tag, COUNT(*) as SelectedCount
FROM GenerationHashtagSelections ghs
JOIN Hashtags h ON ghs.HashtagId = h.HashtagId
GROUP BY h.Tag
ORDER BY SelectedCount DESC;
```

---

## 🎉 Hoàn Tất!

Hashtag Generator đã sẵn sàng với Groq API:
- ✅ Miễn phí hoàn toàn
- ✅ Không cần thẻ tín dụng
- ✅ Tốc độ nhanh gấp 16x OpenAI
- ✅ Rate limit cao (30 req/min)
- ✅ Chất lượng tương đương GPT-3.5
- ✅ Tự động fallback nếu API fail

**Next Steps:**
1. Lấy Groq API key từ https://console.groq.com
2. Paste vào appsettings.json
3. Chạy `dotnet run`
4. Test tại https://localhost:7125/Hashtag/Generator

**Chúc bạn thành công! 🚀**
