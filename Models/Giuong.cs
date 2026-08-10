using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng GIUONG - giường trong phòng.</summary>
    [Table("GIUONG")]
    public class Giuong
    {
        [Key, MaxLength(15)]
        public string MaGiuong { get; set; } = "";

        [Required, MaxLength(10)]
        public string MaPhong { get; set; } = "";

        /// <summary>'Trong', 'DaSuDung', 'BaoTri'.</summary>
        [Required, MaxLength(10)]
        public string TrangThai { get; set; } = "Trong";

        // Navigation
        public Phong? Phong { get; set; }
        public ICollection<PhieuDangKy> PhieuDangKys { get; set; } = new List<PhieuDangKy>();
        public ICollection<DonYeuCau> DonYeuCauChuyenDens { get; set; } = new List<DonYeuCau>();
    }
}
