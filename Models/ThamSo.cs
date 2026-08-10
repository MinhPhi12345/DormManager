using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng THAMSO - tham số cấu hình quy định của hệ thống (TS1-TS12).</summary>
    [Table("THAMSO")]
    public class ThamSo
    {
        [Key, MaxLength(10)]
        public string MaThamSo { get; set; } = "";

        [Required, MaxLength(20)]
        public string GiaTri { get; set; } = "";

        [MaxLength(200)]
        public string? GhiChu { get; set; }
    }
}
