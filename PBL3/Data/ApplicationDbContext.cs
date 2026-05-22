using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PBL3.Models;

namespace PBL3.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

    public virtual DbSet<MaGiamGium> MaGiamGia { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BangGiaPhong>(entity =>
        {
            entity.Property(e => e.MaBangGia).IsFixedLength();
            entity.Property(e => e.MaLoaiPhong).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            entity.Property(e => e.UuTien).HasDefaultValue(1);

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.BangGiaPhongs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BangGiaPhong_LoaiPhong");
        });

        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.Property(e => e.MaCthd).IsFixedLength();
            entity.Property(e => e.MaDv).IsFixedLength();
            entity.Property(e => e.MaGiamGia).IsFixedLength();
            entity.Property(e => e.MaHoaDon).IsFixedLength();
            entity.Property(e => e.MaLoaiPhong).IsFixedLength();
            entity.Property(e => e.MaPhong).IsFixedLength();
            entity.Property(e => e.SoLuong).HasDefaultValue(1, "DF_CTHD_SoLuong");
            entity.Property(e => e.TrangThai).HasDefaultValue("HIEU_LUC", "DF_CTHD_TrangThai");

            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.ChiTietHoaDons).HasConstraintName("FK_CTHD_DichVu");

            entity.HasOne(d => d.MaGiamGiaNavigation).WithMany(p => p.ChiTietHoaDons).HasConstraintName("FK_CTHD_MaGiamGia");

            entity.HasOne(d => d.MaHoaDonNavigation).WithMany(p => p.ChiTietHoaDons)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHD_HoaDon");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.ChiTietHoaDons).HasConstraintName("FK_CTHD_LoaiPhong");

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.ChiTietHoaDons).HasConstraintName("FK_CTHD_Phong");
        });

        modelBuilder.Entity<DatPhong>(entity =>
        {
            entity.Property(e => e.MaDatPhong).IsFixedLength();
            entity.Property(e => e.MaKh).IsFixedLength();
            entity.Property(e => e.MaNv).IsFixedLength();
            entity.Property(e => e.NgayDat).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TrangThai).HasDefaultValue("GIU_CHO");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.DatPhongs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_KhachHang");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.DatPhongs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DatPhong_NhanVien");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.Property(e => e.MaDv).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
            entity.Property(e => e.LoaiDichVu).HasMaxLength(50);
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.Property(e => e.MaHoaDon).IsFixedLength();
            entity.Property(e => e.MaDatPhong).IsFixedLength();
            entity.Property(e => e.MaGiamGiaPhong).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("CHUA_THANH_TOAN");

            entity.HasOne(d => d.MaDatPhongNavigation).WithOne(p => p.HoaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HoaDon_DatPhong");

            entity.HasOne(d => d.MaGiamGiaPhongNavigation).WithMany(p => p.HoaDons)
                .HasConstraintName("FK_HoaDon_MaGiamGiaPhong");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.Property(e => e.MaKh).IsFixedLength();
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.Property(e => e.MaLoaiPhong).IsFixedLength();
        });

        modelBuilder.Entity<LinkAnh>(entity =>
        {
            entity.Property(e => e.MaAnh).IsFixedLength();
            entity.Property(e => e.LaAnhDaiDien).HasDefaultValue(false);
            entity.Property(e => e.ThuTu).HasDefaultValue(1);
            entity.Property(e => e.TrangThai).HasDefaultValue(DomainValues.LinkAnhTrangThai.HoatDong);
        });

        modelBuilder.Entity<MaGiamGium>(entity =>
        {
            entity.Property(e => e.MaGiamGia).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.Property(e => e.MaNv).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Đang làm");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.Property(e => e.MaPhong).IsFixedLength();
            entity.Property(e => e.MaLoaiPhong).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Trống");

            entity.HasOne(d => d.MaLoaiPhongNavigation).WithMany(p => p.Phongs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Phong_LoaiPhong");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.Property(e => e.MaTk).IsFixedLength();
            entity.Property(e => e.MaNv).IsFixedLength();
            entity.Property(e => e.MaVaiTro).IsFixedLength();
            entity.Property(e => e.TrangThai).HasDefaultValue("Hoạt động");

            entity.HasOne(d => d.MaNvNavigation).WithOne(p => p.TaiKhoan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaiKhoan_NhanVien");

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.TaiKhoans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaiKhoan_VaiTro");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.Property(e => e.MaVaiTro).IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
