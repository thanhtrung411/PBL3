USE [PBL3_Ver5];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Phong_MaLoaiPhong_TrangThai' AND object_id = OBJECT_ID(N'dbo.Phong'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Phong_MaLoaiPhong_TrangThai]
    ON [dbo].[Phong] ([MaLoaiPhong], [TrangThai]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CTHD_LoaiMuc_TrangThai_MaLoaiPhong_MaHoaDon' AND object_id = OBJECT_ID(N'dbo.ChiTietHoaDon'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CTHD_LoaiMuc_TrangThai_MaLoaiPhong_MaHoaDon]
    ON [dbo].[ChiTietHoaDon] ([LoaiMuc], [TrangThai], [MaLoaiPhong], [MaHoaDon])
    INCLUDE ([MaPhong], [NoiDung], [SoLuong], [SoNguoi], [ThanhTien]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CTHD_LoaiMuc_TrangThai_MaPhong_MaHoaDon' AND object_id = OBJECT_ID(N'dbo.ChiTietHoaDon'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CTHD_LoaiMuc_TrangThai_MaPhong_MaHoaDon]
    ON [dbo].[ChiTietHoaDon] ([LoaiMuc], [TrangThai], [MaPhong], [MaHoaDon])
    INCLUDE ([MaLoaiPhong], [NoiDung], [SoLuong], [SoNguoi], [ThanhTien]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HoaDon_TrangThai_PhuongThuc_MaDatPhong' AND object_id = OBJECT_ID(N'dbo.HoaDon'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HoaDon_TrangThai_PhuongThuc_MaDatPhong]
    ON [dbo].[HoaDon] ([TrangThai], [PhuongThucThanhToan], [MaDatPhong])
    INCLUDE ([MaHoaDon], [TongTienPhong], [TongThanhToan], [SoTienDaThanhToan]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HoaDon_MaGiamGiaPhong_TrangThai' AND object_id = OBJECT_ID(N'dbo.HoaDon'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HoaDon_MaGiamGiaPhong_TrangThai]
    ON [dbo].[HoaDon] ([MaGiamGiaPhong], [TrangThai])
    INCLUDE ([TienGiamGiaPhong]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatPhong_TrangThai_NgayDat' AND object_id = OBJECT_ID(N'dbo.DatPhong'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DatPhong_TrangThai_NgayDat]
    ON [dbo].[DatPhong] ([TrangThai], [NgayDat])
    INCLUDE ([MaDatPhong], [NgayNhanPhong], [NgayTraPhong], [MaKH], [MaNV]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatPhong_TrangThai_NgayNhan_NgayTra' AND object_id = OBJECT_ID(N'dbo.DatPhong'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DatPhong_TrangThai_NgayNhan_NgayTra]
    ON [dbo].[DatPhong] ([TrangThai], [NgayNhanPhong], [NgayTraPhong])
    INCLUDE ([MaDatPhong], [NgayDat], [MaKH], [MaNV]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LinkAnh_DoiTuong_TrangThai_DaiDien' AND object_id = OBJECT_ID(N'dbo.LinkAnh'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LinkAnh_DoiTuong_TrangThai_DaiDien]
    ON [dbo].[LinkAnh] ([DoiTuong], [TrangThai], [LaAnhDaiDien], [ThuTu])
    INCLUDE ([UrlAnh]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MaGiamGia_ActiveLookup' AND object_id = OBJECT_ID(N'dbo.MaGiamGia'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MaGiamGia_ActiveLookup]
    ON [dbo].[MaGiamGia] ([TuNgay], [DenNgay], [HoaDonToiThieu], [TrangThai])
    INCLUDE ([MaGiamGia], [CodeGiamGia], [TenMaGiamGia], [LoaiGiamGia], [GiaTriGiam], [GiamToiDa], [SoLuongPhatHanh]);
END;
GO
