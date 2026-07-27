using System.Data;
using DormManager.Helpers;

namespace DormManager.Data
{
    /// <summary>
    /// Wrapper cho các Stored Procedure phức tạp phía quản trị viên
    /// (nhiều result set, transaction, SCOPE_IDENTITY). CRUD/truy vấn đơn giản
    /// viết SQL trực tiếp trong Controller.
    /// </summary>
    public static class QuanTriRepo
    {
        /// <summary>sp_ThongKe: 4 result set (6 chỉ số tổng, doanh thu, sản lượng, đơn theo trạng thái).</summary>
        public static DataSet ThongKe() => Db.QuerySetProc("sp_ThongKe");

        /// <summary>sp_BaoCaoCongNo: chi tiết công nợ từng sinh viên (tổng nợ, số ngày trễ nhất).</summary>
        public static DataTable BaoCaoCongNo() => Db.QueryProc("sp_BaoCaoCongNo");

        /// <summary>sp_XoaPhong: transaction xóa giường + xóa phòng.</summary>
        public static void XoaPhong(string maPhong)
            => Db.ExecProc("sp_XoaPhong", Db.P("@MaPhong", maPhong));

        /// <summary>sp_CapNhatDonGia: transaction cho hết hiệu lực biểu giá cũ + thêm biểu giá mới.</summary>
        public static void CapNhatDonGia(decimal giaDien, decimal giaNuoc, decimal phiDichVu, DateTime ngayApDung)
            => Db.ExecProc("sp_CapNhatDonGia",
                Db.P("@GiaDien", giaDien), Db.P("@GiaNuoc", giaNuoc), Db.P("@PhiDichVu", phiDichVu), Db.P("@NgayApDung", ngayApDung));

        /// <summary>sp_CapNhatTrangThaiDot: cập nhật trạng thái các đợt đăng ký theo thời gian.</summary>
        public static void CapNhatTrangThaiDot() => Db.ExecProc("sp_CapNhatTrangThaiDot");

        /// <summary>sp_TaoTaiKhoan: INSERT tài khoản, trả về MaTK mới (SCOPE_IDENTITY).</summary>
        public static int TaoTaiKhoan(string tenDangNhap, string matKhauHash, string email, string? sdt, string vaiTro)
            => Convert.ToInt32(Db.ScalarProc("sp_TaoTaiKhoan",
                Db.P("@TenDangNhap", tenDangNhap), Db.P("@MatKhau", matKhauHash),
                Db.P("@Email", email), Db.P("@SDT", sdt), Db.P("@VaiTro", vaiTro)));

        /// <summary>sp_CapNhatThongTinCaNhan: cập nhật hồ sơ SV/QL theo vai trò.</summary>
        public static void CapNhatThongTinCaNhan(int maTK, string vaiTro, string hoTen, string? khoaHoc, string? doiTuong, string? maToa)
            => Db.ExecProc("sp_CapNhatThongTinCaNhan",
                Db.P("@MaTK", maTK), Db.P("@VaiTro", vaiTro), Db.P("@HoTen", hoTen),
                Db.P("@KhoaHoc", khoaHoc), Db.P("@DoiTuong", doiTuong), Db.P("@MaToa", maToa));
    }
}
