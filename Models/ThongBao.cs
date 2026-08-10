using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng THONGBAO - lịch sử thông báo email gửi sinh viên (QD12: gửi qua email khi hóa đơn phát hành).</summary>
    [Table("THONGBAO")]
    public class ThongBao
    {
        [Key]
        public int MaTB { get; set; }

        [Required, MaxLength(10)]
        public string MSSV { get; set; } = "";

        public int? MaHD { get; set; }

        [Required]
        public string NoiDung { get; set; } = "";

        /// <summary>Luôn là 'Email' - hệ thống chỉ gửi thông báo qua email thật (SMTP).</summary>
        [Required, MaxLength(5)]
        public string Kenh { get; set; } = "Email";

        public DateTime ThoiGianGui { get; set; } = DateTime.Now;

        /// <summary>'ThanhCong', 'ThatBai', 'ChoGuiLai'.</summary>
        [Required, MaxLength(10)]
        public string TrangThaiGui { get; set; } = "ThanhCong";

        // Navigation
        public SinhVien? SinhVien { get; set; }
        public HoaDon? HoaDon { get; set; }
    }
}
