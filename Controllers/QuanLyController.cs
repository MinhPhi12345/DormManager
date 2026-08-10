using System.Data;
using DormManager.Data;
using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DormManager.Controllers
{
    [PhanQuyen("QL", "QTV")]
    public class QuanLyController : Controller
    {
        private readonly AppDbContext _db;

        public QuanLyController(AppDbContext db)
        {
            _db = db;
        }

        private string? MaNV => HttpContext.Session.GetString("MaNV");

        // ============ Danh sách + tra cứu + sắp xếp ưu tiên đơn ============
        public async Task<IActionResult> DonYeuCau(string? tuKhoa, string? loai, string? trangThai, string? uuTien)
        {
            var q = _db.DonYeuCaus.Include(d => d.SinhVien).AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(d => d.TieuDe.Contains(tk) || d.SinhVien!.HoTen.Contains(tk) || d.MSSV.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(loai)) q = q.Where(d => d.LoaiDon == loai);
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(d => d.TrangThai == trangThai);
            if (!string.IsNullOrWhiteSpace(uuTien)) q = q.Where(d => d.MucUuTien == uuTien);

            var ds = await q
                .OrderBy(d => d.MucUuTien == "Cao" ? 0 : d.MucUuTien == "TrungBinh" ? 1 : 2)
                .ThenBy(d => d.TrangThai == "ChoXuLy" ? 0 : d.TrangThai == "DangXuLy" ? 1 : 2)
                .ThenByDescending(d => d.NgayTao)
                .Select(d => new object?[] { d.MaDon, d.TieuDe, d.LoaiDon, d.MucUuTien, d.TrangThai, d.NgayTao, d.SinhVien!.HoTen, d.MSSV })
                .ToListAsync();

            ViewBag.DsDon = DataTableHelper.Build(
                new[] { "MaDon", "TieuDe", "LoaiDon", "MucUuTien", "TrangThai", "NgayTao", "HoTen", "MSSV" }, ds);
            return View();
        }

        // Chi tiết + xử lý đơn (đổi trạng thái, ưu tiên, phản hồi → gửi thông báo)
        public async Task<IActionResult> ChiTietDon(int id)
        {
            var d = await _db.DonYeuCaus
                .Where(x => x.MaDon == id)
                .Select(x => new
                {
                    x.MaDon,
                    x.MSSV,
                    x.MaNV,
                    x.MaPhieu,
                    x.MaGiuongMoi,
                    x.LoaiDon,
                    x.TieuDe,
                    x.NoiDung,
                    x.MucUuTien,
                    x.TrangThai,
                    x.PhanHoi,
                    x.NgayTao,
                    HoTen = x.SinhVien!.HoTen,
                    TenNV = x.QuanLy != null ? x.QuanLy.HoTen : null,
                    MaPhongCu = x.PhieuDangKy != null ? x.PhieuDangKy.Giuong!.MaPhong : null,
                    MaPhongMoi = x.GiuongMoi != null ? x.GiuongMoi.MaPhong : null
                })
                .FirstOrDefaultAsync();
            if (d == null) return RedirectToAction("DonYeuCau");

            ViewBag.Don = DataTableHelper.BuildRow(
                new[] { "MaDon", "MSSV", "MaNV", "MaPhieu", "MaGiuongMoi", "LoaiDon", "TieuDe", "NoiDung",
                        "MucUuTien", "TrangThai", "PhanHoi", "NgayTao", "HoTen", "TenNV", "MaPhongCu", "MaPhongMoi" },
                new object?[] { d.MaDon, d.MSSV, d.MaNV, d.MaPhieu, d.MaGiuongMoi, d.LoaiDon, d.TieuDe, d.NoiDung,
                        d.MucUuTien, d.TrangThai, d.PhanHoi, d.NgayTao, d.HoTen, d.TenNV, d.MaPhongCu, d.MaPhongMoi });
            return View();
        }

        [HttpPost]
        public IActionResult XuLyDon(int maDon, string mucUuTien, string trangThai, string? phanHoi)
        {
            try
            {
                // sp_XuLyDon: transaction đổi trạng thái/ưu tiên + gửi thông báo
                QuanLyRepo.XuLyDon(maDon, mucUuTien, trangThai, phanHoi, MaNV);
                TempData["ThanhCong"] = "Cập nhật đơn thành công. Hệ thống đã gửi thông báo tới sinh viên.";
            }
            catch (SqlException ex)
            {
                // SP chặn các thay đổi không hợp lệ (VD: sửa lại đơn chuyển/trả phòng đã xử lý xong) bằng RAISERROR
                TempData["Loi"] = ex.Message;
            }
            return RedirectToAction("ChiTietDon", new { id = maDon });
        }

        // ============ Danh sách + tra cứu + chi tiết đơn đăng ký ============
        public async Task<IActionResult> DonDangKy(string? tuKhoa, string? trangThai)
        {
            var q = _db.PhieuDangKys
                .Include(p => p.SinhVien)
                .Include(p => p.Giuong)
                .Include(p => p.DotDangKy)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(p => p.MSSV.Contains(tk) || p.SinhVien!.HoTen.Contains(tk) || p.Giuong!.MaPhong.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(trangThai)) q = q.Where(p => p.TrangThai == trangThai);

            var list = await q
                .OrderBy(p => p.TrangThai == "ChoDoiChieu" ? 0 : 1)
                .ThenByDescending(p => p.NgayDangKy)
                .Select(p => new object?[]
                {
                    p.MaPhieu, p.NgayDangKy, p.NgayBatDau, p.NgayKetThuc, p.TrangThai,
                    p.MSSV, p.SinhVien!.HoTen, p.Giuong!.MaPhong, p.MaGiuong, p.DotDangKy!.TenDot
                })
                .ToListAsync();

            ViewBag.DsPhieu = DataTableHelper.Build(
                new[] { "MaPhieu", "NgayDangKy", "NgayBatDau", "NgayKetThuc", "TrangThai",
                        "MSSV", "HoTen", "MaPhong", "MaGiuong", "TenDot" }, list);
            return View();
        }

        public async Task<IActionResult> ChiTietDangKy(int id)
        {
            var p = await _db.PhieuDangKys
                .Where(x => x.MaPhieu == id)
                .Select(x => new
                {
                    x.MaPhieu,
                    x.MSSV,
                    x.MaGiuong,
                    x.MaDot,
                    x.NgayDangKy,
                    x.NgayBatDau,
                    x.NgayKetThuc,
                    x.TrangThai,
                    HoTen = x.SinhVien!.HoTen,
                    x.SinhVien.KhoaHoc,
                    x.SinhVien.GioiTinh,
                    x.SinhVien.DoiTuong,
                    x.SinhVien.DiemViPham,
                    MaPhong = x.Giuong!.MaPhong,
                    x.Giuong.Phong!.GiaPhong,
                    x.Giuong.Phong.LoaiPhong,
                    TenToa = x.Giuong.Phong.ToaNha!.TenToa,
                    x.DotDangKy!.TenDot,
                    x.DotDangKy.HocKy
                })
                .FirstOrDefaultAsync();
            if (p == null) return RedirectToAction("DonDangKy");

            ViewBag.Phieu = DataTableHelper.BuildRow(
                new[] { "MaPhieu", "MSSV", "MaGiuong", "MaDot", "NgayDangKy", "NgayBatDau", "NgayKetThuc", "TrangThai",
                        "HoTen", "KhoaHoc", "GioiTinh", "DoiTuong", "DiemViPham", "MaPhong", "GiaPhong", "LoaiPhong",
                        "TenToa", "TenDot", "HocKy" },
                new object?[] { p.MaPhieu, p.MSSV, p.MaGiuong, p.MaDot, p.NgayDangKy, p.NgayBatDau, p.NgayKetThuc, p.TrangThai,
                        p.HoTen, p.KhoaHoc, p.GioiTinh, p.DoiTuong, p.DiemViPham, p.MaPhong, p.GiaPhong, p.LoaiPhong,
                        p.TenToa, p.TenDot, p.HocKy });
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

        // ============ Danh sách + tra cứu phòng ============
        public async Task<IActionResult> Phong(string? tuKhoa, string? maToa)
        {
            var q = _db.Phongs.Include(p => p.ToaNha).AsQueryable();
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(p => p.MaPhong.Contains(tk));
            }
            if (!string.IsNullOrWhiteSpace(maToa)) q = q.Where(p => p.MaToa == maToa);

            var list = await q
                .OrderBy(p => p.MaToa).ThenBy(p => p.Tang).ThenBy(p => p.MaPhong)
                .Select(p => new object?[]
                {
                    p.MaPhong, p.MaToa, p.Tang, p.LoaiPhong, p.SoGiuong, p.GiaPhong, p.TrangThai, p.SoGiuongTrong,
                    p.ToaNha!.TenToa
                })
                .ToListAsync();
            ViewBag.DsPhong = DataTableHelper.Build(
                new[] { "MaPhong", "MaToa", "Tang", "LoaiPhong", "SoGiuong", "GiaPhong", "TrangThai", "SoGiuongTrong", "TenToa" }, list);

            var toas = await _db.ToaNhas.OrderBy(t => t.MaToa)
                .Select(t => new object?[] { t.MaToa, t.TenToa }).ToListAsync();
            ViewBag.DsToa = DataTableHelper.Build(new[] { "MaToa", "TenToa" }, toas);
            return View();
        }

        // ============ Danh sách giường + cập nhật trạng thái ============
        public async Task<IActionResult> Giuong(string maPhong)
        {
            ViewBag.MaPhong = maPhong;

            var list = await _db.Giuongs
                .Where(g => g.MaPhong == maPhong)
                .OrderBy(g => g.MaGiuong)
                .Select(g => new object?[]
                {
                    g.MaGiuong,
                    g.TrangThai,
                    g.PhieuDangKys.Where(pd => pd.TrangThai == "DangO" || pd.TrangThai == "ChoDoiChieu")
                                  .Select(pd => pd.SinhVien!.HoTen).FirstOrDefault(),
                    g.PhieuDangKys.Where(pd => pd.TrangThai == "DangO" || pd.TrangThai == "ChoDoiChieu")
                                  .Select(pd => pd.MSSV).FirstOrDefault()
                })
                .ToListAsync();
            ViewBag.DsGiuong = DataTableHelper.Build(new[] { "MaGiuong", "TrangThai", "HoTen", "MSSV" }, list);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CapNhatGiuong(string maGiuong, string maPhong, string trangThai)
        {
            bool dangO = await _db.PhieuDangKys.AnyAsync(pd =>
                pd.MaGiuong == maGiuong && (pd.TrangThai == "DangO" || pd.TrangThai == "ChoDoiChieu"));
            if (dangO && trangThai != "DaSuDung")
            {
                TempData["Loi"] = "Giường đang có sinh viên ở/giữ chỗ, không thể đổi trạng thái.";
                return RedirectToAction("Giuong", new { maPhong });
            }

            QuanLyRepo.CapNhatGiuong(maGiuong, maPhong, trangThai);   // sp_CapNhatGiuong (transaction đồng bộ số giường trống)
            TempData["ThanhCong"] = $"Đã cập nhật trạng thái giường {maGiuong}.";
            return RedirectToAction("Giuong", new { maPhong });
        }

        // ============ Nhập chỉ số điện/nước ============
        public async Task<IActionResult> ChiSo(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;

            var list = await _db.Phongs
                .Where(p => p.TrangThai == "HoatDong" &&
                            p.Giuongs.Any(g => g.PhieuDangKys.Any(pd => pd.TrangThai == "DangO")))
                .OrderBy(p => p.MaPhong)
                .Select(p => new object?[]
                {
                    p.MaPhong,
                    p.SoGiuong - p.SoGiuongTrong,
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => (int?)c.MaChiSo).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => (decimal?)c.DienDauKy).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => (decimal?)c.DienCuoiKy).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => (decimal?)c.NuocDauKy).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => (decimal?)c.NuocCuoiKy).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang == thang).Select(c => c.TrangThai).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang != thang)
                        .OrderByDescending(c => c.Thang.Substring(3, 4)).ThenByDescending(c => c.Thang.Substring(0, 2))
                        .Select(c => (decimal?)c.DienCuoiKy).FirstOrDefault(),
                    p.ChiSoDienNuocs.Where(c => c.Thang != thang)
                        .OrderByDescending(c => c.Thang.Substring(3, 4)).ThenByDescending(c => c.Thang.Substring(0, 2))
                        .Select(c => (decimal?)c.NuocCuoiKy).FirstOrDefault(),
                })
                .ToListAsync();

            ViewBag.DsPhong = DataTableHelper.Build(
                new[] { "MaPhong", "SoNguoiO", "MaChiSo", "DienDauKy", "DienCuoiKy", "NuocDauKy", "NuocCuoiKy",
                        "TrangThai", "DienKyTruoc", "NuocKyTruoc" }, list);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LuuChiSo(string maPhong, string thang, decimal dienDauKy, decimal dienCuoiKy, decimal nuocDauKy, decimal nuocCuoiKy)
        {
            // Chỉ số cuối kỳ >= đầu kỳ, cảnh báo giá trị âm/bất thường
            if (dienCuoiKy < dienDauKy || nuocCuoiKy < nuocDauKy || dienDauKy < 0 || nuocDauKy < 0)
            {
                TempData["Loi"] = $"Phòng {maPhong}: chỉ số cuối kỳ phải lớn hơn hoặc bằng đầu kỳ và không âm.";
                return RedirectToAction("ChiSo", new { thang });
            }

            bool daChot = await _db.ChiSoDienNuocs.AnyAsync(c => c.MaPhong == maPhong && c.Thang == thang && c.TrangThai == "DaChot");
            if (daChot)
            {
                TempData["Loi"] = $"Chỉ số phòng {maPhong} tháng {thang} đã chốt (đã xuất hóa đơn), không thể sửa.";
                return RedirectToAction("ChiSo", new { thang });
            }

            QuanLyRepo.LuuChiSo(maPhong, thang, dienDauKy, dienCuoiKy, nuocDauKy, nuocCuoiKy);   // sp_LuuChiSo (MERGE)
            TempData["ThanhCong"] = $"Đã lưu chỉ số điện/nước phòng {maPhong} tháng {thang}.";
            return RedirectToAction("ChiSo", new { thang });
        }

        // ============ Tạo hóa đơn ============
        public async Task<IActionResult> TaoHoaDon(string? thang)
        {
            thang ??= DateTime.Now.ToString("MM/yyyy");
            ViewBag.Thang = thang;

            var dg = await _db.DonGias.Where(d => d.TrangThai == "HieuLuc").OrderByDescending(d => d.NgayApDung).FirstOrDefaultAsync();
            ViewBag.DonGia = dg == null ? null : DataTableHelper.BuildRow(
                new[] { "MaDonGia", "GiaDien", "GiaNuoc", "PhiDichVu", "NgayApDung", "TrangThai" },
                new object?[] { dg.MaDonGia, dg.GiaDien, dg.GiaNuoc, dg.PhiDichVu, dg.NgayApDung, dg.TrangThai });

            ViewBag.DsXemTruoc = QuanLyRepo.NguonTaoHoaDon(thang);   // sp_NguonTaoHoaDon (tính số người ở trọn tháng)

            var dsNhap = await _db.HoaDons.Where(h => h.TrangThai == "Nhap")
                .OrderByDescending(h => h.Thang.Substring(3, 4)).ThenByDescending(h => h.Thang.Substring(0, 2)).ThenBy(h => h.MaPhong)
                .Select(h => new object?[] { h.MaHD, h.MaPhong, h.Thang, h.TienPhong, h.TienDien, h.TienNuoc, h.TongTien, h.HanThanhToan })
                .ToListAsync();
            ViewBag.DsNhap = DataTableHelper.Build(
                new[] { "MaHD", "MaPhong", "Thang", "TienPhong", "TienDien", "TienNuoc", "TongTien", "HanThanhToan" }, dsNhap);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TaoHoaDonThang(string thang, int hanThanhToanNgay = 15)
        {
            var dg = await _db.DonGias.Where(d => d.TrangThai == "HieuLuc").OrderByDescending(d => d.NgayApDung).FirstOrDefaultAsync();
            if (dg == null) { TempData["Loi"] = "Chưa cấu hình đơn giá điện/nước hiệu lực."; return RedirectToAction("TaoHoaDon", new { thang }); }

            var ds = QuanLyRepo.NguonTaoHoaDon(thang);
            if (ds.Rows.Count == 0) { TempData["Loi"] = "Không có chỉ số nào cần tạo hóa đơn cho tháng này."; return RedirectToAction("TaoHoaDon", new { thang }); }

            int soHD = 0;
            foreach (DataRow r in ds.Rows)
            {
                decimal tienDien = Math.Round(Convert.ToDecimal(r["Kwh"]) * dg.GiaDien);
                decimal tienNuoc = Math.Round(Convert.ToDecimal(r["M3"]) * dg.GiaNuoc);
                // Tiền phòng = giá phòng x số người dọn vào từ ngày TS12 của tháng trở về trước
                // (SoNguoiO đã được sp_NguonTaoHoaDon tính sẵn theo NgayBatDau <= ngày TS12 - SV
                // dọn vào SAU ngày TS12 chưa được tính, bắt đầu tính từ tháng kế tiếp, tránh bị
                // tính đủ 1 tháng tiền phòng khi chỉ mới ở vài ngày cuối tháng).
                decimal tienPhong = Convert.ToDecimal(r["GiaPhong"]) * Convert.ToInt32(r["SoNguoiO"]) + dg.PhiDichVu;
                decimal tongTien = tienPhong + tienDien + tienNuoc;

                QuanLyRepo.TaoHoaDonDong(r["MaPhong"], r["MaChiSo"], dg.MaDonGia, thang,
                    tienPhong, tienDien, tienNuoc, tongTien, DateTime.Today.AddDays(hanThanhToanNgay));   // sp_TaoHoaDonDong (transaction)
                soHD++;
            }
            TempData["ThanhCong"] = $"Đã tạo {soHD} hóa đơn (bản nháp) cho tháng {thang}. Hãy kiểm tra và xác nhận gửi.";
            return RedirectToAction("TaoHoaDon", new { thang });
        }

        // ============ Xác nhận gửi hóa đơn ============
        [HttpPost]
        public async Task<IActionResult> GuiHoaDon(int maHD)
        {
            int soSV = await GuiMotHoaDonAsync(maHD);
            if (soSV < 0) { TempData["Loi"] = "Hóa đơn không hợp lệ."; return RedirectToAction("TaoHoaDon"); }

            TempData["ThanhCong"] = $"Đã gửi hóa đơn #{maHD} và thông báo tới {soSV} sinh viên trong phòng.";
            return RedirectToAction("TaoHoaDon");
        }

        // ============ Gửi hàng loạt hóa đơn nháp đã chọn ============
        [HttpPost]
        public async Task<IActionResult> GuiHoaDonHangLoat(List<int> maHDs)
        {
            if (maHDs == null || maHDs.Count == 0)
            {
                TempData["Loi"] = "Vui lòng chọn ít nhất 1 hóa đơn để gửi.";
                return RedirectToAction("TaoHoaDon");
            }

            int soHD = 0, soThongBao = 0;
            foreach (var maHD in maHDs)
            {
                int soSV = await GuiMotHoaDonAsync(maHD);
                if (soSV >= 0) { soHD++; soThongBao += soSV; }
            }
            TempData["ThanhCong"] = $"Đã gửi {soHD}/{maHDs.Count} hóa đơn và {soThongBao} thông báo tới sinh viên.";
            return RedirectToAction("TaoHoaDon");
        }

        /// <summary>Chuyển 1 hóa đơn Nhap -> ChoThanhToan + gửi thông báo tới SV trong phòng. Trả về -1 nếu hóa đơn không hợp lệ.</summary>
        private async Task<int> GuiMotHoaDonAsync(int maHD)
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHD == maHD && h.TrangThai == "Nhap");
            if (hd == null) return -1;

            hd.TrangThai = "ChoThanhToan";
            hd.NgayPhatHanh = DateTime.Now;
            await _db.SaveChangesAsync();

            // Gửi thông báo Email/SMS tới từng sinh viên trong phòng, lưu lịch sử
            var dsSV = await _db.PhieuDangKys
                .Where(pd => pd.TrangThai == "DangO" && pd.Giuong!.MaPhong == hd.MaPhong)
                .Select(pd => new { pd.MSSV, pd.SinhVien!.HoTen, Email = pd.SinhVien!.TaiKhoan!.Email })
                .ToListAsync();

            string noiDung = $"Hóa đơn tháng {hd.Thang} phòng {hd.MaPhong}: {hd.TongTien:N0}đ. Hạn thanh toán {hd.HanThanhToan:dd/MM/yyyy}.";
            foreach (var sv in dsSV)
            {
                // Gửi email THẬT qua SMTP (nếu đã cấu hình ở appsettings.json - mục EmailSMTP).
                // Chưa cấu hình thì bỏ qua gửi thật, chỉ ghi lịch sử như trước (không làm gián đoạn nghiệp vụ).
                bool daGuiThat = CauHinhEmail.DaCauHinh && CauHinhEmail.Gui(
                    sv.Email,
                    $"[KTX] Hóa đơn tháng {hd.Thang} - Phòng {hd.MaPhong}",
                    $"<p>Chào {sv.HoTen},</p>" +
                    $"<p>Phòng <b>{hd.MaPhong}</b> vừa có hóa đơn điện/nước/tiền phòng tháng <b>{hd.Thang}</b>:</p>" +
                    $"<ul>" +
                    $"<li>Tiền phòng: {hd.TienPhong:N0}đ</li>" +
                    $"<li>Tiền điện: {hd.TienDien:N0}đ</li>" +
                    $"<li>Tiền nước: {hd.TienNuoc:N0}đ</li>" +
                    $"<li><b>Tổng cộng: {hd.TongTien:N0}đ</b></li>" +
                    $"</ul>" +
                    $"<p>Hạn thanh toán: <b>{hd.HanThanhToan:dd/MM/yyyy}</b>. Vui lòng đăng nhập hệ thống để xem chi tiết và thanh toán.</p>" +
                    $"<p><i>Đây là email tự động, vui lòng không trả lời.</i></p>");

                string trangThai = CauHinhEmail.DaCauHinh ? (daGuiThat ? "ThanhCong" : "ThatBai") : "ThanhCong";
                CommonRepo.ThemThongBao(sv.MSSV, maHD, noiDung, "Email", trangThai);
            }
            return dsSV.Count;
        }

        // ============ Hủy hóa đơn nháp (undo) - mở lại chỉ số điện/nước để sửa nếu có sai sót ============
        [HttpPost]
        public async Task<IActionResult> HuyHoaDonNhap(int maHD)
        {
            bool ok = await HuyMotHoaDonNhapAsync(maHD);
            TempData[ok ? "ThanhCong" : "Loi"] = ok
                ? $"Đã hủy hóa đơn nháp #{maHD}. Chỉ số điện/nước đã được mở lại để chỉnh sửa."
                : "Hóa đơn không hợp lệ hoặc đã được gửi (không thể hủy hóa đơn đã gửi).";
            return RedirectToAction("TaoHoaDon");
        }

        // ============ Hủy hàng loạt hóa đơn nháp đã chọn ============
        [HttpPost]
        public async Task<IActionResult> HuyHoaDonHangLoat(List<int> maHDs)
        {
            if (maHDs == null || maHDs.Count == 0)
            {
                TempData["Loi"] = "Vui lòng chọn ít nhất 1 hóa đơn để hủy.";
                return RedirectToAction("TaoHoaDon");
            }

            int soHD = 0;
            foreach (var maHD in maHDs)
                if (await HuyMotHoaDonNhapAsync(maHD)) soHD++;

            TempData["ThanhCong"] = $"Đã hủy {soHD}/{maHDs.Count} hóa đơn nháp. Chỉ số điện/nước tương ứng đã được mở lại để chỉnh sửa.";
            return RedirectToAction("TaoHoaDon");
        }

        /// <summary>Xóa 1 hóa đơn đang ở trạng thái Nhap (chưa gửi) và mở lại chỉ số điện/nước (DaChot -> Nhap)
        /// để quản lý có thể sửa lại chỉ số nếu phát hiện sai sót, rồi tạo lại hóa đơn từ đầu.
        /// Chỉ áp dụng cho hóa đơn CHƯA gửi - hóa đơn đã gửi (ChoThanhToan/QuaHan/DaThanhToan) không thể hủy qua đây
        /// vì sinh viên đã được thông báo.</summary>
        private async Task<bool> HuyMotHoaDonNhapAsync(int maHD)
        {
            var hd = await _db.HoaDons.FirstOrDefaultAsync(h => h.MaHD == maHD && h.TrangThai == "Nhap");
            if (hd == null) return false;

            var chiSo = await _db.ChiSoDienNuocs.FirstOrDefaultAsync(c => c.MaChiSo == hd.MaChiSo);
            if (chiSo != null) chiSo.TrangThai = "Nhap";

            _db.HoaDons.Remove(hd);
            await _db.SaveChangesAsync();
            return true;
        }

        // ============ Lịch sử thông báo hóa đơn + tra cứu ============
        public async Task<IActionResult> LichSuThongBao(string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, int trang = 1)
        {
            // Chưa từng lọc (mở trang lần đầu) -> mặc định chỉ xem 30 ngày gần nhất, tránh tải toàn bộ lịch sử thông báo.
            // Nếu người dùng đã submit form (kể cả để trống ô ngày để xem tất cả) thì tôn trọng lựa chọn đó, không ép lại.
            bool daTungLoc = Request.Query.ContainsKey("tuNgay") || Request.Query.ContainsKey("denNgay") || Request.Query.ContainsKey("tuKhoa");
            if (!daTungLoc) tuNgay = DateTime.Today.AddDays(-30);
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            var q = _db.ThongBaos.Include(t => t.SinhVien).ThenInclude(sv => sv!.TaiKhoan).AsQueryable();
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(t => t.MSSV.Contains(tk) || t.SinhVien!.HoTen.Contains(tk) || t.NoiDung.Contains(tk));
            }
            if (tuNgay != null) q = q.Where(t => t.ThoiGianGui >= tuNgay.Value.Date);
            if (denNgay != null) q = q.Where(t => t.ThoiGianGui < denNgay.Value.Date.AddDays(1));

            int tongSoDong = await q.CountAsync();
            var (trangHT, tongSoTrang) = PagingHelper.Chuan(trang, tongSoDong);

            // Hệ thống chỉ gửi thông báo qua email thật (QD12) nên hiển thị thẳng địa chỉ email đã gửi
            // tới, hữu ích hơn cột "Kênh" vốn luôn cố định là "Email" cho mọi dòng.
            var list = await q.OrderByDescending(t => t.ThoiGianGui)
                .Skip((trangHT - 1) * PagingHelper.KichThuocTrangMacDinh).Take(PagingHelper.KichThuocTrangMacDinh)
                .Select(t => new object?[] { t.MaTB, t.MSSV, t.SinhVien!.HoTen, t.MaHD, t.NoiDung, t.SinhVien!.TaiKhoan!.Email, t.ThoiGianGui, t.TrangThaiGui })
                .ToListAsync();
            ViewBag.DsTB = DataTableHelper.Build(
                new[] { "MaTB", "MSSV", "HoTen", "MaHD", "NoiDung", "Email", "ThoiGianGui", "TrangThaiGui" }, list);
            ViewBag.Pager = PagingHelper.TaoPager(trangHT, tongSoTrang, tongSoDong, Request.Query);
            return View();
        }

        // ============ Sinh viên vi phạm + mở khóa tài khoản ============
        public async Task<IActionResult> ViPham(string? tuKhoa)
        {
            // Job tự động (mô phỏng khi mở trang): quét hóa đơn quá hạn -> khóa TK (QD05), ghi vi phạm (QD06)
            QuanLyRepo.QuetHoaDonQuaHan();

            var q = _db.SinhViens.Include(sv => sv.TaiKhoan)
                .Where(sv => sv.DiemViPham > 0 || sv.TaiKhoan!.TrangThai == "BiKhoa");
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                string tk = tuKhoa.Trim();
                q = q.Where(sv => sv.MSSV.Contains(tk) || sv.HoTen.Contains(tk));
            }
            var dsViPham = await q.OrderByDescending(sv => sv.DiemViPham)
                .Select(sv => new object?[] { sv.MSSV, sv.HoTen, sv.KhoaHoc, sv.DiemViPham, sv.TaiKhoan!.TrangThai, sv.TaiKhoan.MaTK })
                .ToListAsync();
            ViewBag.DsViPham = DataTableHelper.Build(
                new[] { "MSSV", "HoTen", "KhoaHoc", "DiemViPham", "TrangThaiTK", "MaTK" }, dsViPham);

            var lichSu = await _db.VIPhams.Include(v => v.SinhVien).OrderByDescending(v => v.NgayGhiNhan)
                .Select(v => new object?[] { v.MaVP, v.MSSV, v.MaHD, v.NgayGhiNhan, v.SoDiem, v.LyDo, v.SinhVien!.HoTen })
                .ToListAsync();
            ViewBag.LichSu = DataTableHelper.Build(
                new[] { "MaVP", "MSSV", "MaHD", "NgayGhiNhan", "SoDiem", "LyDo", "HoTen" }, lichSu);
            return View();
        }

        // Xác nhận mở khóa tài khoản
        [HttpPost]
        public async Task<IActionResult> MoKhoa(int maTK)
        {
            // Chỉ mở khóa khi SV đã hoàn tất nghĩa vụ tài chính (không còn hóa đơn quá hạn) -
            // nếu không, job quét tự động (QuetHoaDonQuaHan) sẽ khóa lại ngay ở lần tải trang kế tiếp.
            bool conNo = await _db.HoaDons.AnyAsync(h => h.TrangThai == "QuaHan" &&
                _db.PhieuDangKys.Any(pd => pd.TrangThai == "DangO" && pd.Giuong!.MaPhong == h.MaPhong && pd.SinhVien!.MaTK == maTK));
            if (conNo)
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
        public async Task<IActionResult> ThuTienMat(string mssv)
        {
            var dsNo = await _db.HoaDons
                .Where(h => h.TrangThai == "QuaHan" &&
                    _db.PhieuDangKys.Any(pd => pd.MSSV == mssv && pd.TrangThai == "DangO" && pd.Giuong!.MaPhong == h.MaPhong))
                .Select(h => new { h.MaHD, h.TongTien })
                .ToListAsync();

            if (dsNo.Count == 0)
            {
                TempData["Loi"] = "Sinh viên này không còn hóa đơn quá hạn nào.";
                return RedirectToAction("ViPham");
            }

            foreach (var r in dsNo)
            {
                string maGDCong = "TIENMAT" + DateTime.Now.ToString("yyyyMMddHHmmss") + r.MaHD;
                // Ghi nhận dưới hình thức "NganHang" (thu trực tiếp tại văn phòng, đối soát thủ công) -
                // không phát sinh giao dịch cổng thanh toán trực tuyến thật.
                SinhVienRepo.ThanhToan(r.MaHD, mssv, "NganHang", r.TongTien, maGDCong,
                    $"Quản lý đã xác nhận thu tiền mặt hóa đơn #{r.MaHD} tại văn phòng.");
            }

            int? maTKBiKhoa = await _db.SinhViens.Where(sv => sv.MSSV == mssv && sv.TaiKhoan!.TrangThai == "BiKhoa")
                .Select(sv => (int?)sv.MaTK).FirstOrDefaultAsync();
            if (maTKBiKhoa != null)
                QuanLyRepo.MoKhoa(maTKBiKhoa.Value);

            TempData["ThanhCong"] = $"Đã xác nhận thu {dsNo.Count} hóa đơn quá hạn và mở khóa tài khoản (nếu đang bị khóa).";
            return RedirectToAction("ViPham");
        }

        // ============ Danh sách + tra cứu hóa đơn toàn hệ thống ============
        public async Task<IActionResult> HoaDon(string? tuKhoa, string? thang, string? trangThai, int trang = 1)
        {
            // Không chọn bộ lọc nào -> mặc định chỉ xem tháng hiện tại, tránh tải toàn bộ lịch sử hóa đơn cùng lúc
            // Phân biệt "chưa từng lọc" (mở trang lần đầu, không có query string) với "đã lọc nhưng cố tình để trống"
            // (đã submit form) - chỉ tự động mặc định tháng hiện tại ở trường hợp đầu, tránh ép buộc lại bộ lọc
            // khi người dùng chủ động xóa ô Tháng để xem tất cả.
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

        // ============ Báo cáo công nợ sinh viên ============
        public IActionResult BaoCaoCongNo(int trang = 1)
        {
            var ds = QuanLyRepo.BaoCaoCongNo();   // sp_BaoCaoCongNo - báo cáo hiện trạng, không phải log theo thời gian
                                                    // nên chỉ cần phân trang khi hiển thị, không cần lọc theo ngày
            decimal tongNo = 0;
            foreach (DataRow r in ds.Rows) tongNo += Convert.ToDecimal(r["TongNo"]);
            ViewBag.TongNoHeThong = tongNo;
            ViewBag.SoSVNo = ds.Rows.Count;

            var (trangHT, tongSoTrang) = PagingHelper.Chuan(trang, ds.Rows.Count);
            var dsTrang = ds.Clone();
            foreach (DataRow r in ds.Rows.Cast<DataRow>()
                         .Skip((trangHT - 1) * PagingHelper.KichThuocTrangMacDinh).Take(PagingHelper.KichThuocTrangMacDinh))
                dsTrang.ImportRow(r);
            ViewBag.DsCongNo = dsTrang;
            ViewBag.Pager = PagingHelper.TaoPager(trangHT, tongSoTrang, ds.Rows.Count, Request.Query);
            return View();
        }
    }
}
