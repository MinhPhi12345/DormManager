using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng NHATKY - nhật ký thao tác hệ thống (audit log), chỉ QTV xem được.</summary>
    [Table("NHATKY")]
    public class NhatKy
    {
        [Key]
        public int MaNhatKy { get; set; }

        public int? MaTK { get; set; }

        [MaxLength(100)]
        public string? HoTenNguoiThucHien { get; set; }

        [MaxLength(10)]
        public string? VaiTro { get; set; }

        /// <summary>'Them', 'Sua', 'Xoa'.</summary>
        [Required, MaxLength(20)]
        public string HanhDong { get; set; } = "";

        [Required, MaxLength(50)]
        public string DoiTuong { get; set; } = "";

        [Required, MaxLength(500)]
        public string NoiDung { get; set; } = "";

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        // Navigation
        public TaiKhoan? TaiKhoan { get; set; }
    }
}
