using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    [PhanQuyen("QTV")]
    public class QuanTriController : Controller
    {
        // ============ Tổng quan thống kê ============
        public IActionResult ThongKe()
        {
            // 1 lượt gọi SP -> 4 result set (6 chỉ số, doanh thu, sản lượng, đơn theo trạng thái)
            var ds = Db.QuerySetProc("sp_ThongKe");
            var chiSo = ds.Tables[0].Rows[0];

            ViewBag.TongPhong    = chiSo["TongPhong"];
            ViewBag.SVDangO      = chiSo["SVDangO"];
            ViewBag.HDChuaTT     = chiSo["HDChuaTT"];
            ViewBag.DonChoXuLy   = chiSo["DonChoXuLy"];
            ViewBag.GiuongTrong  = chiSo["GiuongTrong"];
            ViewBag.TongThu      = chiSo["TongThu"];

            ViewBag.DoanhThu  = ds.Tables[1];
            ViewBag.SanLuong  = ds.Tables[2];
            ViewBag.DonTheoTT = ds.Tables[3];

            return View();
        }

        // ============ Danh sách + tra cứu phòng hệ thống ============
        public IActionResult Phong(string? tuKhoa, string? maToa, string? trangThai)
        {
            ViewBag.DsPhong = Db.QueryProc("sp_DsPhongQuanTri",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            ViewBag.DsToa = Db.QueryProc("sp_DsToaChiTietQuanTri");
            return View();
        }

        // ============ Thêm phòng mới ============
        [HttpPost]
        public IActionResult ThemPhong(string maToa, int tang, string loaiPhong, decimal giaPhong)
        {
            int soGiuong = int.Parse(loaiPhong);
            var toa = Db.QueryProc("sp_ThongTinToa", Db.P("@MaToa", maToa));
            if (toa.Rows.Count == 0 || tang < 1 || tang > Convert.ToInt32(toa.Rows[0]["SoTang"]))
            {
                TempData["Loi"] = "Tầng không hợp lệ với tòa nhà đã chọn.";
                return RedirectToAction("Phong");
            }

            // Sinh mã phòng tự động: {Toa}-{Tang}{STT 2 chữ số}
            int stt = 1;
            string maPhong;
            do { maPhong = $"{maToa}-{tang}{stt:D2}"; stt++; }
            while (Convert.ToInt32(Db.ScalarProc("sp_KiemTraTrungMaPhong", Db.P("@MaPhong", maPhong))) > 0);

            Db.ExecProc("sp_ThemPhong",
                Db.P("@MaPhong", maPhong), Db.P("@MaToa", maToa), Db.P("@Tang", tang),
                Db.P("@LoaiPhong", loaiPhong), Db.P("@SoGiuong", soGiuong), Db.P("@GiaPhong", giaPhong));

            for (int i = 1; i <= soGiuong; i++)
                Db.ExecProc("sp_ThemGiuong", Db.P("@MaGiuong", $"{maPhong}-G{i}"), Db.P("@MaPhong", maPhong));

            TempData["ThanhCong"] = $"Đã thêm phòng {maPhong} với {soGiuong} giường.";
            return RedirectToAction("Phong");
        }

        // ============ Cập nhật phòng ============
        [HttpPost]
        public IActionResult CapNhatPhong(string maPhong, decimal giaPhong, string trangThai)
        {
            Db.ExecProc("sp_CapNhatPhong", Db.P("@MaPhong", maPhong), Db.P("@GiaPhong", giaPhong), Db.P("@TrangThai", trangThai));
            TempData["ThanhCong"] = $"Đã cập nhật phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Xóa phòng (chỉ khi không có SV đang ở) ============
        [HttpPost]
        public IActionResult XoaPhong(string maPhong)
        {
            var dangO = Db.ScalarProc("sp_KiemTraDangO", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(dangO) > 0)
            {
                TempData["Loi"] = "Không thể xóa: phòng đang có sinh viên ở hoặc giữ chỗ.";
                return RedirectToAction("Phong");
            }

            var tongLichSu = Db.ScalarProc("sp_KiemTraLichSu", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(tongLichSu) > 0)
            {
                // Có dữ liệu lịch sử → chỉ ngừng sử dụng thay vì xóa vật lý
                Db.ExecProc("sp_NgungSuDung", Db.P("@MaPhong", maPhong));
                TempData["ThanhCong"] = $"Phòng {maPhong} có dữ liệu lịch sử nên đã chuyển sang trạng thái Ngừng sử dụng.";
                return RedirectToAction("Phong");
            }

            Db.ExecProc("sp_XoaPhong", Db.P("@MaPhong", maPhong));
            TempData["ThanhCong"] = $"Đã xóa phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Cập nhật đơn giá điện/nước ============
        public IActionResult DonGia()
        {
            ViewBag.DsDonGia = Db.QueryProc("sp_DsDonGia");
            ViewBag.TS7 = Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS7"))?.ToString();
            ViewBag.TS8 = Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS8"))?.ToString();
            return View();
        }

        [HttpPost]
        public IActionResult CapNhatDonGia(decimal giaDien, decimal giaNuoc, decimal phiDichVu, DateTime ngayApDung)
        {
            //Đơn giá không vượt trần pháp luật (TS7, TS8) - kiểm tra lại ở tầng ứng dụng để hiển thị thông báo rõ ràng
            decimal ts7 = decimal.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS7"))!.ToString()!);
            decimal ts8 = decimal.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS8"))!.ToString()!);
            if (giaDien <= 0 || giaDien > ts7) { TempData["Loi"] = $"Đơn giá điện phải > 0 và không vượt {ts7:N0}đ/kWh."; return RedirectToAction("DonGia"); }
            if (giaNuoc <= 0 || giaNuoc > ts8) { TempData["Loi"] = $"Đơn giá nước phải > 0 và không vượt {ts8:N0}đ/m³."; return RedirectToAction("DonGia"); }

            Db.ExecProc("sp_CapNhatDonGia",
                Db.P("@GiaDien", giaDien), Db.P("@GiaNuoc", giaNuoc), Db.P("@PhiDichVu", phiDichVu), Db.P("@NgayApDung", ngayApDung));
            TempData["ThanhCong"] = "Đã cập nhật biểu giá điện/nước mới.";
            return RedirectToAction("DonGia");
        }

        // ============ Thiết lập thời gian mở cổng đăng ký ============
        public IActionResult DotDangKy()
        {
            // Job tự động cập nhật trạng thái đợt theo thời gian
            Db.ExecProc("sp_CapNhatTrangThaiDot");
            ViewBag.DsDot = Db.QueryProc("sp_DsDot");
            ViewBag.TS1 = Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS1"))?.ToString();
            return View();
        }

        [HttpPost]
        public IActionResult TaoDot(string tenDot, string loaiDot, string hocKy, DateTime ngayMo, DateTime? ngayDong)
        {
            //Đợt ưu tiên tự động đóng sau TS1 ngày
            if (loaiDot == "UuTien")
            {
                int ts1 = int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS1"))!.ToString()!);
                ngayDong = ngayMo.AddDays(ts1);
            }
            if (ngayDong == null || ngayDong <= ngayMo)
            {
                TempData["Loi"] = "Ngày đóng cổng phải sau ngày mở cổng.";
                return RedirectToAction("DotDangKy");
            }

            Db.ExecProc("sp_TaoDot",
                Db.P("@TenDot", tenDot.Trim()), Db.P("@LoaiDot", loaiDot), Db.P("@HocKy", hocKy.Trim()),
                Db.P("@NgayMo", ngayMo), Db.P("@NgayDong", ngayDong));
            TempData["ThanhCong"] = "Đã thiết lập đợt đăng ký mới.";
            return RedirectToAction("DotDangKy");
        }

        [HttpPost]
        public IActionResult DongDot(int maDot)
        {
            Db.ExecProc("sp_DongDot", Db.P("@MaDot", maDot));
            TempData["ThanhCong"] = "Đã đóng cổng đăng ký.";
            return RedirectToAction("DotDangKy");
        }

        // ============ Danh sách + tra cứu tài khoản ============
        public IActionResult TaiKhoan(string? tuKhoa, string? vaiTro, string? trangThai)
        {
            ViewBag.DsTK = Db.QueryProc("sp_DsTaiKhoan",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@VaiTro", string.IsNullOrWhiteSpace(vaiTro) ? null : vaiTro),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            ViewBag.DsToa = Db.QueryProc("sp_DsToaTaiKhoan");
            return View();
        }

        // ============ Tạo tài khoản ============
        [HttpPost]
        public IActionResult TaoTaiKhoan(string tenDangNhap, string matKhau, string email, string? sdt,
                                          string vaiTro, string? hoTen, string? khoaHoc, string? doiTuong, string? maToa)
        {
            var trung = Db.ScalarProc("sp_KiemTraTrung", Db.P("@TenDangNhap", tenDangNhap.Trim()), Db.P("@Email", email.Trim()));
            if (Convert.ToInt32(trung) > 0)
            {
                TempData["Loi"] = "Tên đăng nhập hoặc email đã tồn tại.";
                return RedirectToAction("TaiKhoan");
            }

            var maTKObj = Db.ScalarProc("sp_TaoTaiKhoan",
                Db.P("@TenDangNhap", tenDangNhap.Trim()), Db.P("@MatKhau", AuthHelper.Sha256(matKhau)),
                Db.P("@Email", email.Trim()), Db.P("@SDT", sdt), Db.P("@VaiTro", vaiTro));
            int maTK = Convert.ToInt32(maTKObj);

            if (vaiTro == "SV")
                Db.ExecProc("sp_ThemSinhVien",
                    Db.P("@MSSV", tenDangNhap.Trim()), Db.P("@MaTK", maTK), Db.P("@HoTen", hoTen ?? tenDangNhap),
                    Db.P("@KhoaHoc", khoaHoc ?? "K" + DateTime.Now.Year), Db.P("@DoiTuong", doiTuong ?? "BinhThuong"));
            else if (vaiTro == "QL")
            {
                int stt = Convert.ToInt32(Db.ScalarProc("sp_DemQuanLy")) + 1;
                Db.ExecProc("sp_ThemQuanLy",
                    Db.P("@MaNV", $"NV{stt:D3}"), Db.P("@MaTK", maTK), Db.P("@HoTen", hoTen ?? tenDangNhap), Db.P("@MaToa", maToa));
            }

            TempData["ThanhCong"] = $"Đã tạo tài khoản {tenDangNhap} ({vaiTro}).";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Cập nhật tài khoản (đổi email, SĐT, khóa/mở, reset mật khẩu) ============
        [HttpPost]
        [HttpPost]
        public IActionResult CapNhatTaiKhoan(int maTK, string vaiTro, string email, string? sdt,
                              string hoTen, string? khoaHoc, string? doiTuong, string? maToa, string? matKhauMoi)
        {
            Db.ExecProc("sp_CapNhatTaiKhoan", Db.P("@MaTK", maTK), Db.P("@Email", email.Trim()), Db.P("@SDT", sdt));

            Db.ExecProc("sp_CapNhatThongTinCaNhan",
                Db.P("@MaTK", maTK), Db.P("@VaiTro", vaiTro), Db.P("@HoTen", hoTen.Trim()),
                Db.P("@KhoaHoc", khoaHoc), Db.P("@DoiTuong", doiTuong), Db.P("@MaToa", maToa));

            if (!string.IsNullOrWhiteSpace(matKhauMoi))
                Db.ExecProc("sp_DoiMatKhau", Db.P("@MaTK", maTK), Db.P("@MatKhauMoi", AuthHelper.Sha256(matKhauMoi)));

            TempData["ThanhCong"] = "Đã cập nhật tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Xóa tài khoản ============
        [HttpPost]
        public IActionResult XoaTaiKhoan(int maTK)
        {
            // Không xóa tài khoản còn dữ liệu ràng buộc (hợp đồng, hóa đơn, đơn từ...)
            var mssv = Db.ScalarProc("sp_LayMSSV", Db.P("@MaTK", maTK))?.ToString();
            if (mssv != null)
            {
                var lienQuan = Db.ScalarProc("sp_DemLienQuanSV", Db.P("@MSSV", mssv));
                if (Convert.ToInt32(lienQuan) > 0)
                {
                    Db.ExecProc("sp_KhoaTaiKhoan", Db.P("@MaTK", maTK));
                    TempData["Loi"] = "Tài khoản có dữ liệu liên quan nên không thể xóa - đã chuyển sang trạng thái Bị khóa.";
                    return RedirectToAction("TaiKhoan");
                }
                Db.ExecProc("sp_XoaSinhVien", Db.P("@MaTK", maTK));
            }
            var maNV = Db.ScalarProc("sp_LayMaNV", Db.P("@MaTK", maTK))?.ToString();
            if (maNV != null)
            {
                var coDon = Db.ScalarProc("sp_DemLienQuanQL", Db.P("@MaNV", maNV));
                if (Convert.ToInt32(coDon) > 0)
                {
                    Db.ExecProc("sp_KhoaTaiKhoan", Db.P("@MaTK", maTK));
                    TempData["Loi"] = "Quản lý đang phụ trách đơn từ nên không thể xóa - đã khóa tài khoản.";
                    return RedirectToAction("TaiKhoan");
                }
                Db.ExecProc("sp_XoaQuanLy", Db.P("@MaTK", maTK));
            }

            Db.ExecProc("sp_XoaTaiKhoan", Db.P("@MaTK", maTK));
            TempData["ThanhCong"] = "Đã xóa tài khoản.";
            return RedirectToAction("TaiKhoan");
        }
    }
}
