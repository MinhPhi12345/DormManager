using System.Data;
using DormManager.Helpers;

namespace DormManager.Data
{
    /// <summary>
    /// Wrapper cho các Stored Procedure phức tạp phía quản lý
    /// (transaction nhiều bảng, xử lý theo tập hợp, job quét). CRUD/truy vấn
    /// đơn giản viết SQL trực tiếp trong Controller.
    /// </summary>
    public static class QuanLyRepo
    {
        /// <summary>sp_XuLyDon: transaction đổi trạng thái/ưu tiên đơn + gửi thông báo.</summary>
        public static void XuLyDon(int maDon, string mucUuTien, string trangThai, string? phanHoi, string? maNV)
            => Db.ExecProc("sp_XuLyDon",
                Db.P("@MaDon", maDon), Db.P("@MucUuTien", mucUuTien), Db.P("@TrangThai", trangThai),
                Db.P("@PhanHoi", phanHoi), Db.P("@MaNV", maNV));

        /// <summary>sp_XacNhanNhanPhong: transaction chuyển hợp đồng sang Đang ở + thông báo.</summary>
        public static void XacNhanNhanPhong(int maPhieu)
            => Db.ExecProc("sp_XacNhanNhanPhong", Db.P("@MaPhieu", maPhieu));

        /// <summary>sp_HuyPhieu: transaction hủy phiếu + trả giường về Trống.</summary>
        public static void HuyPhieu(int maPhieu)
            => Db.ExecProc("sp_HuyPhieu", Db.P("@MaPhieu", maPhieu));

        /// <summary>sp_XacNhanTraPhong: transaction kết thúc hợp đồng + giải phóng giường + thông báo.</summary>
        public static void XacNhanTraPhong(int maDon, string? maNV)
            => Db.ExecProc("sp_XacNhanTraPhong", Db.P("@MaDon", maDon), Db.P("@MaNV", maNV));

        /// <summary>sp_CapNhatGiuong: transaction đổi trạng thái giường + đồng bộ số giường trống.</summary>
        public static void CapNhatGiuong(string maGiuong, string maPhong, string trangThai)
            => Db.ExecProc("sp_CapNhatGiuong", Db.P("@MaGiuong", maGiuong), Db.P("@MaPhong", maPhong), Db.P("@TrangThai", trangThai));

        /// <summary>sp_LuuChiSo: MERGE chỉ số điện/nước (thêm mới hoặc cập nhật).</summary>
        public static void LuuChiSo(string maPhong, string thang, decimal dienDauKy, decimal dienCuoiKy, decimal nuocDauKy, decimal nuocCuoiKy)
            => Db.ExecProc("sp_LuuChiSo",
                Db.P("@MaPhong", maPhong), Db.P("@Thang", thang),
                Db.P("@DienDauKy", dienDauKy), Db.P("@DienCuoiKy", dienCuoiKy),
                Db.P("@NuocDauKy", nuocDauKy), Db.P("@NuocCuoiKy", nuocCuoiKy));

        /// <summary>sp_NguonTaoHoaDon: nguồn tạo hóa đơn kèm số người ở trọn tháng (correlated subquery).</summary>
        public static DataTable NguonTaoHoaDon(string thang)
            => Db.QueryProc("sp_NguonTaoHoaDon", Db.P("@Thang", thang));

        /// <summary>sp_TaoHoaDonDong: transaction tạo hóa đơn nháp + chốt chỉ số.</summary>
        public static void TaoHoaDonDong(object maPhong, object maChiSo, object maDonGia, string thang,
            decimal tienPhong, decimal tienDien, decimal tienNuoc, decimal tongTien, DateTime hanThanhToan)
            => Db.ExecProc("sp_TaoHoaDonDong",
                Db.P("@MaPhong", maPhong), Db.P("@MaChiSo", maChiSo), Db.P("@MaDonGia", maDonGia),
                Db.P("@Thang", thang), Db.P("@TienPhong", tienPhong), Db.P("@TienDien", tienDien),
                Db.P("@TienNuoc", tienNuoc), Db.P("@TongTien", tongTien), Db.P("@HanThanhToan", hanThanhToan));

        /// <summary>Job mô phỏng theo tập hợp: cập nhật hóa đơn quá hạn, khóa TK (QD05),
        /// ghi vi phạm (QD06), cộng điểm vi phạm.</summary>
        public static void QuetHoaDonQuaHan()
        {
            Db.ExecProc("sp_CapNhatHoaDonQuaHan");
            Db.ExecProc("sp_KhoaTaiKhoanQuaHan", Db.P("@TS3", CommonRepo.LayThamSoInt("TS3")));
            Db.ExecProc("sp_GhiNhanViPham", Db.P("@TS4", CommonRepo.LayThamSoInt("TS4")), Db.P("@TS6", CommonRepo.LayThamSoInt("TS6")));
            Db.ExecProc("sp_CapNhatDiemViPham");
        }

        /// <summary>sp_MoKhoa: transaction mở khóa tài khoản + gửi thông báo.</summary>
        public static void MoKhoa(int maTK)
            => Db.ExecProc("sp_MoKhoa", Db.P("@MaTK", maTK));

        /// <summary>sp_XacNhanChuyenPhong: transaction đóng phiếu cũ + mở phiếu mới + cập nhật 2 phòng.</summary>
        public static void XacNhanChuyenPhong(int maDon, string? maNV)
            => Db.ExecProc("sp_XacNhanChuyenPhong", Db.P("@MaDon", maDon), Db.P("@MaNV", maNV));

        /// <summary>sp_BaoCaoCongNo: chi tiết công nợ từng sinh viên (tổng nợ, số ngày trễ nhất).</summary>
        public static DataTable BaoCaoCongNo() => Db.QueryProc("sp_BaoCaoCongNo");

    }
}
