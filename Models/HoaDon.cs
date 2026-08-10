using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>
    /// Bảng HOADON - hóa đơn điện/nước/tiền phòng, lập chung theo phòng mỗi tháng.
    /// TienPhong = GiaPhong x số người dọn vào từ ngày TS12 của tháng trở về trước (QD18).
    /// </summary>
    [Table("HOADON")]
    public class HoaDon
    {
        [Key]
        public int MaHD { get; set; }

        [Required, MaxLength(10)]
        public string MaPhong { get; set; } = "";

        public int MaChiSo { get; set; }

        public int MaDonGia { get; set; }

        /// <summary>Định dạng 'MM/YYYY'.</summary>
        [Required, MaxLength(7)]
        public string Thang { get; set; } = "";

        [Column(TypeName = "decimal(12,0)")]
        public decimal TienPhong { get; set; }

        [Column(TypeName = "decimal(12,0)")]
        public decimal TienDien { get; set; }

        [Column(TypeName = "decimal(12,0)")]
        public decimal TienNuoc { get; set; }

        [Column(TypeName = "decimal(12,0)")]
        public decimal TongTien { get; set; }

        public DateTime? NgayPhatHanh { get; set; }

        [Column(TypeName = "date")]
        public DateTime HanThanhToan { get; set; }

        /// <summary>'Nhap', 'ChoThanhToan', 'QuaHan', 'DaThanhToan'.</summary>
        [Required, MaxLength(15)]
        public string TrangThai { get; set; } = "Nhap";

        // Navigation
        public Phong? Phong { get; set; }
        public ChiSoDienNuoc? ChiSoDienNuoc { get; set; }
        public DonGia? DonGia { get; set; }
        public ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
        public ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
        public ICollection<VIPham> VIPhams { get; set; } = new List<VIPham>();
    }
}
