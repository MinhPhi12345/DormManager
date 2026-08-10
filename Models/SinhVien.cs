using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng SINHVIEN - hồ sơ sinh viên nội trú.</summary>
    [Table("SINHVIEN")]
    public class SinhVien
    {
        [Key, MaxLength(10)]
        public string MSSV { get; set; } = "";

        public int MaTK { get; set; }

        [Required, MaxLength(100)]
        public string HoTen { get; set; } = "";

        public DateTime? NgaySinh { get; set; }

        /// <summary>'Nam', 'Nu'.</summary>
        [MaxLength(5)]
        public string? GioiTinh { get; set; }

        [Required, MaxLength(10)]
        public string KhoaHoc { get; set; } = "";

        /// <summary>'BinhThuong', 'ChinhSach'.</summary>
        [Required, MaxLength(15)]
        public string DoiTuong { get; set; } = "BinhThuong";

        public int DiemViPham { get; set; } = 0;

        // Navigation
        public TaiKhoan? TaiKhoan { get; set; }
        public ICollection<PhieuDangKy> PhieuDangKys { get; set; } = new List<PhieuDangKy>();
        public ICollection<DonYeuCau> DonYeuCaus { get; set; } = new List<DonYeuCau>();
        public ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
        public ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
        public ICollection<VIPham> VIPhams { get; set; } = new List<VIPham>();
    }
}
