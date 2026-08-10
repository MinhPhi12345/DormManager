using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng TOANHA - tòa nhà ký túc xá.</summary>
    [Table("TOANHA")]
    public class ToaNha
    {
        [Key, MaxLength(10)]
        public string MaToa { get; set; } = "";

        [Required, MaxLength(50)]
        public string TenToa { get; set; } = "";

        [MaxLength(200)]
        public string? DiaChi { get; set; }

        public int SoTang { get; set; }

        // Navigation
        public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
        public ICollection<QuanLy> QuanLys { get; set; } = new List<QuanLy>();
    }
}
