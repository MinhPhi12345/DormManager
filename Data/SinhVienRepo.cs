using System.Data;
using DormManager.Helpers;

namespace DormManager.Data
{
    /// <summary>
    /// Wrapper cho các Stored Procedure phức tạp phía sinh viên
    /// (nhiều result set / transaction nhiều bảng). CRUD đơn giản viết
    /// SQL trực tiếp trong Controller.
    /// </summary>
    public static class SinhVienRepo
    {
        /// <summary>sp_TongQuan: 4 result set (phòng đang ở, bạn cùng phòng, hóa đơn chưa TT, đơn gần đây).</summary>
        public static DataSet TongQuan(string mssv)
            => Db.QuerySetProc("sp_TongQuan", Db.P("@MSSV", mssv));

        /// <summary>sp_DotDangMoChoSV: danh sách đợt đăng ký đang mở phù hợp với sinh viên.</summary>
        public static DataTable DotDangMoChoSV(string mssv)
            => Db.QueryProc("sp_DotDangMoChoSV", Db.P("@MSSV", mssv));

        /// <summary>sp_XacNhanDangKy: transaction tạo phiếu + giữ giường + trừ giường trống.</summary>
        public static void XacNhanDangKy(string mssv, string maGiuong, object maDot, DateTime ngayBatDau, DateTime ngayKetThuc)
            => Db.ExecProc("sp_XacNhanDangKy",
                Db.P("@MSSV", mssv), Db.P("@MaGiuong", maGiuong), Db.P("@MaDot", maDot),
                Db.P("@NgayBatDau", ngayBatDau), Db.P("@NgayKetThuc", ngayKetThuc));

        /// <summary>sp_GiaHan: transaction gia hạn hợp đồng + ghi thông báo.</summary>
        public static void GiaHan(int maPhieu, int soThang, string mssv, string noiDung)
            => Db.ExecProc("sp_GiaHan",
                Db.P("@MaPhieu", maPhieu), Db.P("@SoThang", soThang), Db.P("@MSSV", mssv), Db.P("@NoiDung", noiDung));

        /// <summary>sp_ThanhToan: transaction ghi giao dịch + cập nhật hóa đơn + gửi thông báo.</summary>
        public static void ThanhToan(int maHD, string mssv, string phuongThuc, object soTien, string maGDCong, string noiDungTB)
            => Db.ExecProc("sp_ThanhToan",
                Db.P("@MaHD", maHD), Db.P("@MSSV", mssv), Db.P("@PhuongThuc", phuongThuc),
                Db.P("@SoTien", soTien), Db.P("@MaGDCong", maGDCong), Db.P("@NoiDungTB", noiDungTB));
    }
}
