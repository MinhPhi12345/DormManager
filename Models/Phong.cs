using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng PHONG - phòng ở trong tòa nhà.</summary>
    [Table("PHONG")]
    public class Phong
    {
        [Key, MaxLength(10)]
        public string MaPhong { get; set; } = "";

        [Required, MaxLength(10)]
        public string MaToa { get; set; } = "";

        public int Tang { get; set; }

        /// <summary>'4', '6', '8'.</summary>
        [Required, MaxLength(2)]
        public string LoaiPhong { get; set; } = "";

        public int SoGiuong { get; set; }

        [Column(TypeName = "decimal(10,0)")]
        public decimal GiaPhong { get; set; }

        /// <summary>'HoatDong', 'BaoTri', 'NgungSuDung'.</summary>
        [Required, MaxLength(15)]
        public string TrangThai { get; set; } = "HoatDong";

        public int SoGiuongTrong { get; set; }

        // Navigation
        public ToaNha? ToaNha { get; set; }
        public ICollection<Giuong> Giuongs { get; set; } = new List<Giuong>();
        public ICollection<AnhPhong> AnhPhongs { get; set; } = new List<AnhPhong>();
        public ICollection<ChiSoDienNuoc> ChiSoDienNuocs { get; set; } = new List<ChiSoDienNuoc>();
        public ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    }
}
