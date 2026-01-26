# Changelog - TrendTag

Tất cả các thay đổi quan trọng của dự án TrendTag sẽ được ghi lại trong file này.

## [Unreleased] - 2026-01-12

### Added
- ✨ **Trang Privacy Policy** (`/chinh-sach-bao-mat`)
  - Chính sách bảo mật đầy đủ với URL thân thiện tiếng Việt
  - SEO meta tags hoàn chỉnh
  - Nội dung bằng tiếng Việt, rõ ràng và minh bạch
  - Giải thích về cookies, session và bảo mật

- ✨ **Trang Terms of Service** (`/dieu-khoan-su-dung`)
  - Điều khoản sử dụng chi tiết
  - URL SEO-friendly
  - Quy định rõ ràng về sử dụng dịch vụ, giới hạn trách nhiệm, hành vi bị cấm

- 🍪 **Cookie Debug Script**
  - Hiển thị thông tin cookie minh bạch trên browser console
  - Phân loại tự động các loại cookie (Session, Consent, Preference)
  - Thông báo bảo mật rõ ràng cho người dùng
  - Styled output với emoji và màu sắc

- 📱 **Mobile Responsive - Homepage**
  - Search button tối ưu cho mobile (icon-only)
  - 3 feature cards có thể click với links:
    - "Trending Real-time" → `/Analytics/Growth`
    - "Đa Nguồn Dữ Liệu" → Ẩn trên mobile
    - "Gợi Ý Thông Minh" → `/Hashtag/GenerateV2`
  - Card "Đa Nguồn Dữ Liệu" ẩn trên mobile để tiết kiệm không gian
  - Hover effects cho cards trên desktop

### Changed
- 🔧 **Footer Links**
  - Cập nhật link Privacy Policy: `/chinh-sach-bao-mat`
  - Cập nhật link Terms of Service: `/dieu-khoan-su-dung`
  - Thay thế link placeholder bằng URL thực tế

- 🎨 **Homepage Feature Cards**
  - Thêm hover effect (lift + shadow)
  - Thêm call-to-action text và icon
  - Compact hơn trên mobile (smaller icons, fonts, padding)

### Fixed
- 🐛 **Mobile Search Layout**
  - Sửa lỗi search button overlap với input
  - Button giờ chỉ hiển thị icon trên mobile
  - Giảm chiều cao từ 70px → 55px

## [Previous Updates]

### 2026-01-10
- 🔒 **Data Protection Keys Persistence**
  - Lưu trữ keys vào folder `DataProtectionKeys`
  - Sửa lỗi session cookie decryption sau khi restart app

### 2026-01-09
- 📈 **Hashtag Detail Pages SEO**
  - Thêm Usage Tips, Statistics Insights, FAQ sections
  - Không dùng AI để tiết kiệm chi phí
  - Dynamic content dựa trên database

- 📝 **Crawl Error Logging Enhancement**
  - Track errors chi tiết với error type classification
  - Bulk insert optimization (N+1 → 8 queries)
  - Performance cải thiện 8-10x

### 2026-01-08
- 🌍 **Growth Tracker Page SEO**
  - Vietnamese dropdown cho categories
  - SEO meta tags đầy đủ
  - Feature boxes giải thích tính năng

---

## Quy Ước Commit Messages

- ✨ `feat:` - Tính năng mới
- 🐛 `fix:` - Sửa bug
- 📝 `docs:` - Cập nhật documentation
- 🎨 `style:` - Thay đổi UI/CSS
- 🔧 `chore:` - Maintenance tasks
- ♻️ `refactor:` - Refactor code
- ⚡ `perf:` - Performance improvements
- 🔒 `security:` - Security fixes

## Contributors

- **Claude Sonnet 4.5** - AI Assistant
- **Developer** - Project Owner

---

**Note:** File này được cập nhật thường xuyên. Kiểm tra lại trước khi deploy để đảm bảo không bỏ sót thay đổi nào.
