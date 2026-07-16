using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    [PhanQuyen("SV")]
    public class SinhVienController : Controller
    {
        private string MSSV => HttpContext.Session.GetString("MSSV")!;

        // ============ Thông tin tổng quan ============
        public IActionResult TongQuan()
        {
            // 1 lượt gọi SP -> 4 result set (Phòng đang ở, Bạn cùng phòng, Hóa đơn chưa TT, Đơn gần đây)
            var ds = Db.QuerySetProc("sp_TongQuan", Db.P("@MSSV", MSSV));
            ViewBag.PhongDangO   = ds.Tables[0];
            ViewBag.BanCungPhong = ds.Tables[1];
            ViewBag.HoaDonChuaTT = ds.Tables[2];
            ViewBag.DonGanDay    = ds.Tables[3];
            ViewBag.DsDotMo = Db.QueryProc("sp_DotDangMoChoSV", Db.P("@MSSV", MSSV));
            return View();
        }

        // ============ Tra cứu phòng ============
        public IActionResult TraCuuPhong(string? tuKhoa, string? maToa, string? loaiPhong, string? mucGia)
        {
            // SV diện chính sách chỉ được xem phòng tiêu chuẩn 6-8 giường
            var doiTuong = Db.ScalarProc("sp_DoiTuongSV", Db.P("@MSSV", MSSV))?.ToString();
            bool laChinhSach = doiTuong == "ChinhSach";

            ViewBag.DsPhong = Db.QueryProc("sp_TraCuuPhong",
                Db.P("@TuKhoa", string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim()),
                Db.P("@MaToa", string.IsNullOrWhiteSpace(maToa) ? null : maToa),
                Db.P("@LoaiPhong", string.IsNullOrWhiteSpace(loaiPhong) ? null : loaiPhong),
                Db.P("@MucGia", string.IsNullOrWhiteSpace(mucGia) ? null : mucGia),
                Db.P("@ChiTieuChuan", laChinhSach));

            ViewBag.DsToa = Db.QueryProc("sp_DsToaTraCuuPhong");
            ViewBag.LaChinhSach = laChinhSach;
            ViewBag.DsDotMo = Db.QueryProc("sp_DotDangMoChoSV", Db.P("@MSSV", MSSV));
            return View();
        }

        // ============ Chi tiết thông tin phòng ============
        public IActionResult ChiTietPhong(string id)
        {
            var phong = Db.QueryProc("sp_ChiTietPhong", Db.P("@MaPhong", id));
            if (phong.Rows.Count == 0) return RedirectToAction("TraCuuPhong");

            ViewBag.Phong = phong.Rows[0];
            ViewBag.DsGiuong = Db.QueryProc("sp_DsGiuongTrongPhong", Db.P("@MaPhong", id));
            ViewBag.ThanhVien = Db.QueryProc("sp_ThanhVien", Db.P("@MaPhong", id));
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
            var (loi, giuong, dot) = KiemTraDangKy(maGiuong);
            if (loi != null || giuong == null || dot == null)
            {
                TempData["Loi"] = loi ?? "Không thể đăng ký giường này.";
                return RedirectToAction("TraCuuPhong");
            }

            // Thời hạn hợp đồng: KyHe = TS10 tháng, còn lại tối đa TS2 tháng
            int soThang = dot["LoaiDot"].ToString() == "KyHe"
                ? int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS10"))!.ToString()!)
                : int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS2"))!.ToString()!);

            var batDau = DateTime.Today;
            Db.ExecProc("sp_XacNhanDangKy",
                Db.P("@MSSV", MSSV), Db.P("@MaGiuong", maGiuong), Db.P("@MaDot", dot["MaDot"]),
                Db.P("@NgayBatDau", batDau), Db.P("@NgayKetThuc", batDau.AddMonths(soThang)));

            TempData["ThanhCong"] = $"Đăng ký giường {maGiuong} thành công! Vui lòng đến văn phòng quản lý tòa nhà để đối chiếu giấy tờ (thẻ sinh viên) và hoàn tất nhận phòng.";
            return RedirectToAction("HopDong");
        }

        /// <summary>Kiểm tra điều kiện đăng ký: đợt mở, ưu tiên tân SV, chính sách, giường trống.</summary>
        private (string? loi, System.Data.DataRow? giuong, System.Data.DataRow? dot) KiemTraDangKy(string maGiuong)
        {
            var g = Db.QueryProc("sp_ThongTinGiuong", Db.P("@MaGiuong", maGiuong));
            if (g.Rows.Count == 0) return ("Giường không tồn tại hoặc phòng ngừng hoạt động.", null, null);
            var giuong = g.Rows[0];

            if (giuong["TrangThai"].ToString() != "Trong")
                return ("Giường này đã có người đăng ký. Vui lòng chọn giường khác.", giuong, null);

            // Đã có hợp đồng hiệu lực?
            var daCo = Db.ScalarProc("sp_KiemTraHopDongHieuLuc", Db.P("@MSSV", MSSV));
            if (Convert.ToInt32(daCo) > 0)
                return ("Bạn đang có hợp đồng lưu trú hiệu lực, không thể đăng ký thêm.", giuong, null);

            // Đợt đăng ký đang mở
            var dot = Db.QueryProc("sp_LayDotDangMo");
            if (dot.Rows.Count == 0)
                return ("Hiện chưa có đợt đăng ký nào đang mở cổng. Vui lòng quay lại sau.", giuong, null);
            var d = dot.Rows[0];

            // Đợt ưu tiên: chỉ tân sinh viên (khóa mới nhất)
            if (d["LoaiDot"].ToString() == "UuTien")
            {
                var ttSV = Db.QueryProc("sp_LayKhoaHocDoiTuongSV", Db.P("@MSSV", MSSV));
                var khoa = ttSV.Rows.Count > 0 ? ttSV.Rows[0]["KhoaHoc"].ToString() : null;
                var khoaMoiNhat = Db.ScalarProc("sp_KhoaHocMoiNhat")?.ToString();
                if (khoa != khoaMoiNhat)
                    return ("Đợt đăng ký ưu tiên chỉ dành cho Tân sinh viên. Vui lòng chờ đợt đại trà.", giuong, null);
            }

            // SV chính sách chỉ được phòng 6-8 giường
            var doiTuongDt = Db.QueryProc("sp_LayKhoaHocDoiTuongSV", Db.P("@MSSV", MSSV));
            var doiTuong = doiTuongDt.Rows.Count > 0 ? doiTuongDt.Rows[0]["DoiTuong"].ToString() : null;
            if (doiTuong == "ChinhSach" && giuong["LoaiPhong"].ToString() == "4")
                return ("Sinh viên diện chính sách chỉ được đăng ký phòng tiêu chuẩn 6-8 giường.", giuong, null);

            return (null, giuong, d);
        }

        // ============ Hợp đồng đăng ký + Gia hạn ============
        public IActionResult HopDong()
        {
            ViewBag.DsHopDong = Db.QueryProc("sp_DsHopDong", Db.P("@MSSV", MSSV));
            var sv = Db.QueryProc("sp_LayKhoaHocDoiTuongSV", Db.P("@MSSV", MSSV));
            ViewBag.DoiTuong = sv.Rows.Count > 0 ? sv.Rows[0]["DoiTuong"].ToString() : null;
            return View();
        }

        [HttpPost]
        public IActionResult GiaHan(int maPhieu, int soThang)
        {
            var pd = Db.QueryProc("sp_KiemTraHopDong", Db.P("@MaPhieu", maPhieu), Db.P("@MSSV", MSSV));
            if (pd.Rows.Count == 0) { TempData["Loi"] = "Không tìm thấy hợp đồng đang hiệu lực."; return RedirectToAction("HopDong"); }

            var svDt = Db.QueryProc("sp_LayKhoaHocDoiTuongSV", Db.P("@MSSV", MSSV));
            var doiTuong = svDt.Rows.Count > 0 ? svDt.Rows[0]["DoiTuong"].ToString() : null;

            // SV thường: chỉ gia hạn khi cổng đang mở; SV chính sách gia hạn bất kỳ lúc nào
            if (doiTuong != "ChinhSach")
            {
                var dangMo = Db.ScalarProc("sp_DemDotDangMo");
                if (Convert.ToInt32(dangMo) == 0)
                {
                    TempData["Loi"] = "Cổng gia hạn hiện đã đóng. Chỉ sinh viên diện chính sách được gia hạn ngoài thời hạn.";
                    return RedirectToAction("HopDong");
                }
            }

            int ts2 = int.Parse(Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", "TS2"))!.ToString()!);
            if (soThang < 1 || soThang > ts2)
            {
                TempData["Loi"] = $"Số tháng gia hạn phải từ 1 đến {ts2} tháng.";
                return RedirectToAction("HopDong");
            }

            Db.ExecProc("sp_GiaHan",
                Db.P("@MaPhieu", maPhieu), Db.P("@SoThang", soThang), Db.P("@MSSV", MSSV),
                Db.P("@NoiDung", $"Hợp đồng #{maPhieu} đã được gia hạn thêm {soThang} tháng."));
            TempData["ThanhCong"] = $"Gia hạn hợp đồng thành công thêm {soThang} tháng.";
            return RedirectToAction("HopDong");
        }

        // ============ Yêu cầu trả phòng ============
        [HttpPost]
        public IActionResult TraPhong(int maPhieu)
        {
            var pd = Db.QueryProc("sp_ThongTinHopDong", Db.P("@MaPhieu", maPhieu), Db.P("@MSSV", MSSV));
            if (pd.Rows.Count == 0) { TempData["Loi"] = "Không tìm thấy hợp đồng."; return RedirectToAction("HopDong"); }
            string maPhong = pd.Rows[0]["MaPhong"].ToString()!;

            var noHD = Db.ScalarProc("sp_KiemTraNoHoaDon", Db.P("@MaPhong", maPhong));
            if (Convert.ToInt32(noHD) > 0)
            {
                TempData["Loi"] = "Phòng còn hóa đơn chưa thanh toán. Vui lòng hoàn thành nghĩa vụ tài chính trước khi trả phòng.";
                return RedirectToAction("HopDong");
            }

            Db.ExecProc("sp_TaoYeuCauTraPhong",
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
            ViewBag.DsDon = Db.QueryProc("sp_DsDonSV", Db.P("@MSSV", MSSV), Db.P("@LoaiDon", loai));
            return View();
        }

        [HttpGet]
        public IActionResult TaoDon(string loai = "PhanHoi") { ViewBag.Loai = loai; return View(); }

        [HttpPost]
        public IActionResult TaoDon(string loai, string tieuDe, string noiDung)
        {
            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
            {
                ViewBag.Loai = loai; ViewBag.Loi = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
                return View();
            }
            Db.ExecProc("sp_TaoDon",
                Db.P("@MSSV", MSSV), Db.P("@LoaiDon", loai), Db.P("@TieuDe", tieuDe.Trim()), Db.P("@NoiDung", noiDung.Trim()));
            TempData["ThanhCong"] = loai == "PhanHoi" ? "Gửi đơn phản hồi thành công." : "Gửi đơn đề xuất thành công.";
            return RedirectToAction("DonYeuCau", new { loai });
        }

        // Chi tiết đơn
        public IActionResult ChiTietDon(int id)
        {
            var dt = Db.QueryProc("sp_ChiTietDonSV", Db.P("@MaDon", id), Db.P("@MSSV", MSSV));
            if (dt.Rows.Count == 0) return RedirectToAction("DonYeuCau");
            ViewBag.Don = dt.Rows[0];
            return View();
        }

        // ============ Lịch sử + tra cứu hóa đơn ============
        public IActionResult HoaDon(string? thang, string? trangThai)
        {
            ViewBag.DsHoaDon = Db.QueryProc("sp_DsHoaDonSV",
                Db.P("@MSSV", MSSV),
                Db.P("@Thang", string.IsNullOrWhiteSpace(thang) ? null : thang),
                Db.P("@TrangThai", string.IsNullOrWhiteSpace(trangThai) ? null : trangThai));
            return View();
        }

        // ============ Chi tiết hóa đơn & thanh toán ============
        public IActionResult ChiTietHoaDon(int id)
        {
            var dt = Db.QueryProc("sp_ChiTietHoaDon", Db.P("@MaHD", id));
            if (dt.Rows.Count == 0) return RedirectToAction("HoaDon");
            ViewBag.HD = dt.Rows[0];
            ViewBag.DsGiaoDich = Db.QueryProc("sp_DsGiaoDich", Db.P("@MaHD", id));
            return View();
        }

        // Mô phỏng cổng thanh toán trực tuyến
        [HttpPost]
        public IActionResult ThanhToan(int maHD, string phuongThuc)
        {
            var hd = Db.QueryProc("sp_KiemTraHoaDon", Db.P("@MaHD", maHD));
            if (hd.Rows.Count == 0 || hd.Rows[0]["TrangThai"].ToString() == "DaThanhToan")
            {
                TempData["Loi"] = "Hóa đơn không hợp lệ hoặc đã được thanh toán.";
                return RedirectToAction("HoaDon");
            }

            string maGDCong = phuongThuc.ToUpper() + DateTime.Now.ToString("yyyyMMddHHmmss");
            Db.ExecProc("sp_ThanhToan",
                Db.P("@MaHD", maHD), Db.P("@MSSV", MSSV), Db.P("@PhuongThuc", phuongThuc),
                Db.P("@SoTien", hd.Rows[0]["TongTien"]), Db.P("@MaGDCong", maGDCong),
                Db.P("@NoiDungTB", $"Thanh toán hóa đơn #{maHD} thành công qua {phuongThuc}. Mã giao dịch: {maGDCong}."));
            TempData["ThanhCong"] = $"Thanh toán thành công qua {phuongThuc}! Mã giao dịch: {maGDCong}";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }

    }
}
