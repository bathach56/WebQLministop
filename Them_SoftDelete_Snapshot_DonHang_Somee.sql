-- Them cot an mem danh muc va snapshot san pham cho chi tiet don hang.
-- Chay file nay tren database Somee truoc/sau khi deploy code moi.
-- Script co IF COL_LENGTH nen co the chay lai an toan.

IF COL_LENGTH('dbo.DanhMucs', 'KichHoat') IS NULL
BEGIN
    ALTER TABLE dbo.DanhMucs
    ADD KichHoat bit NOT NULL
        CONSTRAINT DF_DanhMucs_KichHoat DEFAULT (1) WITH VALUES;
END;

IF COL_LENGTH('dbo.ChiTietDonHangs', 'MaSanPham') IS NULL
BEGIN
    ALTER TABLE dbo.ChiTietDonHangs
    ADD MaSanPham nvarchar(50) NULL;
END;

IF COL_LENGTH('dbo.ChiTietDonHangs', 'TenSanPham') IS NULL
BEGIN
    ALTER TABLE dbo.ChiTietDonHangs
    ADD TenSanPham nvarchar(150) NULL;
END;

IF COL_LENGTH('dbo.ChiTietDonHangs', 'DonViSanPham') IS NULL
BEGIN
    ALTER TABLE dbo.ChiTietDonHangs
    ADD DonViSanPham nvarchar(30) NULL;
END;

EXEC sp_executesql N'
UPDATE c
SET
    MaSanPham = COALESCE(NULLIF(c.MaSanPham, N''''), s.Ma),
    TenSanPham = COALESCE(NULLIF(c.TenSanPham, N''''), s.Ten),
    DonViSanPham = COALESCE(NULLIF(c.DonViSanPham, N''''), s.DonVi)
FROM dbo.ChiTietDonHangs c
INNER JOIN dbo.SanPhams s ON s.Id = c.SanPhamId
WHERE c.MaSanPham IS NULL
   OR c.TenSanPham IS NULL
   OR c.DonViSanPham IS NULL;
';
