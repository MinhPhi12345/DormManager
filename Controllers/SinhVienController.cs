using DormManager.Data;
using DormManager.Helpers;
using DormManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DormManager.Controllers
{
    [PhanQuyen("SV")]
    public class SinhVienController : Controller
    {
        private readonly AppDbContext _db;

        public SinhVienController(AppDbContext db)
        {
            _db = db;
        }

        private string MSSV => HttpContext.Session.GetString("MSSV")!;
        // QD05: SV quá hạn thanh toán bị khóa quyền đăng ký dịch vụ tiện ích phát sinh (đăng ký/gia hạn),
        // nhưng vẫn đăng nhập và thanh toán được để tự gỡ khóa.
        private bool BiKhoa => HttpContext.Session.GetString("BiKhoa") == "1";

        /// <summary>QD02: SV diện chính sách chỉ được ở loại phòng tiêu chuẩn theo tham số TS9 (dạng "min-max",
        /// vd "6-8"). LoaiPhong chỉ có 3 giá trị 4/6/8 theo ràng buộc CHECK của bảng PHONG, nên hàm này trả về
        /// danh sách các LoaiPhong hợp lệ để dùng trong LINQ .Contains() (dịch được sang SQL, không như gọi
        /// thẳng 1 hàm C# tùy ý bên trong .Where() trên IQueryable). Nếu TS9 sai định dạng thì không chặn gì
        /// cả (an toàn - tránh khóa cứng sinh viên vì lỗi cấu hình).</summary>
        private static List<string> LoaiPhongHopLeChoChinhSach()
        {
            var tatCa = new List<string> { "4", "6", "8" };
            string ts9 = CommonRepo.LayThamSo("TS9") ?? "";
            var parts = ts9.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                return tatCa.Where(l => int.Parse(l) >= min && int.Parse(l) <= max).ToList();
            return tatCa;
        }

        // ============ Thông tin tổng quan ============
        public async Task<IActionResult> TongQuan()
        {
            // QD07: giả lập job quét + gửi nhắc nhở hạn thanh toán mỗi khi có SV mở trang Tổng quan
            // (cùng cách "giả lập job nền" đã dùng cho quét hóa đơn quá hạn ở vai trò Quản lý - xem
            // QuanLyRepo.QuetHoaDonQuaHan). sp_NguonNhacNhoHanThanhToan tự loại hóa đơn đã nhắc rồi
            // nên quét lại nhiều lần không gửi trùng email.
            QuetNhacNhoHanThanhToan();

            // sp_TongQuan: 1 lượt gọi -> 4 result set (Phòng đang ở, Bạn cùng phòng, Hóa đơn chưa TT, Đơn gần đây)
            var ds = SinhVienRepo.TongQuan(MSSV);
            ViewBag.PhongDangO = ds.Tables[0];
            ViewBag.BanCungPhong = ds.Tables[1];
            ViewBag.HoaDonChuaTT = ds.Tables[2];
            ViewBag.DonGanDay = ds.Tables[3];
            ViewBag.DsDotMo = SinhVienRepo.DotDangMoChoSV(MSSV);

            // Banner nhắc nhở: hiện các hóa đơn ĐÃ từng gửi nhắc nhở cho SV này mà vẫn CHƯA thanh toán
            // (tự động biến mất khi hóa đơn được thanh toán hoặc chuyển Quá hạn).
            var nhacNho = await _db.ThongBaos.Include(t => t.HoaDon)
                .Where(t => t.MSSV == MSSV && t.NoiDung.StartsWith("Nhắc nhở thanh toán:")
                    && t.HoaDon != null && t.HoaDon.TrangThai == "ChoThanhToan")
                .OrderByDescending(t => t.ThoiGianGui)
                .Select(t => new object?[] { t.HoaDon!.MaHD, t.HoaDon.MaPhong, t.HoaDon.Thang, t.HoaDon.TongTien, t.HoaDon.HanThanhToan })
                .ToListAsync();
            ViewBag.NhacNhoHanThanhToan = DataTableHelper.Build(
                new[] { "MaHD", "MaPhong", "Thang", "TongTien", "HanThanhToan" }, nhacNho);

            return View();
        }

        /// <summary>QD07: quét hóa đơn Chờ thanh toán còn tối đa TS5 ngày là đến hạn và CHƯA từng được
        /// nhắc, gửi email nhắc nhở thật (dùng lại CauHinhEmail như khi Quản lý gửi hóa đơn) rồi ghi
        /// lịch sử vào THONGBAO với nội dung bắt đầu bằng "Nhắc nhở thanh toán:" - đây là dấu hiệu để
        /// sp_NguonNhacNhoHanThanhToan biết hóa đơn nào đã nhắc rồi, tránh gửi trùng ở lần quét sau.</summary>
        private static void QuetNhacNhoHanThanhToan()
        {
            int ts5 = CommonRepo.LayThamSoInt("TS5");
            var ds = QuanLyRepo.NguonNhacNhoHanThanhToan(ts5);
            foreach (DataRow r in ds.Rows)
            {
                string email = r["Email"].ToString() ?? "";
                string hoTen = r["HoTen"].ToString() ?? "";
                string mssv = r["MSSV"].ToString()!;
                int maHD = Convert.ToInt32(r["MaHD"]);
                string maPhong = r["MaPhong"].ToString()!;
                string thang = r["Thang"].ToString()!;
                decimal tongTien = Convert.ToDecimal(r["TongTien"]);
                DateTime hanTT = Convert.ToDateTime(r["HanThanhToan"]);
                int soNgayConLai = Convert.ToInt32(r["SoNgayConLai"]);

                string noiDung = $"Nhắc nhở thanh toán: Hóa đơn tháng {thang} phòng {maPhong} ({tongTien:N0}đ) còn {soNgayConLai} ngày nữa là đến hạn ({hanTT:dd/MM/yyyy}). Vui lòng thanh toán sớm để tránh bị khóa quyền dịch vụ hoặc ghi vi phạm.";

                bool daGuiThat = CauHinhEmail.DaCauHinh && CauHinhEmail.Gui(
                    email,
                    $"[KTX] Nhắc nhở: hóa đơn tháng {thang} phòng {maPhong} sắp đến hạn thanh toán",
                    $"<p>Chào {hoTen},</p>" +
                    $"<p>Hóa đơn tháng <b>{thang}</b> phòng <b>{maPhong}</b> còn <b>{soNgayConLai} ngày</b> nữa là đến hạn thanh toán (<b>{hanTT:dd/MM/yyyy}</b>).</p>" +
                    $"<p>Số tiền cần thanh toán: <b>{tongTien:N0}đ</b>.</p>" +
                    $"<p>Vui lòng đăng nhập hệ thống và thanh toán sớm để tránh bị khóa quyền đăng ký dịch vụ tiện ích hoặc bị ghi nhận vi phạm nếu quá hạn.</p>" +
                    $"<p><i>Đây là email tự động, vui lòng không trả lời.</i></p>");

                string trangThai = CauHinhEmail.DaCauHinh ? (daGuiThat ? "ThanhCong" : "ThatBai") : "ThanhCong";
                CommonRepo.ThemThongBao(mssv, maHD, noiDung, "Email", trangThai);
            }
        }

        // ============ Tra cứu phòng ============
        public async Task<IActionResult> TraCuuPhong(string? tuKhoa, string? maToa, string? loaiPhong, string? mucGia)
        {
            // SV diện chính sách chỉ được xem phòng tiêu chuẩn 6-8 giường
            var sv = await _db.SinhViens.FindAsync(MSSV);
            bool laChinhSach = sv?.DoiTuong == "ChinhSach";

            var q = _db.Phongs.Include(p => p.ToaNha).Where(p => p.TrangThai == "HoatDong").AsQueryable();
            if (laChinhSach)
            {
                var dsLoaiHopLe = LoaiPhongHopLeChoChinhSach();
                q = q.Where(p => dsLoaiHopLe.Contains(p.LoaiPhong));
            }
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(p => p.MaPhong.Contains(tk) || p.ToaNha!.TenToa.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(maToa)) q = q.Where(p => p.MaToa == maToa);
            if (!string.IsNullOrWhiteSpace(loaiPhong)) q = q.Where(p => p.LoaiPhong == loaiPhong);
            if (!string.IsNullOrWhiteSpace(mucGia))
            {
                if (mucGia == "duoi400") q = q.Where(p => p.GiaPhong < 400000);
                else if (mucGia == "400den600") q = q.Where(p => p.GiaPhong >= 400000 && p.GiaPhong <= 600000);
                else if (mucGia == "tren600") q = q.Where(p => p.GiaPhong > 600000);
            }

            var dsPhong = await q.OrderBy(p => p.MaToa).ThenBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .Select(p => new object?[]
                {
                    p.MaPhong, p.Tang, p.LoaiPhong, p.SoGiuong, p.SoGiuongTrong,
                    p.GiaPhong, p.TrangThai, p.ToaNha!.TenToa, p.MaToa
                })
                .ToListAsync();
            ViewBag.DsPhong = DataTableHelper.Build(
                new[] { "MaPhong", "Tang", "LoaiPhong", "SoGiuong", "SoGiuongTrong", "GiaPhong", "TrangThai", "TenToa", "MaToa" }, dsPhong);

            var toas = await _db.ToaNhas.OrderBy(t => t.MaToa).Select(t => new object?[] { t.MaToa, t.TenToa }).ToListAsync();
            ViewBag.DsToa = DataTableHelper.Build(new[] { "MaToa", "TenToa" }, toas);
            ViewBag.LaChinhSach = laChinhSach;
            ViewBag.DsDotMo = SinhVienRepo.DotDangMoChoSV(MSSV);
            return View();
        }

        // ============ Chi tiết thông tin phòng ============
        public async Task<IActionResult> ChiTietPhong(string id)
        {
            var p = await _db.Phongs.Include(x => x.ToaNha).FirstOrDefaultAsync(x => x.MaPhong == id);
            if (p == null) return RedirectToAction("TraCuuPhong");

            ViewBag.Phong = DataTableHelper.BuildRow(
                new[] { "MaPhong", "MaToa", "Tang", "LoaiPhong", "SoGiuong", "GiaPhong", "TrangThai", "SoGiuongTrong", "TenToa", "DiaChi" },
                new object?[] { p.MaPhong, p.MaToa, p.Tang, p.LoaiPhong, p.SoGiuong, p.GiaPhong, p.TrangThai, p.SoGiuongTrong, p.ToaNha?.TenToa, p.ToaNha?.DiaChi });

            var dsGiuong = await _db.Giuongs.Where(g => g.MaPhong == id).OrderBy(g => g.MaGiuong)
                .Select(g => new object?[] { g.MaGiuong, g.TrangThai }).ToListAsync();
            ViewBag.DsGiuong = DataTableHelper.Build(new[] { "MaGiuong", "TrangThai" }, dsGiuong);

            var thanhVien = await _db.PhieuDangKys.Where(pd => pd.Giuong!.MaPhong == id && pd.TrangThai == "DangO")
                .Select(pd => new object?[] { pd.SinhVien!.HoTen, pd.SinhVien.MSSV, pd.SinhVien.KhoaHoc })
                .ToListAsync();
            ViewBag.ThanhVien = DataTableHelper.Build(new[] { "HoTen", "MSSV", "KhoaHoc" }, thanhVien);

            var dsAnh = await _db.AnhPhongs.Where(a => a.MaPhong == id).OrderBy(a => a.ThuTu).ThenBy(a => a.MaAnh)
                .Select(a => new object?[] { a.DuongDan }).ToListAsync();
            ViewBag.DsAnh = DataTableHelper.Build(new[] { "DuongDan" }, dsAnh);

            return View();
        }

        // ============ Đăng ký ở KTX / Đăng ký trước / Học kỳ hè ============
        [HttpGet]
        public async Task<IActionResult> DangKy(string maGiuong)
        {
            var (loi, giuong, dot) = await KiemTraDangKyAsync(maGiuong);
            if (giuong == null) { TempData["Loi"] = loi; return RedirectToAction("TraCuuPhong"); }
            ViewBag.Loi = loi;
            ViewBag.Giuong = giuong;
            ViewBag.Dot = dot;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> XacNhanDangKy(string maGiuong)
        {
            if (BiKhoa)
            {
                TempData["Loi"] = "Tài khoản đang bị khóa quyền đăng ký do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.";
                return RedirectToAction("TraCuuPhong");
            }

            var (loi, giuong, dot) = await KiemTraDangKyAsync(maGiuong);
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
        private async Task<(string? loi, DataRow? giuong, DataRow? dot)> KiemTraDangKyAsync(string maGiuong)
        {
            var g = await _db.Giuongs.Include(x => x.Phong).ThenInclude(p => p!.ToaNha)
                .Where(x => x.MaGiuong == maGiuong && x.Phong!.TrangThai == "HoatDong")
                .Select(x => new
                {
                    x.MaGiuong,
                    x.TrangThai,
                    MaPhong = x.Phong!.MaPhong,
                    x.Phong.LoaiPhong,
                    x.Phong.GiaPhong,
                    x.Phong.Tang,
                    TenToa = x.Phong.ToaNha!.TenToa
                })
                .FirstOrDefaultAsync();
            if (g == null) return ("Giường không tồn tại hoặc phòng ngừng hoạt động.", null, null);

            var giuongRow = DataTableHelper.BuildRow(
                new[] { "MaGiuong", "TrangThai", "MaPhong", "LoaiPhong", "GiaPhong", "Tang", "TenToa" },
                new object?[] { g.MaGiuong, g.TrangThai, g.MaPhong, g.LoaiPhong, g.GiaPhong, g.Tang, g.TenToa });

            if (g.TrangThai != "Trong")
                return ("Giường này đã có người đăng ký. Vui lòng chọn giường khác.", giuongRow, null);

            // Đã có hợp đồng hiệu lực?
            bool daCo = await _db.PhieuDangKys.AnyAsync(p => p.MSSV == MSSV && (p.TrangThai == "ChoDoiChieu" || p.TrangThai == "DangO"));
            if (daCo)
                return ("Bạn đang có hợp đồng lưu trú hiệu lực, không thể đăng ký thêm.", giuongRow, null);

            // Đợt đăng ký đang mở
            var dot = await _db.DotDangKys
                .Where(d => DateTime.Now >= d.NgayMo && DateTime.Now <= d.NgayDong)
                .OrderBy(d => d.LoaiDot == "UuTien" ? 0 : 1)
                .FirstOrDefaultAsync();
            if (dot == null)
                return ("Hiện chưa có đợt đăng ký nào đang mở cổng. Vui lòng quay lại sau.", giuongRow, null);

            var dotRow = DataTableHelper.BuildRow(
                new[] { "MaDot", "TenDot", "LoaiDot", "HocKy", "NgayMo", "NgayDong", "TrangThai" },
                new object?[] { dot.MaDot, dot.TenDot, dot.LoaiDot, dot.HocKy, dot.NgayMo, dot.NgayDong, dot.TrangThai });

            var sv = await _db.SinhViens.FindAsync(MSSV);

            // Đợt ưu tiên: chỉ tân sinh viên (khóa mới nhất)
            if (dot.LoaiDot == "UuTien")
            {
                var khoaMoiNhat = await _db.SinhViens.MaxAsync(s => s.KhoaHoc);
                if (sv?.KhoaHoc != khoaMoiNhat)
                    return ("Đợt đăng ký ưu tiên chỉ dành cho Tân sinh viên. Vui lòng chờ đợt đại trà.", giuongRow, null);
            }

            // SV chính sách chỉ được phòng tiêu chuẩn theo TS9 (QD02)
            if (sv?.DoiTuong == "ChinhSach" && !LoaiPhongHopLeChoChinhSach().Contains(g.LoaiPhong))
                return ($"Sinh viên diện chính sách chỉ được đăng ký phòng tiêu chuẩn {CommonRepo.LayThamSo("TS9")} giường.", giuongRow, null);

            return (null, giuongRow, dotRow);
        }

        // ============ Hợp đồng đăng ký + Gia hạn ============
        public async Task<IActionResult> HopDong()
        {
            var list = await _db.PhieuDangKys
                .Include(pd => pd.Giuong).ThenInclude(g => g!.Phong).ThenInclude(p => p!.ToaNha)
                .Include(pd => pd.DotDangKy)
                .Where(pd => pd.MSSV == MSSV)
                .OrderByDescending(pd => pd.NgayDangKy)
                .Select(pd => new object?[]
                {
                    pd.MaPhieu, pd.MaGiuong, pd.NgayDangKy, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai,
                    pd.Giuong!.Phong!.MaPhong, pd.Giuong.Phong.GiaPhong, pd.Giuong.Phong.ToaNha!.TenToa,
                    pd.DotDangKy!.TenDot, pd.DotDangKy.HocKy,
                    _db.DonYeuCaus.Where(d => d.MaPhieu == pd.MaPhieu && d.LoaiDon == "TraPhong" && (d.TrangThai == "ChoXuLy" || d.TrangThai == "DangXuLy"))
                        .Select(d => (int?)d.MaDon).FirstOrDefault(),
                    _db.DonYeuCaus.Where(d => d.MaPhieu == pd.MaPhieu && d.LoaiDon == "ChuyenPhong" && (d.TrangThai == "ChoXuLy" || d.TrangThai == "DangXuLy"))
                        .Select(d => (int?)d.MaDon).FirstOrDefault()
                })
                .ToListAsync();
            ViewBag.DsHopDong = DataTableHelper.Build(
                new[] { "MaPhieu", "MaGiuong", "NgayDangKy", "NgayBatDau", "NgayKetThuc", "TrangThai",
                        "MaPhong", "GiaPhong", "TenToa", "TenDot", "HocKy", "MaDonTraPhongChoXL", "MaDonChuyenPhongChoXL" }, list);

            var sv = await _db.SinhViens.FindAsync(MSSV);
            ViewBag.DoiTuong = sv?.DoiTuong;

            // Cho View biết ngưỡng nghiệp vụ để hiển thị đúng trạng thái/giải thích cho từng hợp đồng
            ViewBag.TS2 = CommonRepo.LayThamSoInt("TS2");    // số tháng tối đa mỗi lần gia hạn (1 học kỳ)
            ViewBag.TS13 = CommonRepo.LayThamSoInt("TS13");  // chỉ được gia hạn khi còn <= TS13 ngày là hết hạn
            ViewBag.CongGiaHanDangMo = await _db.DotDangKys.AnyAsync(d =>
                d.LoaiDot != "UuTien" && DateTime.Now >= d.NgayMo && DateTime.Now <= d.NgayDong);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GiaHan(int maPhieu, int soThang)
        {
            if (BiKhoa)
            {
                TempData["Loi"] = "Tài khoản đang bị khóa quyền gia hạn do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.";
                return RedirectToAction("HopDong");
            }

            var pd = await _db.PhieuDangKys.FirstOrDefaultAsync(p => p.MaPhieu == maPhieu && p.MSSV == MSSV && p.TrangThai == "DangO");
            if (pd == null) { TempData["Loi"] = "Không tìm thấy hợp đồng đang hiệu lực."; return RedirectToAction("HopDong"); }

            // Chặn gia hạn chồng chất: chỉ được gia hạn khi hợp đồng còn tối đa TS13 ngày là hết hạn (hoặc đã hết hạn).
            // Áp dụng cho MỌI sinh viên kể cả diện chính sách - đặc quyền "không cần chờ đợt" của SV chính sách
            // chỉ miễn điều kiện đợt mở cổng bên dưới, không phải giấy phép gia hạn dồn dập nhiều lần liên tiếp
            // (TS2 = số tháng tối đa của MỘT học kỳ sẽ vô nghĩa nếu gia hạn được lúc nào cũng được).
            int ts13 = CommonRepo.LayThamSoInt("TS13");
            int soNgayConLai = (pd.NgayKetThuc.Date - DateTime.Today).Days;
            if (soNgayConLai > ts13)
            {
                TempData["Loi"] = $"Hợp đồng còn {soNgayConLai} ngày mới hết hạn. Chỉ được gia hạn khi còn tối đa {ts13} ngày (tránh gia hạn chồng chất nhiều lần liên tiếp).";
                return RedirectToAction("HopDong");
            }

            var sv = await _db.SinhViens.FindAsync(MSSV);
            var doiTuong = sv?.DoiTuong;

            // SV thường: chỉ gia hạn khi đợt Đại trà/Học kỳ hè đang mở - KHÔNG tính đợt Ưu tiên vì đợt đó
            // chỉ dành riêng cho Tân sinh viên đăng ký lần đầu, không liên quan đến việc gia hạn của SV đang ở.
            // SV chính sách: không cần chờ đợt nào cả (vẫn phải tuân thủ mốc TS13 ở trên).
            if (doiTuong != "ChinhSach")
            {
                bool dangMo = await _db.DotDangKys.AnyAsync(d => d.LoaiDot != "UuTien" && DateTime.Now >= d.NgayMo && DateTime.Now <= d.NgayDong);
                if (!dangMo)
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
        public async Task<IActionResult> TraPhong(int maPhieu)
        {
            var pd = await _db.PhieuDangKys.Include(p => p.Giuong)
                .FirstOrDefaultAsync(p => p.MaPhieu == maPhieu && p.MSSV == MSSV && p.TrangThai == "DangO");
            if (pd == null) { TempData["Loi"] = "Không tìm thấy hợp đồng."; return RedirectToAction("HopDong"); }
            string maPhong = pd.Giuong!.MaPhong;

            bool noHD = await _db.HoaDons.AnyAsync(h => h.MaPhong == maPhong && (h.TrangThai == "ChoThanhToan" || h.TrangThai == "QuaHan"));
            if (noHD)
            {
                TempData["Loi"] = "Phòng còn hóa đơn chưa thanh toán. Vui lòng hoàn thành nghĩa vụ tài chính trước khi trả phòng.";
                return RedirectToAction("HopDong");
            }

            _db.DonYeuCaus.Add(new DonYeuCau
            {
                MSSV = MSSV,
                LoaiDon = "TraPhong",
                TieuDe = $"Yêu cầu trả phòng {maPhong}",
                NoiDung = $"Sinh viên yêu cầu trả phòng {maPhong} (hợp đồng #{maPhieu}). Vui lòng đối chiếu và xác nhận.",
                MucUuTien = "Cao", // Chuyển/trả phòng ảnh hưởng trực tiếp đến chỗ ở của SV -> ưu tiên cao mặc định
                TrangThai = "ChoXuLy",
                MaPhieu = maPhieu
            });
            await _db.SaveChangesAsync();

            TempData["ThanhCong"] = "Đã gửi yêu cầu trả phòng tới Ban quản lý. Vui lòng chờ xác nhận và mang chìa khóa/thẻ từ khi bàn giao phòng.";
            return RedirectToAction("HopDong");
        }

        // ============ Đơn phản hồi / đề xuất ============
        public async Task<IActionResult> DonYeuCau(string loai = "PhanHoi")
        {
            ViewBag.Loai = loai;
            var list = await _db.DonYeuCaus.Where(d => d.MSSV == MSSV && d.LoaiDon == loai)
                .OrderByDescending(d => d.NgayTao)
                .Select(d => new object?[] { d.MaDon, d.TieuDe, d.MucUuTien, d.TrangThai, d.NgayTao })
                .ToListAsync();
            ViewBag.DsDon = DataTableHelper.Build(new[] { "MaDon", "TieuDe", "MucUuTien", "TrangThai", "NgayTao" }, list);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TaoDon(string loai = "PhanHoi")
        {
            if (!await DangOPhongAsync())
            {
                TempData["Loi"] = "Bạn cần đang ở một phòng trong ký túc xá mới có thể gửi đơn phản hồi/đề xuất.";
                return RedirectToAction("DonYeuCau", new { loai });
            }
            ViewBag.Loai = loai; return View();
        }

        [HttpPost]
        public async Task<IActionResult> TaoDon(string loai, string tieuDe, string noiDung)
        {
            if (!await DangOPhongAsync())
            {
                TempData["Loi"] = "Bạn cần đang ở một phòng trong ký túc xá mới có thể gửi đơn phản hồi/đề xuất.";
                return RedirectToAction("DonYeuCau", new { loai });
            }
            if (string.IsNullOrWhiteSpace(tieuDe) || string.IsNullOrWhiteSpace(noiDung))
            {
                ViewBag.Loai = loai; ViewBag.Loi = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
                return View();
            }

            _db.DonYeuCaus.Add(new DonYeuCau { MSSV = MSSV, LoaiDon = loai, TieuDe = tieuDe.Trim(), NoiDung = noiDung.Trim() });
            await _db.SaveChangesAsync();

            TempData["ThanhCong"] = loai == "PhanHoi" ? "Gửi đơn phản hồi thành công." : "Gửi đơn đề xuất thành công.";
            return RedirectToAction("DonYeuCau", new { loai });
        }

        /// <summary>SV chỉ được gửi đơn phản hồi/đề xuất khi đang có phòng hiệu lực (tránh đơn "ma" khi chưa ở KTX).</summary>
        private async Task<bool> DangOPhongAsync()
            => await _db.PhieuDangKys.AnyAsync(p => p.MSSV == MSSV && p.TrangThai == "DangO");

        // Chi tiết đơn
        public async Task<IActionResult> ChiTietDon(int id)
        {
            var d = await _db.DonYeuCaus.Include(x => x.QuanLy).FirstOrDefaultAsync(x => x.MaDon == id && x.MSSV == MSSV);
            if (d == null) return RedirectToAction("DonYeuCau");

            ViewBag.Don = DataTableHelper.BuildRow(
                new[] { "MaDon", "MSSV", "MaNV", "MaPhieu", "MaGiuongMoi", "LoaiDon", "TieuDe", "NoiDung", "MucUuTien", "TrangThai", "PhanHoi", "NgayTao", "TenNV" },
                new object?[] { d.MaDon, d.MSSV, d.MaNV, d.MaPhieu, d.MaGiuongMoi, d.LoaiDon, d.TieuDe, d.NoiDung, d.MucUuTien, d.TrangThai, d.PhanHoi, d.NgayTao, d.QuanLy?.HoTen });
            return View();
        }

        // Xóa đơn phản hồi/đề xuất - chỉ cho xóa khi đơn của chính mình và CHƯA được quản lý xử lý
        [HttpPost]
        public async Task<IActionResult> XoaDon(int maDon)
        {
            var d = await _db.DonYeuCaus.FirstOrDefaultAsync(x => x.MaDon == maDon && x.MSSV == MSSV);
            if (d == null) { TempData["Loi"] = "Không tìm thấy đơn."; return RedirectToAction("DonYeuCau"); }

            string loai = d.LoaiDon;
            if (d.TrangThai != "ChoXuLy")
            {
                TempData["Loi"] = "Đơn đã được Ban quản lý tiếp nhận xử lý nên không thể xóa.";
                return RedirectToAction("ChiTietDon", new { id = maDon });
            }

            _db.DonYeuCaus.Remove(d);
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã xóa đơn.";
            return RedirectToAction("DonYeuCau", new { loai });
        }

        // ============ Đăng ký chuyển phòng (bước 1: chọn phòng - lưới phòng như Tra cứu phòng) ============
        [HttpGet]
        public async Task<IActionResult> ChuyenPhong(int maPhieu, string? tuKhoa, string? maToa, string? loaiPhong, string? mucGia)
        {
            var (loi, pd) = await KiemTraChuyenPhongAsync(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var sv = await _db.SinhViens.FindAsync(MSSV);
            bool laChinhSach = sv?.DoiTuong == "ChinhSach";

            string maPhongHienTai = pd["MaPhong"].ToString()!;
            var q = _db.Phongs.Include(p => p.ToaNha)
                .Where(p => p.TrangThai == "HoatDong" && p.SoGiuongTrong > 0 && p.MaPhong != maPhongHienTai)
                .AsQueryable();
            if (laChinhSach)
            {
                var dsLoaiHopLe = LoaiPhongHopLeChoChinhSach();
                q = q.Where(p => dsLoaiHopLe.Contains(p.LoaiPhong));
            }
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(p => p.MaPhong.Contains(tk) || p.ToaNha!.TenToa.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(maToa)) q = q.Where(p => p.MaToa == maToa);
            if (!string.IsNullOrWhiteSpace(loaiPhong)) q = q.Where(p => p.LoaiPhong == loaiPhong);
            if (!string.IsNullOrWhiteSpace(mucGia))
            {
                if (mucGia == "duoi400") q = q.Where(p => p.GiaPhong < 400000);
                else if (mucGia == "400den600") q = q.Where(p => p.GiaPhong >= 400000 && p.GiaPhong <= 600000);
                else if (mucGia == "tren600") q = q.Where(p => p.GiaPhong > 600000);
            }

            var dsPhong = await q.OrderBy(p => p.MaToa).ThenBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .Select(p => new object?[] { p.MaPhong, p.Tang, p.LoaiPhong, p.SoGiuong, p.SoGiuongTrong, p.GiaPhong, p.ToaNha!.TenToa, p.MaToa })
                .ToListAsync();

            ViewBag.Phieu = pd;
            ViewBag.LaChinhSach = laChinhSach;
            var toas = await _db.ToaNhas.OrderBy(t => t.MaToa).Select(t => new object?[] { t.MaToa, t.TenToa }).ToListAsync();
            ViewBag.DsToa = DataTableHelper.Build(new[] { "MaToa", "TenToa" }, toas);
            ViewBag.DsPhong = DataTableHelper.Build(
                new[] { "MaPhong", "Tang", "LoaiPhong", "SoGiuong", "SoGiuongTrong", "GiaPhong", "TenToa", "MaToa" }, dsPhong);
            return View();
        }

        // ============ Đăng ký chuyển phòng (bước 2: chọn giường trống trong phòng đã chọn) ============
        [HttpGet]
        public async Task<IActionResult> ChonGiuongChuyenPhong(int maPhieu, string maPhongMoi)
        {
            var (loi, pd) = await KiemTraChuyenPhongAsync(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var phong = await _db.Phongs.Include(p => p.ToaNha)
                .FirstOrDefaultAsync(p => p.MaPhong == maPhongMoi && p.TrangThai == "HoatDong");
            if (phong == null || phong.MaPhong == pd["MaPhong"].ToString())
            {
                TempData["Loi"] = "Phòng không hợp lệ để chuyển đến.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            // QD02: chặn sớm nếu SV diện chính sách cố tình sửa URL để nhảy tới phòng không đúng loại
            // tiêu chuẩn (danh sách ở bước 1 chỉ lọc hiển thị, không phải kiểm soát bảo mật).
            var svKiemTra = await _db.SinhViens.FindAsync(MSSV);
            if (svKiemTra?.DoiTuong == "ChinhSach" && !LoaiPhongHopLeChoChinhSach().Contains(phong.LoaiPhong))
            {
                TempData["Loi"] = $"Sinh viên diện chính sách chỉ được chuyển đến phòng tiêu chuẩn {CommonRepo.LayThamSo("TS9")} giường.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            ViewBag.Phieu = pd;
            ViewBag.PhongMoi = DataTableHelper.BuildRow(
                new[] { "MaPhong", "MaToa", "Tang", "LoaiPhong", "SoGiuong", "GiaPhong", "TrangThai", "SoGiuongTrong", "TenToa" },
                new object?[] { phong.MaPhong, phong.MaToa, phong.Tang, phong.LoaiPhong, phong.SoGiuong, phong.GiaPhong, phong.TrangThai, phong.SoGiuongTrong, phong.ToaNha?.TenToa });

            var dsGiuong = await _db.Giuongs.Where(g => g.MaPhong == maPhongMoi).OrderBy(g => g.MaGiuong)
                .Select(g => new object?[] { g.MaGiuong, g.TrangThai }).ToListAsync();
            ViewBag.DsGiuong = DataTableHelper.Build(new[] { "MaGiuong", "TrangThai" }, dsGiuong);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GuiDonChuyenPhong(int maPhieu, string maGiuongMoi)
        {
            var (loi, pd) = await KiemTraChuyenPhongAsync(maPhieu);
            if (pd == null) { TempData["Loi"] = loi; return RedirectToAction("HopDong"); }

            var g = await _db.Giuongs.Include(x => x.Phong).FirstOrDefaultAsync(x => x.MaGiuong == maGiuongMoi);
            if (g == null || g.TrangThai != "Trong" || g.Phong!.TrangThai != "HoatDong")
            {
                TempData["Loi"] = "Giường bạn chọn không còn trống. Vui lòng chọn giường khác.";
                if (g != null)
                    return RedirectToAction("ChonGiuongChuyenPhong", new { maPhieu, maPhongMoi = g.MaPhong });
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            string maPhongCu = pd["MaPhong"].ToString()!;
            string maPhongMoi = g.MaPhong;
            if (maPhongMoi == maPhongCu)
            {
                TempData["Loi"] = "Bạn đang ở chính phòng này rồi, vui lòng chọn phòng khác.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            // QD02: kiểm tra lại LẦN CUỐI ngay trước khi ghi yêu cầu - đây là điểm chặn thật sự (bảo mật),
            // không phụ thuộc vào bộ lọc hiển thị ở các bước trước (có thể bị bỏ qua bằng cách sửa URL/form).
            var sv = await _db.SinhViens.FindAsync(MSSV);
            if (sv?.DoiTuong == "ChinhSach" && !LoaiPhongHopLeChoChinhSach().Contains(g.Phong!.LoaiPhong))
            {
                TempData["Loi"] = $"Sinh viên diện chính sách chỉ được chuyển đến phòng tiêu chuẩn {CommonRepo.LayThamSo("TS9")} giường.";
                return RedirectToAction("ChuyenPhong", new { maPhieu });
            }

            _db.DonYeuCaus.Add(new DonYeuCau
            {
                MSSV = MSSV,
                LoaiDon = "ChuyenPhong",
                TieuDe = $"Yêu cầu chuyển phòng {maPhongCu} → {maPhongMoi}",
                NoiDung = $"Sinh viên yêu cầu chuyển từ phòng {maPhongCu} sang phòng {maPhongMoi} (giường {maGiuongMoi}). Vui lòng đối chiếu và xác nhận.",
                MucUuTien = "Cao", // Chuyển/trả phòng ảnh hưởng trực tiếp đến chỗ ở của SV -> ưu tiên cao mặc định
                TrangThai = "ChoXuLy",
                MaPhieu = maPhieu,
                MaGiuongMoi = maGiuongMoi
            });
            await _db.SaveChangesAsync();

            TempData["ThanhCong"] = "Đã gửi yêu cầu chuyển phòng tới Ban quản lý. Vui lòng chờ xác nhận.";
            return RedirectToAction("HopDong");
        }

        /// <summary>Kiểm tra điều kiện chuyển phòng: tài khoản không bị khóa do quá hạn thanh toán (QD05),
        /// hợp đồng đang ở, chưa có đơn chuyển/trả phòng nào đang chờ xử lý, đã ở đủ số ngày tối thiểu
        /// (TS11), không còn nợ hóa đơn phòng hiện tại.</summary>
        private async Task<(string? loi, DataRow? pd)> KiemTraChuyenPhongAsync(int maPhieu)
        {
            // QD05: SV bị khóa do quá hạn thanh toán không được đăng ký/gia hạn - áp dụng tương tự
            // cho chuyển phòng vì bản chất cũng là một hình thức đăng ký ở KTX (giường/hợp đồng mới).
            if (BiKhoa)
                return ("Tài khoản đang bị khóa quyền chuyển phòng do quá hạn thanh toán. Vui lòng thanh toán hết hóa đơn còn nợ trước.", null);

            var pd = await _db.PhieuDangKys.Include(p => p.Giuong).ThenInclude(g => g!.Phong)
                .Where(p => p.MaPhieu == maPhieu && p.MSSV == MSSV)
                .Select(p => new
                {
                    p.MaPhieu,
                    p.NgayBatDau,
                    p.NgayKetThuc,
                    p.TrangThai,
                    MaPhong = p.Giuong!.Phong!.MaPhong,
                    p.Giuong.Phong.LoaiPhong
                })
                .FirstOrDefaultAsync();
            if (pd == null) return ("Không tìm thấy hợp đồng.", null);

            if (pd.TrangThai != "DangO")
                return ("Chỉ có thể chuyển phòng khi hợp đồng đang ở trạng thái Đang ở.", null);

            var pdRow = DataTableHelper.BuildRow(
                new[] { "MaPhieu", "NgayBatDau", "NgayKetThuc", "TrangThai", "MaPhong", "LoaiPhong" },
                new object?[] { pd.MaPhieu, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai, pd.MaPhong, pd.LoaiPhong });

            bool coDonChoXL = await _db.DonYeuCaus.AnyAsync(d =>
                d.MaPhieu == maPhieu && (d.LoaiDon == "ChuyenPhong" || d.LoaiDon == "TraPhong") && (d.TrangThai == "ChoXuLy" || d.TrangThai == "DangXuLy"));
            if (coDonChoXL)
                return ("Bạn đang có yêu cầu chuyển phòng/trả phòng chờ xử lý cho hợp đồng này.", null);

            int soNgayToiThieu = CommonRepo.LayThamSoInt("TS11");
            int soNgayDaO = (DateTime.Today - pd.NgayBatDau).Days;
            if (soNgayDaO < soNgayToiThieu)
                return ($"Bạn cần ở tối thiểu {soNgayToiThieu} ngày trước khi được đăng ký chuyển phòng (hiện đã ở {Math.Max(soNgayDaO, 0)} ngày).", null);

            bool noHD = await _db.HoaDons.AnyAsync(h => h.MaPhong == pd.MaPhong && (h.TrangThai == "ChoThanhToan" || h.TrangThai == "QuaHan"));
            if (noHD)
                return ("Phòng hiện tại còn hóa đơn chưa thanh toán. Vui lòng hoàn thành nghĩa vụ tài chính trước khi chuyển phòng.", null);

            return (null, pdRow);
        }

        // ============ Lịch sử + tra cứu hóa đơn ============
        public async Task<IActionResult> HoaDon(string? thang, string? trangThai)
        {
            var q = _db.HoaDons
                .Where(h => h.TrangThai != "Nhap" && h.Phong!.Giuongs.Any(g => g.PhieuDangKys.Any(pd => pd.MSSV == MSSV)))
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(thang)) q = q.Where(h => h.Thang == thang);
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(h => h.TrangThai == trangThai);

            var list = await q.Distinct().OrderByDescending(h => h.MaHD)
                .Select(h => new object?[] { h.MaHD, h.MaPhong, h.Thang, h.TongTien, h.NgayPhatHanh, h.HanThanhToan, h.TrangThai })
                .ToListAsync();
            ViewBag.DsHoaDon = DataTableHelper.Build(
                new[] { "MaHD", "MaPhong", "Thang", "TongTien", "NgayPhatHanh", "HanThanhToan", "TrangThai" }, list);
            return View();
        }

        // ============ Chi tiết hóa đơn & thanh toán ============
        public async Task<IActionResult> ChiTietHoaDon(int id)
        {
            var h = await _db.HoaDons.Include(x => x.ChiSoDienNuoc).Include(x => x.DonGia)
                .Include(x => x.Phong).ThenInclude(p => p!.ToaNha)
                .FirstOrDefaultAsync(x => x.MaHD == id);
            if (h == null) return RedirectToAction("HoaDon");

            var hdRow = DataTableHelper.BuildRow(
                new[] { "MaHD", "MaPhong", "MaChiSo", "MaDonGia", "Thang", "TienPhong", "TienDien", "TienNuoc", "TongTien",
                        "NgayPhatHanh", "HanThanhToan", "TrangThai", "DienDauKy", "DienCuoiKy", "NuocDauKy", "NuocCuoiKy",
                        "GiaDien", "GiaNuoc", "PhiDichVu", "TenToa" },
                new object?[]
                {
                    h.MaHD, h.MaPhong, h.MaChiSo, h.MaDonGia, h.Thang, h.TienPhong, h.TienDien, h.TienNuoc, h.TongTien,
                    h.NgayPhatHanh, h.HanThanhToan, h.TrangThai,
                    h.ChiSoDienNuoc?.DienDauKy, h.ChiSoDienNuoc?.DienCuoiKy, h.ChiSoDienNuoc?.NuocDauKy, h.ChiSoDienNuoc?.NuocCuoiKy,
                    h.DonGia?.GiaDien, h.DonGia?.GiaNuoc, h.DonGia?.PhiDichVu, h.Phong?.ToaNha?.TenToa
                });
            ViewBag.HD = hdRow;

            var giaoDich = await _db.ThanhToans.Where(t => t.MaHD == id).OrderByDescending(t => t.ThoiGian)
                .Select(t => new object?[] { t.MaGD, t.MaHD, t.MSSV, t.PhuongThuc, t.SoTien, t.MaGDCong, t.ThoiGian, t.KetQua })
                .ToListAsync();
            ViewBag.DsGiaoDich = DataTableHelper.Build(
                new[] { "MaGD", "MaHD", "MSSV", "PhuongThuc", "SoTien", "MaGDCong", "ThoiGian", "KetQua" }, giaoDich);

            // Mã QR chuyển khoản (VietQR) - tự điền đúng số tiền + nội dung "HD<mã hóa đơn>" để đối soát
            ViewBag.TenNganHang = CauHinhThanhToan.TenNganHang;
            ViewBag.SoTaiKhoan = CauHinhThanhToan.SoTaiKhoan;
            ViewBag.ChuTaiKhoan = CauHinhThanhToan.ChuTaiKhoan;
            ViewBag.QrUrl = CauHinhThanhToan.TaoUrlQr(h.TongTien, $"HD{id} {MSSV}");
            return View();
        }

        // Mô phỏng cổng thanh toán trực tuyến
        [HttpPost]
        public async Task<IActionResult> ThanhToan(int maHD, string phuongThuc)
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null || hd.TrangThai == "DaThanhToan")
            {
                TempData["Loi"] = "Hóa đơn không hợp lệ hoặc đã được thanh toán.";
                return RedirectToAction("HoaDon");
            }

            string maGDCong = phuongThuc.ToUpper() + DateTime.Now.ToString("yyyyMMddHHmmss");
            SinhVienRepo.ThanhToan(maHD, MSSV, phuongThuc, hd.TongTien, maGDCong,
                $"Thanh toán hóa đơn #{maHD} thành công qua {phuongThuc}. Mã giao dịch: {maGDCong}.");

            string thongBaoThem = "";
            if (BiKhoa)
            {
                bool conNo = await _db.HoaDons.AnyAsync(h => h.TrangThai == "QuaHan"
                    && h.Phong!.Giuongs.Any(g => g.PhieuDangKys.Any(pd => pd.MSSV == MSSV && pd.TrangThai == "DangO")));
                if (!conNo)
                {
                    var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.SinhVien!.MSSV == MSSV && t.TrangThai == "BiKhoa");
                    if (tk != null) { tk.TrangThai = "HoatDong"; await _db.SaveChangesAsync(); }
                    HttpContext.Session.SetString("BiKhoa", "0");
                    thongBaoThem = " Bạn đã thanh toán hết nợ nên tài khoản được tự động mở khóa.";
                }
            }

            TempData["ThanhCong"] = $"Thanh toán thành công qua {phuongThuc}! Mã giao dịch: {maGDCong}.{thongBaoThem}";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }
    }
}
