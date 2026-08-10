using DormManager.Data;
using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DormManager.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly AppDbContext _db;

        public TaiKhoanController(AppDbContext db)
        {
            _db = db;
        }

        // ============ Đăng nhập ============
        [HttpGet]
        public IActionResult DangNhap()
        {
            if (HttpContext.Session.GetString("VaiTro") != null)
                return ChuyenTrangTheoVaiTro(HttpContext.Session.GetString("VaiTro")!);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(string tenDangNhap, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View();
            }

            string tdn = tenDangNhap.Trim();
            var taiKhoan = await _db.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == tdn || t.Email == tdn);

            if (taiKhoan == null || taiKhoan.MatKhau != AuthHelper.Sha256(matKhau))
            {
                ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            bool biKhoa = taiKhoan.TrangThai == "BiKhoa";
            // QD05: quá hạn thanh toán chỉ khóa quyền đăng ký dịch vụ tiện ích phát sinh,
            // KHÔNG khóa đăng nhập - để SV vẫn vào được để tự thanh toán nợ và gỡ khóa.
            // (Quản lý vẫn có thể khóa hẳn tài khoản qua "Cập nhật tài khoản" nếu cần, đó là
            // trường hợp khác, không phải cờ BiKhoa tự động do quá hạn.)

            HttpContext.Session.SetInt32("MaTK", taiKhoan.MaTK);
            HttpContext.Session.SetString("VaiTro", taiKhoan.VaiTro);
            HttpContext.Session.SetString("TenDangNhap", taiKhoan.TenDangNhap);
            HttpContext.Session.SetString("BiKhoa", biKhoa ? "1" : "0");

            // Lấy họ tên + mã định danh theo vai trò
            if (taiKhoan.VaiTro == "SV")
            {
                var sv = await _db.SinhViens.FirstOrDefaultAsync(x => x.MaTK == taiKhoan.MaTK);
                if (sv != null)
                {
                    HttpContext.Session.SetString("MSSV", sv.MSSV);
                    HttpContext.Session.SetString("HoTen", sv.HoTen);
                }
            }
            else if (taiKhoan.VaiTro == "QL")
            {
                var ql = await _db.QuanLys.FirstOrDefaultAsync(x => x.MaTK == taiKhoan.MaTK);
                if (ql != null)
                {
                    HttpContext.Session.SetString("MaNV", ql.MaNV);
                    HttpContext.Session.SetString("HoTen", ql.HoTen);
                }
            }
            else
            {
                HttpContext.Session.SetString("HoTen", "Quản trị viên");
            }

            return ChuyenTrangTheoVaiTro(taiKhoan.VaiTro);
        }

        private IActionResult ChuyenTrangTheoVaiTro(string vaiTro) => vaiTro switch
        {
            "SV"  => RedirectToAction("TongQuan", "SinhVien"),
            "QL"  => RedirectToAction("DonYeuCau", "QuanLy"),
            "QTV" => RedirectToAction("ThongKe", "QuanTri"),
            _     => RedirectToAction("DangNhap")
        };

        // ============ Quên mật khẩu ============
        [HttpGet]
        public IActionResult QuenMatKhau() => View();

        [HttpPost]
        public async Task<IActionResult> QuenMatKhau(string hoTen, string email)
        {
            string emailChuan = (email ?? "").Trim();
            bool tonTai = await _db.TaiKhoans.AnyAsync(t => t.Email == emailChuan);
            if (!tonTai)
            {
                ViewBag.Loi = "Email không tồn tại trong hệ thống. Vui lòng kiểm tra lại email được cấp.";
                return View();
            }

            // Mô phỏng gửi email đặt lại mật khẩu qua Dịch vụ Email/SMS
            ViewBag.ThanhCong = $"Yêu cầu đặt lại mật khẩu đã được gửi tới email {email}. Vui lòng kiểm tra hộp thư (mô phỏng).";
            return View();
        }

        // ============ Đổi mật khẩu ============
        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            // Kiểm tra người dùng đã đăng nhập chưa
            if (HttpContext.Session.GetInt32("MaTK") == null)
            {
                return RedirectToAction("DangNhap");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            if (HttpContext.Session.GetInt32("MaTK") == null)
            {
                return RedirectToAction("DangNhap");
            }

            if (string.IsNullOrWhiteSpace(matKhauCu) || string.IsNullOrWhiteSpace(matKhauMoi) || string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ các trường thông tin.";
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.Loi = "Mật khẩu xác nhận không khớp với mật khẩu mới.";
                return View();
            }

            if (matKhauMoi.Length < 6)
            {
                ViewBag.Loi = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View();
            }

            int maTK = HttpContext.Session.GetInt32("MaTK")!.Value;
            string mkCuHash = AuthHelper.Sha256(matKhauCu);

            // Kiểm tra mật khẩu cũ
            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTK == maTK && t.MatKhau == mkCuHash);
            if (taiKhoan == null)
            {
                ViewBag.Loi = "Mật khẩu hiện tại không chính xác.";
                return View();
            }

            // Mã hóa mật khẩu mới và lưu vào CSDL
            taiKhoan.MatKhau = AuthHelper.Sha256(matKhauMoi);
            await _db.SaveChangesAsync();

            TempData["ThanhCong"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("DoiMatKhau");
        }

        // ============ Đăng xuất ============
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }

        public IActionResult KhongCoQuyen() => View();
    }
}
