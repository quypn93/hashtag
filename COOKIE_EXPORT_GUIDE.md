# Hướng Dẫn Export TikTok Cookies

## Phương Án 1: Sử dụng Chrome DevTools Console (Nhanh Nhất)

### Bước 1: Mở Chrome DevTools
1. Truy cập https://ads.tiktok.com/business/creativecenter/
2. Đăng nhập vào tài khoản của bạn
3. Nhấn **F12** để mở DevTools
4. Chuyển sang tab **Console**

### Bước 2: Chạy Script Export
1. Mở file `export-cookies.js` trong project
2. Copy toàn bộ nội dung
3. Paste vào Console và nhấn Enter
4. Script sẽ tự động:
   - Collect tất cả cookies
   - Convert sang JSON format
   - Copy vào clipboard
   - In ra console

### Bước 3: Paste vào Configuration
1. Mở `HashTag/appsettings.json`
2. Tìm `"TikTokCookies": ""`
3. Paste JSON đã copy vào giữa 2 dấu `""`
4. **Quan trọng**: Escape dấu `"` bằng cách thay thế:
   - Tìm: `"`
   - Thay: `\"`

   **Hoặc** dùng find/replace trong VS Code:
   - Find: `"`
   - Replace: `\"`

### Ví Dụ:

**JSON gốc:**
```json
[{"name":"msToken","value":"abc123","domain":".tiktok.com"}]
```

**Sau khi escape (paste vào appsettings.json):**
```json
{
  "CrawlerSettings": {
    "TikTokCookies": "[{\"name\":\"msToken\",\"value\":\"abc123\",\"domain\":\".tiktok.com\"}]"
  }
}
```

---

## Phương Án 2: Sử dụng Chrome Extension

### Bước 1: Cài Extension
1. Mở Chrome Web Store
2. Tìm một trong các extension sau:
   - **EditThisCookie** (khuyến nghị)
   - **Cookie-Editor**
   - **Export Cookie**

### Bước 2: Export Cookies
1. Truy cập https://ads.tiktok.com và đăng nhập
2. Click icon extension trên toolbar
3. Click nút **Export** hoặc **Copy All**
4. Chọn format **JSON**
5. Copy kết quả

### Bước 3: Paste vào Configuration
- Giống như Phương Án 1, Bước 3

---

## Phương Án 3: Manual Copy từ DevTools (Từ Screenshot)

Từ screenshot bạn đã cung cấp, tôi thấy các cookies quan trọng:

### Cookies Cần Thiết:
```
msToken
sessionid (nếu có)
sid_guard_ads
sid_ucp_sso_v1_ads
sid_ucp_v1_ads
ssid_ucp_sso_v1_ads
ssid_ucp_v1_ads
passport_auth_status_ads
csrf_session_id
tiktok_webapp_theme
```

### Cách Copy Manual:
1. Trong tab **Application** → **Cookies** → **https://ads.tiktok.com**
2. Click vào từng cookie
3. Copy **Name** và **Value**
4. Tạo JSON theo format:

```json
[
  {
    "name": "msToken",
    "value": "fk4keVsI_FDRzMk_YjOMGmIFtinhOyIrCDkSQ03rs_4MEz5Xlwo9X35KrQhPXQy3wl3gFNVRNy_u14Jqfk5_JKv3pfnDCKlVe7KCgPzN6rBX_OuSC1I8SnDxM9xS7O-7R-cK",
    "domain": ".tiktok.com",
    "path": "/",
    "expires": 1767139200,
    "httpOnly": false,
    "secure": true,
    "sameSite": "None"
  },
  {
    "name": "sid_guard_ads",
    "value": "bb126dbb10bee2f11b1bf6de002111bcd5%7C1766642615%7C7259200%7CSat%2C+22-Feb-2025+16%3A30%3A15+GMT",
    "domain": ".tiktok.com",
    "path": "/",
    "expires": 1767139200,
    "httpOnly": true,
    "secure": true,
    "sameSite": "None"
  }
]
```

---

## Test Configuration

Sau khi thêm cookies vào `appsettings.json`, test bằng cách:

1. **Chạy Crawler:**
   ```bash
   # Từ Admin page hoặc
   dotnet run
   ```

2. **Kiểm Tra Logs:**
   ```
   TikTok: Added 15 cookies for authenticated session
   TikTok: Starting infinite scroll to load hashtags...
   TikTok: Initial hashtags in DOM: 3
   TikTok: Scroll 1: 3 -> 12 hashtags (9 new)
   TikTok: Scroll 2: 12 -> 24 hashtags (12 new)
   ```

3. **Nếu thấy lỗi authentication:**
   - Cookies đã hết hạn → Export lại
   - Format JSON sai → Kiểm tra escape characters
   - Thiếu cookies quan trọng → Export đầy đủ hơn

---

## Troubleshooting

### Lỗi: JSON parsing error
**Nguyên nhân:** Chưa escape dấu `"`

**Giải pháp:**
```bash
# Dùng VS Code find/replace (Ctrl+H)
Find: "
Replace: \"
```

### Lỗi: Still only getting 3 hashtags
**Nguyên nhân:** Cookies không hợp lệ hoặc chưa đăng nhập

**Giải pháp:**
1. Đảm bảo đã đăng nhập vào TikTok Creative Center
2. Refresh trang và export cookies lại
3. Kiểm tra logs xem có message "Added X cookies"

### Lỗi: Cookies expired
**Nguyên nhân:** Cookies TikTok thường hết hạn sau 1-3 tháng

**Giải pháp:**
- Export cookies mới định kỳ
- Hoặc tự động refresh bằng cách login lại

---

## Security Notes

⚠️ **QUAN TRỌNG:**
- **KHÔNG** commit file `appsettings.json` chứa cookies lên Git public
- Thêm vào `.gitignore`:
  ```
  appsettings.json
  appsettings.Development.json
  ```
- Sử dụng `appsettings.Development.json` cho local development
- Cookies là thông tin nhạy cảm, giống như password

---

## Tóm Tắt

✅ **Khuyến nghị:** Dùng **Phương Án 1** (DevTools Console + script)
- Nhanh nhất
- Tự động format đúng
- Tự động copy vào clipboard

⏱️ **Thời gian:** ~2 phút
🔒 **Bảo mật:** Nhớ không share cookies với người khác
