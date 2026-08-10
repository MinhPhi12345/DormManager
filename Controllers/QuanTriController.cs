using DormManager.Data;
using DormManager.Helpers;
using DormManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DormManager.Controllers
{
    [PhanQuyen("QTV")]
    public class QuanTriController : Controller
    {
        private readonly AppDbContext _db;

        public QuanTriController(AppDbContext db)
        {
            _db = db;
        }

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

            // Tính tổng các khoản thu của toàn bộ thời gian để vẽ biểu đồ tròn
            decimal tongTienPhong = 0, tongTienDien = 0, tongTienNuoc = 0;
            foreach (System.Data.DataRow r in dt.Rows)
            {
                tongTienPhong += r["TongTienPhong"] != DBNull.Value ? Convert.ToDecimal(r["TongTienPhong"]) : 0;
                tongTienDien += r["TongTienDien"] != DBNull.Value ? Convert.ToDecimal(r["TongTienDien"]) : 0;
                tongTienNuoc += r["TongTienNuoc"] != DBNull.Value ? Convert.ToDecimal(r["TongTienNuoc"]) : 0;
            }
            ViewBag.TyTrongDoanhThu = new decimal[] { tongTienPhong, tongTienDien, tongTienNuoc };

            return View();
        }

        // ============ Danh sách + tra cứu hóa đơn toàn hệ thống (dành cho QTV) ============
        public async Task<IActionResult> HoaDon(string? tuKhoa, string? thang, string? trangThai, int trang = 1)
        {
            // Phân biệt "chưa từng lọc" (mở trang lần đầu) với "đã lọc nhưng cố tình để trống" (đã submit form) -
            // chỉ tự động mặc định tháng hiện tại ở trường hợp đầu.
            bool daTungLoc = Request.Query.ContainsKey("tuKhoa") || Request.Query.ContainsKey("thang") || Request.Query.ContainsKey("trangThai");
            if (!daTungLoc) thang = DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;

            var q = _db.HoaDons.Include(h => h.Phong).ThenInclude(p => p!.ToaNha)
                .Where(h => h.TrangThai != "Nhap");
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(h => h.MaPhong.Contains(tk) || h.Phong!.ToaNha!.TenToa.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(thang)) q = q.Where(h => h.Thang == thang);
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(h => h.TrangThai == trangThai);

            int tongSoDong = await q.CountAsync();
            var (trangHT, tongSoTrang) = PagingHelper.Chuan(trang, tongSoDong);

            var list = await q.OrderByDescending(h => h.MaHD)
                .Skip((trangHT - 1) * PagingHelper.KichThuocTrangMacDinh).Take(PagingHelper.KichThuocTrangMacDinh)
                .Select(h => new object?[]
                {
                    h.MaHD, h.MaPhong, h.Thang, h.TienPhong, h.TienDien, h.TienNuoc,
                    h.TongTien, h.NgayPhatHanh, h.HanThanhToan, h.TrangThai, h.Phong!.ToaNha!.TenToa
                })
                .ToListAsync();
            ViewBag.DsHoaDon = DataTableHelper.Build(
                new[] { "MaHD", "MaPhong", "Thang", "TienPhong", "TienDien", "TienNuoc",
                        "TongTien", "NgayPhatHanh", "HanThanhToan", "TrangThai", "TenToa" }, list);
            ViewBag.Pager = PagingHelper.TaoPager(trangHT, tongSoTrang, tongSoDong, Request.Query);
            return View();
        }

        // ============ Danh sách + tra cứu phòng hệ thống ============
        public async Task<IActionResult> Phong(string? tuKhoa, string? maToa, string? trangThai)
        {
            var q = _db.Phongs.Include(p => p.ToaNha).AsQueryable();
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(p => p.MaPhong.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(maToa)) q = q.Where(p => p.MaToa == maToa);
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(p => p.TrangThai == trangThai);

            var list = await q
                .OrderBy(p => p.MaToa).ThenBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .Select(p => new object?[]
                {
                    p.MaPhong, p.MaToa, p.Tang, p.LoaiPhong, p.SoGiuong, p.GiaPhong, p.TrangThai, p.SoGiuongTrong,
                    p.ToaNha!.TenToa,
                    p.Giuongs.Count(g => g.PhieuDangKys.Any(pd => pd.TrangThai == "DangO" || pd.TrangThai == "ChoDoiChieu"))
                })
                .ToListAsync();
            ViewBag.DsPhong = DataTableHelper.Build(
                new[] { "MaPhong", "MaToa", "Tang", "LoaiPhong", "SoGiuong", "GiaPhong", "TrangThai", "SoGiuongTrong", "TenToa", "SoSVO" }, list);

            var toas = await _db.ToaNhas.OrderBy(t => t.MaToa)
                .Select(t => new object?[] { t.MaToa, t.TenToa, t.SoTang }).ToListAsync();
            ViewBag.DsToa = DataTableHelper.Build(new[] { "MaToa", "TenToa", "SoTang" }, toas);
            return View();
        }

        // ============ Thêm phòng mới ============
        [HttpPost]
        public async Task<IActionResult> ThemPhong(string maToa, int tang, string loaiPhong, decimal giaPhong)
        {
            int soGiuong = int.Parse(loaiPhong);
            var toa = await _db.ToaNhas.FirstOrDefaultAsync(t => t.MaToa == maToa);
            if (toa == null || tang < 1 || tang > toa.SoTang)
            {
                TempData["Loi"] = "Tầng không hợp lệ với tòa nhà đã chọn.";
                return RedirectToAction("Phong");
            }

            // Sinh mã phòng tự động: {Toa}-{Tang}{STT 2 chữ số}
            int stt = 1;
            string maPhong;
            do { maPhong = $"{maToa}-{tang}{stt:D2}"; stt++; }
            while (await _db.Phongs.AnyAsync(p => p.MaPhong == maPhong));

            _db.Phongs.Add(new Phong
            {
                MaPhong = maPhong,
                MaToa = maToa,
                Tang = tang,
                LoaiPhong = loaiPhong,
                SoGiuong = soGiuong,
                GiaPhong = giaPhong,
                TrangThai = "HoatDong",
                SoGiuongTrong = soGiuong
            });
            for (int i = 1; i <= soGiuong; i++)
                _db.Giuongs.Add(new Giuong { MaGiuong = $"{maPhong}-G{i}", MaPhong = maPhong, TrangThai = "Trong" });
            await _db.SaveChangesAsync();

            GhiNhatKy("Them", "Phòng", $"Thêm phòng {maPhong} ({soGiuong} giường) vào tòa {maToa}, tầng {tang}, giá {giaPhong:N0}đ.");
            TempData["ThanhCong"] = $"Đã thêm phòng {maPhong} với {soGiuong} giường.";
            return RedirectToAction("Phong");
        }

        // ============ Cập nhật phòng ============
        [HttpPost]
        public async Task<IActionResult> CapNhatPhong(string maPhong, decimal giaPhong, string trangThai)
        {
            var p = await _db.Phongs.FindAsync(maPhong);
            if (p != null)
            {
                p.GiaPhong = giaPhong;
                p.TrangThai = trangThai;
                await _db.SaveChangesAsync();
            }

            GhiNhatKy("Sua", "Phòng", $"Cập nhật phòng {maPhong}: giá {giaPhong:N0}đ, trạng thái {trangThai}.");
            TempData["ThanhCong"] = $"Đã cập nhật phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Xóa phòng (chỉ khi không có SV đang ở) ============
        [HttpPost]
        public async Task<IActionResult> XoaPhong(string maPhong)
        {
            bool dangO = await _db.PhieuDangKys.AnyAsync(pd =>
                pd.Giuong!.MaPhong == maPhong && (pd.TrangThai == "DangO" || pd.TrangThai == "ChoDoiChieu"));
            if (dangO)
            {
                TempData["Loi"] = "Không thể xóa: phòng đang có sinh viên ở hoặc giữ chỗ.";
                return RedirectToAction("Phong");
            }

            int tongLichSu = await _db.HoaDons.CountAsync(h => h.MaPhong == maPhong)
                + await _db.ChiSoDienNuocs.CountAsync(c => c.MaPhong == maPhong)
                + await _db.PhieuDangKys.CountAsync(pd => pd.Giuong!.MaPhong == maPhong);
            if (tongLichSu > 0)
            {
                // Có dữ liệu lịch sử → chỉ ngừng sử dụng thay vì xóa vật lý
                var p = await _db.Phongs.FindAsync(maPhong);
                if (p != null) { p.TrangThai = "NgungSuDung"; await _db.SaveChangesAsync(); }

                GhiNhatKy("Sua", "Phòng", $"Phòng {maPhong} có dữ liệu lịch sử nên chuyển sang Ngừng sử dụng (thay vì xóa).");
                TempData["ThanhCong"] = $"Phòng {maPhong} có dữ liệu lịch sử nên đã chuyển sang trạng thái Ngừng sử dụng.";
                return RedirectToAction("Phong");
            }

            // Xóa file ảnh vật lý trước khi xóa dữ liệu (sp_XoaPhong sẽ xóa các dòng ANHPHONG)
            var dsAnhXoa = await _db.AnhPhongs.Where(a => a.MaPhong == maPhong).Select(a => a.DuongDan).ToListAsync();
            foreach (var duongDan in dsAnhXoa) XoaFileAnh(duongDan);

            QuanTriRepo.XoaPhong(maPhong);   // sp_XoaPhong (transaction xóa ảnh + giường + phòng)
            GhiNhatKy("Xoa", "Phòng", $"Xóa phòng {maPhong}.");
            TempData["ThanhCong"] = $"Đã xóa phòng {maPhong}.";
            return RedirectToAction("Phong");
        }

        // ============ Quản lý ảnh phòng (admin thêm nhiều ảnh, sinh viên xem khi tra cứu/đăng ký) ============
        private static readonly string[] DinhDangAnhChoPhep = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long DungLuongAnhToiDa = 5 * 1024 * 1024; // 5MB/ảnh

        public async Task<IActionResult> AnhPhong(string maPhong)
        {
            var p = await _db.Phongs.Include(x => x.ToaNha).FirstOrDefaultAsync(x => x.MaPhong == maPhong);
            if (p == null)
            {
                TempData["Loi"] = "Phòng không tồn tại.";
                return RedirectToAction("Phong");
            }
            ViewBag.Phong = DataTableHelper.BuildRow(
                new[] { "MaPhong", "MaToa", "Tang", "LoaiPhong", "SoGiuong", "GiaPhong", "TrangThai", "SoGiuongTrong", "TenToa" },
                new object?[] { p.MaPhong, p.MaToa, p.Tang, p.LoaiPhong, p.SoGiuong, p.GiaPhong, p.TrangThai, p.SoGiuongTrong, p.ToaNha?.TenToa });

            var dsAnh = await _db.AnhPhongs.Where(a => a.MaPhong == maPhong)
                .OrderBy(a => a.ThuTu).ThenBy(a => a.MaAnh)
                .Select(a => new object?[] { a.MaAnh, a.DuongDan, a.ThuTu })
                .ToListAsync();
            ViewBag.DsAnh = DataTableHelper.Build(new[] { "MaAnh", "DuongDan", "ThuTu" }, dsAnh);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ThemAnhPhong(string maPhong, List<IFormFile> anhPhong)
        {
            bool tonTai = await _db.Phongs.AnyAsync(p => p.MaPhong == maPhong);
            if (!tonTai) { TempData["Loi"] = "Phòng không tồn tại."; return RedirectToAction("Phong"); }

            if (anhPhong == null || anhPhong.Count == 0)
            {
                TempData["Loi"] = "Vui lòng chọn ít nhất 1 ảnh.";
                return RedirectToAction("AnhPhong", new { maPhong });
            }

            string thuMucVatLy = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "phong", maPhong);
            Directory.CreateDirectory(thuMucVatLy);

            int? maxThuTu = await _db.AnhPhongs.Where(a => a.MaPhong == maPhong).Select(a => (int?)a.ThuTu).MaxAsync();
            int thuTu = (maxThuTu ?? -1) + 1;
            int soLuongLuu = 0, soLuongLoi = 0;
            foreach (var file in anhPhong)
            {
                if (file.Length == 0) continue;
                string duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!DinhDangAnhChoPhep.Contains(duoi) || file.Length > DungLuongAnhToiDa)
                {
                    soLuongLoi++;
                    continue;
                }

                string tenFile = $"{maPhong}_{DateTime.Now.Ticks}_{soLuongLuu}{duoi}";
                string duongDanVatLy = Path.Combine(thuMucVatLy, tenFile);
                using (var stream = new FileStream(duongDanVatLy, FileMode.Create))
                    await file.CopyToAsync(stream);

                string duongDanLuu = $"/uploads/phong/{maPhong}/{tenFile}";
                _db.AnhPhongs.Add(new AnhPhong { MaPhong = maPhong, DuongDan = duongDanLuu, ThuTu = thuTu });
                thuTu++;
                soLuongLuu++;
            }

            if (soLuongLuu == 0)
            {
                TempData["Loi"] = "Không có ảnh hợp lệ nào được tải lên (chỉ nhận JPG/PNG/WEBP, tối đa 5MB/ảnh).";
            }
            else
            {
                await _db.SaveChangesAsync();
                GhiNhatKy("Them", "Ảnh phòng", $"Thêm {soLuongLuu} ảnh cho phòng {maPhong}.");
                TempData["ThanhCong"] = $"Đã thêm {soLuongLuu} ảnh cho phòng {maPhong}."
                    + (soLuongLoi > 0 ? $" ({soLuongLoi} file bị bỏ qua do sai định dạng hoặc quá 5MB.)" : "");
            }
            return RedirectToAction("AnhPhong", new { maPhong });
        }

        [HttpPost]
        public async Task<IActionResult> XoaAnhPhong(int maAnh, string maPhong)
        {
            var anh = await _db.AnhPhongs.FirstOrDefaultAsync(a => a.MaAnh == maAnh && a.MaPhong == maPhong);
            if (anh != null)
            {
                XoaFileAnh(anh.DuongDan);
                _db.AnhPhongs.Remove(anh);
                await _db.SaveChangesAsync();
                GhiNhatKy("Xoa", "Ảnh phòng", $"Xóa 1 ảnh của phòng {maPhong}.");
                TempData["ThanhCong"] = "Đã xóa ảnh.";
            }
            return RedirectToAction("AnhPhong", new { maPhong });
        }

        /// <summary>Xóa file ảnh vật lý trong wwwroot theo đường dẫn lưu trong DB (an toàn nếu file không tồn tại).</summary>
        private static void XoaFileAnh(string? duongDan)
        {
            if (string.IsNullOrWhiteSpace(duongDan)) return;
            string duongDanVatLy = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                duongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(duongDanVatLy)) System.IO.File.Delete(duongDanVatLy);
        }

        // ============ Cập nhật đơn giá điện/nước ============
        public async Task<IActionResult> DonGia()
        {
            var list = await _db.DonGias.OrderByDescending(d => d.NgayApDung)
                .Select(d => new object?[] { d.MaDonGia, d.GiaDien, d.GiaNuoc, d.PhiDichVu, d.NgayApDung, d.TrangThai })
                .ToListAsync();
            ViewBag.DsDonGia = DataTableHelper.Build(
                new[] { "MaDonGia", "GiaDien", "GiaNuoc", "PhiDichVu", "NgayApDung", "TrangThai" }, list);
            ViewBag.TS7 = CommonRepo.LayThamSo("TS7");
            ViewBag.TS8 = CommonRepo.LayThamSo("TS8");
            return View();
        }

        [HttpPost]
        public IActionResult CapNhatDonGia(decimal giaDien, decimal giaNuoc, decimal phiDichVu, DateTime ngayApDung)
        {
            // Đơn giá không vượt trần pháp luật (TS7, TS8) - kiểm tra lại ở tầng ứng dụng để hiển thị thông báo rõ ràng
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
        public async Task<IActionResult> DotDangKy()
        {
            QuanTriRepo.CapNhatTrangThaiDot();   // sp_CapNhatTrangThaiDot (cập nhật trạng thái đợt theo thời gian)
            var list = await _db.DotDangKys.OrderByDescending(d => d.NgayMo)
                .Select(d => new object?[] { d.MaDot, d.TenDot, d.LoaiDot, d.HocKy, d.NgayMo, d.NgayDong, d.TrangThai })
                .ToListAsync();
            ViewBag.DsDot = DataTableHelper.Build(
                new[] { "MaDot", "TenDot", "LoaiDot", "HocKy", "NgayMo", "NgayDong", "TrangThai" }, list);
            ViewBag.TS1 = CommonRepo.LayThamSo("TS1");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TaoDot(string tenDot, string loaiDot, string hocKy, DateTime ngayMo, DateTime? ngayDong)
        {
            // Đợt ưu tiên tự động đóng sau TS1 ngày
            if (loaiDot == "UuTien")
                ngayDong = ngayMo.AddDays(CommonRepo.LayThamSoInt("TS1"));

            if (ngayDong == null || ngayDong <= ngayMo)
            {
                TempData["Loi"] = "Ngày đóng cổng phải sau ngày mở cổng.";
                return RedirectToAction("DotDangKy");
            }

            string trangThai = DateTime.Now < ngayMo ? "ChuaMo"
                : (DateTime.Now >= ngayMo && DateTime.Now <= ngayDong.Value ? "DangMo" : "DaDong");

            _db.DotDangKys.Add(new DotDangKy
            {
                TenDot = tenDot.Trim(),
                LoaiDot = loaiDot,
                HocKy = hocKy.Trim(),
                NgayMo = ngayMo,
                NgayDong = ngayDong.Value,
                TrangThai = trangThai
            });
            await _db.SaveChangesAsync();

            GhiNhatKy("Them", "Đợt đăng ký", $"Mở đợt đăng ký \"{tenDot.Trim()}\" ({loaiDot}, HK {hocKy.Trim()}), từ {ngayMo:dd/MM/yyyy} đến {ngayDong:dd/MM/yyyy}.");
            TempData["ThanhCong"] = "Đã thiết lập đợt đăng ký mới.";
            return RedirectToAction("DotDangKy");
        }

        [HttpPost]
        public async Task<IActionResult> DongDot(int maDot)
        {
            var dot = await _db.DotDangKys.FindAsync(maDot);
            if (dot != null)
            {
                dot.NgayDong = DateTime.Now;
                dot.TrangThai = "DaDong";
                await _db.SaveChangesAsync();
                GhiNhatKy("Sua", "Đợt đăng ký", $"Đóng cổng đăng ký đợt #{maDot} \"{dot.TenDot}\" trước hạn.");
            }
            TempData["ThanhCong"] = "Đã đóng cổng đăng ký.";
            return RedirectToAction("DotDangKy");
        }

        // ============ Danh sách + tra cứu tài khoản ============
        public async Task<IActionResult> TaiKhoan(string? tuKhoa, string? vaiTro, string? trangThai)
        {
            var q = _db.TaiKhoans.Include(t => t.SinhVien).Include(t => t.QuanLy).AsQueryable();
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(t => t.TenDangNhap.Contains(tk) || t.Email.Contains(tk)
                    || (t.SinhVien != null && t.SinhVien.HoTen.Contains(tk))
                    || (t.QuanLy != null && t.QuanLy.HoTen.Contains(tk)));
            }
            if (!string.IsNullOrWhiteSpace(vaiTro)) q = q.Where(t => t.VaiTro == vaiTro);
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(t => t.TrangThai == trangThai);

            var list = await q.OrderBy(t => t.MaTK)
                .Select(t => new object?[]
                {
                    t.MaTK, t.TenDangNhap, t.Email, t.SDT, t.VaiTro, t.TrangThai, t.NgayTao,
                    t.SinhVien != null ? t.SinhVien.HoTen : (t.QuanLy != null ? t.QuanLy.HoTen : "Quản trị viên"),
                    t.SinhVien != null ? t.SinhVien.MSSV : null,
                    t.SinhVien != null ? t.SinhVien.KhoaHoc : null,
                    t.SinhVien != null ? t.SinhVien.DoiTuong : null,
                    t.QuanLy != null ? t.QuanLy.MaNV : null,
                    t.QuanLy != null ? t.QuanLy.MaToa : null
                })
                .ToListAsync();
            ViewBag.DsTK = DataTableHelper.Build(
                new[] { "MaTK", "TenDangNhap", "Email", "SDT", "VaiTro", "TrangThai", "NgayTao",
                        "HoTen", "MSSV", "KhoaHoc", "DoiTuong", "MaNV", "MaToa" }, list);

            var toas = await _db.ToaNhas.OrderBy(t => t.MaToa).Select(t => new object?[] { t.MaToa, t.TenToa }).ToListAsync();
            ViewBag.DsToa = DataTableHelper.Build(new[] { "MaToa", "TenToa" }, toas);
            return View();
        }

        // ============ Tạo tài khoản ============
        [HttpPost]
        public async Task<IActionResult> TaoTaiKhoan(string tenDangNhap, string matKhau, string email, string? sdt,
                                          string vaiTro, string? hoTen, string? khoaHoc, string? doiTuong, string? maToa)
        {
            string tdn = tenDangNhap.Trim();
            string em = email.Trim();
            bool trung = await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tdn || t.Email == em);
            if (trung)
            {
                TempData["Loi"] = "Tên đăng nhập hoặc email đã tồn tại.";
                return RedirectToAction("TaiKhoan");
            }

            // sp_TaoTaiKhoan: INSERT + trả về MaTK qua SCOPE_IDENTITY
            int maTK = QuanTriRepo.TaoTaiKhoan(tdn, AuthHelper.Sha256(matKhau), em, sdt, vaiTro);

            if (vaiTro == "SV")
            {
                _db.SinhViens.Add(new SinhVien
                {
                    MSSV = tdn,
                    MaTK = maTK,
                    HoTen = hoTen ?? tenDangNhap,
                    KhoaHoc = khoaHoc ?? "K" + DateTime.Now.Year,
                    DoiTuong = doiTuong ?? "BinhThuong"
                });
                await _db.SaveChangesAsync();
            }
            else if (vaiTro == "QL")
            {
                int stt = await _db.QuanLys.CountAsync() + 1;
                _db.QuanLys.Add(new QuanLy { MaNV = $"NV{stt:D3}", MaTK = maTK, HoTen = hoTen ?? tenDangNhap, MaToa = maToa });
                await _db.SaveChangesAsync();
            }

            GhiNhatKy("Them", "Tài khoản", $"Tạo tài khoản {tenDangNhap} (vai trò {vaiTro}).");
            TempData["ThanhCong"] = $"Đã tạo tài khoản {tenDangNhap} ({vaiTro}).";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Cập nhật tài khoản (đổi email, SĐT, khóa/mở, reset mật khẩu) ============
        [HttpPost]
        public async Task<IActionResult> CapNhatTaiKhoan(int maTK, string vaiTro, string email, string? sdt,
                              string hoTen, string? khoaHoc, string? doiTuong, string? maToa, string? matKhauMoi, string? trangThai)
        {
            // Không tự khóa chính tài khoản QTV đang đăng nhập (tránh tự khóa bản thân ra khỏi hệ thống)
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai == "BiKhoa" && maTK == MaTK)
            {
                TempData["Loi"] = "Bạn không thể tự khóa tài khoản đang đăng nhập của chính mình.";
                return RedirectToAction("TaiKhoan");
            }

            bool daMoKhoa = false;
            var tk = await _db.TaiKhoans.FindAsync(maTK);
            if (tk != null)
            {
                tk.Email = email.Trim();
                tk.SDT = sdt;
                if (!string.IsNullOrWhiteSpace(matKhauMoi))
                    tk.MatKhau = AuthHelper.Sha256(matKhauMoi);
                if (trangThai == "HoatDong" || trangThai == "BiKhoa")
                {
                    daMoKhoa = tk.TrangThai == "BiKhoa" && trangThai == "HoatDong";
                    tk.TrangThai = trangThai;
                }
                await _db.SaveChangesAsync();
            }

            // sp_CapNhatThongTinCaNhan: cập nhật hồ sơ SV/QL theo vai trò
            QuanTriRepo.CapNhatThongTinCaNhan(maTK, vaiTro, hoTen.Trim(), khoaHoc, doiTuong, maToa);

            GhiNhatKy("Sua", "Tài khoản", $"Cập nhật tài khoản #{maTK} ({hoTen.Trim()})."
                + (string.IsNullOrWhiteSpace(matKhauMoi) ? "" : " Đã đặt lại mật khẩu."));
            if (daMoKhoa)
                GhiNhatKy("MoKhoa", "Tài khoản", $"Mở khóa tài khoản #{maTK} ({hoTen.Trim()}).");
            TempData["ThanhCong"] = "Đã cập nhật tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Xóa tài khoản ============
        [HttpPost]
        public async Task<IActionResult> XoaTaiKhoan(int maTK)
        {
            // Không xóa tài khoản còn dữ liệu ràng buộc (hợp đồng, hóa đơn, đơn từ...)
            string? mssv = await _db.SinhViens.Where(sv => sv.MaTK == maTK).Select(sv => sv.MSSV).FirstOrDefaultAsync();
            if (mssv != null)
            {
                int lienQuan = await _db.PhieuDangKys.CountAsync(p => p.MSSV == mssv)
                    + await _db.DonYeuCaus.CountAsync(d => d.MSSV == mssv)
                    + await _db.ThanhToans.CountAsync(t => t.MSSV == mssv)
                    + await _db.ThongBaos.CountAsync(t => t.MSSV == mssv)
                    + await _db.VIPhams.CountAsync(v => v.MSSV == mssv);
                if (lienQuan > 0)
                {
                    var tkLock = await _db.TaiKhoans.FindAsync(maTK);
                    if (tkLock != null) { tkLock.TrangThai = "BiKhoa"; await _db.SaveChangesAsync(); }
                    GhiNhatKy("Khoa", "Tài khoản", $"Tài khoản #{maTK} (SV {mssv}) có dữ liệu liên quan nên khóa thay vì xóa.");
                    TempData["Loi"] = "Tài khoản có dữ liệu liên quan nên không thể xóa - đã chuyển sang trạng thái Bị khóa.";
                    return RedirectToAction("TaiKhoan");
                }
                var svEnt = await _db.SinhViens.FindAsync(mssv);
                if (svEnt != null) { _db.SinhViens.Remove(svEnt); await _db.SaveChangesAsync(); }
            }

            string? maNV = await _db.QuanLys.Where(q => q.MaTK == maTK).Select(q => q.MaNV).FirstOrDefaultAsync();
            if (maNV != null)
            {
                int coDon = await _db.DonYeuCaus.CountAsync(d => d.MaNV == maNV);
                if (coDon > 0)
                {
                    var tkLock = await _db.TaiKhoans.FindAsync(maTK);
                    if (tkLock != null) { tkLock.TrangThai = "BiKhoa"; await _db.SaveChangesAsync(); }
                    GhiNhatKy("Khoa", "Tài khoản", $"Tài khoản #{maTK} (QL {maNV}) đang phụ trách đơn từ nên khóa thay vì xóa.");
                    TempData["Loi"] = "Quản lý đang phụ trách đơn từ nên không thể xóa - đã khóa tài khoản.";
                    return RedirectToAction("TaiKhoan");
                }
                var qlEnt = await _db.QuanLys.FindAsync(maNV);
                if (qlEnt != null) { _db.QuanLys.Remove(qlEnt); await _db.SaveChangesAsync(); }
            }

            var tkFinal = await _db.TaiKhoans.FindAsync(maTK);
            if (tkFinal != null) { _db.TaiKhoans.Remove(tkFinal); await _db.SaveChangesAsync(); }

            GhiNhatKy("Xoa", "Tài khoản", $"Xóa tài khoản #{maTK}.");
            TempData["ThanhCong"] = "Đã xóa tài khoản.";
            return RedirectToAction("TaiKhoan");
        }

        // ============ Nhật ký thao tác hệ thống (audit log) ============
        public async Task<IActionResult> NhatKy(string? hanhDong, string? doiTuong, DateTime? tuNgay, DateTime? denNgay, int trang = 1)
        {
            // Chưa từng lọc (mở trang lần đầu) -> mặc định chỉ xem 30 ngày gần nhất, tránh tải toàn bộ nhật ký hệ thống.
            // Nếu người dùng đã submit form (kể cả để trống ô ngày để xem tất cả) thì tôn trọng lựa chọn đó.
            bool daTungLoc = Request.Query.ContainsKey("tuNgay") || Request.Query.ContainsKey("denNgay")
                || Request.Query.ContainsKey("hanhDong") || Request.Query.ContainsKey("doiTuong");
            if (!daTungLoc) tuNgay = DateTime.Today.AddDays(-30);
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            var q = _db.NhatKys.AsQueryable();
            if (!string.IsNullOrWhiteSpace(hanhDong)) q = q.Where(n => n.HanhDong == hanhDong);
            if (!string.IsNullOrWhiteSpace(doiTuong)) q = q.Where(n => n.DoiTuong == doiTuong);
            if (tuNgay != null) q = q.Where(n => n.ThoiGian >= tuNgay.Value.Date);
            if (denNgay != null) q = q.Where(n => n.ThoiGian < denNgay.Value.Date.AddDays(1));

            int tongSoDong = await q.CountAsync();
            var (trangHT, tongSoTrang) = PagingHelper.Chuan(trang, tongSoDong);

            var list = await q.OrderByDescending(n => n.ThoiGian)
                .Skip((trangHT - 1) * PagingHelper.KichThuocTrangMacDinh).Take(PagingHelper.KichThuocTrangMacDinh)
                .Select(n => new object?[] { n.MaNhatKy, n.HoTenNguoiThucHien, n.VaiTro, n.HanhDong, n.DoiTuong, n.NoiDung, n.ThoiGian })
                .ToListAsync();
            ViewBag.DsNhatKy = DataTableHelper.Build(
                new[] { "MaNhatKy", "HoTenNguoiThucHien", "VaiTro", "HanhDong", "DoiTuong", "NoiDung", "ThoiGian" }, list);
            ViewBag.Pager = PagingHelper.TaoPager(trangHT, tongSoTrang, tongSoDong, Request.Query);

            var doiTuongs = await _db.NhatKys.Select(n => n.DoiTuong).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.DsDoiTuong = DataTableHelper.Build(new[] { "DoiTuong" }, doiTuongs.Select(x => new object?[] { x }));
            return View();
        }
    }
}
