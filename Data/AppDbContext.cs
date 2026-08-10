using DormManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DormManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
        public DbSet<SinhVien> SinhViens => Set<SinhVien>();
        public DbSet<ToaNha> ToaNhas => Set<ToaNha>();
        public DbSet<QuanLy> QuanLys => Set<QuanLy>();
        public DbSet<Phong> Phongs => Set<Phong>();
        public DbSet<AnhPhong> AnhPhongs => Set<AnhPhong>();
        public DbSet<Giuong> Giuongs => Set<Giuong>();
        public DbSet<DotDangKy> DotDangKys => Set<DotDangKy>();
        public DbSet<PhieuDangKy> PhieuDangKys => Set<PhieuDangKy>();
        public DbSet<DonYeuCau> DonYeuCaus => Set<DonYeuCau>();
        public DbSet<ChiSoDienNuoc> ChiSoDienNuocs => Set<ChiSoDienNuoc>();
        public DbSet<DonGia> DonGias => Set<DonGia>();
        public DbSet<HoaDon> HoaDons => Set<HoaDon>();
        public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
        public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
        public DbSet<VIPham> VIPhams => Set<VIPham>();
        public DbSet<ThamSo> ThamSos => Set<ThamSo>();
        public DbSet<NhatKy> NhatKys => Set<NhatKy>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ---- TAIKHOAN ----
            mb.Entity<TaiKhoan>(e =>
            {
                e.HasIndex(x => x.TenDangNhap).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
            });

            // ---- SINHVIEN (1-1 với TAIKHOAN qua MaTK) ----
            mb.Entity<SinhVien>(e =>
            {
                e.HasIndex(x => x.MaTK).IsUnique();
                e.HasOne(sv => sv.TaiKhoan).WithOne(tk => tk.SinhVien)
                 .HasForeignKey<SinhVien>(sv => sv.MaTK).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- QUANLY (1-1 với TAIKHOAN qua MaTK; N-1 với TOANHA) ----
            mb.Entity<QuanLy>(e =>
            {
                e.HasIndex(x => x.MaTK).IsUnique();
                e.HasOne(ql => ql.TaiKhoan).WithOne(tk => tk.QuanLy)
                 .HasForeignKey<QuanLy>(ql => ql.MaTK).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(ql => ql.ToaNha).WithMany(t => t.QuanLys)
                 .HasForeignKey(ql => ql.MaToa).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- PHONG ----
            mb.Entity<Phong>(e =>
            {
                e.HasOne(p => p.ToaNha).WithMany(t => t.Phongs)
                 .HasForeignKey(p => p.MaToa).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- ANHPHONG ----
            mb.Entity<AnhPhong>(e =>
            {
                e.HasOne(a => a.Phong).WithMany(p => p.AnhPhongs)
                 .HasForeignKey(a => a.MaPhong).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- GIUONG ----
            mb.Entity<Giuong>(e =>
            {
                e.HasOne(g => g.Phong).WithMany(p => p.Giuongs)
                 .HasForeignKey(g => g.MaPhong).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- DOTDANGKY ----
            mb.Entity<DotDangKy>(e =>
            {
                e.HasCheckConstraint("CK_DotDangKy_Ngay", "[NgayDong] > [NgayMo]");
            });

            // ---- PHIEUDANGKY ----
            mb.Entity<PhieuDangKy>(e =>
            {
                e.HasOne(pd => pd.SinhVien).WithMany(sv => sv.PhieuDangKys)
                 .HasForeignKey(pd => pd.MSSV).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(pd => pd.Giuong).WithMany(g => g.PhieuDangKys)
                 .HasForeignKey(pd => pd.MaGiuong).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(pd => pd.DotDangKy).WithMany(d => d.PhieuDangKys)
                 .HasForeignKey(pd => pd.MaDot).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- DONYEUCAU ----
            mb.Entity<DonYeuCau>(e =>
            {
                e.HasOne(d => d.SinhVien).WithMany(sv => sv.DonYeuCaus)
                 .HasForeignKey(d => d.MSSV).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.QuanLy).WithMany(ql => ql.DonYeuCauXuLys)
                 .HasForeignKey(d => d.MaNV).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.PhieuDangKy).WithMany(pd => pd.DonYeuCaus)
                 .HasForeignKey(d => d.MaPhieu).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.GiuongMoi).WithMany(g => g.DonYeuCauChuyenDens)
                 .HasForeignKey(d => d.MaGiuongMoi).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- CHISODIENNUOC ----
            mb.Entity<ChiSoDienNuoc>(e =>
            {
                e.HasIndex(x => new { x.MaPhong, x.Thang }).IsUnique();
                e.HasOne(c => c.Phong).WithMany(p => p.ChiSoDienNuocs)
                 .HasForeignKey(c => c.MaPhong).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- HOADON (1-1 với CHISODIENNUOC qua MaChiSo) ----
            mb.Entity<HoaDon>(e =>
            {
                e.HasIndex(x => x.MaChiSo).IsUnique();
                e.HasOne(h => h.Phong).WithMany(p => p.HoaDons)
                 .HasForeignKey(h => h.MaPhong).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(h => h.ChiSoDienNuoc).WithOne(c => c.HoaDon)
                 .HasForeignKey<HoaDon>(h => h.MaChiSo).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(h => h.DonGia).WithMany(dg => dg.HoaDons)
                 .HasForeignKey(h => h.MaDonGia).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- THANHTOAN ----
            mb.Entity<ThanhToan>(e =>
            {
                e.HasOne(t => t.HoaDon).WithMany(h => h.ThanhToans)
                 .HasForeignKey(t => t.MaHD).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.SinhVien).WithMany(sv => sv.ThanhToans)
                 .HasForeignKey(t => t.MSSV).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- THONGBAO ----
            mb.Entity<ThongBao>(e =>
            {
                e.HasOne(t => t.SinhVien).WithMany(sv => sv.ThongBaos)
                 .HasForeignKey(t => t.MSSV).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.HoaDon).WithMany(h => h.ThongBaos)
                 .HasForeignKey(t => t.MaHD).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- VIPHAM ----
            mb.Entity<VIPham>(e =>
            {
                e.HasOne(v => v.SinhVien).WithMany(sv => sv.VIPhams)
                 .HasForeignKey(v => v.MSSV).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(v => v.HoaDon).WithMany(h => h.VIPhams)
                 .HasForeignKey(v => v.MaHD).OnDelete(DeleteBehavior.Restrict);
            });

            // ---- NHATKY ----
            mb.Entity<NhatKy>(e =>
            {
                e.HasOne(n => n.TaiKhoan).WithMany(tk => tk.NhatKys)
                 .HasForeignKey(n => n.MaTK).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
