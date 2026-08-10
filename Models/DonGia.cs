using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng DONGIA - biểu giá điện, nước và phí dịch vụ.</summary>
    [Table("DONGIA")]
    public class DonGia
    {
        [Key]
        public int MaDonGia { get; set; }

        [Column(TypeName = "decimal(10,0)")]
        public decimal GiaDien { get; set; }

        [Column(TypeName = "decimal(10,0)")]
        public decimal GiaNuoc { get; set; }

        [Column(TypeName = "decimal(10,0)")]
        public decimal PhiDichVu { get; set; } = 0;

        [Column(TypeName = "date")]
        public DateTime NgayApDung { get; set; }

        /// <summary>'HieuLuc', 'HetHieuLuc'.</summary>
        [Required, MaxLength(15)]
        public string TrangThai { get; set; } = "HieuLuc";

        // Navigation
        public ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();
    }
}
