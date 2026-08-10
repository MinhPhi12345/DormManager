using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng QUANLY - nhân viên quản lý ký túc xá.</summary>
    [Table("QUANLY")]
    public class QuanLy
    {
        [Key, MaxLength(10)]
        public string MaNV { get; set; } = "";

        public int MaTK { get; set; }

        [Required, MaxLength(100)]
        public string HoTen { get; set; } = "";

        [MaxLength(10)]
        public string? MaToa { get; set; }

        // Navigation
        public TaiKhoan? TaiKhoan { get; set; }
        public ToaNha? ToaNha { get; set; }
        public ICollection<DonYeuCau> DonYeuCauXuLys { get; set; } = new List<DonYeuCau>();
    }
}
