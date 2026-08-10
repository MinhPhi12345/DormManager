using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng TAIKHOAN - tài khoản đăng nhập của mọi người dùng (SV/QL/QTV).</summary>
    [Table("TAIKHOAN")]
    public class TaiKhoan
    {
        [Key]
        public int MaTK { get; set; }

        [Required, MaxLength(50)]
        public string TenDangNhap { get; set; } = "";

        /// <summary>Mật khẩu đã băm SHA-256.</summary>
        [Required, MaxLength(255)]
        public string MatKhau { get; set; } = "";

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [MaxLength(15)]
        public string? SDT { get; set; }

        /// <summary>'SV', 'QL', 'QTV'.</summary>
        [Required, MaxLength(10)]
        public string VaiTro { get; set; } = "";

        /// <summary>'HoatDong', 'BiKhoa'.</summary>
        [Required, MaxLength(15)]
        public string TrangThai { get; set; } = "HoatDong";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation (1-1 tùy vai trò)
        public SinhVien? SinhVien { get; set; }
        public QuanLy? QuanLy { get; set; }
        public ICollection<NhatKy> NhatKys { get; set; } = new List<NhatKy>();
    }
}
