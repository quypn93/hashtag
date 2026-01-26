-- =====================================================
-- Update Category Slugs to Vietnamese
-- =====================================================

USE [TrendTagDb];
GO

-- Update all categories with Vietnamese slugs AND DisplayNameVi
UPDATE [dbo].[HashtagCategories]
SET Slug = 'phuong-tien-giao-thong', DisplayNameVi = N'Phương Tiện & Giao Thông'
WHERE Name = 'Vehicle & Transportation';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'giao-duc', DisplayNameVi = N'Giáo Dục'
WHERE Name = 'Education';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'cong-nghe-dien-tu', DisplayNameVi = N'Công Nghệ & Điện Tử'
WHERE Name = 'Tech & Electronics';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'lam-dep-cham-soc-ca-nhan', DisplayNameVi = N'Làm Đẹp & Chăm Sóc Cá Nhân'
WHERE Name = 'Beauty & Personal Care';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'thoi-trang-phu-kien', DisplayNameVi = N'Thời Trang & Phụ Kiện'
WHERE Name = 'Apparel & Accessories';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'do-gia-dung', DisplayNameVi = N'Đồ Gia Dụng'
WHERE Name = 'Household Products';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'thu-cung', DisplayNameVi = N'Thú Cưng'
WHERE Name = 'Pets';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'cai-thien-nha-o', DisplayNameVi = N'Cải Thiện Nhà Ở'
WHERE Name = 'Home Improvement';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'tin-tuc-giai-tri', DisplayNameVi = N'Tin Tức & Giải Trí'
WHERE Name = 'News & Entertainment';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'tro-choi', DisplayNameVi = N'Trò Chơi'
WHERE Name = 'Games';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'dich-vu-song', DisplayNameVi = N'Dịch Vụ Sống'
WHERE Name = 'Life Services';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'thuc-pham-do-uong', DisplayNameVi = N'Thực Phẩm & Đồ Uống'
WHERE Name = 'Food & Beverage';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'the-thao-ngoai-troi', DisplayNameVi = N'Thể Thao & Ngoài Trời'
WHERE Name = 'Sports & Outdoor';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'du-lich', DisplayNameVi = N'Du Lịch'
WHERE Name = 'Travel';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'dich-vu-tai-chinh', DisplayNameVi = N'Dịch Vụ Tài Chính'
WHERE Name = 'Financial Services';

UPDATE [dbo].[HashtagCategories]
SET Slug = 'em-be-tre-em-me-bau', DisplayNameVi = N'Em Bé, Trẻ Em & Mẹ Bầu'
WHERE Name = 'Baby, Kids & Maternity';

PRINT 'Updated all category slugs and DisplayNameVi to Vietnamese';
GO

-- Verify the results
SELECT
    Id,
    Name,
    DisplayNameVi,
    Slug,
    IsActive
FROM [dbo].[HashtagCategories]
ORDER BY Name;
GO
