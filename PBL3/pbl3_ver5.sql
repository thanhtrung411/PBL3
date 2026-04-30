SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================
-- DROP TABLES (reverse dependency order)
-- =============================================
IF OBJECT_ID(N'dbo.ChiTietHoaDon', N'U') IS NOT NULL DROP TABLE dbo.ChiTietHoaDon;
IF OBJECT_ID(N'dbo.HoaDon', N'U') IS NOT NULL DROP TABLE dbo.HoaDon;
IF OBJECT_ID(N'dbo.DatPhong', N'U') IS NOT NULL DROP TABLE dbo.DatPhong;
IF OBJECT_ID(N'dbo.BangGiaPhong', N'U') IS NOT NULL DROP TABLE dbo.BangGiaPhong;
IF OBJECT_ID(N'dbo.MaGiamGia', N'U') IS NOT NULL DROP TABLE dbo.MaGiamGia;
IF OBJECT_ID(N'dbo.DichVu', N'U') IS NOT NULL DROP TABLE dbo.DichVu;
IF OBJECT_ID(N'dbo.KhachHang', N'U') IS NOT NULL DROP TABLE dbo.KhachHang;
IF OBJECT_ID(N'dbo.Phong', N'U') IS NOT NULL DROP TABLE dbo.Phong;
IF OBJECT_ID(N'dbo.LoaiPhong', N'U') IS NOT NULL DROP TABLE dbo.LoaiPhong;
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NOT NULL DROP TABLE dbo.TaiKhoan;
IF OBJECT_ID(N'dbo.NhanVien', N'U') IS NOT NULL DROP TABLE dbo.NhanVien;
IF OBJECT_ID(N'dbo.VaiTro', N'U') IS NOT NULL DROP TABLE dbo.VaiTro;
GO

IF OBJECT_ID(N'dbo.Seq_HoaDon', N'SO') IS NOT NULL DROP SEQUENCE dbo.Seq_HoaDon;
IF OBJECT_ID(N'dbo.Seq_ChiTietHoaDon', N'SO') IS NOT NULL DROP SEQUENCE dbo.Seq_ChiTietHoaDon;
IF OBJECT_ID(N'dbo.Seq_DatPhong', N'SO') IS NOT NULL DROP SEQUENCE dbo.Seq_DatPhong;
IF OBJECT_ID(N'dbo.Seq_KhachHang', N'SO') IS NOT NULL DROP SEQUENCE dbo.Seq_KhachHang;
GO

-- =============================================
-- SEQUENCES (safe code generation)
-- =============================================
CREATE SEQUENCE dbo.Seq_KhachHang
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

CREATE SEQUENCE dbo.Seq_DatPhong
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

CREATE SEQUENCE dbo.Seq_ChiTietHoaDon
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

CREATE SEQUENCE dbo.Seq_HoaDon
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

-- =============================================
-- 1. Vai trò
-- =============================================
CREATE TABLE dbo.VaiTro
(
    MaVaiTro    CHAR(10)        NOT NULL,
    TenVaiTro   NVARCHAR(50)    NOT NULL,
    MoTa        NVARCHAR(255)   NULL,

    CONSTRAINT PK_VaiTro PRIMARY KEY (MaVaiTro),
    CONSTRAINT UQ_VaiTro_TenVaiTro UNIQUE (TenVaiTro)
);
GO

-- =============================================
-- 2. Nhân viên
-- =============================================
CREATE TABLE dbo.NhanVien
(
    MaNV            CHAR(10)        NOT NULL,
    HoTen           NVARCHAR(100)   NOT NULL,
    GioiTinh        NVARCHAR(10)    NULL,
    NgaySinh        DATE            NULL,
    SoDienThoai     VARCHAR(15)     NULL,
    Email           VARCHAR(100)    NULL,
    DiaChi          NVARCHAR(255)   NULL,
    ChucVu          NVARCHAR(50)    NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Đang làm',

    CONSTRAINT PK_NhanVien PRIMARY KEY (MaNV),
    CONSTRAINT UQ_NhanVien_SDT UNIQUE (SoDienThoai),
    CONSTRAINT UQ_NhanVien_Email UNIQUE (Email),
    CONSTRAINT CK_NhanVien_GioiTinh CHECK (GioiTinh IN (N'Nam', N'Nữ', N'Khác') OR GioiTinh IS NULL),
    CONSTRAINT CK_NhanVien_TrangThai CHECK (TrangThai IN (N'Đang làm', N'Tạm nghỉ', N'Nghỉ việc'))
);
GO

-- =============================================
-- 3. Tài khoản nhân viên
-- Khách hàng không đăng nhập
-- =============================================
CREATE TABLE dbo.TaiKhoan
(
    MaTK            CHAR(10)        NOT NULL,
    TenDangNhap     VARCHAR(50)     NOT NULL,
    MatKhau         NVARCHAR(255)   NOT NULL,
    MaNV            CHAR(10)        NOT NULL,
    MaVaiTro        CHAR(10)        NOT NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Hoạt động',

    CONSTRAINT PK_TaiKhoan PRIMARY KEY (MaTK),
    CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT UQ_TaiKhoan_MaNV UNIQUE (MaNV),
    CONSTRAINT FK_TaiKhoan_NhanVien FOREIGN KEY (MaNV)
        REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT FK_TaiKhoan_VaiTro FOREIGN KEY (MaVaiTro)
        REFERENCES dbo.VaiTro(MaVaiTro),
    CONSTRAINT CK_TaiKhoan_TrangThai CHECK (TrangThai IN (N'Hoạt động', N'Tạm khóa', N'Ngừng sử dụng'))
);
GO

-- =============================================
-- 4. Loại phòng
-- Không lưu giá cơ bản ở đây
-- =============================================
CREATE TABLE dbo.LoaiPhong
(
    MaLoaiPhong     CHAR(10)        NOT NULL,
    TenLoaiPhong    NVARCHAR(50)    NOT NULL,
    SoNguoiToiDa    INT             NOT NULL,
    MoTa            NVARCHAR(255)   NULL,

    CONSTRAINT PK_LoaiPhong PRIMARY KEY (MaLoaiPhong),
    CONSTRAINT UQ_LoaiPhong_TenLoaiPhong UNIQUE (TenLoaiPhong),
    CONSTRAINT CK_LoaiPhong_SoNguoiToiDa CHECK (SoNguoiToiDa > 0)
);
GO

-- =============================================
-- 5. Phòng
-- =============================================
CREATE TABLE dbo.Phong
(
    MaPhong         CHAR(10)        NOT NULL,
    SoPhong         VARCHAR(10)     NOT NULL,
    MaLoaiPhong     CHAR(10)        NOT NULL,
    Tang            INT             NOT NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Trống',
    GhiChu          NVARCHAR(255)   NULL,

    CONSTRAINT PK_Phong PRIMARY KEY (MaPhong),
    CONSTRAINT UQ_Phong_SoPhong UNIQUE (SoPhong),
    CONSTRAINT FK_Phong_LoaiPhong FOREIGN KEY (MaLoaiPhong)
        REFERENCES dbo.LoaiPhong(MaLoaiPhong),
    CONSTRAINT CK_Phong_Tang CHECK (Tang > 0),
    CONSTRAINT CK_Phong_TrangThai CHECK (TrangThai IN (N'Trống', N'Đã đặt', N'Đang ở', N'Đang dọn', N'Bảo trì', N'Ngưng sử dụng'))
);
GO

-- =============================================
-- 6. Khách hàng
-- =============================================
CREATE TABLE dbo.KhachHang
(
    MaKH            CHAR(10)        NOT NULL,
    HoTen           NVARCHAR(100)   NOT NULL,
    GioiTinh        NVARCHAR(10)    NULL,
    NgaySinh        DATE            NULL,
    CCCD            VARCHAR(20)     NULL,
    SoDienThoai     VARCHAR(15)     NULL,
    Email           VARCHAR(100)    NULL,
    DiaChi          NVARCHAR(255)   NULL,
    QuocTich        NVARCHAR(50)    NULL,

    CONSTRAINT PK_KhachHang PRIMARY KEY (MaKH),
    CONSTRAINT UQ_KhachHang_CCCD UNIQUE (CCCD),
    CONSTRAINT CK_KhachHang_GioiTinh CHECK (GioiTinh IN (N'Nam', N'Nữ', N'Khác') OR GioiTinh IS NULL)
);
GO

-- =============================================
-- 7. Dịch vụ
-- =============================================
CREATE TABLE dbo.DichVu
(
    MaDV            CHAR(10)        NOT NULL,
    TenDV           NVARCHAR(100)   NOT NULL,
    DonGia          DECIMAL(18,2)   NOT NULL,
    DonViTinh       NVARCHAR(30)    NOT NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Hoạt động',
    GhiChu          NVARCHAR(255)   NULL,

    CONSTRAINT PK_DichVu PRIMARY KEY (MaDV),
    CONSTRAINT UQ_DichVu_TenDV UNIQUE (TenDV),
    CONSTRAINT CK_DichVu_DonGia CHECK (DonGia >= 0),
    CONSTRAINT CK_DichVu_TrangThai CHECK (TrangThai IN (N'Hoạt động', N'Tạm ngưng', N'Ngừng cung cấp'))
);
GO

-- =============================================
-- 8. Mã giảm giá
-- =============================================
CREATE TABLE dbo.MaGiamGia
(
    MaGiamGia           CHAR(10)        NOT NULL,
    CodeGiamGia         VARCHAR(30)     NOT NULL,
    TenMaGiamGia        NVARCHAR(100)   NOT NULL,
    LoaiGiamGia         NVARCHAR(20)    NOT NULL,
    GiaTriGiam          DECIMAL(18,2)   NOT NULL,
    HoaDonToiThieu      DECIMAL(18,2)   NOT NULL DEFAULT 0,
    GiamToiDa           DECIMAL(18,2)   NULL,
    PhamViApDung        NVARCHAR(20)    NOT NULL,
    TuNgay              DATE            NOT NULL,
    DenNgay             DATE            NOT NULL,
    SoLuongPhatHanh     INT             NOT NULL DEFAULT 0,
    SoLuongDaDung       INT             NOT NULL DEFAULT 0,
    TrangThai           NVARCHAR(30)    NOT NULL DEFAULT N'Hoạt động',
    MoTa                NVARCHAR(255)   NULL,
    GhiChu              NVARCHAR(255)   NULL,

    CONSTRAINT PK_MaGiamGia PRIMARY KEY (MaGiamGia),
    CONSTRAINT UQ_MaGiamGia_Code UNIQUE (CodeGiamGia),
    CONSTRAINT CK_MaGiamGia_LoaiGiamGia CHECK (LoaiGiamGia IN (N'PHANTRAM', N'TIENMAT')),
    CONSTRAINT CK_MaGiamGia_PhamVi CHECK (PhamViApDung IN (N'PHONG', N'DICHVU')),
    CONSTRAINT CK_MaGiamGia_GiaTri CHECK (GiaTriGiam > 0),
    CONSTRAINT CK_MaGiamGia_HoaDonToiThieu CHECK (HoaDonToiThieu >= 0),
    CONSTRAINT CK_MaGiamGia_GiamToiDa CHECK (GiamToiDa IS NULL OR GiamToiDa >= 0),
    CONSTRAINT CK_MaGiamGia_SoLuong CHECK (SoLuongPhatHanh >= 0 AND SoLuongDaDung >= 0 AND SoLuongDaDung <= SoLuongPhatHanh),
    CONSTRAINT CK_MaGiamGia_ThoiGian CHECK (TuNgay <= DenNgay),
    CONSTRAINT CK_MaGiamGia_TrangThai CHECK (TrangThai IN (N'Hoạt động', N'Tạm khóa', N'Hết hạn', N'Hết lượt'))
);
GO

-- =============================================
-- 9. Bảng giá phòng
-- =============================================
CREATE TABLE dbo.BangGiaPhong
(
    MaBangGia       CHAR(10)        NOT NULL,
    MaLoaiPhong     CHAR(10)        NOT NULL,
    TuNgay          DATE            NOT NULL,
    DenNgay         DATE            NOT NULL,
    ThuApDung       TINYINT         NULL,
    GiaApDung       DECIMAL(18,2)   NOT NULL,
    LoaiGia         NVARCHAR(20)    NOT NULL,
    UuTien          INT             NOT NULL DEFAULT 1,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Hoạt động',
    GhiChu          NVARCHAR(255)   NULL,

    CONSTRAINT PK_BangGiaPhong PRIMARY KEY (MaBangGia),
    CONSTRAINT FK_BangGiaPhong_LoaiPhong FOREIGN KEY (MaLoaiPhong)
        REFERENCES dbo.LoaiPhong(MaLoaiPhong),
    CONSTRAINT CK_BangGiaPhong_ThoiGian CHECK (TuNgay <= DenNgay),
    CONSTRAINT CK_BangGiaPhong_ThuApDung CHECK (ThuApDung IS NULL OR ThuApDung BETWEEN 1 AND 8),
    CONSTRAINT CK_BangGiaPhong_Gia CHECK (GiaApDung >= 0),
    CONSTRAINT CK_BangGiaPhong_LoaiGia CHECK (LoaiGia IN (N'MACDINH', N'DACBIET')),
    CONSTRAINT CK_BangGiaPhong_UuTien CHECK (UuTien > 0),
    CONSTRAINT CK_BangGiaPhong_TrangThai CHECK (TrangThai IN (N'Hoạt động', N'Tạm ngưng', N'Ngừng áp dụng'))
);
GO

CREATE INDEX IX_BangGiaPhong_MaLoaiPhong ON dbo.BangGiaPhong(MaLoaiPhong);
CREATE INDEX IX_BangGiaPhong_TuNgay_DenNgay ON dbo.BangGiaPhong(TuNgay, DenNgay);
GO

-- =============================================
-- 10. Đặt phòng (Booking)
-- Quản lý thông tin đặt phòng, tách khỏi hóa đơn
-- Lưu snapshot thông tin khách tại thời điểm đặt
-- =============================================
CREATE TABLE dbo.DatPhong
(
    MaDatPhong          CHAR(10)        NOT NULL,
    MaKH                CHAR(10)        NOT NULL,
    MaNV                CHAR(10)        NOT NULL,

    -- Snapshot thông tin khách hàng tại thời điểm đặt
    TenKH_Snapshot      NVARCHAR(100)   NOT NULL,
    CCCD_Snapshot        VARCHAR(20)     NULL,
    SDT_Snapshot         VARCHAR(15)     NULL,

    NgayDat             DATETIME        NOT NULL DEFAULT GETDATE(),
    NgayNhanPhong       DATE            NOT NULL,
    NgayTraPhong        DATE            NOT NULL,
    TrangThai           NVARCHAR(30)    NOT NULL DEFAULT N'GIU_CHO',
    GhiChu              NVARCHAR(255)   NULL,

    CONSTRAINT PK_DatPhong PRIMARY KEY (MaDatPhong),
    CONSTRAINT FK_DatPhong_KhachHang FOREIGN KEY (MaKH)
        REFERENCES dbo.KhachHang(MaKH),
    CONSTRAINT FK_DatPhong_NhanVien FOREIGN KEY (MaNV)
        REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT CK_DatPhong_ThoiGian CHECK (NgayNhanPhong < NgayTraPhong),
    CONSTRAINT CK_DatPhong_TrangThai CHECK (TrangThai IN (
        N'GIU_CHO',
        N'DA_DAT_COC',
        N'NHAN_PHONG',
        N'TRA_PHONG',
        N'DA_HUY'
    ))
);
GO

CREATE INDEX IX_DatPhong_MaKH ON dbo.DatPhong(MaKH);
CREATE INDEX IX_DatPhong_MaNV ON dbo.DatPhong(MaNV);
CREATE INDEX IX_DatPhong_NgayDat ON dbo.DatPhong(NgayDat);
CREATE INDEX IX_DatPhong_NgayNhanPhong ON dbo.DatPhong(NgayNhanPhong);
CREATE INDEX IX_DatPhong_TrangThai ON dbo.DatPhong(TrangThai);
GO

-- =============================================
-- 11. Hóa đơn (Invoice)
-- Chỉ quản lý tài chính, liên kết với DatPhong
-- =============================================
CREATE TABLE dbo.HoaDon
(
    MaHoaDon                CHAR(10)        NOT NULL,
    MaDatPhong              CHAR(10)        NOT NULL,
    TongTienPhong           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TongTienDichVu          DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TienDatCoc              DECIMAL(18,2)   NOT NULL DEFAULT 0,
    MaGiamGiaPhong          CHAR(10)        NULL,
    TienGiamGiaPhong        DECIMAL(18,2)   NOT NULL DEFAULT 0,
    TongThanhToan           DECIMAL(18,2)   NOT NULL DEFAULT 0,
    SoTienDaThanhToan       DECIMAL(18,2)   NOT NULL DEFAULT 0,
    NgayThanhToanCuoi       DATETIME        NULL,
    PhuongThucThanhToan     NVARCHAR(30)    NULL,
    TrangThai               NVARCHAR(30)    NOT NULL DEFAULT N'CHUA_THANH_TOAN',
    GhiChu                  NVARCHAR(255)   NULL,

    CONSTRAINT PK_HoaDon PRIMARY KEY (MaHoaDon),
    CONSTRAINT FK_HoaDon_DatPhong FOREIGN KEY (MaDatPhong)
        REFERENCES dbo.DatPhong(MaDatPhong),
    CONSTRAINT UQ_HoaDon_MaDatPhong UNIQUE (MaDatPhong),
    CONSTRAINT FK_HoaDon_MaGiamGiaPhong FOREIGN KEY (MaGiamGiaPhong)
        REFERENCES dbo.MaGiamGia(MaGiamGia),
    CONSTRAINT CK_HoaDon_Tien CHECK (
        TongTienPhong >= 0 AND
        TongTienDichVu >= 0 AND
        TienDatCoc >= 0 AND
        TienGiamGiaPhong >= 0 AND
        TongThanhToan >= 0 AND
        SoTienDaThanhToan >= 0
    ),
    CONSTRAINT CK_HoaDon_PhuongThucThanhToan CHECK (
        PhuongThucThanhToan IN (N'Tiền mặt', N'Chuyển khoản', N'Thẻ', N'QR') OR PhuongThucThanhToan IS NULL
    ),
    CONSTRAINT CK_HoaDon_TrangThai CHECK (
        TrangThai IN (
            N'CHUA_THANH_TOAN',
            N'THANH_TOAN_MOT_PHAN',
            N'DA_THANH_TOAN',
            N'DA_HUY'
        )
    )
);
GO

-- =============================================
-- 12. Chi tiết hóa đơn
-- =============================================
CREATE TABLE dbo.ChiTietHoaDon
(
    MaCTHD         CHAR(10)         NOT NULL,
    MaHoaDon       CHAR(10)         NOT NULL,
    LoaiMuc        NVARCHAR(30)     NOT NULL,
    MaPhong        CHAR(10)         NULL,
    MaDV           CHAR(10)         NULL,
    MaGiamGia      CHAR(10)         NULL,
    NoiDung        NVARCHAR(255)    NOT NULL,
    NgayApDung     DATE             NULL,
    SoNguoi        INT              NOT NULL,
    SoLuong        INT              NOT NULL CONSTRAINT DF_CTHD_SoLuong DEFAULT (1),
    DonGia         DECIMAL(18,2)    NOT NULL CONSTRAINT DF_CTHD_DonGia DEFAULT (0),
    ThanhTien      DECIMAL(18,2)    NOT NULL CONSTRAINT DF_CTHD_ThanhTien DEFAULT (0),
    TrangThai      NVARCHAR(20)     NOT NULL CONSTRAINT DF_CTHD_TrangThai DEFAULT (N'HIEU_LUC'),
    GhiChu         NVARCHAR(500)    NULL,

    CONSTRAINT PK_ChiTietHoaDon PRIMARY KEY (MaCTHD),
    CONSTRAINT FK_CTHD_HoaDon FOREIGN KEY (MaHoaDon)
        REFERENCES dbo.HoaDon(MaHoaDon),
    CONSTRAINT FK_CTHD_Phong FOREIGN KEY (MaPhong)
        REFERENCES dbo.Phong(MaPhong),
    CONSTRAINT FK_CTHD_DichVu FOREIGN KEY (MaDV)
        REFERENCES dbo.DichVu(MaDV),
    CONSTRAINT FK_CTHD_MaGiamGia FOREIGN KEY (MaGiamGia)
        REFERENCES dbo.MaGiamGia(MaGiamGia),
    CONSTRAINT CK_CTHD_LoaiMuc CHECK (LoaiMuc IN (N'PHONG', N'DICHVU', N'DATCOC', N'GIAMGIADICHVU', N'PHUTHU')),
    CONSTRAINT CK_CTHD_TrangThai CHECK (TrangThai IN (N'HIEU_LUC', N'HUY', N'DA_TINH_TIEN')),
    CONSTRAINT CK_CTHD_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CK_CTHD_SoNguoi CHECK (SoNguoi > 0),
    CONSTRAINT CK_CTHD_DonGia CHECK (DonGia >= 0)
);
GO

CREATE INDEX IX_CTHD_MaHoaDon ON dbo.ChiTietHoaDon(MaHoaDon);
CREATE INDEX IX_CTHD_MaPhong ON dbo.ChiTietHoaDon(MaPhong);
CREATE INDEX IX_CTHD_MaDV ON dbo.ChiTietHoaDon(MaDV);
CREATE INDEX IX_CTHD_NgayApDung ON dbo.ChiTietHoaDon(NgayApDung);
GO

-- =============================================
-- Gợi ý nghiệp vụ:
--
-- [Luồng đặt phòng]
-- 1) Khách nhập CCCD → Lookup KhachHang theo CCCD
-- 2) Nếu tìm thấy → auto-fill, cho sửa → UPDATE KhachHang
-- 3) Nếu không tìm thấy → INSERT KhachHang mới
-- 4) Tạo DatPhong (GIU_CHO) + snapshot TenKH, CCCD, SDT
-- 5) Khách đặt cọc → DatPhong: DA_DAT_COC
-- 6) Nhận phòng → DatPhong: NHAN_PHONG → Tạo HoaDon
-- 7) Sử dụng dịch vụ → Thêm ChiTietHoaDon (DICHVU)
-- 8) Trả phòng → DatPhong: TRA_PHONG → Tổng hợp HoaDon
-- 9) Thanh toán → HoaDon: DA_THANH_TOAN
--
-- [Lấy giá phòng]
-- 1) Tìm trong BangGiaPhong theo MaLoaiPhong và ngày cần tính
-- 2) Nếu có giá DACBIET hợp lệ thì lấy giá đó (UuTien cao hơn)
-- 3) Nếu không có thì lấy giá MACDINH
-- =============================================
