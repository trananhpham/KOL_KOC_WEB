-- Script tạo dữ liệu mẫu cho Idol (KOL/KOC)
-- Lưu ý: Chạy script này trong SQL Server Management Studio hoặc công cụ quản lý DB của bạn.

DECLARE @RoleId UNIQUEIDENTIFIER;
SELECT TOP 1 @RoleId = Id FROM Roles WHERE Code = 'KOL';

-- Nếu chưa có Role KOL, tạo mới
IF @RoleId IS NULL
BEGIN
    SET @RoleId = NEWID();
    INSERT INTO Roles (Id, Code, [Name]) VALUES (@RoleId, 'KOL', 'KOL');
END

-- Thêm 3 Idol mẫu
DECLARE @U1 UNIQUEIDENTIFIER = NEWID();
DECLARE @U2 UNIQUEIDENTIFIER = NEWID();
DECLARE @U3 UNIQUEIDENTIFIER = NEWID();

-- 1. Thêm Users
INSERT INTO Users (Id, Email, PasswordHash, FullName, [Status], CreatedAt) VALUES 
(@U1, 'ngoctrinh@test.com', '$2a$11$qR7X.N0oP.L8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8', N'Ngọc Trinh', 'active', GETDATE()),
(@U2, 'sontung@test.com', '$2a$11$qR7X.N0oP.L8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8', N'Sơn Tùng MTP', 'active', GETDATE()),
(@U3, 'denvau@test.com', '$2a$11$qR7X.N0oP.L8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8X8', N'Đen Vâu', 'active', GETDATE());

-- 2. Gán Role
INSERT INTO UserRoles (UserId, RoleId) VALUES (@U1, @RoleId), (@U2, @RoleId), (@U3, @RoleId);

-- 3. Thêm KolProfiles
INSERT INTO KolProfiles (UserId, InfluencerType, Bio, Gender, LocationCity, MinBudget, RatingAvg, RatingCount, IsVerified, CreatedAt, UpdatedAt) VALUES 
(@U1, 'Mega', N'Nữ hoàng nội y, chuyên mảng thời trang và làm đẹp.', 'Female', N'TP. Hồ Chí Minh', 50000000, 4.8, 150, 1, GETDATE(), GETDATE()),
(@U2, 'Mega', N'Ca sĩ hàng đầu Việt Nam, phong cách thời trang độc đáo.', 'Male', N'Thái Bình', 200000000, 4.9, 500, 1, GETDATE(), GETDATE()),
(@U3, 'Macro', N'Rapper tử tế, phù hợp với các chiến dịch cộng đồng, trải nghiệm.', 'Male', N'Quảng Ninh', 80000000, 4.7, 200, 1, GETDATE(), GETDATE());

-- 4. Thêm RateCards
DECLARE @RC1 UNIQUEIDENTIFIER = NEWID();
DECLARE @RC2 UNIQUEIDENTIFIER = NEWID();
DECLARE @RC3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO RateCards (Id, KolUserId, Title, Currency, IsActive, CreatedAt, UpdatedAt) VALUES 
(@RC1, @U1, N'Bảng giá 2024', 'VND', 1, GETDATE(), GETDATE()),
(@RC2, @U2, N'Booking Event/Social', 'VND', 1, GETDATE(), GETDATE()),
(@RC3, @U3, N'Chiến dịch Marketing', 'VND', 1, GETDATE(), GETDATE());

-- 5. Thêm RateCardItems (Giá dịch vụ)
INSERT INTO RateCardItems (Id, RateCardId, ServiceType, Platform, UnitPrice, Unit, CreatedAt, UpdatedAt) VALUES 
(NEWID(), @RC1, 'Post', 'Facebook', 20000000, N'Bài viết', GETDATE(), GETDATE()),
(NEWID(), @RC1, 'Video', 'TikTok', 35000000, N'Video', GETDATE(), GETDATE()),
(NEWID(), @RC2, 'Performance', 'Event', 500000000, N'Buổi diễn', GETDATE(), GETDATE()),
(NEWID(), @RC3, 'Promotion', 'YouTube', 120000000, N'Video', GETDATE(), GETDATE());
