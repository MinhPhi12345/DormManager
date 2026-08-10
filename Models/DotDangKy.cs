using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng DOTDANGKY - đợt mở cổng đăng ký lưu trú.</summary>
    [Table("DOTDANGKY")]
    public class DotDangKy
    {
        [Key]
        public int MaDot { get; set; }

        [Required, MaxLength(100)]
        public string TenDot { get; set; } = "";

        /// <summary>'UuTien', 'DaiTra', 'KyHe'.</summary>
        [Required, MaxLength(10)]
        public string LoaiDot { get; set; } = "";

        [Required, MaxLength(20)]
        public string HocKy { get; set; } = "";

        public DateTime NgayMo { get; set; }
        public DateTime NgayDong { get; set; }

        /// <summary>'ChuaMo', 'DangMo', 'DaDong'.</summary>
        [Required, MaxLength(10)]
        public string TrangThai { get; set; } = "ChuaMo";

        // Navigation
        public ICollection<PhieuDangKy> PhieuDangKys { get; set; } = new List<PhieuDangKy>();
    }
}
