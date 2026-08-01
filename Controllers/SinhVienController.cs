using DormManager.Data;
using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    [PhanQuyen("SV")]
    public class SinhVienController : Controller
    {
        private string MSSV => HttpContext.Session.GetString("MSSV")!;
        // QD05: SV quá hạn thanh toán bị khóa quyền đăng ký dịch vụ tiện ích phát sinh (đăng ký/gia hạn),
        // nhưng vẫn đăng nhập và thanh toán được để tự gỡ khóa.
        private bool BiKhoa => HttpContext.Session.GetString("BiKhoa") == "1";

        // ============ Thông tin tổng quan ============
        public IActionResult TongQuan()
        {
            // sp_TongQuan: 1 lượt gọi -> 4 result set (Phòng đang ở, Bạn cùng phòng, Hóa đơn chưa TT, Đơn gần đây)
            var ds = SinhVienRepo.TongQuan(MSSV);
            ViewBag.PhongDangO = ds.Tables[0];
            ViewBag.BanCungPhong = ds.Tables[1];
            ViewBag.HoaDonChuaTT = ds.Tables[2];
            ViewBag.DonGanDay = ds.Tables[3];
            ViewBag.DsDotMo = SinhVienRepo.DotDangMoChoSV(MSSV);
            return View();
        }

        // ============ Tra cứu phòng ============
        public IActionResult TraCuuPhong(string? tuKhoa, string? maToa, string? loaiPhong, string? mucGia)
        {
            // SV diện chính sách chỉ được xem phòng tiêu chuẩn 6-8 giường
            var doiTuong = Db.Scalar(@"SELECT DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV))?.ToString();
            bool laChinhSach = doiTuong == "ChinhSach";

            ViewBag.DsPhong = Db.Query(@"SELECT p.MaPhong, p.Tang, p.LoaiPhong, p.SoGiuong, p.SoGiuongTrong,
           p.GiaPhong, p.TrangThai, t.TenToa, t.MaToa
    FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE p.TrangThai = 'HoatDong'
      AND (@ChiTieuChuan = 0 OR p.LoaiPhong IN ('6','8'))
      AND (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%' OR t.TenToa LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
      AND (@LoaiPhong IS NULL OR p.LoaiPhong = @LoaiPhong)
      AND (@MucGia IS NULL
           OR (@MucGia = 'duoi400'    AND p.GiaPhong < 400000)
           OR (@MucGia = '400den600' AND p.GiaPhong BETWEEN 400000 AND 600000)
           OR (@MucGia = 'tren600'   AND p.GiaPhong > 600000))
    ORDER BY t.MaToa, p.Tang, p.MaPhong;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa),
                Db.P("@LoaiPhong", string.IsNullOrWhiteSpace(loaiPhong) ? null : loaiPhong),
                Db.P("@MucGia", string.IsNullOrWhiteSpace(mucGia) ? null : mucGia),
                Db.P("@ChiTieuChuan", laChinhSach));

            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;");
            ViewBag.LaChinhSach = laChinhSach;
            ViewBag.DsDotMo = SinhVienRepo.DotDangMoChoSV(MSSV);
            return View();
        }

        // ============ Chi tiết thông tin phòng ============
        public IActionResult ChiTietPhong(string id)
        {
            var phong = Db.Query(@"SELECT p.*, t.TenToa, t.DiaChi FROM PHONG p
    JOIN TOANHA t ON t.MaToa = p.MaToa WHERE p.MaPhong = @MaPhong;", Db.P("@MaPhong", id));
            if (phong.Rows.Count == 0) return RedirectToAction("TraCuuPhong");

            ViewBag.Phong = phong.Rows[0];
            ViewBag.DsGiuong = Db.Query(@"SELECT MaGiuong, TrangThai FROM GIUONG WHERE MaPhong = @MaPhong ORDER BY MaGiuong;", Db.P("@MaPhong", id));
            ViewBag.ThanhVien = Db.Query(@"SELECT sv.HoTen, sv.MSSV, sv.KhoaHoc FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai = 'DangO';", Db.P("@MaPhong", id));
            ViewBag.DsAnh = Db.Query(@"SELECT DuongDan FROM ANHPHONG WHERE MaPhong = @MaPhong ORDER BY ThuTu, MaAnh;", Db.P("@MaPhong", id));

            return View();
        }

        // ============ Đăng ký ở KTX / Đăng ký trước / Học kỳ hè ============
        [HttpGet]
        public IActionResult DangKy(string maGiuong)
        {
            var (loi, giuong, dot) = KiemTraDangKy(maGiuong);
            if (giuong == null) { TempData["Loi"] = loi; return RedirectToAction("TraCuuPhong"); }
            ViewBag.Loi = loi;
            ViewBag.Giuong = giuong;
            ViewBag.Dot = dot;
            return View();
        }

        [HttpPost]
        public IActionResult XacNhanDangKy(string maGiuong)
        {
            if (BiKhoa)
            {
                TempData["Loi"] = "Tài khoản đang bị khóa quyền đăng ký do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.";
                return RedirectToAction("TraCuuPhong");
            }

            var (loi, giuong, dot) = KiemTraDangKy(maGiuong);
            if (loi != null || giuong == null || dot == null)
            {
                TempData["Loi"] = loi ?? "Không thể đăng ký giường này.";
                return RedirectToAction("TraCuuPhong");
            }

            // Thời hạn hợp đồng: KyHe = TS10 tháng, còn lại tối đa TS2 tháng
            int soThang = dot["LoaiDot"].ToString() == "KyHe"
                ? CommonRepo.LayThamSoInt("TS10")
                : CommonRepo.LayThamSoInt("TS2");

            var batDau = DateTime.Today;
            SinhVienRepo.XacNhanDangKy(MSSV, maGiuong, dot["MaDot"], batDau, batDau.AddMonths(soThang));

            TempData["ThanhCong"] = $"Đăng ký giường {maGiuong} thành công! Vui lòng đến văn phòng quản lý tòa nhà để đối chiếu giấy tờ (thẻ sinh viên) và hoàn tất nhận phòng.";
            return RedirectToAction("HopDong");
        }

        /// <summary>Kiểm tra điều kiện đăng ký: đợt mở, ưu tiên tân SV, chính sách, giường trống.</summary>
        private (string? loi, System.Data.DataRow? giuong, System.Data.DataRow? dot) KiemTraDangKy(string maGiuong)
        {
            var g = Db.Query(@"SELECT g.MaGiuong, g.TrangThai, p.MaPhong, p.LoaiPhong, p.GiaPhong, p.Tang, t.TenToa
    FROM GIUONG g JOIN PHONG p ON p.MaPhong = g.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE g.MaGiuong = @MaGiuong AND p.TrangThai = 'HoatDong';", Db.P("@MaGiuong", maGiuong));
            if (g.Rows.Count == 0) return ("Giường không tồn tại hoặc phòng ngừng hoạt động.", null, null);
            var giuong = g.Rows[0];

            if (giuong["TrangThai"].ToString() != "Trong")
                return ("Giường này đã có người đăng ký. Vui lòng chọn giường khác.", giuong, null);

            // Đã có hợp đồng hiệu lực?
            var daCo = Db.Scalar(@"SELECT COUNT(*) FROM PHIEUDANGKY
    WHERE MSSV = @MSSV AND TrangThai IN ('ChoDoiChieu','DangO');", Db.P("@MSSV", MSSV));
            if (Convert.ToInt32(daCo) > 0)
                return ("Bạn đang có hợp đồng lưu trú hiệu lực, không thể đăng ký thêm.", giuong, null);

            // Đợt đăng ký đang mở
            var dot = Db.Query(@"SELECT TOP 1 * FROM DOTDANGKY
    WHERE GETDATE() BETWEEN NgayMo AND NgayDong
    ORDER BY CASE LoaiDot WHEN 'UuTien' THEN 0 ELSE 1 END;");
            if (dot.Rows.Count == 0)
                return ("Hiện chưa có đợt đăng ký nào đang mở cổng. Vui lòng quay lại sau.", giuong, null);
            var d = dot.Rows[0];

            // Đợt ưu tiên: chỉ tân sinh viên (khóa mới nhất)
            if (d["LoaiDot"].ToString() == "UuTien")
            {
                var ttSV = Db.Query(@"SELECT KhoaHoc, DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV));
                var khoa = ttSV.Rows.Count > 0 ? ttSV.Rows[0]["KhoaHoc"].ToString() : null;
                var khoaMoiNhat = Db.Scalar(@"SELECT MAX(KhoaHoc) FROM SINHVIEN;")?.ToString();
                if (khoa != khoaMoiNhat)
                    return ("Đợt đăng ký ưu tiên chỉ dành cho Tân sinh viên. Vui lòng chờ đợt đại trà.", giuong, null);
            }

            // SV chính sách chỉ được phòng 6-8 giường
            var doiTuongDt = Db.Query(@"SELECT KhoaHoc, DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV));
            var doiTuong = doiTuongDt.Rows.Count > 0 ? doiTuongDt.Rows[0]["DoiTuong"].ToString() : null;
            if (doiTuong == "ChinhSach" && giuong["LoaiPhong"].ToString() == "4")
                return ("Sinh viên diện chính sách chỉ được đăng ký phòng tiêu chuẩn 6-8 giường.", giuong, null);

            return (null, giuong, d);
        }

        // ============ Hợp đồng đăng ký + Gia hạn ============
        public IActionResult HopDong()
        {
            ViewBag.DsHopDong = Db.Query(@"SELECT pd.MaPhieu, pd.MaGiuong, pd.NgayDangKy, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai,
           p.MaPhong, p.GiaPhong, t.TenToa, d.TenDot, d.HocKy,
           (SELECT TOP 1 MaDon FROM DONYEUCAU
            WHERE MaPhieu = pd.MaPhieu AND LoaiDon = 'TraPhong' AND TrangThai IN ('ChoXuLy','DangXuLy')) AS MaDonTraPhongChoXL,
           (SELECT TOP 1 MaDon FROM DONYEUCAU
            WHERE MaPhieu = pd.MaPhieu AND LoaiDon = 'ChuyenPhong' AND TrangThai IN ('ChoXuLy','DangXuLy')) AS MaDonChuyenPhongChoXL
    FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN PHONG p  ON p.MaPhong  = g.MaPhong
    JOIN TOANHA t ON t.MaToa    = p.MaToa
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE pd.MSSV = @MSSV ORDER BY pd.NgayDangKy DESC;", Db.P("@MSSV", MSSV));
            var sv = Db.Query(@"SELECT KhoaHoc, DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV));
            ViewBag.DoiTuong = sv.Rows.Count > 0 ? sv.Rows[0]["DoiTuong"].ToString() : null;
            return View();
        }

        [HttpPost]
        public IActionResult GiaHan(int maPhieu, int soThang)
        {
            if (BiKhoa)
            {
                TempData["Loi"] = "Tài khoản đang bị khóa quyền gia hạn do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.";
                return RedirectToAction("HopDong");
            }

            var pd = Db.Query(@"SELECT * FROM PHIEUDANGKY WHERE MaPhieu = @MaPhieu AND MSSV = @MSSV AND TrangThai = 'DangO';", Db.P("@MaPhieu", maPhieu), Db.P("@MSSV", MSSV));
            if (pd.Rows.Count == 0) { TempData["Loi"] = "Không tìm thấy hợp đồng đang hiệu lực."; return RedirectToAction("HopDong"); }

            var svDt = Db.Query(@"SELECT KhoaHoc, DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV));
            var doiTuong = svDt.Rows.Count > 0 ? svDt.Rows[0]["DoiTuong"].ToString() : null;

            // SV thường: chỉ gia hạn khi cổng đang mở; SV chính sách gia hạn bất kỳ lúc nào
            if (doiTuong != "ChinhSach")
            {
                var dangMo = Db.Scalar(@"SELECT COUNT(*) FROM DOTDANGKY WHERE GETDATE() BETWEEN NgayMo AND NgayDong;");
                if (Convert.ToInt32(dangMo) == 0)
                {
                    TempData["Loi"] = "Cổng gia hạn hiện đã đóng. Chỉ sinh viên diện chính sách được gia hạn ngoài thời hạn.";
                    return RedirectToAction("HopDong");
                }
            }

            int ts2 = CommonRepo.LayThamSoInt("TS2");
            if (soThang < 1 || soThang > ts2)
            {
                TempData["Loi"] = $"Số tháng gia hạn phải từ 1 đến {ts2} tháng.";
                return RedirectToAction("HopDong");
            }

            SinhVienRepo.GiaHan(maPhieu, soThang, MSSV, $"Hợp đồng #{maPhieu} đã được gia hạn thêm {soThang} tháng.");
            TempData["ThanhCong"] = $"Gia hạn hợp đồng thành công thêm {soThang} tháng.";
            return RedirectToAction("HopDong");
        }

        // ============ Yêu cầu trả phòng ============
        [HttpPost]
        public IActionResult TraPhong(int maPhieu)
        {
            var pd = Db.Query(@"SELECT pd.MaGiuong, g.MaPhong FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE pd.MaPhieu = @MaPhieu AND pd.MSSV = @MSSV AND pd.TrangThai = 'DangO';", Db.P("@MaPhieu", maPhieu), Db.P("@MSSV", MSSV));
            if (pd.Rows.Count == 0) { TempData["Loi"] = "Không tìm thấy hợp đồng."; return RedirectToAction("HopDong"); }
            string maPhong = pd.Rows[0]["MaPhong"].ToString()!;

            var noHD = Db.Scalar(@"SELECT COUNT(*) FROM HOADON WHERE MaPhong = @MaPhong AND TrangThai IN ('ChoThanhToan','QuaHan');", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(noHD) > 0)
            {
                TempData["Loi"] = "Phòng còn hóa đơn chưa thanh toán. Vui lòng hoàn thành nghĩa vụ tài chính trước khi trả phòng.";
                return RedirectToAction("HopDong");
            }

            Db.Exec(@"INSERT INTO DONYEUCAU (MSSV, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, MaPhieu)
    VALUES (@MSSV, 'TraPhong', @TieuDe, @NoiDung, 'TrungBinh', 'ChoXuLy', @MaPhieu);",
                Db.P("@MSSV", MSSV), Db.P("@MaPhieu", maPhieu),
                Db.P("@TieuDe", $"Yêu cầu trả phòng {maPhong}"),
                Db.P("@NoiDung", $"Sinh viên yêu cầu trả phòng {maPhong} (hợp đồng #{maPhieu}). Vui lòng đối chiếu và xác nhận."));

            TempData["ThanhCong"] = "Đã gửi yêu cầu trả phòng tới Ban quản lý. Vui lòng chờ xác nhận và mang chìa khóa/thẻ từ khi bàn giao phòng.";
            return RedirectToAction("HopDong");
        }

        // ============ Đơn phản hồi / đề xuất ============
        public IActionResult DonYeuCau(string loai = "PhanHoi")
        {
            ViewBag.Loai = loai;
            ViewBag.DsDon = Db.Query(@"SELECT MaDon, TieuDe, MucUuTien, TrangThai, NgayTao
    FROM DONYEUCAU WHERE MSSV = @MSSV AND LoaiDon = @LoaiDon
    ORDER BY NgayTao DESC;", Db.P("@MSSV", MSSV), Db.P("@LoaiDon", loai));
            return View();
        }

        [HttpGet]
        public IActionResult TaoDon(string loai = "PhanHoi")
        {
            if (!DangOPhong())
            {
                TempData["Loi"] = "Bạn cần đang ở một phòng trong ký túc xá mới có thể gửi đơn phản hồi/đề xuất.";
                return RedirectToAction("DonYeuCau", new { loai });
            }
            ViewBag.Loai = loai; return View();
        }

        [HttpPost]
        public IActionResult TaoDon(string loai, string tieuDe, string noiDung)
        {
            if (!DangOPhong())
            {
                TempData["Loi"] = "Bạn cần đang ở một phòng trong ký túc xá mới có thể gửi đơn phản hồi/đề xuất.";
                return RedirectToAction("DonYeuCau", new { loai });
            }
            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
            {
                ViewBag.Loai = loai; ViewBag.Loi = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
                return View();
            }
            Db.Exec(@"INSERT INTO DONYEUCAU (MSSV, LoaiDon, TieuDe, NoiDung) VALUES (@MSSV, @LoaiDon, @TieuDe, @NoiDung);",
                Db.P("@MSSV", MSSV), Db.P("@LoaiDon", loai), Db.P("@TieuDe", tieuDe.Trim()), Db.P("@NoiDung", noiDung.Trim()));
            TempData["ThanhCong"] = loai == "PhanHoi" ? "Gửi đơn phản hồi thành công." : "Gửi đơn đề xuất thành công.";
            return RedirectToAction("DonYeuCau", new { loai });
        }

        /// <summary>SV chỉ được gửi đơn phản hồi/đề xuất khi đang có phòng hiệu lực (tránh đơn "ma" khi chưa ở KTX).</summary>
        private bool DangOPhong()
        {
            var soLuong = Db.Scalar(@"SELECT COUNT(*) FROM PHIEUDANGKY WHERE MSSV = @MSSV AND TrangThai = 'DangO';", Db.P("@MSSV", MSSV));
            return Convert.ToInt32(soLuong) > 0;
        }

        // Chi tiết đơn
        public IActionResult ChiTietDon(int id)
        {
            var dt = Db.Query(@"SELECT d.*, q.HoTen AS TenNV FROM DONYEUCAU d
    LEFT JOIN QUANLY q ON q.MaNV = d.MaNV
    WHERE d.MaDon = @MaDon AND d.MSSV = @MSSV;", Db.P("@MaDon", id), Db.P("@MSSV", MSSV));
            if (dt.Rows.Count == 0) return RedirectToAction("DonYeuCau");
            ViewBag.Don = dt.Rows[0];
            return View();
        }

        // Xóa đơn phản hồi/đề xuất - chỉ cho xóa khi đơn của chính mình và CHƯA được quản lý xử lý
        [HttpPost]
        public IActionResult XoaDon(int maDon)
        {
            var dt = Db.Query(@"SELECT LoaiDon, TrangThai FROM DONYEUCAU WHERE MaDon = @MaDon AND MSSV = @MSSV;",
                Db.P("@MaDon", maDon), Db.P("@MSSV", MSSV));
            if (dt.Rows.Count == 0) { TempData["Loi"] = "Không tìm thấy đơn."; return RedirectToAction("DonYeuCau"); }

            string loai = dt.Rows[0]["LoaiDon"].ToString()!;
            if (dt.Rows[0]["TrangThai"].ToString() != "ChoXuLy")
            {
                TempData["Loi"] = "Đơn đã được Ban quản lý tiếp nhận xử lý nên không thể xóa.";
                return RedirectToAction("ChiTietDon", new { id = maDon });
            }

            Db.Exec(@"DELETE FROM DONYEUCAU WHERE MaDon = @MaDon AND MSSV = @MSSV;", Db.P("@MaDon", maDon), Db.P("@MSSV", MSSV));
            TempData["ThanhCong"] = "Đã xóa đơn.";
            return RedirectToAction("DonYeuCau", new { loai });
        }

        // ============ Đăng ký chuyển phòng (bước 1: chọn phòng - lưới phòng như Tra cứu phòng) ============
        [HttpGet]
        public IActionResult ChuyenPhong(int maPhieu, string? tuKhoa, string? maToa, string? loaiPhong, string? mucGia)
        {
            var (loi, pd) = KiemTraChuyenPhong(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var doiTuong = Db.Scalar(@"SELECT DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;", Db.P("@MSSV", MSSV))?.ToString();
            bool laChinhSach = doiTuong == "ChinhSach";

            ViewBag.Phieu = pd;
            ViewBag.LaChinhSach = laChinhSach;
            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;");
            ViewBag.DsPhong = Db.Query(@"SELECT p.MaPhong, p.Tang, p.LoaiPhong, p.SoGiuong, p.SoGiuongTrong, p.GiaPhong, t.TenToa, t.MaToa
    FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE p.TrangThai = 'HoatDong' AND p.SoGiuongTrong > 0 AND p.MaPhong <> @MaPhongHienTai
      AND (@ChiTieuChuan = 0 OR p.LoaiPhong IN ('6','8'))
      AND (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%' OR t.TenToa LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
      AND (@LoaiPhong IS NULL OR p.LoaiPhong = @LoaiPhong)
      AND (@MucGia IS NULL
           OR (@MucGia = 'duoi400'    AND p.GiaPhong < 400000)
           OR (@MucGia = '400den600' AND p.GiaPhong BETWEEN 400000 AND 600000)
           OR (@MucGia = 'tren600'   AND p.GiaPhong > 600000))
    ORDER BY t.MaToa, p.Tang, p.MaPhong;",
                Db.P("@MaPhongHienTai", pd["MaPhong"]), Db.P("@ChiTieuChuan", laChinhSach),
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa),
                Db.P("@LoaiPhong", string.IsNullOrWhiteSpace(loaiPhong) ? null : loaiPhong),
                Db.P("@MucGia", string.IsNullOrWhiteSpace(mucGia) ? null : mucGia));
            return View();
        }

        // ============ Đăng ký chuyển phòng (bước 2: chọn giường trống trong phòng đã chọn) ============
        [HttpGet]
        public IActionResult ChonGiuongChuyenPhong(int maPhieu, string maPhongMoi)
        {
            var (loi, pd) = KiemTraChuyenPhong(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var phong = Db.Query(@"SELECT p.*, t.TenToa FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE p.MaPhong = @MaPhong AND p.TrangThai = 'HoatDong';", Db.P("@MaPhong", maPhongMoi));
            if (phong.Rows.Count == 0 || phong.Rows[0]["MaPhong"].ToString() == pd["MaPhong"].ToString())
            {
                TempData["Loi"] = "Phòng không hợp lệ để chuyển đến.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            ViewBag.Phieu = pd;
            ViewBag.PhongMoi = phong.Rows[0];
            ViewBag.DsGiuong = Db.Query(@"SELECT MaGiuong, TrangThai FROM GIUONG WHERE MaPhong = @MaPhong ORDER BY MaGiuong;", Db.P("@MaPhong", maPhongMoi));
            return View();
        }

        [HttpPost]
        public IActionResult GuiDonChuyenPhong(int maPhieu, string maGiuongMoi)
        {
            var (loi, pd) = KiemTraChuyenPhong(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var g = Db.Query(@"SELECT g.MaGiuong, g.TrangThai, p.MaPhong, p.TrangThai AS TrangThaiPhong
    FROM GIUONG g JOIN PHONG p ON p.MaPhong = g.MaPhong WHERE g.MaGiuong = @MaGiuongMoi;", Db.P("@MaGiuongMoi", maGiuongMoi));
            if (g.Rows.Count == 0 || g.Rows[0]["TrangThai"].ToString() != "Trong" || g.Rows[0]["TrangThaiPhong"].ToString() != "HoatDong")
            {
                TempData["Loi"] = "Giường bạn chọn không còn trống. Vui lòng chọn giường khác.";
                if (g.Rows.Count > 0)
                    return RedirectToAction("ChonGiuongChuyenPhong", new { maPhieu, maPhongMoi = g.Rows[0]["MaPhong"] });
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            string maPhongCu = pd["MaPhong"].ToString()!;
            string maPhongMoi = g.Rows[0]["MaPhong"].ToString()!;
            if (maPhongMoi == maPhongCu)
            {
                TempData["Loi"] = "Bạn đang ở chính phòng này rồi, vui lòng chọn phòng khác.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            Db.Exec(@"INSERT INTO DONYEUCAU (MSSV, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, MaPhieu, MaGiuongMoi)
    VALUES (@MSSV, 'ChuyenPhong', @TieuDe, @NoiDung, 'TrungBinh', 'ChoXuLy', @MaPhieu, @MaGiuongMoi);",
                Db.P("@MSSV", MSSV), Db.P("@MaPhieu", maPhieu), Db.P("@MaGiuongMoi", maGiuongMoi),
                Db.P("@TieuDe", $"Yêu cầu chuyển phòng {maPhongCu} → {maPhongMoi}"),
                Db.P("@NoiDung", $"Sinh viên yêu cầu chuyển từ phòng {maPhongCu} sang phòng {maPhongMoi} (giường {maGiuongMoi}). Vui lòng đối chiếu và xác nhận."));

            TempData["ThanhCong"] = "Đã gửi yêu cầu chuyển phòng tới Ban quản lý. Vui lòng chờ xác nhận.";
            return RedirectToAction("HopDong");
        }

        /// <summary>Kiểm tra điều kiện chuyển phòng: tài khoản không bị khóa do quá hạn thanh toán (QD05),
        /// hợp đồng đang ở, chưa có đơn chuyển/trả phòng nào đang chờ xử lý, đã ở đủ số ngày tối thiểu
        /// (TS11), không còn nợ hóa đơn phòng hiện tại.</summary>
        private (string? loi, System.Data.DataRow? pd) KiemTraChuyenPhong(int maPhieu)
        {
            // QD05: SV bị khóa do quá hạn thanh toán không được đăng ký/gia hạn - áp dụng tương tự
            // cho chuyển phòng vì bản chất cũng là một hình thức đăng ký ở KTX (giường/hợp đồng mới).
            if (BiKhoa)
                return ("Tài khoản đang bị khóa quyền chuyển phòng do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.", null);

            var dt = Db.Query(@"SELECT pd.MaPhieu, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai, g.MaPhong, p.LoaiPhong
    FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong JOIN PHONG p ON p.MaPhong = g.MaPhong
    WHERE pd.MaPhieu = @MaPhieu AND pd.MSSV = @MSSV;", Db.P("@MaPhieu", maPhieu), Db.P("@MSSV", MSSV));
            if (dt.Rows.Count == 0) return ("Không tìm thấy hợp đồng.", null);
            var pd = dt.Rows[0];

            if (pd["TrangThai"].ToString() != "DangO")
                return ("Chỉ có thể chuyển phòng khi hợp đồng đang ở trạng thái Đang ở.", null);

            var coDonChoXL = Db.Scalar(@"SELECT COUNT(*) FROM DONYEUCAU
    WHERE MaPhieu = @MaPhieu AND LoaiDon IN ('ChuyenPhong','TraPhong') AND TrangThai IN ('ChoXuLy','DangXuLy');", Db.P("@MaPhieu", maPhieu));
            if (Convert.ToInt32(coDonChoXL) > 0)
                return ("Bạn đang có yêu cầu chuyển phòng/trả phòng chờ xử lý cho hợp đồng này.", null);

            int soNgayToiThieu = CommonRepo.LayThamSoInt("TS11");
            int soNgayDaO = (DateTime.Today - Convert.ToDateTime(pd["NgayBatDau"])).Days;
            if (soNgayDaO < soNgayToiThieu)
                return ($"Bạn cần ở tối thiểu {soNgayToiThieu} ngày trước khi được đăng ký chuyển phòng (hiện đã ở {Math.Max(soNgayDaO, 0)} ngày).", null);

            var noHD = Db.Scalar(@"SELECT COUNT(*) FROM HOADON WHERE MaPhong = @MaPhong AND TrangThai IN ('ChoThanhToan','QuaHan');", Db.P("@MaPhong", pd["MaPhong"]));
            if (Convert.ToInt32(noHD) > 0)
                return ("Phòng hiện tại còn hóa đơn chưa thanh toán. Vui lòng hoàn thành nghĩa vụ tài chính trước khi chuyển phòng.", null);

            return (null, pd);
        }

        // ============ Lịch sử + tra cứu hóa đơn ============
        public IActionResult HoaDon(string? thang, string? trangThai)
        {
            ViewBag.DsHoaDon = Db.Query(@"SELECT DISTINCT h.MaHD, h.MaPhong, h.Thang, h.TongTien, h.NgayPhatHanh, h.HanThanhToan, h.TrangThai
    FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.MSSV = @MSSV
    WHERE h.TrangThai <> 'Nhap'
      AND (@Thang IS NULL OR h.Thang = @Thang)
      AND (@TrangThai IS NULL OR h.TrangThai = @TrangThai)
    ORDER BY h.MaHD DESC;",
                Db.P("@MSSV", MSSV),
                Db.P("@Thang", string.IsNullOrWhiteSpace(thang) ? null : thang),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            return View();
        }

        // ============ Chi tiết hóa đơn & thanh toán ============
        public IActionResult ChiTietHoaDon(int id)
        {
            var dt = Db.Query(@"SELECT h.*, c.DienDauKy, c.DienCuoiKy, c.NuocDauKy, c.NuocCuoiKy,
           dg.GiaDien, dg.GiaNuoc, dg.PhiDichVu, t.TenToa
    FROM HOADON h
    JOIN CHISODIENNUOC c ON c.MaChiSo = h.MaChiSo
    JOIN DONGIA dg ON dg.MaDonGia = h.MaDonGia
    JOIN PHONG p ON p.MaPhong = h.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE h.MaHD = @MaHD;", Db.P("@MaHD", id));
            if (dt.Rows.Count == 0) return RedirectToAction("HoaDon");
            var hd = dt.Rows[0];
            ViewBag.HD = hd;
            ViewBag.DsGiaoDich = Db.Query(@"SELECT * FROM THANHTOAN WHERE MaHD = @MaHD ORDER BY ThoiGian DESC;", Db.P("@MaHD", id));

            // Mã QR chuyển khoản (VietQR) - tự điền đúng số tiền + nội dung "HD<mã hóa đơn>" để đối soát
            ViewBag.TenNganHang = CauHinhThanhToan.TenNganHang;
            ViewBag.SoTaiKhoan = CauHinhThanhToan.SoTaiKhoan;
            ViewBag.ChuTaiKhoan = CauHinhThanhToan.ChuTaiKhoan;
            ViewBag.QrUrl = CauHinhThanhToan.TaoUrlQr(Convert.ToDecimal(hd["TongTien"]), $"HD{id} {MSSV}");
            return View();
        }

        // Mô phỏng cổng thanh toán trực tuyến
        [HttpPost]
        public IActionResult ThanhToan(int maHD, string phuongThuc)
        {
            var hd = Db.Query(@"SELECT TongTien, TrangThai FROM HOADON WHERE MaHD = @MaHD;", Db.P("@MaHD", maHD));
            if (hd.Rows.Count == 0 || hd.Rows[0]["TrangThai"].ToString() == "DaThanhToan")
            {
                TempData["Loi"] = "Hóa đơn không hợp lệ hoặc đã được thanh toán.";
                return RedirectToAction("HoaDon");
            }

            string maGDCong = phuongThuc.ToUpper() + DateTime.Now.ToString("yyyyMMddHHmmss");
            SinhVienRepo.ThanhToan(maHD, MSSV, phuongThuc, hd.Rows[0]["TongTien"], maGDCong,
                $"Thanh toán hóa đơn #{maHD} thành công qua {phuongThuc}. Mã giao dịch: {maGDCong}.");

            string thongBaoThem = "";
            if (BiKhoa)
            {
                var conNo = Db.Scalar(@"SELECT COUNT(*) FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.MSSV = @MSSV AND pd.TrangThai = 'DangO'
    WHERE h.TrangThai = 'QuaHan';", Db.P("@MSSV", MSSV));
                if (Convert.ToInt32(conNo) == 0)
                {
                    Db.Exec(@"UPDATE TAIKHOAN SET TrangThai = 'HoatDong'
    WHERE MaTK = (SELECT MaTK FROM SINHVIEN WHERE MSSV = @MSSV) AND TrangThai = 'BiKhoa';", Db.P("@MSSV", MSSV));
                    HttpContext.Session.SetString("BiKhoa", "0");
                    thongBaoThem = " Bạn đã thanh toán hết nợ nên tài khoản được tự động mở khóa.";
                }
            }

            TempData["ThanhCong"] = $"Thanh toán thành công qua {phuongThuc}! Mã giao dịch: {maGDCong}.{thongBaoThem}";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }
    }
}
