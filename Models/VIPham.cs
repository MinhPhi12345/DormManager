using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng VIPHAM - lịch sử điểm vi phạm nội quy.</summary>
    [Table("VIPHAM")]
    public class VIPham
    {
        [Key]
        public int MaVP { get; set; }

        [Required, MaxLength(10)]
        public string MSSV { get; set; } = "";

        public int? MaHD { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayGhiNhan { get; set; } = DateTime.Today;

        public int SoDiem { get; set; } = 1;

        [Required, MaxLength(200)]
        public string LyDo { get; set; } = "";

        // Navigation
        public SinhVien? SinhVien { get; set; }
        public HoaDon? HoaDon { get; set; }
    }
}
