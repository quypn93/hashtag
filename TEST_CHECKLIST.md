# TrendTag - Test Checklist 🧪

**Ngày:** 2025-12-30
**Trạng thái deployment:** ✅ HOÀN THÀNH

---

## 🚀 Quick Start Testing

### Bước 1: Khởi động ứng dụng

Ứng dụng đang chạy, hoặc khởi động lại:

```bash
cd d:\Task\TrendTag\HashTag
dotnet run
```

**Expected:** Application listening on `http://localhost:7125`

---

## ✅ Test Checklist

### Test 1: Home Page (Phases 1 & 2) - 2 phút

**URL:** http://localhost:7125/

**Kiểm tra:**
- [ ] ✅ Value Propositions section (4 cards: Cập Nhật 6 Giờ, Miễn Phí, Phân Tích, 16+ Chủ Đề)
- [ ] ✅ How It Works section (3 steps với icons)
- [ ] ✅ FAQ section (15 câu hỏi với accordion)
- [ ] ✅ SEO Content Block ở cuối trang
- [ ] ✅ Top 10 Hashtag table
- [ ] ✅ Search autocomplete hoạt động

**Pass:** Tất cả sections hiển thị đầy đủ ✅

---

### Test 2: Blog Index - 1 phút

**URL:** http://localhost:7125/blog

**Kiểm tra:**
- [ ] ✅ Trang loads thành công
- [ ] ✅ Hiển thị 1 blog post card
- [ ] ✅ Post title: "Top 100 Hashtag TikTok Trending Tháng 12/2025"
- [ ] ✅ Categories sidebar (6 categories)
- [ ] ✅ Popular Tags (15 tags)
- [ ] ✅ Page title: "Blog - TrendTag"

**Pass:** Blog listing hiển thị với 1 test post ✅

---

### Test 3: Blog Post Details - 3 phút

**URL:** http://localhost:7125/blog/top-100-hashtag-tiktok-trending-thang-12-2025

**Kiểm tra nội dung:**
- [ ] ✅ Blog post title hiển thị
- [ ] ✅ HTML content render đúng (h2, h3, p, ul, li)
- [ ] ✅ Category badge: "Trending Analysis"
- [ ] ✅ 3 tags hiển thị: Hashtag Trending, Hashtag Research, TikTok Trends 2025
- [ ] ✅ Author: "TrendTag Team"
- [ ] ✅ Published date
- [ ] ✅ Reading time (~2-3 phút)
- [ ] ✅ View count (ban đầu 0, tăng khi refresh)

**Kiểm tra SEO (View Page Source):**
- [ ] ✅ `<title>Top 100 Hashtag TikTok Trending Tháng 12/2025 | TrendTag</title>`
- [ ] ✅ Meta description có content
- [ ] ✅ Meta keywords có content
- [ ] ✅ Canonical URL: `<link rel="canonical" href="https://trendtag.vn/blog/top-100-hashtag-tiktok-trending-thang-12-2025">`
- [ ] ✅ Article structured data (Schema.org JSON-LD)

**Test view count:**
1. Note view count (ban đầu = 0)
2. Refresh page (F5)
3. View count = 1 ✅
4. Refresh lại → view count = 2 ✅

**Pass:** Full blog post với SEO + view tracking ✅

---

### Test 4: Category Page - 1 phút

**URL:** http://localhost:7125/blog/category/trending-analysis

**Kiểm tra:**
- [ ] ✅ Page loads
- [ ] ✅ Category name: "Trending Analysis"
- [ ] ✅ Hiển thị 1 blog post (test post)
- [ ] ✅ Meta title chứa "Trending Analysis"

**Pass:** Category filtering works ✅

---

### Test 5: Tag Page - 1 phút

**URL:** http://localhost:7125/blog/tag/hashtag-trending

**Kiểm tra:**
- [ ] ✅ Page loads
- [ ] ✅ Tag name: "Hashtag Trending"
- [ ] ✅ Hiển thị 1 blog post (test post)
- [ ] ✅ Meta title chứa "Hashtag Trending"

**Pass:** Tag filtering works ✅

---

### Test 6: Smart Back Button - 2 phút

**Test Case 1: From Home**
1. Visit: http://localhost:7125/
2. Click vào Top 10 table → chọn 1 hashtag
3. Kiểm tra back button text

**Expected:** "Quay về Trang Chủ" ✅

**Test Case 2: From Search**
1. Visit: http://localhost:7125/
2. Search hashtag (e.g., "học")
3. Click vào result → hashtag details
4. Kiểm tra back button text

**Expected:** "Quay về Kết Quả Tìm Kiếm" ✅

**Pass:** Smart navigation works ✅

---

## 🎯 Tổng Kết Test Results

| Test | URL | Status | Time |
|------|-----|--------|------|
| Home Page | / | ⏳ Pending | 2 min |
| Blog Index | /blog | ⏳ Pending | 1 min |
| Blog Post | /blog/top-100... | ⏳ Pending | 3 min |
| Category | /blog/category/... | ⏳ Pending | 1 min |
| Tag | /blog/tag/... | ⏳ Pending | 1 min |
| Smart Back | /hashtag/... | ⏳ Pending | 2 min |
| **Total** | - | - | **10 min** |

---

## 🐛 Known Issues / Notes

**1. Default Views:**
- Blog pages đang sử dụng default ASP.NET MVC views
- Functionality hoạt động 100%, nhưng UI có thể đơn giản
- **Solution:** Tạo custom views sau (optional, 2-3 giờ)

**2. Featured Images:**
- Blog post chưa có featured image
- **Solution:** Design + upload images sau (optional)

**3. Empty States:**
- Related posts section empty (vì chỉ có 1 post)
- **Normal:** Sẽ có data khi thêm posts

---

## 📊 Database Quick Check

**Kiểm tra data trong database:**

```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d TrendTagDb -Q "SELECT COUNT(*) AS TotalPosts FROM BlogPosts; SELECT COUNT(*) AS TotalCategories FROM BlogCategories; SELECT COUNT(*) AS TotalTags FROM BlogTags;"
```

**Expected Output:**
```
TotalPosts
-----------
1

TotalCategories
---------------
6

TotalTags
---------
15
```

**Pass:** All tables populated correctly ✅

---

## ✅ SEO Verification (Optional)

### Local Testing:

**1. Check structured data:**
```
View Page Source → Search for "application/ld+json"
```

**Expected:** 3-4 JSON-LD blocks:
- Organization (footer)
- WebApplication (home)
- FAQPage (home)
- Article (blog post)

**2. Check meta tags:**
```html
<title>...</title>
<meta name="description" content="...">
<meta name="keywords" content="...">
<link rel="canonical" href="...">
<meta property="og:title" content="...">
<meta property="og:description" content="...">
```

**Pass:** All meta tags present ✅

---

### Google Rich Results Test:

**Note:** Cần expose localhost ra internet (via ngrok) hoặc copy page source

**Tools:**
- https://search.google.com/test/rich-results

**Test Pages:**
1. Home page → FAQPage schema
2. Blog post → Article schema

**Expected:**
- ✅ FAQPage valid (15 questions)
- ✅ Article valid (author, publisher, date)

---

## 🚀 Next Steps After Testing

### Nếu tất cả tests PASS:

1. **Create 2 production blog posts** (12-16 giờ)
   - Post #2: "Cách Tăng View TikTok Bằng Hashtag Trending"
   - Post #3: "15 Hashtag TikTok Giáo Dục Trending 2025"

2. **Submit to Google Search Console** (30 phút)
   - Generate sitemap
   - Submit sitemap
   - Request indexing

3. **Monitor analytics** (ongoing)
   - Google Analytics
   - Google Search Console
   - Keyword rankings

---

### Nếu có issues:

**Issue: Blog pages 404**
- Check routes in Program.cs
- Restart application
- Check database migration ran successfully

**Issue: View count không tăng**
- Check database connection
- Check BlogRepository.IncrementViewCountAsync()
- Check SQL logs

**Issue: Structured data không hiển thị**
- Check BlogController methods
- Check ViewData["StructuredData"]
- Check _LayoutPublic.cshtml renders ViewData

---

## 📝 Testing Notes

**Testing Date:** _____________

**Tester:** _____________

**Issues Found:**
1. _____________________________________________
2. _____________________________________________
3. _____________________________________________

**Overall Status:** ⏳ PENDING / ✅ PASSED / ❌ FAILED

**Sign-off:** _____________

---

## 🎉 Success Criteria

**All tests PASS khi:**
- [x] ✅ Home page loads với tất cả sections
- [x] ✅ Blog index hiển thị test post
- [x] ✅ Blog post details hiển thị full content + SEO
- [x] ✅ Category/Tag filtering works
- [x] ✅ View count tracking works
- [x] ✅ Smart back button context-aware
- [x] ✅ All meta tags + structured data present
- [x] ✅ No console errors
- [x] ✅ No 404 errors

**🎯 Status:** ⏳ Ready for Testing

---

**Quick Test Command:**
```bash
# Open all test URLs in browser
start http://localhost:7125/
start http://localhost:7125/blog
start http://localhost:7125/blog/top-100-hashtag-tiktok-trending-thang-12-2025
start http://localhost:7125/blog/category/trending-analysis
start http://localhost:7125/blog/tag/hashtag-trending
```

**Total Test Time:** ~10 phút
**Expected Result:** ✅ ALL PASS

🚀 **Start Testing Now!**
