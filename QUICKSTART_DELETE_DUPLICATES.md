# Quick Start - Xóa Hashtag Trùng TagDisplay

## 🎯 Mục Đích
Xóa hashtag có khoảng trắng **CHỈ KHI** có hashtag khác cùng `TagDisplay`.

**Ví dụ:**
```
BEFORE:
  Id: 5746, Tag: "doituyenvietnam",  TagDisplay: "#doituyenvietnam" ✅ Keep
  Id: 7313, Tag: " doituyenvietnam", TagDisplay: "#doituyenvietnam" ❌ Delete (duplicate TagDisplay)
  Id: 9999, Tag: " uniquetag",       TagDisplay: "#uniquetag"       ✅ Keep (unique TagDisplay)

AFTER:
  Id: 5746, Tag: "doituyenvietnam",  TagDisplay: "#doituyenvietnam" ✅ Kept
  Id: 9999, Tag: " uniquetag",       TagDisplay: "#uniquetag"       ✅ Kept (unique)
  (Id: 7313 deleted)
```

---

## 📝 Các Bước Nhanh

### 1. Preview (An Toàn)
Mở file: `CLEANUP_UPDATE_TRIM_LEADING_WHITESPACE.sql`

Chạy **Step 1-3** để xem:
- Hashtag nào sẽ XÓA vs GIỮ LẠI
- Đếm số lượng
- Xem duplicate TagDisplay

### 2. Execute DELETE
**Uncomment Step 4** (dòng 92-121):
- Bỏ `/*` và `*/`
- Đổi `ROLLBACK TRANSACTION;` thành `COMMIT TRANSACTION;`

**Chạy:**
```sql
BEGIN TRANSACTION;

DELETE h1
FROM [TrendTagDb].[dbo].[Hashtags] h1
WHERE h1.Tag <> LTRIM(h1.Tag) -- Has leading whitespace
  AND EXISTS (
      SELECT 1 FROM [TrendTagDb].[dbo].[Hashtags] h2
      WHERE h2.TagDisplay = h1.TagDisplay AND h2.Id <> h1.Id
  );

PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' hashtags';

COMMIT TRANSACTION;
```

### 3. Verify
Chạy **Step 5**:
```sql
-- Should return 0 rows (no duplicate TagDisplay)
SELECT TagDisplay, COUNT(*) as DuplicateCount
FROM [TrendTagDb].[dbo].[Hashtags]
GROUP BY TagDisplay
HAVING COUNT(*) > 1;
```

### 4. Restart App
```bash
dotnet run --project d:\Task\TrendTag\HashTag
```

### 5. Test Search
- `/Hashtag/Search?q=doituyenvietnam` → Chỉ 1 result
- Không còn duplicate TagDisplay

---

## ✅ Success Criteria

- ✅ Không còn duplicate `TagDisplay` (query trả về 0 rows)
- ✅ Search chỉ hiển thị 1 kết quả cho mỗi hashtag
- ✅ Hashtag unique (dù có whitespace) vẫn được giữ lại

---

**File chi tiết:** [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)
