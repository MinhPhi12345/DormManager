using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    [PhanQuyen("QL", "QTV")]
    public class QuanLyController : Controller
    {
        private string? MaNV => HttpContext.Session.GetString("MaNV");

        // ============ Danh sách + tra cứu + sắp xếp ưu tiên đơn ============
        public IActionResult DonYeuCau(string? tuKhoa, string? loai, string? trangThai, string? uuTien)
        {
            ViewBag.DsDon = Db.QueryProc("sp_DsDon",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@LoaiDon", string.IsNullOrWhiteSpace(loai) ? null : loai),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai),
                Db.P("@UuTien", string.IsNullOrWhiteSpace(uuTien) ? null : uuTien));
            return View();
        }

        // Chi tiết + xử lý đơn (đổi trạng thái, ưu tiên, phản hồi → gửi thông báo)
        public IActionResult ChiTietDon(int id)
        {
            var dt = Db.QueryProc("sp_ChiTietDon", Db.P("@MaDon", id));
            if (dt.Rows.Count == 0) return RedirectToAction("DonYeuCau");
            ViewBag.Don = dt.Rows[0];
            return View();
        }

        [HttpPost]
        public IActionResult XuLyDon(int maDon, string mucUuTien, string trangThai, string? phanHoi)
        {
            Db.ExecProc("sp_XuLyDon",
                Db.P("@MaDon", maDon), Db.P("@MucUuTien", mucUuTien), Db.P("@TrangThai", trangThai),
                Db.P("@PhanHoi", phanHoi), Db.P("@MaNV", MaNV));
            TempData["ThanhCong"] = "Cập nhật đơn thành công. Hệ thống đã gửi thông báo tới sinh viên.";
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // ============ Danh sách + tra cứu + chi tiết đơn đăng ký ============
        public IActionResult DonDangKy(string? tuKhoa, string? trangThai)
        {
            ViewBag.DsPhieu = Db.QueryProc("sp_DsPhieu",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            return View();
        }

        public IActionResult ChiTietDangKy(int id)
        {
            var dt = Db.QueryProc("sp_ChiTietPhieu", Db.P("@MaPhieu", id));
            if (dt.Rows.Count == 0) return RedirectToAction("DonDangKy");
            ViewBag.Phieu = dt.Rows[0];
            return View();
        }

        // Xác nhận hoàn tất nhận phòng (ChoDoiChieu → DangO)
        [HttpPost]
        public IActionResult XacNhanNhanPhong(int maPhieu)
        {
            Db.ExecProc("sp_XacNhanNhanPhong", Db.P("@MaPhieu", maPhieu));
            TempData["ThanhCong"] = "Xác nhận hoàn tất nhận phòng thành công. Đã gửi thông báo cho sinh viên.";
            return RedirectToAction("ChiTietDangKy", new { id = maPhieu });
        }

        // Từ chối / hủy phiếu → trả giường về Trống
        [HttpPost]
        public IActionResult HuyPhieu(int maPhieu)
        {
            Db.ExecProc("sp_HuyPhieu", Db.P("@MaPhieu", maPhieu));
            TempData["ThanhCong"] = "Đã hủy phiếu và trả giường về trạng thái trống.";
            return RedirectToAction("DonDangKy");
        }

        // Xác nhận hoàn tất trả phòng (DangO → DaSuDung)
        [HttpPost]
        public IActionResult XacNhanTraPhong(int maDon)
        {
            Db.ExecProc("sp_XacNhanTraPhong", Db.P("@MaDon", maDon), Db.P("@MaNV", MaNV));
            TempData["ThanhCong"] = "Đã xác nhận trả phòng. Giường đã được giải phóng và sinh viên đã được thông báo.";
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // ============ Danh sách + tra cứu phòng ============
        public IActionResult Phong(string? tuKhoa, string? maToa)
        {
            ViewBag.DsPhong = Db.QueryProc("sp_DsPhongQuanLy",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa));
            ViewBag.DsToa = Db.QueryProc("sp_DsToaQuanLy");
            return View();
        }

        // ============ Danh sách giường + cập nhật trạng thái ============
        public IActionResult Giuong(string maPhong)
        {
            ViewBag.MaPhong = maPhong;
            ViewBag.DsGiuong = Db.QueryProc("sp_DsGiuongChiTietQuanLy", Db.P("@MaPhong", maPhong));
            return View();
        }

        [HttpPost]
        public IActionResult CapNhatGiuong(string maGiuong, string maPhong, string trangThai)
        {
            var dangO = Db.ScalarProc("sp_KiemTraGiuongDangO", Db.P("@MaGiuong", maGiuong));
            if (Convert.ToInt32(dangO) > 0 && trangThai != "DaSuDung")
            {
                TempData["Loi"] = "Giường đang có sinh viên ở/giữ chỗ, không thể đổi trạng thái.";
                return RedirectToAction("Giuong", new { maPhong });
            }

            Db.ExecProc("sp_CapNhatGiuong", Db.P("@MaGiuong", maGiuong), Db.P("@MaPhong", maPhong), Db.P("@TrangThai", trangThai));
            TempData["ThanhCong"] = $"Đã cập nhật trạng thái giường {maGiuong}.";
            return RedirectToAction("Giuong", new { maPhong });
        }

        // ============ Nhập chỉ số điện/nước ============
        public IActionResult ChiSo(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;
            ViewBag.DsPhong = Db.QueryProc("sp_DsPhongChiSo", Db.P("@Thang", thang));
            return View();
        }

        [HttpPost]
        public IActionResult LuuChiSo(string maPhong, string thang, decimal dienDauKy, decimal dienCuoiKy, decimal nuocDauKy, decimal nuocCuoiKy)
        {
            //Chỉ số cuối kỳ >= đầu kỳ, cảnh báo giá trị âm/bất thường
            if (dienCuoiKy < dienDauKy || nuocCuoiKy < nuocDauKy || dienDauKy < 0 || nuocDauKy < 0)
            {
                TempData["Loi"] = $"Phòng {maPhong}: chỉ số cuối kỳ phải lớn hơn hoặc bằng đầu kỳ và không âm.";
                return RedirectToAction("ChiSo", new { thang });
            }

            var daChot = Db.ScalarProc("sp_KiemTraDaChot", Db.P("@MaPhong", maPhong), Db.P("@Thang", thang));
            if (Convert.ToInt32(daChot) > 0)
            {
                TempData["Loi"] = $"Chỉ số phòng {maPhong} tháng {thang} đã chốt (đã xuất hóa đơn), không thể sửa.";
                return RedirectToAction("ChiSo", new { thang });
            }

            Db.ExecProc("sp_LuuChiSo",
                Db.P("@MaPhong", maPhong), Db.P("@Thang", thang),
                Db.P("@DienDauKy", dienDauKy), Db.P("@DienCuoiKy", dienCuoiKy),
                Db.P("@NuocDauKy", nuocDauKy), Db.P("@NuocCuoiKy", nuocCuoiKy));
            TempData["ThanhCong"] = $"Đã lưu chỉ số điện/nước phòng {maPhong} tháng {thang}.";
            return RedirectToAction("ChiSo", new { thang });
        }

        // ============ Tạo hóa đơn ============
        public IActionResult TaoHoaDon(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;
            var donGia = Db.QueryProc("sp_DonGiaHieuLuc");
            ViewBag.DonGia = donGia.Rows.Count > 0 ? donGia.Rows[0] : null;
            ViewBag.DsXemTruoc = Db.QueryProc("sp_NguonTaoHoaDon", Db.P("@Thang", thang));
            ViewBag.DsNhap = Db.QueryProc("sp_DsNhap");
            return View();
        }

        [HttpPost]
        public IActionResult TaoHoaDonThang(string thang, int hanThanhToanNgay = 15)
        {
            var donGia = Db.QueryProc("sp_DonGiaHieuLuc");
            if (donGia.Rows.Count == 0) { TempData["Loi"] = "Chưa cấu hình đơn giá điện/nước hiệu lực."; return RedirectToAction("TaoHoaDon", new { thang }); }
            var dg = donGia.Rows[0];

            var ds = Db.QueryProc("sp_NguonTaoHoaDon", Db.P("@Thang", thang));
            if (ds.Rows.Count == 0) { TempData["Loi"] = "Không có chỉ số nào cần tạo hóa đơn cho tháng này."; return RedirectToAction("TaoHoaDon", new { thang }); }

            int soHD = 0;
            foreach (System.Data.DataRow r in ds.Rows)
            {
                decimal tienDien = Math.Round(Convert.ToDecimal(r["Kwh"]) * Convert.ToDecimal(dg["GiaDien"]));
                decimal tienNuoc = Math.Round(Convert.ToDecimal(r["M3"]) * Convert.ToDecimal(dg["GiaNuoc"]));
                decimal phiDV = Convert.ToDecimal(dg["PhiDichVu"]);
                // Tiền phòng = giá phòng x số người đã ở TRỌN VẸN từ đầu tháng (SoNguoiO đã được
                // sp_NguonTaoHoaDon tính sẵn theo NgayBatDau <= đầu tháng - SV mới dọn vào giữa
                // tháng chưa được tính, bắt đầu tính từ tháng kế tiếp).
                decimal tienPhong = Convert.ToDecimal(r["GiaPhong"]) * Convert.ToInt32(r["SoNguoiO"]) + phiDV;
                decimal tongTien = tienPhong + tienDien + tienNuoc;

                Db.ExecProc("sp_TaoHoaDonDong",
                    Db.P("@MaPhong", r["MaPhong"]), Db.P("@MaChiSo", r["MaChiSo"]), Db.P("@MaDonGia", dg["MaDonGia"]),
                    Db.P("@Thang", thang), Db.P("@TienPhong", tienPhong), Db.P("@TienDien", tienDien),
                    Db.P("@TienNuoc", tienNuoc), Db.P("@TongTien", tongTien),
                    Db.P("@HanThanhToan", DateTime.Today.AddDays(hanThanhToanNgay)));
                soHD++;
            }
            TempData["ThanhCong"] = $"Đã tạo {soHD} hóa đơn (bản nháp) cho tháng {thang}. Hãy kiểm tra và xác nhận gửi.";
            return RedirectToAction("TaoHoaDon", new { thang });
        }

        // ============ Xác nhận gửi hóa đơn ============
        [HttpPost]
        public IActionResult GuiHoaDon(int maHD)
        {
            var hd = Db.QueryProc("sp_ThongTinHoaDonNhap", Db.P("@MaHD", maHD));
            if (hd.Rows.Count == 0) { TempData["Loi"] = "Hóa đơn không hợp lệ."; return RedirectToAction("TaoHoaDon"); }
            var r = hd.Rows[0];

            Db.ExecProc("sp_GuiHoaDon", Db.P("@MaHD", maHD));
            // Gửi thông báo Email/SMS tới từng sinh viên trong phòng, lưu lịch sử
            var dsSV = Db.QueryProc("sp_DsSVTrongPhong", Db.P("@MaPhong", r["MaPhong"]), Db.P("@Thang", r["Thang"]));
            foreach (System.Data.DataRow sv in dsSV.Rows)
            {
                Db.ExecProc("sp_Chung_ThemThongBao",
                    Db.P("@MSSV", sv["MSSV"]), Db.P("@MaHD", maHD),
                    Db.P("@NoiDung", $"Hóa đơn tháng {r["Thang"]} phòng {r["MaPhong"]}: {Convert.ToDecimal(r["TongTien"]):N0}đ. Hạn thanh toán {Convert.ToDateTime(r["HanThanhToan"]):dd/MM/yyyy}."),
                    Db.P("@Kenh", "Email"));
            }
            TempData["ThanhCong"] = $"Đã gửi hóa đơn #{maHD} và thông báo tới {dsSV.Rows.Count} sinh viên trong phòng.";
            return RedirectToAction("TaoHoaDon");
        }

        // ============ Lịch sử thông báo hóa đơn + tra cứu ============
        public IActionResult LichSuThongBao(string? tuKhoa)
        {
            ViewBag.DsTB = Db.QueryProc("sp_DsThongBao", Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()));
            return View();
        }

        // ============ Sinh viên vi phạm + mở khóa tài khoản ============
        public IActionResult ViPham(string? tuKhoa)
        {
            // Job tự động: quét hóa đơn quá hạn - mô phỏng khi mở trang
            QuetHoaDonQuaHan();

            ViewBag.DsViPham = Db.QueryProc("sp_DsViPham", Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()));
            ViewBag.LichSu = Db.QueryProc("sp_LichSuViPham");
            return View();
        }

        /// <summary>Mô phỏng job tự động: khóa TK khi quá hạn >= TS3 ngày; ghi vi phạm khi quá hạn > TS4 ngày.</summary>
        private void QuetHoaDonQuaHan()
        {
            Db.ExecProc("sp_CapNhatHoaDonQuaHan");

            int ts3 = int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS3"))!.ToString()!);
            int ts4 = int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS4"))!.ToString()!);
            int ts6 = int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS6"))!.ToString()!);

            Db.ExecProc("sp_KhoaTaiKhoanQuaHan", Db.P("@TS3", ts3));
            Db.ExecProc("sp_GhiNhanViPham", Db.P("@TS4", ts4), Db.P("@TS6", ts6));
            Db.ExecProc("sp_CapNhatDiemViPham");
        }
        // Xác nhận mở khóa tài khoản
        [HttpPost]
        public IActionResult MoKhoa(int maTK)
        {
            Db.ExecProc("sp_MoKhoa", Db.P("@MaTK", maTK));
            TempData["ThanhCong"] = "Đã mở khóa tài khoản và gửi thông báo.";
            return RedirectToAction("ViPham");
        }
    }
}
