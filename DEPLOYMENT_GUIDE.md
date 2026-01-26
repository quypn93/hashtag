# Hướng Dẫn Triển Khai - Xóa Hashtag Trùng Lặp TagDisplay

## 🎯 Mục Đích
Xóa các hashtag có **khoảng trắng ở đầu Tag** nhưng **CHỈ KHI có hashtag khác cùng TagDisplay** (để loại bỏ trùng lặp mà user thấy).

**Lưu ý quan trọng:**
- ✅ XÓA: Hashtag có khoảng trắng + có hashtag khác cùng TagDisplay
- ✅ GIỮ LẠI: Hashtag có khoảng trắng nhưng TagDisplay là duy nhất

---

## ⚠️ Lưu Ý Quan Trọng

**Tại sao phải XÓA thay vì UPDATE?**
- Database có unique index `IX_Hashtags_Tag`
- Không thể UPDATE `" doituyenvietnam"` → `"doituyenvietnam"` nếu `"doituyenvietnam"` đã tồn tại
- Lỗi: `Cannot insert duplicate key row in object 'dbo.Hashtags' with unique index 'IX_Hashtags_Tag'`
- Giải pháp: XÓA các hashtag có khoảng trắng, giữ lại phiên bản sạch

---

## 📋 Các Bước Triển Khai

### Bước 1: Preview Dữ Liệu (An Toàn)

Mở file: `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql`

**Chạy Step 1** để xem hashtag nào sẽ bị xóa:
```sql
-- Dòng 7-31
SELECT
    Id, Tag, LTRIM(Tag) as CleanTag,
    LatestViewCount, LatestPostCount,
    CASE
        WHEN Tag <> LTRIM(Tag) THEN '❌ WILL DELETE'
        ELSE '✅ KEEP'
    END as Action
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag)
ORDER BY LTRIM(Tag), LatestViewCount DESC;
```

**Kết quả mong đợi:**
```
Id    | Tag                | TagDisplay         | Action
------|--------------------|--------------------|---------------------------------------------
7313  | " doituyenvietnam" | "#doituyenvietnam" | ❌ WILL DELETE (duplicate TagDisplay)
5746  | "doituyenvietnam"  | "#doituyenvietnam" | ✅ KEEP
9999  | " uniquetag"       | "#uniquetag"       | ✅ KEEP (unique TagDisplay, no duplicate)
```

---

### Bước 2: Đếm Số Lượng (Kiểm Tra)

**Chạy Step 2**:
```sql
-- Dòng 38-67
SELECT
    'Hashtags with LEADING whitespace + duplicate TagDisplay (will DELETE)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag)
  AND EXISTS (
      SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay AND h2.Id <> h1.Id
  )

UNION ALL

SELECT
    'Hashtags with LEADING whitespace but UNIQUE TagDisplay (will KEEP)' as Description,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag)
  AND NOT EXISTS (
      SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay AND h2.Id <> h1.Id
  );
```

**Kết quả mẫu:**
```
Description                                                      | Count
-----------------------------------------------------------------|------
Hashtags with LEADING whitespace + duplicate TagDisplay (DELETE) | 30
Hashtags with LEADING whitespace but UNIQUE TagDisplay (KEEP)    | 20
Total hashtags with LEADING whitespace                           | 50
```

---

### Bước 3: Kiểm Tra Trùng Lặp TagDisplay

**Chạy Step 3** để xem TagDisplay nào bị trùng:
```sql
-- Dòng 69-83
SELECT
    TagDisplay,
    COUNT(*) as TotalCount,
    STRING_AGG(
        CAST(Id AS VARCHAR) +
        ' (Tag:"' + Tag + '", Views:' + CAST(ISNULL(LatestViewCount, 0) AS VARCHAR) +
        CASE WHEN Tag <> LTRIM(Tag) THEN ', HAS_WHITESPACE' ELSE '' END + ')',
        '; '
    ) as Details
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY TagDisplay
HAVING COUNT(*) > 1
ORDER BY MAX(LatestViewCount) DESC;
```

**Kết quả mẫu:**
```
TagDisplay        | TotalCount | Details
------------------|------------|------------------------------------------------
#doituyenvietnam  | 2          | 5746 (Tag:"doituyenvietnam", Views:1000);
                  |            | 7313 (Tag:" doituyenvietnam", Views:1311, HAS_WHITESPACE)
```

Hashtag có `HAS_WHITESPACE` sẽ bị xóa.

---

### Bước 4: Thực Hiện XÓA (CHỈ XÓA TRÙNG LẶP)

**Chỉ xóa hashtag có khoảng trắng KHI có duplicate TagDisplay:**

1. **Uncomment Step 4** (dòng 92-121):
   - Bỏ dấu `/*` ở dòng 92
   - Bỏ dấu `*/` ở dòng 121

2. **Đổi ROLLBACK thành COMMIT**:
   - Dòng 119: Bỏ comment `COMMIT TRANSACTION;`
   - Dòng 120: Comment hoặc xóa `ROLLBACK TRANSACTION;`

**Code sau khi sửa:**
```sql
BEGIN TRANSACTION;

-- Delete hashtags with leading whitespace that have duplicate TagDisplay
DELETE h1
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag) -- Has leading whitespace
  AND EXISTS (
      -- Check if another hashtag with same TagDisplay exists
      SELECT 1
      FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay
        AND h2.Id <> h1.Id
  );

DECLARE @DuplicatesDeleted INT = @@ROWCOUNT;
PRINT 'Deleted ' + CAST(@DuplicatesDeleted AS VARCHAR) + ' hashtags with duplicate TagDisplay';

-- Show what was kept (hashtags with leading whitespace but unique TagDisplay)
SELECT
    Id, Tag, TagDisplay, LatestViewCount,
    'KEPT (has leading whitespace but unique TagDisplay)' as Status
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION; -- ✅ Commented out
```

3. **Chạy script** → Xóa hashtag trùng lặp, giữ lại hashtag duy nhất

---

### Bước 5: Xác Minh Kết Quả

**Chạy Step 5** để kiểm tra:
```sql
-- Dòng 127-156

-- Kiểm tra hashtag có khoảng trắng còn lại (có thể có nếu unique TagDisplay)
SELECT
    'Hashtags with LEADING whitespace remaining (kept because unique TagDisplay)' as Status,
    COUNT(*) as Count
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag);

-- Xem hashtag nào còn khoảng trắng (vì unique TagDisplay)
SELECT TOP 10
    Id, Tag, TagDisplay, LatestViewCount,
    'KEPT (unique TagDisplay)' as Reason
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag <> LTRIM(Tag)
ORDER BY LatestViewCount DESC;

-- QUAN TRỌNG: Kiểm tra không còn duplicate TagDisplay
SELECT
    TagDisplay,
    COUNT(*) as DuplicateCount,
    STRING_AGG(CAST(Id AS VARCHAR) + ' (Tag:"' + Tag + '")', ', ') as Details
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY TagDisplay
HAVING COUNT(*) > 1
ORDER BY DuplicateCount DESC;
-- Kết quả mong đợi: 0 rows (không còn duplicate TagDisplay)
```

**Kết quả mong đợi:**
```
-- Query 1: Có thể có hashtag với khoảng trắng (nếu unique)
Count: 20 (ví dụ: 20 hashtag có khoảng trắng nhưng TagDisplay duy nhất)

-- Query 2: Hiển thị những hashtag được giữ lại
Id   | Tag          | TagDisplay    | Reason
-----|--------------|---------------|------------------------
9999 | " uniquetag" | "#uniquetag"  | KEPT (unique TagDisplay)

-- Query 3: QUAN TRỌNG - Không còn duplicate TagDisplay
0 rows (✅ Không còn trùng lặp mà user thấy)
```

---

### Bước 6: Restart Application

Sau khi xóa xong database, cần restart app để áp dụng code mới:

1. **Stop ứng dụng hiện tại** (Ctrl+C trong terminal đang chạy)

2. **Restart:**
```bash
dotnet run --project d:\Task\TrendTag\HashTag
```

---

### Bước 7: Test Kết Quả

1. **Test Search:**
   - Truy cập: `/Hashtag/Search?q=doituyenvietnam`
   - Kết quả mong đợi: Chỉ hiển thị **1 hashtag** (không còn trùng lặp)
   - Hashtag hiển thị phải có **ViewCount cao nhất**

2. **Kiểm tra database:**
```sql
-- Tìm hashtag "doituyenvietnam"
SELECT Id, Tag, TagDisplay, LatestViewCount, LatestPostCount
FROM [TrendTagDb].[dbo].[Hashtags]
WHERE Tag LIKE '%doituyenvietnam%'
ORDER BY LatestViewCount DESC;
```

**Kết quả mong đợi:**
```
Id   | Tag               | TagDisplay         | LatestViewCount
-----|-------------------|--------------------|----------------
5746 | doituyenvietnam   | #doituyenvietnam   | 1000
```

Chỉ còn **1 dòng**, phiên bản có khoảng trắng (Id: 7313) đã bị xóa.

---

## ✅ Checklist Hoàn Thành

- [ ] **Bước 1:** Preview dữ liệu (Step 1)
- [ ] **Bước 2:** Đếm số lượng (Step 2)
- [ ] **Bước 3:** Kiểm tra trùng lặp (Step 3)
- [ ] **Bước 4:** Thực hiện XÓA (Step 4 hoặc Step 5)
  - [ ] Option A: Xóa trùng lặp trước (Step 4)
  - [ ] Option B: Xóa tất cả luôn (Step 5)
- [ ] **Bước 5:** Xác minh kết quả (Step 6)
- [ ] **Bước 6:** Restart application
- [ ] **Bước 7:** Test search và database

---

## 📊 Kết Quả Cuối Cùng

### Trước Khi Fix:
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (Id: 5746, Tag: "doituyenvietnam", Views: 1000)
  Result 2: #doituyenvietnam (Id: 7313, Tag: " doituyenvietnam", Views: 1311) ← Trùng lặp!

Database:
  Id: 5746, Tag = "doituyenvietnam",  TagDisplay = "#doituyenvietnam"  ✅ Clean
  Id: 7313, Tag = " doituyenvietnam", TagDisplay = "#doituyenvietnam"  ❌ Trùng TagDisplay
  Id: 9999, Tag = " uniquetag",       TagDisplay = "#uniquetag"        ⚠️ Có whitespace nhưng unique
```

### Sau Khi Fix:
```
Search "doituyenvietnam" →
  Result 1: #doituyenvietnam (Id: 5746, Views: 1000) ✅ Duy nhất

Search "uniquetag" →
  Result 1: #uniquetag (Id: 9999, Views: 500) ✅ Vẫn hiển thị (unique TagDisplay)

Database:
  Id: 5746, Tag = "doituyenvietnam", TagDisplay = "#doituyenvietnam" ✅ Clean, kept
  Id: 9999, Tag = " uniquetag",      TagDisplay = "#uniquetag"       ✅ Has whitespace, but kept (unique)
  (Id: 7313 đã bị xóa vì trùng TagDisplay với Id: 5746)
```

---

## 🔧 Code Đã Sửa

### 1. Ngăn Chặn Khoảng Trắng Trong Tương Lai

**File:** [HashTag/Repositories/HashtagRepository.cs:202](HashTag/Repositories/HashtagRepository.cs#L202)

```csharp
// ✅ FIX: Trim leading whitespace khi tạo hashtag mới
var normalizedTag = tag.TrimStart('#', ' ').ToLower();
```

### 2. Ưu Tiên ViewCount/PostCount Khi Search

**File:** [HashTag/Repositories/HashtagRepository.cs:116-119](HashTag/Repositories/HashtagRepository.cs#L116-L119)

```csharp
// ✅ PRIORITY: Order by ViewCount DESC, PostCount DESC
.OrderByDescending(h => h.LatestViewCount ?? 0)
.ThenByDescending(h => h.LatestPostCount ?? 0)
.ThenByDescending(h => h.TotalAppearances)
.ThenBy(h => h.FirstSeen)
```

---

## 🎯 Tóm Tắt

| Vấn Đề | Giải Pháp | File |
|--------|-----------|------|
| ❌ Duplicate TagDisplay (user thấy trùng) | XÓA hashtag có whitespace CHỈ KHI trùng TagDisplay | `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql` |
| ✅ Hashtag unique (dù có whitespace) | GIỮ LẠI nếu TagDisplay là duy nhất | `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql` |
| ❌ Search hiển thị trùng lặp | Ưu tiên ViewCount/PostCount | `HashtagRepository.cs:116-119` |
| ❌ Tạo hashtag mới vẫn có whitespace | Trim khi tạo mới | `HashtagRepository.cs:202` |

---

**Trạng Thái:** ✅ Sẵn sàng triển khai
**Thời Gian:** ~10 phút (chạy SQL + restart app)
**Ưu Tiên:** Cao (ảnh hưởng trải nghiệm người dùng)
