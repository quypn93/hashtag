# Deployment Checklist - Stored Procedures Optimization

## Pre-Deployment

### ✅ 1. Backup Database
```sql
-- Tạo backup trước khi deploy
BACKUP DATABASE TrendTag
TO DISK = 'D:\Backups\TrendTag_PreSP_Backup.bak'
WITH FORMAT, INIT,
NAME = 'TrendTag-Pre-StoredProc-Backup';
```

### ✅ 2. Verify Database Connection
```bash
# Test connection string
dotnet run --project HashTag/HashTag.csproj
```

### ✅ 3. Check Current Performance Baseline
```bash
# Record current performance metrics
# - HomePage load time
# - GetTrendingHashtags response time
# - Database query duration
```

## Deployment Steps

### 📋 Step 1: Deploy Stored Procedures (5 minutes)

1. Mở **SQL Server Management Studio** hoặc **Azure Data Studio**
2. Kết nối đến database **TrendTag**
3. Mở file `HashTag/Data/StoredProcedures/DeployAll.sql`
4. Chạy script (Execute hoặc F5)
5. Verify output:
   ```
   ✅ All stored procedures deployed successfully!
   ```

### 📋 Step 2: Test Stored Procedures (2 minutes)

```sql
-- Test 1: GetTrendingHashtags
EXEC sp_GetTrendingHashtags @Limit = 10;
-- Expected: 10 rows returned

-- Test 2: GetActiveCategories
EXEC sp_GetActiveCategories;
-- Expected: 16+ rows returned

-- Test 3: GetRecentBlogPosts
EXEC sp_GetRecentBlogPosts @Count = 5;
-- Expected: 5 blog posts returned
```

### 📋 Step 3: Deploy Application Code (ALREADY DONE ✅)

Code đã được cập nhật:
- ✅ `Services/IStoredProcedureService.cs` - Interface
- ✅ `Services/StoredProcedureService.cs` - ADO.NET implementation
- ✅ `Repositories/HashtagRepository.cs` - Updated to use SP
- ✅ `Repositories/BlogRepository.cs` - Updated to use SP
- ✅ `Program.cs` - Service registration

**Không cần thay đổi gì trong code!**

### 📋 Step 4: Build & Deploy Application (5 minutes)

```bash
# Build application
dotnet build HashTag/HashTag.csproj --configuration Release

# Publish application
dotnet publish HashTag/HashTag.csproj --configuration Release --output ./publish

# Deploy to server (example)
# Copy ./publish/* to production server
```

### 📋 Step 5: Restart Application (1 minute)

```bash
# Development
dotnet run --project HashTag/HashTag.csproj

# Production (IIS)
# - Recycle application pool
# - Or restart IIS

# Production (Linux/Docker)
sudo systemctl restart trendtag
# Or
docker restart trendtag-container
```

## Post-Deployment Verification

### ✅ 1. Check Application Logs

Xem logs để verify stored procedures được gọi:

```bash
# Look for these log entries:
[Debug] Calling stored procedure sp_GetTrendingHashtags via ADO.NET
[Debug] Retrieved 10 trending hashtags via stored procedure
```

### ✅ 2. Performance Testing

```bash
# HomePage load time
curl -w "@curl-format.txt" -o /dev/null -s https://viralhashtag.vn

# Expected improvement:
# Before: ~680ms
# After:  ~85ms (8x faster)
```

### ✅ 3. Functional Testing

- [ ] Trang chủ hiển thị top 10 hashtags ✅
- [ ] Filter theo category hoạt động ✅
- [ ] Blog posts hiển thị đúng ✅
- [ ] Search hashtag vẫn hoạt động ✅

### ✅ 4. Monitor Error Logs

Kiểm tra logs trong 30 phút đầu:

```bash
# Check for errors
tail -f /var/log/trendtag/error.log

# Look for:
# ❌ "Error executing sp_GetTrendingHashtags"
# ❌ "Error calling stored procedure"
```

Nếu có lỗi → Fallback mechanism sẽ tự động dùng EF Core.

## Rollback Plan

### 🚨 Nếu có vấn đề nghiêm trọng

#### Option 1: Disable Stored Procedures (Quick - 2 minutes)

```csharp
// In Program.cs, comment out SP service registration:
// builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();

// Restart application
// → App sẽ tự động fallback về EF Core
```

#### Option 2: Restore Database (Slower - 10 minutes)

```sql
-- Restore từ backup
USE master;
GO

ALTER DATABASE TrendTag SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

RESTORE DATABASE TrendTag
FROM DISK = 'D:\Backups\TrendTag_PreSP_Backup.bak'
WITH REPLACE;
GO

ALTER DATABASE TrendTag SET MULTI_USER;
GO
```

#### Option 3: Drop Stored Procedures (Medium - 5 minutes)

```sql
-- Drop all stored procedures
DROP PROCEDURE IF EXISTS sp_GetTrendingHashtags;
DROP PROCEDURE IF EXISTS sp_GetActiveCategories;
DROP PROCEDURE IF EXISTS sp_GetRecentBlogPosts;

-- Application will auto-fallback to EF Core
```

## Success Criteria

Deployment được coi là thành công khi:

- ✅ Stored procedures được tạo thành công
- ✅ Application khởi động không lỗi
- ✅ HomePage load time giảm > 5x
- ✅ Logs hiển thị "Calling stored procedure..."
- ✅ Không có errors trong 1 giờ đầu
- ✅ Functional tests pass 100%

## Troubleshooting Guide

### Issue 1: "Could not find stored procedure"

**Symptoms**: Application logs show error "Could not find stored procedure 'sp_GetTrendingHashtags'"

**Solution**:
```sql
-- Verify SP exists
SELECT name FROM sys.procedures
WHERE name LIKE 'sp_Get%';

-- If not exists, re-run DeployAll.sql
```

### Issue 2: "Login failed for user"

**Symptoms**: ADO.NET cannot connect to database

**Solution**:
```bash
# Check connection string in appsettings.json
# Ensure SQL Server Authentication is enabled
# Verify firewall rules allow connection
```

### Issue 3: "Execution Timeout Expired"

**Symptoms**: Queries timeout after 30 seconds

**Solution**:
```csharp
// In StoredProcedureService.cs, increase timeout
command.CommandTimeout = 60; // 60 seconds
```

### Issue 4: Performance not improved

**Symptoms**: Page still loads slowly after deployment

**Solution**:
```sql
-- Check if SPs are being called
-- Enable SQL Server Profiler and verify SP execution

-- Check database indexes
SELECT * FROM sys.indexes
WHERE object_id = OBJECT_ID('HashtagHistories');

-- May need to add indexes:
CREATE INDEX IX_HashtagHistories_HashtagId_CollectedDate
ON HashtagHistories(HashtagId, CollectedDate DESC);
```

## Monitoring (Post-Deploy)

### Week 1: Daily monitoring

- [ ] Check error logs daily
- [ ] Monitor response times
- [ ] Review SQL Server performance metrics
- [ ] Check memory usage

### Week 2-4: Weekly monitoring

- [ ] Weekly performance report
- [ ] Check for slow queries
- [ ] Review logs for SP errors

### Ongoing: Set up alerts

```csharp
// Application Insights (Azure)
// - Alert if response time > 500ms
// - Alert if error rate > 1%
// - Alert if SP failure rate > 5%
```

## Contact & Support

- **Dev Team**: dev@trendtag.vn
- **DBA**: dba@trendtag.vn
- **On-Call**: +84-xxx-xxx-xxx

## Approval Sign-off

- [ ] Developer: ________________ Date: ______
- [ ] Tech Lead: ________________ Date: ______
- [ ] DBA: ______________________ Date: ______
- [ ] Product Owner: ____________ Date: ______

---

**Note**: Deployment này có **LOW RISK** vì:
1. Có fallback mechanism tự động
2. Không thay đổi database schema
3. Không ảnh hưởng đến existing functionality
4. Chỉ tối ưu performance, không thay đổi logic

🚀 **Ready to deploy!**
