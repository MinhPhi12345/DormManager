using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng CHISODIENNUOC - chỉ số điện nước hàng tháng của phòng.</summary>
    [Table("CHISODIENNUOC")]
    public class ChiSoDienNuoc
    {
        [Key]
        public int MaChiSo { get; set; }

        [Required, MaxLength(10)]
        public string MaPhong { get; set; } = "";

        /// <summary>Định dạng 'MM/YYYY'.</summary>
        [Required, MaxLength(7)]
        public string Thang { get; set; } = "";

        [Column(TypeName = "decimal(10,1)")]
        public decimal DienDauKy { get; set; }

        [Column(TypeName = "decimal(10,1)")]
        public decimal DienCuoiKy { get; set; }

        [Column(TypeName = "decimal(10,1)")]
        public decimal NuocDauKy { get; set; }

        [Column(TypeName = "decimal(10,1)")]
        public decimal NuocCuoiKy { get; set; }

        /// <summary>'Nhap', 'DaChot'.</summary>
        [Required, MaxLength(10)]
        public string TrangThai { get; set; } = "Nhap";

        // Navigation
        public Phong? Phong { get; set; }
        public HoaDon? HoaDon { get; set; }
    }
}
