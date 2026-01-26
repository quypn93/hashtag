# Hướng Dẫn Lấy Cookie TikTok Cho Crawler

Để crawler TikTok hoạt động, bạn cần cung cấp cookie từ trình duyệt đã đăng nhập TikTok.

---

## 🎯 Tại Sao Cần Cookie?

TikTok Creative Center yêu cầu đăng nhập để xem dữ liệu trending. Cookie giúp:
- Bypass login requirement
- Tránh bị block bởi anti-bot
- Truy cập đầy đủ dữ liệu trending

---

## 📋 Các Bước Lấy Cookie

### Bước 1: Đăng Nhập TikTok Creative Center

1. Mở trình duyệt Chrome/Edge
2. Truy cập: https://ads.tiktok.com/business/creativecenter/hashtag
3. Đăng nhập bằng tài khoản TikTok của bạn
4. Đợi trang load xong và hiển thị danh sách hashtag

### Bước 2: Mở Browser Console

- **Windows/Linux:** Nhấn `F12` hoặc `Ctrl + Shift + I`
- **Mac:** Nhấn `Cmd + Option + I`

### Bước 3: Chạy Script Copy Cookie

Copy đoạn script sau và paste vào Console, sau đó nhấn Enter:

```javascript
// TikTok Cookie Extractor for TrendTag Crawler (Playwright Format)
(function() {
    console.clear();
    console.log('%c🍪 TikTok Cookie Extractor for Playwright', 'font-size: 20px; font-weight: bold; color: #fe2c55;');
    console.log('%c━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━', 'color: #fe2c55;');

    // Get all cookies
    const cookies = document.cookie;

    if (!cookies) {
        console.log('%c❌ Không tìm thấy cookie nào!', 'color: #dc3545; font-weight: bold;');
        console.log('%cVui lòng đăng nhập TikTok Creative Center trước.', 'color: #6c757d;');
        return;
    }

    // Parse cookies into Playwright format
    const cookieArray = cookies.split(';').map(c => {
        const [name, ...valueParts] = c.trim().split('=');
        return {
            name: name.trim(),
            value: valueParts.join('=').trim(),
            domain: '.tiktok.com',
            path: '/',
            expires: Math.floor(Date.now() / 1000) + (365 * 24 * 60 * 60), // 1 year from now
            httpOnly: false,
            secure: true,
            sameSite: 'None'
        };
    });

    // Important TikTok cookies
    const importantCookies = [
        'sessionid', 'sessionid_ss', 'sid_guard', 'sid_tt',
        'uid_tt', 'uid_tt_ss', 'store-idc', 'store-country-code',
        'tt_csrf_token', 'tt_chain_token', 'msToken',
        's_v_web_id', 'passport_csrf_token', 'tt_ticket_guard_client_data'
    ];

    console.log('%c📊 Tổng số cookies:', 'font-weight: bold; color: #495057;');
    console.log(`   Tất cả: ${cookieArray.length}`);
    console.log(`   Quan trọng: ${cookieArray.filter(c => importantCookies.includes(c.name)).length}`);
    console.log('');

    // Format for appsettings.json (escaped JSON string)
    const jsonString = JSON.stringify(cookieArray);
    const escapedJsonString = jsonString
        .replace(/\\/g, '\\\\')  // Escape backslashes first
        .replace(/"/g, '\\"');   // Escape quotes

    console.log('%c✅ Cookie JSON (for appsettings.json):', 'font-weight: bold; color: #28a745; font-size: 14px;');
    console.log('%c━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━', 'color: #28a745;');
    console.log('%cPaste this into appsettings.json → "TikTokCookies":', 'color: #6c757d; font-style: italic;');
    console.log('');
    console.log(`"TikTokCookies": "${escapedJsonString}"`);
    console.log('');
    console.log('%c━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━', 'color: #28a745;');

    // Copy to clipboard
    const finalString = `"TikTokCookies": "${escapedJsonString}"`;
    navigator.clipboard.writeText(finalString).then(() => {
        console.log('%c✨ ĐÃ COPY VÀO CLIPBOARD!', 'font-size: 16px; font-weight: bold; color: #28a745; background: #d4edda; padding: 10px;');
        console.log('%cBây giờ paste vào appsettings.json (thay thế dòng "TikTokCookies" cũ)', 'color: #6c757d; font-style: italic;');
    }).catch(() => {
        console.log('%c⚠️ Không thể tự động copy. Vui lòng copy thủ công từ console.', 'color: #ffc107; font-weight: bold;');
    });

    console.log('');
    console.log('%c🔍 Chi tiết cookies quan trọng:', 'font-weight: bold; color: #007bff;');
    importantCookies.forEach(name => {
        const cookie = cookieArray.find(c => c.name === name);
        if (cookie) {
            console.log(`   ✓ ${name}: ${cookie.value.substring(0, 30)}...`);
        } else {
            console.log(`   ✗ ${name}: KHÔNG TÌM THẤY`);
        }
    });

    console.log('');
    console.log('%c⏰ Cookie Expiry:', 'font-weight: bold; color: #dc3545;');
    console.log('   ⚠️ Cookie TikTok thường hết hạn sau 7-30 ngày');
    console.log('   ⚠️ Nếu crawler lỗi 401/403, cần lấy cookie mới');
    console.log('');
    console.log('%c📋 Preview (first 3 cookies):', 'font-weight: bold; color: #495057;');
    console.log(JSON.stringify(cookieArray.slice(0, 3), null, 2));
    console.log('');
    console.log('%c━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━', 'color: #fe2c55;');
})();
```

### Bước 4: Cookie Đã Được Copy

Nếu thành công, bạn sẽ thấy:
```
✨ ĐÃ COPY VÀO CLIPBOARD!
Bây giờ paste vào appsettings.json (thay thế dòng "TikTokCookies" cũ)
```

---

## 🔧 Cấu Hình appsettings.json

### Bước 1: Mở File appsettings.json

```bash
d:\Task\TrendTag\HashTag\appsettings.json
```

### Bước 2: Thêm/Update Cookie

Tìm dòng `"TikTokCookies"` và thay thế bằng output từ script:

**TRƯỚC:**
```json
{
  "TikTokCookies": "[{\"name\":\"old_cookie\",\"value\":\"...\"}]"
}
```

**SAU (paste output từ script):**
```json
{
  "TikTokCookies": "[{\"name\":\"_ttp\",\"value\":\"2dxRzoDC5z1UNydFqBYi69CXWHd\",\"domain\":\".tiktok.com\",\"path\":\"/\",\"expires\":1799391667,\"httpOnly\":false,\"secure\":true,\"sameSite\":\"None\"},{\"name\":\"msToken\",\"value\":\"...\"}]"
}
```

**LƯU Ý QUAN TRỌNG:**
- ⚠️ Phải paste **TOÀN BỘ** dòng từ script (bao gồm `"TikTokCookies": "..."`)
- ⚠️ Giữ nguyên format escaped JSON (`\"` thay vì `"`)
- ⚠️ Đảm bảo JSON hợp lệ (dùng JSON validator nếu cần)
- ⚠️ Thay thế dòng cũ hoàn toàn, không append

### Bước 3: Restart Application

```bash
# Stop app (Ctrl+C nếu đang chạy)
# Sau đó start lại
dotnet run
```

---

## 🔄 Khi Nào Cần Update Cookie?

Cookie cần được update khi:

### ❌ Dấu Hiệu Cookie Hết Hạn:
- Crawler trả về lỗi `401 Unauthorized`
- Crawler trả về lỗi `403 Forbidden`
- CrawlLogs hiển thị error: "Login required" hoặc "Invalid session"
- TikTok trả về empty data

### ✅ Tần Suất Khuyến Nghị:
- **Mỗi 7 ngày:** Update cookie định kỳ
- **Khi đổi IP:** Update cookie nếu đổi VPN/proxy
- **Sau khi đăng xuất TikTok:** Cookie cũ sẽ invalid

---

## 🛡️ Bảo Mật Cookie

### ⚠️ LƯU Ý QUAN TRỌNG:

1. **KHÔNG chia sẻ cookie** với người khác
2. **KHÔNG commit cookie** lên Git/GitHub
   ```bash
   # Thêm vào .gitignore:
   appsettings.json
   appsettings.*.json
   ```
3. **SỬ DỤNG appsettings.Development.json** cho local testing
4. **SỬ DỤNG Environment Variables** cho production:
   ```bash
   export TikTok__Cookie="your_cookie_here"
   ```

### 🔒 Rủi Ro:
- Cookie bị lộ → Người khác có thể truy cập tài khoản TikTok của bạn
- Cookie có thể chứa session ID, authentication tokens
- TikTok có thể khóa tài khoản nếu phát hiện crawling bất thường

---

## 🐛 Troubleshooting

### Lỗi: "Cookie không hợp lệ"
**Nguyên nhân:** Cookie đã hết hạn hoặc format sai
**Giải pháp:**
1. Đăng xuất TikTok
2. Đăng nhập lại
3. Lấy cookie mới theo hướng dẫn

### Lỗi: "Cannot read cookie"
**Nguyên nhân:** Chưa đăng nhập TikTok Creative Center
**Giải pháp:**
1. Truy cập https://ads.tiktok.com/business/creativecenter/hashtag
2. Đăng nhập
3. Chạy lại script

### Lỗi: "Clipboard copy failed"
**Nguyên nhân:** Browser không cho phép clipboard access
**Giải pháp:**
1. Copy thủ công cookie string từ console
2. Hoặc cho phép clipboard trong browser settings

### Crawler vẫn lỗi sau khi update cookie
**Kiểm tra:**
1. Cookie có đầy đủ không? (cần có `msToken`, `s_v_web_id`, `tt_ticket_guard_client_data`, etc.)
2. Format JSON có đúng không? (phải là escaped JSON string)
3. Restart app sau khi update appsettings.json
4. Kiểm tra CrawlLogs để xem error message cụ thể
5. Thử crawl lại từ TikTok Creative Center trong browser xem còn hoạt động không

### Lỗi: "JSON parsing error" khi restart app
**Nguyên nhân:** Format JSON không hợp lệ
**Giải pháp:**
1. Kiểm tra escape characters (`\"` chứ không phải `"`)
2. Đảm bảo paste đúng toàn bộ dòng từ script
3. Dùng JSON validator online để kiểm tra
4. Xóa dấu phẩy thừa ở cuối dòng (nếu có)

---

## 📞 Hỗ Trợ

Nếu gặp vấn đề:
1. Kiểm tra CrawlLogs: `/Admin/CrawlLogs`
2. Xem error message chi tiết
3. Thử lại với cookie mới
4. Contact: viralhashtagvn@gmail.com

---

**Lưu ý:** Cookie là dữ liệu nhạy cảm. Bảo vệ cookie của bạn như mật khẩu!

🔐 **An toàn - Bảo mật - Minh bạch**
