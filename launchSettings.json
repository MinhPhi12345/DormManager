using DormManager.Data;
using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    [PhanQuyen("QTV")]
    public class QuanTriController : Controller
    {
        private int? MaTK => HttpContext.Session.GetInt32("MaTK");
        private string? HoTen => HttpContext.Session.GetString("HoTen");

        /// <summary>Ghi 1 dòng nhật ký thao tác hệ thống (sp_Chung_ThemNhatKy) cho hành động hiện tại của QTV.</summary>
        private void GhiNhatKy(string hanhDong, string doiTuong, string noiDung)
            => CommonRepo.ThemNhatKy(MaTK, HoTen, "QTV", hanhDong, doiTuong, noiDung);

        // ============ Tổng quan thống kê ============
        public IActionResult ThongKe()
        {
            // sp_ThongKe: 1 lượt gọi -> 4 result set
            var ds = QuanTriRepo.ThongKe();
            var chiSo = ds.Tables[0].Rows[0];

            ViewBag.TongPhong = chiSo["TongPhong"];
            ViewBag.SVDangO = chiSo["SVDangO"];
            ViewBag.HDChuaTT = chiSo["HDChuaTT"];
            ViewBag.DonChoXuLy = chiSo["DonChoXuLy"];
            ViewBag.GiuongTrong = chiSo["GiuongTrong"];
            ViewBag.TongThu = chiSo["TongThu"];

            ViewBag.DoanhThu = ds.Tables[1];
            ViewBag.SanLuong = ds.Tables[2];
            ViewBag.DonTheoTT = ds.Tables[3];

            // % tăng/giảm doanh thu tháng gần nhất so với tháng liền trước
            var dt = ds.Tables[1];
            decimal? phanTramDoanhThu = null;
            if (dt.Rows.Count >= 2)
            {
                decimal thangNay = Convert.ToDecimal(dt.Rows[dt.Rows.Count - 1]["TongTien"]);
                decimal thangTruoc = Convert.ToDecimal(dt.Rows[dt.Rows.Count - 2]["TongTien"]);
                if (thangTruoc > 0) phanTramDoanhThu = Math.Round((thangNay - thangTruoc) / thangTruoc * 100, 1);
            }
            ViewBag.PhanTramDoanhThu = phanTramDoanhThu;

            // CẬP NHẬT: Tính tổng các khoản thu của toàn bộ thời gian (hoặc tháng mới nhất) để vẽ biểu đồ tròn
            decimal tongTienPhong = 0, tongTienDien = 0, tongTienNuoc = 0;
            foreach (System.Data.DataRow r in dt.Rows)
            {
                // Ở đây ta cộng dồn tất cả các tháng. 
                // Nếu muốn chỉ lấy tháng mới nhất, bạn có thể chỉ lấy dòng dt.Rows[dt.Rows.Count - 1]
                tongTienPhong += r["TongTienPhong"] != DBNull.Value ? Convert.ToDecimal(r["TongTienPhong"]) : 0;
                tongTienDien += r["TongTienDien"] != DBNull.Value ? Convert.ToDecimal(r["TongTienDien"]) : 0;
                tongTienNuoc += r["TongTienNuoc"] != DBNull.Value ? Convert.ToDecimal(r["TongTienNuoc"]) : 0;
            }

            // Truyền 3 mảng giá trị ra View
            ViewBag.TyTrongDoanhThu = new decimal[] { tongTienPhong, tongTienDien, tongTienNuoc };

            return View();
        }

        // ============ Danh sách + tra cứu hóa đơn toàn hệ thống (dành cho QTV) ============
        public IActionResult HoaDon(string? tuKhoa, string? thang, string? trangThai)
        {
            ViewBag.DsHoaDon = Db.Query(@"SELECT h.MaHD, h.MaPhong, h.Thang, h.TienPhong, h.TienDien, h.TienNuoc,
           h.TongTien, h.NgayPhatHanh, h.HanThanhToan, h.TrangThai, t.TenToa
    FROM HOADON h
    JOIN PHONG p ON p.MaPhong = h.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE h.TrangThai <> 'Nhap'
      AND (@TuKhoa IS NULL OR h.MaPhong LIKE '%' + @TuKhoa + '%' OR t.TenToa LIKE '%' + @TuKhoa + '%')
      AND (@Thang IS NULL OR h.Thang = @Thang)
      AND (@TrangThai IS NULL OR h.TrangThai = @TrangThai)
    ORDER BY h.MaHD DESC;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@Thang", string.IsNullOrWhiteSpace(thang) ? null : thang),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            return View();
        }

        // ============ Danh sách + tra cứu phòng hệ thống ============
        public IActionResult Phong(string? tuKhoa, string? maToa, string? trangThai)
        {
            ViewBag.DsPhong = Db.Query(@"SELECT p.*, t.TenToa,
           (SELECT COUNT(*) FROM GIUONG g JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
            WHERE g.MaPhong = p.MaPhong AND pd.TrangThai IN ('DangO','ChoDoiChieu')) AS SoSVO
    FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
      AND (@TrangThai IS NULL OR p.TrangThai = @TrangThai)
    ORDER BY p.MaToa, p.Tang, p.MaPhong;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa, SoTang FROM TOANHA ORDER BY MaToa;");
            return View();
        }

        // ============ Thêm phòng mới ============
        [HttpPost]
        public IActionResult ThemPhong(string maToa, int tang, string loaiPhong, decimal giaPhong)
        {
            int soGiuong = int.Parse(loaiPhong);
            var toa = Db.Query(@"SELECT SoTang FROM TOANHA WHERE MaToa = @MaToa;", Db.P("@MaToa", maToa));
            if (toa.Rows.Count == 0 || tang < 1 || tang > Convert.ToInt32(toa.Rows[0]["SoTang"]))
            {
                TempData["Loi"] = "Tầng không hợp lệ với tòa nhà đã chọn.";
                return RedirectToAction("Phong");
            }

            // Sinh mã phòng tự động: {Toa}-{Tang}{STT 2 chữ số}
            int stt = 1;
            string maPhong;
            do { maPhong = $"{maToa}-{tang}{stt:D2}"; stt++; }
            while (Convert.ToInt32(Db.Scalar(@"SELECT COUNT(*) FROM PHONG WHERE MaPhong = @MaPhong;", Db.P("@MaPhong", maPhong))) > 0);

            Db.Exec(@"INSERT INTO PHONG (MaPhong, MaToa, Tang, LoaiPhong, SoGiuong, GiaPhong, TrangThai, SoGiuongTrong)
    VALUES (@MaPhong, @MaToa, @Tang, @LoaiPhong, @SoGiuong, @GiaPhong, 'HoatDong', @SoGiuong);",
                Db.P("@MaPhong", maPhong), Db.P("@MaToa", maToa), Db.P("@Tang", tang),
                Db.P("@LoaiPhong", loaiPhong), Db.P("@SoGiuong", soGiuong), Db.P("@GiaPhong", giaPhong));

            for (int i = 1; i <= soGiuong; i++)
                Db.Exec(@"INSERT INTO GIUONG (MaGiuong, MaPhong) VALUES (@MaGiuong, @MaPhong);", Db.P("@MaGiuong", $"{maPhong}-G{i}"), Db.P("@MaPhong", maPhong));

            GhiNhatKy("Them", "Phòng", $"Thêm phòng {maPhong} ({soGiuong} giường) vào tòa {maToa}, tầng {tang}, giá {giaPhong:N0}đ.");
            TempData["ThanhCong"] = $"Đã thêm phòng {maPhong} với {soGiuong} giường.";
            return RedirectToAction("Phong");
        }

        // ============ Cập nhật phòng ============
        [HttpPost]
        public IActionResult CapNhatPhong(string maPhong, decimal giaPhong, string trangThai)
        {
            Db.Exec(@"UPDATE PHONG SET GiaPhong = @GiaPhong, TrangThai = @TrangThai WHERE MaPhong = @MaPhong;", Db.P("@MaPhong", maPhong), Db.P("@GiaPhong", giaPhong), Db.P("@TrangThai", trangThai));
            GhiNhatKy("Sua", "Phòng", $"Cập nhật phòng {maPhong}: giá {giaPhong:N0}đ, trạng thái {trangThai}.");
            TempData["ThanhCong"] = $"Đã cập nhật phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Xóa phòng (chỉ khi không có SV đang ở) ============
        [HttpPost]
        public IActionResult XoaPhong(string maPhong)
        {
            var dangO = Db.Scalar(@"SELECT COUNT(*) FROM GIUONG g
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai IN ('DangO','ChoDoiChieu');", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(dangO) > 0)
            {
                TempData["Loi"] = "Không thể xóa: phòng đang có sinh viên ở hoặc giữ chỗ.";
                return RedirectToAction("Phong");
            }

            var tongLichSu = Db.Scalar(@"SELECT
        (SELECT COUNT(*) FROM HOADON WHERE MaPhong = @MaPhong) +
        (SELECT COUNT(*) FROM CHISODIENNUOC WHERE MaPhong = @MaPhong) +
        (SELECT COUNT(*) FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong WHERE g.MaPhong = @MaPhong)
        AS TongLichSu;", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(tongLichSu) > 0)
            {
                // Có dữ liệu lịch sử → chỉ ngừng sử dụng thay vì xóa vật lý
                Db.Exec(@"UPDATE PHONG SET TrangThai = 'NgungSuDung' WHERE MaPhong = @MaPhong;", Db.P("@MaPhong", maPhong));
                GhiNhatKy("Sua", "Phòng", $"Phòng {maPhong} có dữ liệu lịch sử nên chuyển sang Ngừng sử dụng (thay vì xóa).");
                TempData["ThanhCong"] = $"Phòng {maPhong} có dữ liệu lịch sử nên đã chuyển sang trạng thái Ngừng sử dụng.";
                return RedirectToAction("Phong");
            }

            QuanTriRepo.XoaPhong(maPhong);   // sp_XoaPhong (transaction xóa giường + phòng)
            GhiNhatKy("Xoa", "Phòng", $"Xóa phòng {maPhong}.");
            TempData["ThanhCong"] = $"Đã xóa phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Cập nhật đơn giá điện/nước ============
        public IActionResult DonGia()
        {
            ViewBag.DsDonGia = Db.Query(@"SELECT * FROM DONGIA ORDER BY NgayApDung DESC;");
            ViewBag.TS7 = CommonRepo.LayThamSo("TS7");
            ViewBag.TS8 = CommonRepo.LayThamSo("TS8");
            return View();
        }

        [HttpPost]
        public IActionResult CapNhatDonGia(decimal giaDien, decimal giaNuoc, decimal phiDichVu, DateTime ngayApDung)
        {
            //Đơn giá không vượt trần pháp luật (TS7, TS8) - kiểm tra lại ở tầng ứng dụng để hiển thị thông báo rõ ràng
            decimal ts7 = decimal.Parse(CommonRepo.LayThamSo("TS7")!);
            decimal ts8 = decimal.Parse(CommonRepo.LayThamSo("TS8")!);
            if (giaDien <= 0 || giaDien > ts7) { TempData["Loi"] = $"Đơn giá điện phải > 0 và không vượt {ts7:N0}đ/kWh."; return RedirectToAction("DonGia"); }
            if (giaNuoc <= 0 || giaNuoc > ts8) { TempData["Loi"] = $"Đơn giá nước phải > 0 và không vượt {ts8:N0}đ/m³."; return RedirectToAction("DonGia"); }

            QuanTriRepo.CapNhatDonGia(giaDien, giaNuoc, phiDichVu, ngayApDung);   // sp_CapNhatDonGia (transaction hết hiệu lực cũ + thêm mới)
            GhiNhatKy("Sua", "Đơn giá điện/nước",
                $"Cập nhật biểu giá mới: điện {giaDien:N0}đ/kWh, nước {giaNuoc:N0}đ/m³, phí dịch vụ {phiDichVu:N0}đ, áp dụng từ {ngayApDung:dd/MM/yyyy}.");
            TempData["ThanhCong"] = "Đã cập nhật biểu giá điện/nước mới.";
            return RedirectToAction("DonGia");
        }

        // ============ Thiết lập thời gian mở cổng đăng ký ============
        public IActionResult DotDangKy()
        {
            QuanTriRepo.CapNhatTrangThaiDot();   // sp_CapNhatTrangThaiDot (cập nhật trạng thái đợt theo thời gian)
            ViewBag.DsDot = Db.Query(@"SELECT * FROM DOTDANGKY ORDER BY NgayMo DESC;");
            ViewBag.TS1 = CommonRepo.LayThamSo("TS1");
            return View();
        }

        [HttpPost]
        public IActionResult TaoDot(string tenDot, string loaiDot, string hocKy, DateTime ngayMo, DateTime? ngayDong)
        {
            //Đợt ưu tiên tự động đóng sau TS1 ngày
            if (loaiDot == "UuTien")
                ngayDong = ngayMo.AddDays(CommonRepo.LayThamSoInt("TS1"));

            if (ngayDong == null || ngayDong <= ngayMo)
            {
                TempData["Loi"] = "Ngày đóng cổng phải sau ngày mở cổng.";
                return RedirectToAction("DotDangKy");
            }

            Db.Exec(@"INSERT INTO DOTDANGKY (TenDot, LoaiDot, HocKy, NgayMo, NgayDong, TrangThai)
    VALUES (@TenDot, @LoaiDot, @HocKy, @NgayMo, @NgayDong,
            CASE WHEN GETDATE() < @NgayMo THEN 'ChuaMo'
                 WHEN GETDATE() BETWEEN @NgayMo AND @NgayDong THEN 'DangMo' ELSE 'DaDong' END);",
                Db.P("@TenDot", tenDot.Trim()), Db.P("@LoaiDot", loaiDot), Db.P("@HocKy", hocKy.Trim()),
                Db.P("@NgayMo", ngayMo), Db.P("@NgayDong", ngayDong));
            TempData["ThanhCong"] = "Đã thiết lập đợt đăng ký mới.";
            return RedirectToAction("DotDangKy");
        }

        [HttpPost]
        public IActionResult DongDot(int maDot)
        {
            Db.Exec(@"UPDATE DOTDANGKY SET NgayDong = GETDATE(), TrangThai = 'DaDong' WHERE MaDot = @MaDot;", Db.P("@MaDot", maDot));
            TempData["ThanhCong"] = "Đã đóng cổng đăng ký.";
            return RedirectToAction("DotDangKy");
        }

        // ============ Danh sách + tra cứu tài khoản ============
        public IActionResult TaiKhoan(string? tuKhoa, string? vaiTro, string? trangThai)
        {
            ViewBag.DsTK = Db.Query(@"SELECT tk.MaTK, tk.TenDangNhap, tk.Email, tk.SDT, tk.VaiTro, tk.TrangThai, tk.NgayTao,
           COALESCE(sv.HoTen, ql.HoTen, N'Quản trị viên') AS HoTen,
           sv.MSSV, sv.KhoaHoc, sv.DoiTuong,
           ql.MaNV, ql.MaToa
    FROM TAIKHOAN tk
    LEFT JOIN SINHVIEN sv ON sv.MaTK = tk.MaTK
    LEFT JOIN QUANLY ql ON ql.MaTK = tk.MaTK
    WHERE (@TuKhoa IS NULL OR tk.TenDangNhap LIKE '%' + @TuKhoa + '%' OR tk.Email LIKE '%' + @TuKhoa + '%'
           OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR ql.HoTen LIKE '%' + @TuKhoa + '%')
      AND (@VaiTro IS NULL OR tk.VaiTro = @VaiTro)
      AND (@TrangThai IS NULL OR tk.TrangThai = @TrangThai)
    ORDER BY tk.MaTK;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@VaiTro", string.IsNullOrWhiteSpace(vaiTro) ? null : vaiTro),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;");
            return View();
        }

        // ============ Tạo tài khoản ============
        [HttpPost]
        public IActionResult TaoTaiKhoan(string tenDangNhap, string matKhau, string email, string? sdt,
                                          string vaiTro, string? hoTen, string? khoaHoc, string? doiTuong, string? maToa)
        {
            var trung = Db.Scalar(@"SELECT COUNT(*) FROM TAIKHOAN WHERE TenDangNhap = @TenDangNhap OR Email = @Email;", Db.P("@TenDangNhap", tenDangNhap.Trim()), Db.P("@Email", email.Trim()));
            if (Convert.ToInt32(trung) > 0)
            {
                TempData["Loi"] = "Tên đăng nhập hoặc email đã tồn tại.";
                return RedirectToAction("TaiKhoan");
            }

            // sp_TaoTaiKhoan: INSERT + trả về MaTK qua SCOPE_IDENTITY
            int maTK = QuanTriRepo.TaoTaiKhoan(tenDangNhap.Trim(), AuthHelper.Sha256(matKhau), email.Trim(), sdt, vaiTro);

            if (vaiTro == "SV")
                Db.Exec(@"INSERT INTO SINHVIEN (MSSV, MaTK, HoTen, KhoaHoc, DoiTuong) VALUES (@MSSV, @MaTK, @HoTen, @KhoaHoc, @DoiTuong);",
                    Db.P("@MSSV", tenDangNhap.Trim()), Db.P("@MaTK", maTK), Db.P("@HoTen", hoTen ?? tenDangNhap),
                    Db.P("@KhoaHoc", khoaHoc ?? "K" + DateTime.Now.Year), Db.P("@DoiTuong", doiTuong ?? "BinhThuong"));
            else if (vaiTro == "QL")
            {
                int stt = Convert.ToInt32(Db.Scalar(@"SELECT COUNT(*) FROM QUANLY;")) + 1;
                Db.Exec(@"INSERT INTO QUANLY (MaNV, MaTK, HoTen, MaToa) VALUES (@MaNV, @MaTK, @HoTen, @MaToa);",
                    Db.P("@MaNV", $"NV{stt:D3}"), Db.P("@MaTK", maTK), Db.P("@HoTen", hoTen ?? tenDangNhap), Db.P("@MaToa", maToa));
            }

            GhiNhatKy("Them", "Tài khoản", $"Tạo tài khoản {tenDangNhap} (vai trò {vaiTro}).");
            TempData["ThanhCong"] = $"Đã tạo tài khoản {tenDangNhap} ({vaiTro}).";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Cập nhật tài khoản (đổi email, SĐT, khóa/mở, reset mật khẩu) ============
        [HttpPost]
        public IActionResult CapNhatTaiKhoan(int maTK, string vaiTro, string email, string? sdt,
                              string hoTen, string? khoaHoc, string? doiTuong, string? maToa, string? matKhauMoi)
        {
            Db.Exec(@"UPDATE TAIKHOAN SET Email = @Email, SDT = @SDT WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK), Db.P("@Email", email.Trim()), Db.P("@SDT", sdt));

            // sp_CapNhatThongTinCaNhan: cập nhật hồ sơ SV/QL theo vai trò
            QuanTriRepo.CapNhatThongTinCaNhan(maTK, vaiTro, hoTen.Trim(), khoaHoc, doiTuong, maToa);

            if (!string.IsNullOrWhiteSpace(matKhauMoi))
                Db.Exec(@"UPDATE TAIKHOAN SET MatKhau = @MatKhauMoi WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK), Db.P("@MatKhauMoi", AuthHelper.Sha256(matKhauMoi)));

            GhiNhatKy("Sua", "Tài khoản", $"Cập nhật tài khoản #{maTK} ({hoTen.Trim()})." + (string.IsNullOrWhiteSpace(matKhauMoi) ? "" : " Đã đặt lại mật khẩu."));
            TempData["ThanhCong"] = "Đã cập nhật tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Xóa tài khoản ============
        [HttpPost]
        public IActionResult XoaTaiKhoan(int maTK)
        {
            // Không xóa tài khoản còn dữ liệu ràng buộc (hợp đồng, hóa đơn, đơn từ...)
            var mssv = Db.Scalar(@"SELECT MSSV FROM SINHVIEN WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK))?.ToString();
            if (mssv != null)
            {
                var lienQuan = Db.Scalar(@"SELECT (SELECT COUNT(*) FROM PHIEUDANGKY WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM DONYEUCAU WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM THANHTOAN WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM THONGBAO WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM VIPHAM WHERE MSSV = @MSSV) AS TongLienQuan;", Db.P("@MSSV", mssv));
                if (Convert.ToInt32(lienQuan) > 0)
                {
                    Db.Exec(@"UPDATE TAIKHOAN SET TrangThai = 'BiKhoa' WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
                    GhiNhatKy("Sua", "Tài khoản", $"Tài khoản #{maTK} (SV {mssv}) có dữ liệu liên quan nên khóa thay vì xóa.");
                    TempData["Loi"] = "Tài khoản có dữ liệu liên quan nên không thể xóa - đã chuyển sang trạng thái Bị khóa.";
                    return RedirectToAction("TaiKhoan");
                }
                Db.Exec(@"DELETE FROM SINHVIEN WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
            }
            var maNV = Db.Scalar(@"SELECT MaNV FROM QUANLY WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK))?.ToString();
            if (maNV != null)
            {
                var coDon = Db.Scalar(@"SELECT COUNT(*) FROM DONYEUCAU WHERE MaNV = @MaNV;", Db.P("@MaNV", maNV));
                if (Convert.ToInt32(coDon) > 0)
                {
                    Db.Exec(@"UPDATE TAIKHOAN SET TrangThai = 'BiKhoa' WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
                    GhiNhatKy("Sua", "Tài khoản", $"Tài khoản #{maTK} (QL {maNV}) đang phụ trách đơn từ nên khóa thay vì xóa.");
                    TempData["Loi"] = "Quản lý đang phụ trách đơn từ nên không thể xóa - đã khóa tài khoản.";
                    return RedirectToAction("TaiKhoan");
                }
                Db.Exec(@"DELETE FROM QUANLY WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
            }

            Db.Exec(@"DELETE FROM TAIKHOAN WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
            GhiNhatKy("Xoa", "Tài khoản", $"Xóa tài khoản #{maTK}.");
            TempData["ThanhCong"] = "Đã xóa tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Báo cáo công nợ sinh viên ============
        public IActionResult BaoCaoCongNo()
        {
            var ds = QuanTriRepo.BaoCaoCongNo();   // sp_BaoCaoCongNo
            ViewBag.DsCongNo = ds;

            decimal tongNo = 0;
            foreach (System.Data.DataRow r in ds.Rows) tongNo += Convert.ToDecimal(r["TongNo"]);
            ViewBag.TongNoHeThong = tongNo;
            ViewBag.SoSVNo = ds.Rows.Count;
            return View();
        }

        // ============ Nhật ký thao tác hệ thống (audit log) ============
        public IActionResult NhatKy(string? hanhDong, string? doiTuong)
        {
            ViewBag.DsNhatKy = Db.Query(@"SELECT MaNhatKy, HoTenNguoiThucHien, VaiTro, HanhDong, DoiTuong, NoiDung, ThoiGian
    FROM NHATKY
    WHERE (@HanhDong IS NULL OR HanhDong = @HanhDong)
      AND (@DoiTuong IS NULL OR DoiTuong = @DoiTuong)
    ORDER BY ThoiGian DESC;",
                Db.P("@HanhDong", string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong),
                Db.P("@DoiTuong", string.IsNullOrWhiteSpace(doiTuong) ? null : doiTuong));
            ViewBag.DsDoiTuong = Db.Query(@"SELECT DISTINCT DoiTuong FROM NHATKY ORDER BY DoiTuong;");
            return View();
        }
    }
}