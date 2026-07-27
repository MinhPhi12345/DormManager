using DormManager.Data;
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
            ViewBag.DsDon = Db.Query(@"SELECT d.MaDon, d.TieuDe, d.LoaiDon, d.MucUuTien, d.TrangThai, d.NgayTao,
           sv.HoTen, sv.MSSV
    FROM DONYEUCAU d JOIN SINHVIEN sv ON sv.MSSV = d.MSSV
    WHERE (@TuKhoa IS NULL OR d.TieuDe LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR sv.MSSV LIKE '%' + @TuKhoa + '%')
      AND (@LoaiDon IS NULL OR d.LoaiDon = @LoaiDon)
      AND (@TrangThai IS NULL OR d.TrangThai = @TrangThai)
      AND (@UuTien IS NULL OR d.MucUuTien = @UuTien)
    ORDER BY CASE d.MucUuTien WHEN 'Cao' THEN 0 WHEN 'TrungBinh' THEN 1 ELSE 2 END,
             CASE d.TrangThai WHEN 'ChoXuLy' THEN 0 WHEN 'DangXuLy' THEN 1 ELSE 2 END, d.NgayTao DESC;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@LoaiDon", string.IsNullOrWhiteSpace(loai) ? null : loai),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai),
                Db.P("@UuTien", string.IsNullOrWhiteSpace(uuTien) ? null : uuTien));
            return View();
        }

        // Chi tiết + xử lý đơn (đổi trạng thái, ưu tiên, phản hồi → gửi thông báo)
        public IActionResult ChiTietDon(int id)
        {
            var dt = Db.Query(@"SELECT d.*, sv.HoTen, q.HoTen AS TenNV,
           gCu.MaPhong AS MaPhongCu,
           gMoi.MaPhong AS MaPhongMoi
    FROM DONYEUCAU d
    JOIN SINHVIEN sv ON sv.MSSV = d.MSSV
    LEFT JOIN QUANLY q ON q.MaNV = d.MaNV
    LEFT JOIN PHIEUDANGKY pd ON pd.MaPhieu = d.MaPhieu
    LEFT JOIN GIUONG gCu ON gCu.MaGiuong = pd.MaGiuong
    LEFT JOIN GIUONG gMoi ON gMoi.MaGiuong = d.MaGiuongMoi
    WHERE d.MaDon = @MaDon;", Db.P("@MaDon", id));
            if (dt.Rows.Count == 0) return RedirectToAction("DonYeuCau");
            ViewBag.Don = dt.Rows[0];
            return View();
        }

        [HttpPost]
        public IActionResult XuLyDon(int maDon, string mucUuTien, string trangThai, string? phanHoi)
        {
            // sp_XuLyDon: transaction đổi trạng thái/ưu tiên + gửi thông báo
            QuanLyRepo.XuLyDon(maDon, mucUuTien, trangThai, phanHoi, MaNV);
            TempData["ThanhCong"] = "Cập nhật đơn thành công. Hệ thống đã gửi thông báo tới sinh viên.";
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // ============ Danh sách + tra cứu + chi tiết đơn đăng ký ============
        public IActionResult DonDangKy(string? tuKhoa, string? trangThai)
        {
            ViewBag.DsPhieu = Db.Query(@"SELECT pd.MaPhieu, pd.NgayDangKy, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai,
           sv.MSSV, sv.HoTen, g.MaPhong, pd.MaGiuong, d.TenDot
    FROM PHIEUDANGKY pd
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE (@TuKhoa IS NULL OR sv.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR g.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@TrangThai IS NULL OR pd.TrangThai = @TrangThai)
    ORDER BY CASE pd.TrangThai WHEN 'ChoDoiChieu' THEN 0 ELSE 1 END, pd.NgayDangKy DESC;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            return View();
        }

        public IActionResult ChiTietDangKy(int id)
        {
            var dt = Db.Query(@"SELECT pd.*, sv.HoTen, sv.KhoaHoc, sv.GioiTinh, sv.DoiTuong, sv.DiemViPham,
           g.MaPhong, p.GiaPhong, p.LoaiPhong, t.TenToa, d.TenDot, d.HocKy
    FROM PHIEUDANGKY pd
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN PHONG p ON p.MaPhong = g.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE pd.MaPhieu = @MaPhieu;", Db.P("@MaPhieu", id));
            if (dt.Rows.Count == 0) return RedirectToAction("DonDangKy");
            ViewBag.Phieu = dt.Rows[0];
            return View();
        }

        // Xác nhận hoàn tất nhận phòng (ChoDoiChieu → DangO)
        [HttpPost]
        public IActionResult XacNhanNhanPhong(int maPhieu)
        {
            QuanLyRepo.XacNhanNhanPhong(maPhieu);   // sp_XacNhanNhanPhong (transaction)
            TempData["ThanhCong"] = "Xác nhận hoàn tất nhận phòng thành công. Đã gửi thông báo cho sinh viên.";
            return RedirectToAction("ChiTietDangKy", new { id = maPhieu });
        }

        // Từ chối / hủy phiếu → trả giường về Trống
        [HttpPost]
        public IActionResult HuyPhieu(int maPhieu)
        {
            QuanLyRepo.HuyPhieu(maPhieu);   // sp_HuyPhieu (transaction)
            TempData["ThanhCong"] = "Đã hủy phiếu và trả giường về trạng thái trống.";
            return RedirectToAction("DonDangKy");
        }

        // Xác nhận hoàn tất trả phòng (DangO → DaSuDung)
        [HttpPost]
        public IActionResult XacNhanTraPhong(int maDon)
        {
            QuanLyRepo.XacNhanTraPhong(maDon, MaNV);   // sp_XacNhanTraPhong (transaction)
            TempData["ThanhCong"] = "Đã xác nhận trả phòng. Giường đã được giải phóng và sinh viên đã được thông báo.";
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // Xác nhận chuyển phòng (đóng phiếu cũ + mở phiếu mới)
        [HttpPost]
        public IActionResult XacNhanChuyenPhong(int maDon)
        {
            QuanLyRepo.XacNhanChuyenPhong(maDon, MaNV);   // sp_XacNhanChuyenPhong (transaction)
            TempData["ThanhCong"] = "Đã xác nhận chuyển phòng cho sinh viên. Đã gửi thông báo.";
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // ============ Thống kê đánh giá phòng/KTX ============
        public IActionResult DanhGiaPhong(string? maToa)
        {
            ViewBag.MaToa = maToa;
            ViewBag.DsThongKe = QuanLyRepo.ThongKeDanhGia(string.IsNullOrWhiteSpace(maToa) ? null : maToa);
            ViewBag.DsNhanXet = Db.Query(@"SELECT dg.MaDanhGia, dg.MaPhong, dg.SoSao, dg.NhanXet, dg.NgayDanhGia, sv.HoTen
    FROM DANHGIA dg JOIN SINHVIEN sv ON sv.MSSV = dg.MSSV
    WHERE (@MaToa IS NULL OR dg.MaPhong IN (SELECT MaPhong FROM PHONG WHERE MaToa = @MaToa))
    ORDER BY dg.NgayDanhGia DESC;", Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa));
            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;");
            return View();
        }

        // ============ Danh sách + tra cứu phòng ============
        public IActionResult Phong(string? tuKhoa, string? maToa)
        {
            ViewBag.DsPhong = Db.Query(@"SELECT p.*, t.TenToa FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
    ORDER BY p.MaToa, p.Tang, p.MaPhong;",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa));
            ViewBag.DsToa = Db.Query(@"SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;");
            return View();
        }

        // ============ Danh sách giường + cập nhật trạng thái ============
        public IActionResult Giuong(string maPhong)
        {
            ViewBag.MaPhong = maPhong;
            ViewBag.DsGiuong = Db.Query(@"SELECT g.MaGiuong, g.TrangThai, sv.HoTen, sv.MSSV
    FROM GIUONG g
    LEFT JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.TrangThai IN ('DangO','ChoDoiChieu')
    LEFT JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE g.MaPhong = @MaPhong ORDER BY g.MaGiuong;", Db.P("@MaPhong", maPhong));
            return View();
        }

        [HttpPost]
        public IActionResult CapNhatGiuong(string maGiuong, string maPhong, string trangThai)
        {
            var dangO = Db.Scalar(@"SELECT COUNT(*) FROM PHIEUDANGKY WHERE MaGiuong = @MaGiuong AND TrangThai IN ('DangO','ChoDoiChieu');", Db.P("@MaGiuong", maGiuong));
            if (Convert.ToInt32(dangO) > 0 && trangThai != "DaSuDung")
            {
                TempData["Loi"] = "Giường đang có sinh viên ở/giữ chỗ, không thể đổi trạng thái.";
                return RedirectToAction("Giuong", new { maPhong });
            }

            QuanLyRepo.CapNhatGiuong(maGiuong, maPhong, trangThai);   // sp_CapNhatGiuong (transaction đồng bộ số giường trống)
            TempData["ThanhCong"] = $"Đã cập nhật trạng thái giường {maGiuong}.";
            return RedirectToAction("Giuong", new { maPhong });
        }

        // ============ Nhập chỉ số điện/nước ============
        public IActionResult ChiSo(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;
            ViewBag.DsPhong = Db.Query(@"SELECT p.MaPhong, p.SoGiuong - p.SoGiuongTrong AS SoNguoiO,
           cs.MaChiSo, cs.DienDauKy, cs.DienCuoiKy, cs.NuocDauKy, cs.NuocCuoiKy, cs.TrangThai,
           truoc.DienCuoiKy AS DienKyTruoc, truoc.NuocCuoiKy AS NuocKyTruoc
    FROM PHONG p
    LEFT JOIN CHISODIENNUOC cs ON cs.MaPhong = p.MaPhong AND cs.Thang = @Thang
    OUTER APPLY (SELECT TOP 1 DienCuoiKy, NuocCuoiKy FROM CHISODIENNUOC
                 WHERE MaPhong = p.MaPhong AND Thang <> @Thang
                 ORDER BY RIGHT(Thang,4) DESC, LEFT(Thang,2) DESC) truoc
    WHERE p.TrangThai = 'HoatDong'
      AND EXISTS (SELECT 1 FROM GIUONG g JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
                  WHERE g.MaPhong = p.MaPhong AND pd.TrangThai = 'DangO')
    ORDER BY p.MaPhong;", Db.P("@Thang", thang));
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

            var daChot = Db.Scalar(@"SELECT COUNT(*) FROM CHISODIENNUOC WHERE MaPhong = @MaPhong AND Thang = @Thang AND TrangThai = 'DaChot';", Db.P("@MaPhong", maPhong), Db.P("@Thang", thang));
            if (Convert.ToInt32(daChot) > 0)
            {
                TempData["Loi"] = $"Chỉ số phòng {maPhong} tháng {thang} đã chốt (đã xuất hóa đơn), không thể sửa.";
                return RedirectToAction("ChiSo", new { thang });
            }

            QuanLyRepo.LuuChiSo(maPhong, thang, dienDauKy, dienCuoiKy, nuocDauKy, nuocCuoiKy);   // sp_LuuChiSo (MERGE)
            TempData["ThanhCong"] = $"Đã lưu chỉ số điện/nước phòng {maPhong} tháng {thang}.";
            return RedirectToAction("ChiSo", new { thang });
        }

        // ============ Tạo hóa đơn ============
        public IActionResult TaoHoaDon(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;
            var donGia = Db.Query(@"SELECT TOP 1 * FROM DONGIA WHERE TrangThai = 'HieuLuc' ORDER BY NgayApDung DESC;");
            ViewBag.DonGia = donGia.Rows.Count > 0 ? donGia.Rows[0] : null;
            ViewBag.DsXemTruoc = QuanLyRepo.NguonTaoHoaDon(thang);   // sp_NguonTaoHoaDon (tính số người ở trọn tháng)
            ViewBag.DsNhap = Db.Query(@"SELECT h.MaHD, h.MaPhong, h.Thang, h.TongTien, h.HanThanhToan
    FROM HOADON h WHERE h.TrangThai = 'Nhap' ORDER BY h.MaPhong;");
            return View();
        }

        [HttpPost]
        public IActionResult TaoHoaDonThang(string thang, int hanThanhToanNgay = 15)
        {
            var donGia = Db.Query(@"SELECT TOP 1 * FROM DONGIA WHERE TrangThai = 'HieuLuc' ORDER BY NgayApDung DESC;");
            if (donGia.Rows.Count == 0) { TempData["Loi"] = "Chưa cấu hình đơn giá điện/nước hiệu lực."; return RedirectToAction("TaoHoaDon", new { thang }); }
            var dg = donGia.Rows[0];

            var ds = QuanLyRepo.NguonTaoHoaDon(thang);
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

                QuanLyRepo.TaoHoaDonDong(r["MaPhong"], r["MaChiSo"], dg["MaDonGia"], thang,
                    tienPhong, tienDien, tienNuoc, tongTien, DateTime.Today.AddDays(hanThanhToanNgay));   // sp_TaoHoaDonDong (transaction)
                soHD++;
            }
            TempData["ThanhCong"] = $"Đã tạo {soHD} hóa đơn (bản nháp) cho tháng {thang}. Hãy kiểm tra và xác nhận gửi.";
            return RedirectToAction("TaoHoaDon", new { thang });
        }

        // ============ Xác nhận gửi hóa đơn ============
        [HttpPost]
        public IActionResult GuiHoaDon(int maHD)
        {
            var hd = Db.Query(@"SELECT MaPhong, Thang, TongTien, HanThanhToan FROM HOADON WHERE MaHD = @MaHD AND TrangThai = 'Nhap';", Db.P("@MaHD", maHD));
            if (hd.Rows.Count == 0) { TempData["Loi"] = "Hóa đơn không hợp lệ."; return RedirectToAction("TaoHoaDon"); }
            var r = hd.Rows[0];

            Db.Exec(@"UPDATE HOADON SET TrangThai = 'ChoThanhToan', NgayPhatHanh = GETDATE() WHERE MaHD = @MaHD;", Db.P("@MaHD", maHD));
            // Gửi thông báo Email/SMS tới từng sinh viên trong phòng, lưu lịch sử
            var dsSV = Db.Query(@"SELECT pd.MSSV FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai = 'DangO';", Db.P("@MaPhong", r["MaPhong"]));
            foreach (System.Data.DataRow sv in dsSV.Rows)
            {
                CommonRepo.ThemThongBao(sv["MSSV"].ToString()!, maHD,
                    $"Hóa đơn tháng {r["Thang"]} phòng {r["MaPhong"]}: {Convert.ToDecimal(r["TongTien"]):N0}đ. Hạn thanh toán {Convert.ToDateTime(r["HanThanhToan"]):dd/MM/yyyy}.");
            }
            TempData["ThanhCong"] = $"Đã gửi hóa đơn #{maHD} và thông báo tới {dsSV.Rows.Count} sinh viên trong phòng.";
            return RedirectToAction("TaoHoaDon");
        }

        // ============ Lịch sử thông báo hóa đơn + tra cứu ============
        public IActionResult LichSuThongBao(string? tuKhoa)
        {
            ViewBag.DsTB = Db.Query(@"SELECT tb.MaTB, tb.MSSV, sv.HoTen, tb.MaHD, tb.NoiDung, tb.Kenh, tb.ThoiGianGui, tb.TrangThaiGui
    FROM THONGBAO tb JOIN SINHVIEN sv ON sv.MSSV = tb.MSSV
    WHERE (@TuKhoa IS NULL OR tb.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR tb.NoiDung LIKE '%' + @TuKhoa + '%')
    ORDER BY tb.ThoiGianGui DESC;", Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()));
            return View();
        }

        // ============ Sinh viên vi phạm + mở khóa tài khoản ============
        public IActionResult ViPham(string? tuKhoa)
        {
            // Job tự động (mô phỏng khi mở trang): quét hóa đơn quá hạn -> khóa TK (QD05), ghi vi phạm (QD06)
            QuanLyRepo.QuetHoaDonQuaHan();

            ViewBag.DsViPham = Db.Query(@"SELECT sv.MSSV, sv.HoTen, sv.KhoaHoc, sv.DiemViPham, tk.TrangThai AS TrangThaiTK, tk.MaTK
    FROM SINHVIEN sv JOIN TAIKHOAN tk ON tk.MaTK = sv.MaTK
    WHERE (sv.DiemViPham > 0 OR tk.TrangThai = 'BiKhoa')
      AND (@TuKhoa IS NULL OR sv.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%')
    ORDER BY sv.DiemViPham DESC;", Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()));
            ViewBag.LichSu = Db.Query(@"SELECT vp.*, sv.HoTen FROM VIPHAM vp
    JOIN SINHVIEN sv ON sv.MSSV = vp.MSSV ORDER BY vp.NgayGhiNhan DESC;");
            return View();
        }

        // Xác nhận mở khóa tài khoản
        [HttpPost]
        public IActionResult MoKhoa(int maTK)
        {
            // Chỉ mở khóa khi SV đã hoàn tất nghĩa vụ tài chính (không còn hóa đơn quá hạn) -
            // nếu không, job quét tự động (QuetHoaDonQuaHan) sẽ khóa lại ngay ở lần tải trang kế tiếp.
            var conNo = Db.Scalar(@"SELECT COUNT(*) FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.TrangThai = 'DangO'
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE sv.MaTK = @MaTK AND h.TrangThai = 'QuaHan';", Db.P("@MaTK", maTK));
            if (Convert.ToInt32(conNo) > 0)
            {
                TempData["Loi"] = "Không thể mở khóa: sinh viên vẫn còn hóa đơn quá hạn chưa thanh toán. Vui lòng yêu cầu sinh viên hoàn tất nghĩa vụ tài chính trước, hoặc dùng chức năng \"Xác nhận đã thu tiền\" nếu SV đã trả trực tiếp.";
                return RedirectToAction("ViPham");
            }

            QuanLyRepo.MoKhoa(maTK);   // sp_MoKhoa (transaction + gửi thông báo)
            TempData["ThanhCong"] = "Đã mở khóa tài khoản và gửi thông báo.";
            return RedirectToAction("ViPham");
        }

        // Quản lý xác nhận đã thu tiền trực tiếp (tiền mặt/chuyển khoản tại văn phòng) hộ SV
        // không tự đăng nhập thanh toán online được - đóng hết hóa đơn quá hạn rồi tự mở khóa luôn.
        [HttpPost]
        public IActionResult ThuTienMat(string mssv)
        {
            var dsNo = Db.Query(@"SELECT h.MaHD, h.TongTien FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.MSSV = @MSSV AND pd.TrangThai = 'DangO'
    WHERE h.TrangThai = 'QuaHan';", Db.P("@MSSV", mssv));

            if (dsNo.Rows.Count == 0)
            {
                TempData["Loi"] = "Sinh viên này không còn hóa đơn quá hạn nào.";
                return RedirectToAction("ViPham");
            }

            foreach (System.Data.DataRow r in dsNo.Rows)
            {
                int maHD = Convert.ToInt32(r["MaHD"]);
                string maGDCong = "TIENMAT" + DateTime.Now.ToString("yyyyMMddHHmmss") + maHD;
                // Ghi nhận dưới hình thức "NganHang" (thu trực tiếp tại văn phòng, đối soát thủ công) -
                // không phát sinh giao dịch cổng thanh toán trực tuyến thật.
                SinhVienRepo.ThanhToan(maHD, mssv, "NganHang", r["TongTien"], maGDCong,
                    $"Quản lý đã xác nhận thu tiền mặt hóa đơn #{maHD} tại văn phòng.");
            }

            var tk = Db.Scalar(@"SELECT tk.MaTK FROM TAIKHOAN tk JOIN SINHVIEN sv ON sv.MaTK = tk.MaTK
    WHERE sv.MSSV = @MSSV AND tk.TrangThai = 'BiKhoa';", Db.P("@MSSV", mssv));
            if (tk != null)
                QuanLyRepo.MoKhoa(Convert.ToInt32(tk));

            TempData["ThanhCong"] = $"Đã xác nhận thu {dsNo.Rows.Count} hóa đơn quá hạn và mở khóa tài khoản (nếu đang bị khóa).";
            return RedirectToAction("ViPham");
        }
    }
}
