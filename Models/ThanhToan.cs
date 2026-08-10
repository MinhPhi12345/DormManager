using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng THANHTOAN - giao dịch thanh toán hóa đơn.</summary>
    [Table("THANHTOAN")]
    public class ThanhToan
    {
        [Key]
        public int MaGD { get; set; }

        public int MaHD { get; set; }

        [Required, MaxLength(10)]
        public string MSSV { get; set; } = "";

        /// <summary>'VNPay', 'MoMo', 'NganHang'.</summary>
        [Required, MaxLength(10)]
        public string PhuongThuc { get; set; } = "NganHang";

        [Column(TypeName = "decimal(12,0)")]
        public decimal SoTien { get; set; }

        [MaxLength(50)]
        public string? MaGDCong { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        /// <summary>'ThanhCong', 'ThatBai'.</summary>
        [Required, MaxLength(10)]
        public string KetQua { get; set; } = "ThanhCong";

        // Navigation
        public HoaDon? HoaDon { get; set; }
        public SinhVien? SinhVien { get; set; }
    }
}
