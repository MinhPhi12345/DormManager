using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DormManager.Models
{
    /// <summary>Bảng ANHPHONG - ảnh minh họa của phòng (1 phòng - nhiều ảnh).</summary>
    [Table("ANHPHONG")]
    public class AnhPhong
    {
        [Key]
        public int MaAnh { get; set; }

        [Required, MaxLength(10)]
        public string MaPhong { get; set; } = "";

        /// <summary>Đường dẫn tương đối trong wwwroot, VD /uploads/phong/A1-101/xxx.jpg.</summary>
        [Required, MaxLength(255)]
        public string DuongDan { get; set; } = "";

        /// <summary>Thứ tự hiển thị - ảnh có ThuTu nhỏ nhất là ảnh đại diện.</summary>
        public int ThuTu { get; set; } = 0;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation
        public Phong? Phong { get; set; }
    }
}
