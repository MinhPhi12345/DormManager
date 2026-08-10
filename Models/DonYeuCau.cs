using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>
    /// Bảng DONYEUCAU - đơn phản hồi sự cố / đơn đề xuất trang bị / yêu cầu trả phòng /
    /// yêu cầu chuyển phòng. MaPhieu dùng khi LoaiDon='TraPhong' hoặc 'ChuyenPhong'.
    /// MaGiuongMoi (giường đích cụ thể) chỉ dùng khi LoaiDon='ChuyenPhong'.
    /// </summary>
    [Table("DONYEUCAU")]
    public class DonYeuCau
    {
        [Key]
        public int MaDon { get; set; }

        [Required, MaxLength(10)]
        public string MSSV { get; set; } = "";

        [MaxLength(10)]
        public string? MaNV { get; set; }

        public int? MaPhieu { get; set; }

        [MaxLength(15)]
        public string? MaGiuongMoi { get; set; }

        /// <summary>'PhanHoi', 'DeXuat', 'TraPhong', 'ChuyenPhong'.</summary>
        [Required, MaxLength(12)]
        public string LoaiDon { get; set; } = "";

        [Required, MaxLength(200)]
        public string TieuDe { get; set; } = "";

        [Required]
        public string NoiDung { get; set; } = "";

        /// <summary>'Cao', 'TrungBinh', 'Thap'.</summary>
        [Required, MaxLength(10)]
        public string MucUuTien { get; set; } = "TrungBinh";

        /// <summary>'ChoXuLy', 'DangXuLy', 'DaXuLy', 'TuChoi'.</summary>
        [Required, MaxLength(10)]
        public string TrangThai { get; set; } = "ChoXuLy";

        public string? PhanHoi { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation
        public SinhVien? SinhVien { get; set; }
        public QuanLy? QuanLy { get; set; }
        public PhieuDangKy? PhieuDangKy { get; set; }
        public Giuong? GiuongMoi { get; set; }
    }
}
