using Microsoft.EntityFrameworkCore;

namespace PBL3.Models
{
    public partial class PBL3Context : DbContext
    {
        public PBL3Context()
        {
        }

        public PBL3Context(DbContextOptions<PBL3Context> options)
            : base(options)
        {
        }

        public virtual DbSet<BangGiaPhong> BangGiaPhongs { get; set; }

        public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

        public virtual DbSet<DatPhong> DatPhongs { get; set; }

        public virtual DbSet<DichVu> DichVus { get; set; }

        public virtual DbSet<HoaDon> HoaDons { get; set; }

        public virtual DbSet<KhachHang> KhachHangs { get; set; }

        public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

        public virtual DbSet<LinkAnh> LinkAnhs { get; set; }

        public virtual DbSet<MaGiamGium> MaGiamGiums { get; set; }

        public virtual DbSet<NhanVien> NhanViens { get; set; }

        public virtual DbSet<Phong> Phongs { get; set; }

        public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

        public virtual DbSet<VaiTro> VaiTros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChiTietHoaDon>(entity =>
            {
                entity.HasKey(e => e.MaCthd).HasName("PK__ChiTietH__1E4FA771C13DFBB2");

                entity.ToTable("ChiTietHoaDon");

                entity.Property(e => e.MaCthd)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaCTHD");
                entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.LoaiMuc).HasMaxLength(20);
                entity.Property(e => e.MaDv)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaDV");
                entity.Property(e => e.MaHoaDon)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.MaLoaiPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.MaPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.NoiDung).HasMaxLength(255);
                entity.Property(e => e.SoLuong).HasDefaultValue(1);
                entity.Property(e => e.ThanhTien)
                    .HasComputedColumnSql("([SoLuong]*[DonGia])", true)
                    .HasColumnType("decimal(29, 2)");

                entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.ChiTietHoaDons)
                    .HasForeignKey(d => d.MaDv)
                    .HasConstraintName("FK__ChiTietHoa__MaDV__02084FDA");

                entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ChiTietHoaDons)
                    .HasForeignKey(d => d.MaHoaDon)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__ChiTietHo__MaHoa__00200768");

                entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.ChiTietHoaDons)
                    .HasForeignKey(d => d.MaLoaiPhong)
                    .HasConstraintName("FK_CTHD_LoaiPhong");

                entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.ChiTietHoaDons)
                    .HasForeignKey(d => d.MaPhong)
                    .HasConstraintName("FK__ChiTietHo__MaPho__01142BA1");
            });

            modelBuilder.Entity<DatPhong>(entity =>
            {
                entity.HasKey(e => e.MaDatPhong).HasName("PK__DatPhong__6344ADEA853318F1");

                entity.ToTable("DatPhong");

                entity.Property(e => e.MaDatPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.GhiChu).HasMaxLength(255);
                entity.Property(e => e.MaKh)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaKH");
                entity.Property(e => e.MaNv)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaNV");
                entity.Property(e => e.NgayDat)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(30)
                    .HasDefaultValue("Đã đặt");

                entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DatPhongs)
                    .HasForeignKey(d => d.MaKh)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DatPhong__MaKH__6D0D32F4");

                entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.DatPhongs)
                    .HasForeignKey(d => d.MaNv)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__DatPhong__MaNV__6E01572D");
            });

            modelBuilder.Entity<DichVu>(entity =>
            {
                entity.HasKey(e => e.MaDv).HasName("PK__DichVu__2725865782C9C718");

                entity.ToTable("DichVu");

                entity.Property(e => e.MaDv)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaDV");
                entity.Property(e => e.DonGia).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DonViTinh).HasMaxLength(20);
                entity.Property(e => e.TenDv)
                    .HasMaxLength(100)
                    .HasColumnName("TenDV");
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");
                entity.Property(e => e.LoaiDichVu).HasMaxLength(50);
            });

            modelBuilder.Entity<HoaDon>(entity =>
            {
                entity.HasKey(e => e.MaHoaDon).HasName("PK__HoaDon__835ED13BC6E7199B");

                entity.ToTable("HoaDon");

                entity.Property(e => e.MaHoaDon)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.GhiChu).HasMaxLength(255);
                entity.Property(e => e.MaDatPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(30)
                    .HasDefaultValue("Chưa thanh toán");
            });

            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1E2799DF18");

                entity.ToTable("KhachHang");

                entity.Property(e => e.MaKh)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaKH");
                entity.Property(e => e.Cccd)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("CCCD");
                entity.Property(e => e.DiaChi).HasMaxLength(255);
                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);
                entity.Property(e => e.GioiTinh).HasMaxLength(10);
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.QuocTich).HasMaxLength(50);
                entity.Property(e => e.SoDienThoai)
                    .HasMaxLength(15)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<LoaiPhong>(entity =>
            {
                entity.HasKey(e => e.MaLoaiPhong).HasName("PK__LoaiPhon__23021217F86E7023");

                entity.ToTable("LoaiPhong");

                entity.Property(e => e.MaLoaiPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.MoTa).HasMaxLength(255);
                entity.Property(e => e.TenLoaiPhong).HasMaxLength(50);
            });

            modelBuilder.Entity<LinkAnh>(entity =>
            {
                entity.HasKey(e => e.MaAnh);

                entity.ToTable("LinkAnh");

                entity.Property(e => e.MaAnh)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.DoiTuong).HasMaxLength(50);
                entity.Property(e => e.UrlAnh).HasMaxLength(500);
                entity.Property(e => e.ThongTin).HasMaxLength(255);
                entity.Property(e => e.ThuTu).HasDefaultValue(1);
                entity.Property(e => e.LaAnhDaiDien).HasDefaultValue(false);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(30)
                    .HasDefaultValue(DomainValues.LinkAnhTrangThai.HoatDong);
            });

            modelBuilder.Entity<NhanVien>(entity =>
            {
                entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70A7D848A8A");

                entity.ToTable("NhanVien");

                entity.Property(e => e.MaNv)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaNV");
                entity.Property(e => e.ChucVu).HasMaxLength(50);
                entity.Property(e => e.DiaChi).HasMaxLength(255);
                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);
                entity.Property(e => e.GioiTinh).HasMaxLength(10);
                entity.Property(e => e.HoTen).HasMaxLength(100);
                entity.Property(e => e.SoDienThoai)
                    .HasMaxLength(15)
                    .IsUnicode(false);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Đang làm");
            });

            modelBuilder.Entity<Phong>(entity =>
            {
                entity.HasKey(e => e.MaPhong).HasName("PK__Phong__20BD5E5B75CBB441");

                entity.ToTable("Phong");

                entity.HasIndex(e => e.SoPhong, "UQ__Phong__7C736CA1E53549CC").IsUnique();

                entity.Property(e => e.MaPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.GhiChu).HasMaxLength(255);
                entity.Property(e => e.MaLoaiPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.SoPhong)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(30)
                    .HasDefaultValue("Trống");

                entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                    .HasForeignKey(d => d.MaLoaiPhong)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Phong__MaLoaiPho__656C112C");
            });

            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasKey(e => e.MaTk).HasName("PK__TaiKhoan__27250070E5736EC3");

                entity.ToTable("TaiKhoan");

                entity.HasIndex(e => e.TenDangNhap, "UQ__TaiKhoan__55F68FC07F743652").IsUnique();

                entity.Property(e => e.MaTk)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaTK");
                entity.Property(e => e.MaNv)
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("MaNV");
                entity.Property(e => e.MaVaiTro)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.MatKhau)
                    .HasMaxLength(255)
                    .IsUnicode(false);
                entity.Property(e => e.TenDangNhap)
                    .HasMaxLength(50)
                    .IsUnicode(false);
                entity.Property(e => e.TrangThai)
                    .HasMaxLength(20)
                    .HasDefaultValue("Hoạt động");

                entity.HasOne(d => d.MaNvNavigation).WithOne(p => p.TaiKhoan)
                    .HasForeignKey<TaiKhoan>(d => d.MaNv)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__TaiKhoan__MaNV__5629CD9C");

                entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.TaiKhoans)
                    .HasForeignKey(d => d.MaVaiTro)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__TaiKhoan__MaVaiT__571DF1D5");
            });

            modelBuilder.Entity<VaiTro>(entity =>
            {
                entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CF21F762CB");

                entity.ToTable("VaiTro");

                entity.Property(e => e.MaVaiTro)
                    .HasMaxLength(10)
                    .IsUnicode(false);
                entity.Property(e => e.MoTa).HasMaxLength(255);
                entity.Property(e => e.TenVaiTro).HasMaxLength(50);

            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

}
