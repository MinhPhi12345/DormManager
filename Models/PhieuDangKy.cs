using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng PHIEUDANGKY - phiếu đăng ký / hợp đồng lưu trú.</summary>
    [Table("PHIEUDANGKY")]
    public class PhieuDangKy
    {
        [Key]
        public int MaPhieu { get; set; }

        [Required, MaxLength(10)]
        public string MSSV { get; set; } = "";

        [Required, MaxLength(15)]
        public string MaGiuong { get; set; } = "";

        public int MaDot { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;

        [Column(TypeName = "date")]
        public DateTime NgayBatDau { get; set; }

        [Column(TypeName = "date")]
        public DateTime NgayKetThuc { get; set; }

        /// <summary>'ChoDoiChieu', 'DangO', 'DaTraPhong', 'DaHuy'.</summary>
        [Required, MaxLength(15)]
        public string TrangThai { get; set; } = "ChoDoiChieu";

        // Navigation
        public SinhVien? SinhVien { get; set; }
        public Giuong? Giuong { get; set; }
        public DotDangKy? DotDangKy { get; set; }
        public ICollection<DonYeuCau> DonYeuCaus { get; set; } = new List<DonYeuCau>();
    }
}
