# Cải Tiến Trang Chủ - Home Page Improvements

## 📋 Tổng Quan

Đã thực hiện các cải tiến về UI/UX và SEO cho trang chủ của TrendTag.

---

## ✅ Các Cải Tiến Đã Thực Hiện

### 1. **Đổi Label Dropdown: "Tất Cả Ngành" → "Tất Cả Chủ Đề"**

**Vấn đề:** Label "Tất Cả Ngành" nghe cứng nhắc, không thân thiện với người dùng.

**Giải pháp:** Đổi thành "Tất Cả Chủ Đề" - nghe tự nhiên và dễ hiểu hơn.

**File thay đổi:**
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L98) - Line 98

**Trước:**
```cshtml
<option value="">Tất Cả Ngành</option>
```

**Sau:**
```cshtml
<option value="">Tất Cả Chủ Đề</option>
```

---

### 2. **Đổi Tên Column: "Ngành" → "Chủ Đề"**

**Vấn đề:** Nhất quán với label dropdown.

**Giải pháp:** Đổi tên column header từ "Ngành" thành "Chủ Đề".

**File thay đổi:**
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L118) - Line 118

---

### 3. **Đổi Tên Column: "Độ Khó" → "Mức Độ Cạnh Tranh"**

**Vấn đề:**
- "Độ Khó" với giá trị "Dễ", "Trung Bình", "Khó" khó hiểu
- Không rõ "khó" về mặt nào (khó tìm? khó sử dụng?)

**Giải pháp:**
- Đổi thành "Mức Độ Cạnh Tranh" - rõ ràng hơn
- Đổi giá trị: "Dễ" → "Thấp", "Trung Bình" → "Trung Bình", "Khó" → "Cao", "Rất Khó" → "Rất Cao"

**File thay đổi:**
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L121) - Line 121 (header)
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L158-L165) - Lines 158-165 (mapping)

**Trước:**
```cshtml
<th class="text-center">Độ Khó</th>

var difficultyVi = hashtag.DifficultyLevel switch
{
    "Easy" => "Dễ",
    "Medium" => "Trung Bình",
    "Hard" => "Khó",
    "Very Hard" => "Rất Khó",
    _ => hashtag.DifficultyLevel
};
```

**Sau:**
```cshtml
<th class="text-center">Mức Độ Cạnh Tranh</th>

var difficultyVi = hashtag.DifficultyLevel switch
{
    "Easy" => "Thấp",
    "Medium" => "Trung Bình",
    "Hard" => "Cao",
    "Very Hard" => "Rất Cao",
    _ => hashtag.DifficultyLevel
};
```

---

### 4. **Thêm Column "Số Bài Đăng" (PostCount)**

**Vấn đề:** Thiếu thông tin về số lượng bài đăng sử dụng hashtag.

**Giải pháp:** Thêm column "Số Bài Đăng" hiển thị `LatestPostCount` với format B/M/K.

**File thay đổi:**
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L120) - Line 120 (header)
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L139-L147) - Lines 139-147 (formatting logic)
- [HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml#L192-L194) - Lines 192-194 (display)

**Code mới:**
```cshtml
<!-- Header -->
<th class="text-center">Số Bài Đăng</th>

<!-- Formatting logic -->
var postCountFormatted = hashtag.LatestPostCount.HasValue && hashtag.LatestPostCount > 0
    ? (hashtag.LatestPostCount.Value >= 1_000_000_000
        ? $"{hashtag.LatestPostCount.Value / 1_000_000_000.0:F1}B"
        : hashtag.LatestPostCount.Value >= 1_000_000
            ? $"{hashtag.LatestPostCount.Value / 1_000_000.0:F1}M"
            : hashtag.LatestPostCount.Value >= 1000
                ? $"{hashtag.LatestPostCount.Value / 1000.0:F1}K"
                : hashtag.LatestPostCount.Value.ToString("N0"))
    : "-";

<!-- Display -->
<td class="text-center">
    <strong class="text-success">@postCountFormatted</strong>
</td>
```

**Kết quả:** Bảng hiện có 7 columns thay vì 6:
1. # (Rank number)
2. Hashtag (Tag + appearances)
3. Chủ Đề (Category)
4. Lượt Xem (ViewCount)
5. **Số Bài Đăng** (PostCount) - MỚI
6. Mức Độ Cạnh Tranh (Difficulty)
7. Hạng (Best Rank)

---

### 5. **SEO-Friendly Category URLs**

**Vấn đề:**
- URL không thân thiện: `/?categoryId=38`
- Không tốt cho SEO
- Khó nhớ và chia sẻ

**Giải pháp:**
- Thêm Slug column vào `HashtagCategories` table
- Tạo route mới: `/chu-de/{slug}`
- Example: `/chu-de/vehicle-transportation` thay vì `/?categoryId=38`

#### 5.1. Database Changes

**File tạo mới:**
- [ADD_SLUG_COLUMN.sql](ADD_SLUG_COLUMN.sql) - Migration script

**Cấu trúc:**
```sql
ALTER TABLE [dbo].[HashtagCategories]
ADD Slug NVARCHAR(200) NULL;

UPDATE [dbo].[HashtagCategories]
SET Slug = LOWER(REPLACE(Name, ' ', '-'))
WHERE Slug IS NULL;
```

**Chạy script:**
```bash
sqlcmd -S localhost -d TrendTagDb -i ADD_SLUG_COLUMN.sql
```

#### 5.2. Model Changes

**File thay đổi:**
- [HashTag/Models/HashtagCategory.cs](HashTag/Models/HashtagCategory.cs#L26-L29) - Lines 26-29

**Code mới:**
```csharp
/// <summary>
/// URL-friendly slug for SEO (e.g., "vehicle-transportation")
/// </summary>
public string? Slug { get; set; }
```

#### 5.3. ViewModel Changes

**File thay đổi:**
- [HashTag/ViewModels/HashtagDashboardViewModel.cs](HashTag/ViewModels/HashtagDashboardViewModel.cs#L47) - Line 47

**Code mới:**
```csharp
public class CategoryOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? DisplayNameVi { get; set; }
    public string? Slug { get; set; }  // NEW
}
```

#### 5.4. Controller Changes

**File thay đổi:**
- [HashTag/Controllers/HomeController.cs](HashTag/Controllers/HomeController.cs#L50) - Line 50 (mapping)
- [HashTag/Controllers/HomeController.cs](HashTag/Controllers/HomeController.cs#L64-L88) - Lines 64-88 (new action)

**Code mới:**
```csharp
// Include Slug in CategoryOption mapping
Categories = categories.Select(c => new CategoryOption
{
    Id = c.Id,
    Name = c.Name,
    DisplayNameVi = c.DisplayNameVi,
    Slug = c.Slug  // NEW
}).ToList(),

// New SEO-friendly category action
/// <summary>
/// SEO-friendly category page (e.g., /chu-de/vehicle-transportation)
/// </summary>
public async Task<IActionResult> Category(string slug)
{
    try
    {
        // Get category by slug
        var categories = await _repository.GetActiveCategoriesAsync();
        var category = categories.FirstOrDefault(c => c.Slug == slug);

        if (category == null)
        {
            return NotFound();
        }

        // Redirect to Index with categoryId
        return RedirectToAction(nameof(Index), new { categoryId = category.Id });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading category page for slug '{Slug}': {Message}", slug, ex.Message);
        return RedirectToAction(nameof(Index));
    }
}
```

#### 5.5. Routing Changes

**File thay đổi:**
- [HashTag/Program.cs](HashTag/Program.cs#L117-L121) - Lines 117-121

**Code mới:**
```csharp
// SEO-friendly category route (must be BEFORE default route)
app.MapControllerRoute(
    name: "category",
    pattern: "chu-de/{slug}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

#### 5.6. URL Examples

| Before (Old) | After (New - SEO Friendly) |
|-------------|---------------------------|
| `/?categoryId=1` | `/chu-de/fashion` |
| `/?categoryId=2` | `/chu-de/tech` |
| `/?categoryId=38` | `/chu-de/vehicle-&-transportation` |
| `/?categoryId=10` | `/chu-de/beauty` |

**Lợi ích:**
- ✅ Dễ đọc và nhớ
- ✅ Tốt cho SEO (search engines index tốt hơn)
- ✅ Dễ chia sẻ
- ✅ Có thể dịch sang tiếng Việt: `/chu-de/thoi-trang`

---

## 📊 Tổng Kết Thay Đổi

### Files Modified (8 files):

1. **[HashTag/Views/Home/Index.cshtml](HashTag/Views/Home/Index.cshtml)**
   - Đổi "Tất Cả Ngành" → "Tất Cả Chủ Đề"
   - Đổi "Ngành" → "Chủ Đề"
   - Đổi "Độ Khó" → "Mức Độ Cạnh Tranh"
   - Thêm column "Số Bài Đăng"
   - Thêm formatting logic cho PostCount

2. **[HashTag/Models/HashtagCategory.cs](HashTag/Models/HashtagCategory.cs)**
   - Thêm property `Slug`

3. **[HashTag/ViewModels/HashtagDashboardViewModel.cs](HashTag/ViewModels/HashtagDashboardViewModel.cs)**
   - Thêm property `Slug` vào `CategoryOption`

4. **[HashTag/Controllers/HomeController.cs](HashTag/Controllers/HomeController.cs)**
   - Include `Slug` trong mapping
   - Thêm action `Category(string slug)`

5. **[HashTag/Program.cs](HashTag/Program.cs)**
   - Thêm SEO-friendly route `/chu-de/{slug}`

### Files Created (2 files):

6. **[ADD_SLUG_COLUMN.sql](ADD_SLUG_COLUMN.sql)** - Database migration script

7. **[HOME_PAGE_IMPROVEMENTS.md](HOME_PAGE_IMPROVEMENTS.md)** - Tài liệu này

---

## 🚀 Hướng Dẫn Triển Khai

### Bước 1: Chạy Database Migration

```bash
sqlcmd -S localhost -d TrendTagDb -i ADD_SLUG_COLUMN.sql
```

Hoặc chạy từng bước trong SQL Server Management Studio.

### Bước 2: Restart Application

Vì app đang chạy và lock file, cần restart:

**Option A: Qua Visual Studio / VS Code**
- Stop debugging (Shift + F5)
- Start lại (F5)

**Option B: Qua Command Line**
- Kill process: `taskkill /F /IM HashTag.exe`
- Hoặc tìm và kill process ID 155348
- Build lại: `dotnet build`
- Run: `dotnet run`

### Bước 3: Verify Changes

1. **Kiểm tra trang chủ:**
   - Mở `http://localhost:7125`
   - Verify dropdown hiển thị "Tất Cả Chủ Đề"
   - Verify bảng có 7 columns (bao gồm "Số Bài Đăng")
   - Verify column "Mức Độ Cạnh Tranh" với giá trị "Thấp", "Cao", etc.

2. **Kiểm tra SEO URLs:**
   - Click vào category trong dropdown
   - Verify URL changed to `/chu-de/{slug}` format
   - Example: `http://localhost:7125/chu-de/vehicle-&-transportation`

3. **Kiểm tra database:**
   ```sql
   SELECT Id, Name, DisplayNameVi, Slug FROM HashtagCategories;
   ```
   Verify tất cả categories có Slug value.

---

## 🎯 Kết Quả Mong Đợi

### Before:
```
URL: http://localhost:7125/?categoryId=38
Dropdown: "Tất Cả Ngành"
Columns: #, Hashtag, Ngành, Lượt Xem, Độ Khó, Hạng (6 columns)
Độ Khó values: Dễ, Trung Bình, Khó, Rất Khó
```

### After:
```
URL: http://localhost:7125/chu-de/vehicle-transportation
Dropdown: "Tất Cả Chủ Đề"
Columns: #, Hashtag, Chủ Đề, Lượt Xem, Số Bài Đăng, Mức Độ Cạnh Tranh, Hạng (7 columns)
Mức Độ Cạnh Tranh values: Thấp, Trung Bình, Cao, Rất Cao
```

---

## 📝 Notes

### Lưu Ý Quan Trọng:

1. **Database Migration:**
   - Script `ADD_SLUG_COLUMN.sql` an toàn để chạy nhiều lần (có check `IF NOT EXISTS`)
   - Nếu Slug đã tồn tại, script sẽ skip

2. **URL Encoding:**
   - Slug có thể chứa ký tự đặc biệt như `&`
   - Browser sẽ tự động encode: `vehicle-&-transportation` → `vehicle-%26-transportation`
   - ASP.NET Core tự động decode

3. **Backward Compatibility:**
   - Old URLs vẫn hoạt động: `/?categoryId=38` vẫn work
   - New URLs SEO-friendly: `/chu-de/vehicle-transportation`

4. **PostCount vs ViewCount:**
   - **PostCount:** Số lượng bài đăng sử dụng hashtag
   - **ViewCount:** Tổng số lượt xem của hashtag
   - Cả hai đều quan trọng cho phân tích trending

---

## ✅ Checklist Hoàn Thành

- [x] Đổi "Tất Cả Ngành" → "Tất Cả Chủ Đề"
- [x] Đổi "Ngành" → "Chủ Đề" (column header)
- [x] Đổi "Độ Khó" → "Mức Độ Cạnh Tranh"
- [x] Đổi giá trị: "Dễ" → "Thấp", "Khó" → "Cao", etc.
- [x] Thêm column "Số Bài Đăng" (PostCount)
- [x] Thêm formatting logic cho PostCount (B/M/K)
- [x] Thêm Slug column vào database model
- [x] Tạo migration script `ADD_SLUG_COLUMN.sql`
- [x] Thêm SEO route `/chu-de/{slug}`
- [x] Thêm `Category(slug)` action trong HomeController
- [x] Tạo tài liệu `HOME_PAGE_IMPROVEMENTS.md`

---

## 🔄 Next Steps (Optional)

Nếu muốn tiếp tục cải thiện:

1. **Tạo Sitemap XML:**
   - Include các URL `/chu-de/{slug}`
   - Submit lên Google Search Console

2. **Add Meta Tags:**
   - Dynamic title/description cho mỗi category page
   - Example: `<title>Top Hashtag {CategoryName} | TrendTag</title>`

3. **Breadcrumbs:**
   - Trang Chủ > Chủ Đề > {CategoryName}

4. **Update Slugs to Vietnamese:**
   - `vehicle-&-transportation` → `phuong-tien-giao-thong`
   - Tốt hơn cho SEO Vietnam market

---

**Trạng thái:** ✅ Hoàn thành
**Thời gian:** ~30 phút
**Ưu tiên:** Trung bình (cải thiện UX/SEO)
