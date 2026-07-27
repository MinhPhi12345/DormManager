using System.Data;
using DormManager.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DormManager.Controllers
{
    public class TaiKhoanController : Controller
    {
        // ============ Đăng nhập ============
        [HttpGet]
        public IActionResult DangNhap()
        {
            if (HttpContext.Session.GetString("VaiTro") != null)
                return ChuyenTrangTheoVaiTro(HttpContext.Session.GetString("VaiTro")!);
            return View();
        }

        [HttpPost]
        public IActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View();
            }

            var dt = Db.Query(@"SELECT MaTK, TenDangNhap, MatKhau, VaiTro, TrangThai
    FROM TAIKHOAN
    WHERE TenDangNhap = @TenDangNhap OR Email = @TenDangNhap;",
                Db.P("@TenDangNhap", tenDangNhap.Trim()));

            if (dt.Rows.Count == 0 || dt.Rows[0]["MatKhau"].ToString() != AuthHelper.Sha256(matKhau))
            {
                ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            var row = dt.Rows[0];
            bool biKhoa = row["TrangThai"].ToString() == "BiKhoa";
            // QD05: quá hạn thanh toán chỉ khóa quyền đăng ký dịch vụ tiện ích phát sinh,
            // KHÔNG khóa đăng nhập - để SV vẫn vào được để tự thanh toán nợ và gỡ khóa.
            // (Quản lý vẫn có thể khóa hẳn tài khoản qua "Cập nhật tài khoản" nếu cần, đó là
            // trường hợp khác, không phải cờ BiKhoa tự động do quá hạn.)

            int maTK = (int)row["MaTK"];
            string vaiTro = row["VaiTro"].ToString()!;
            HttpContext.Session.SetInt32("MaTK", maTK);
            HttpContext.Session.SetString("VaiTro", vaiTro);
            HttpContext.Session.SetString("TenDangNhap", row["TenDangNhap"].ToString()!);
            HttpContext.Session.SetString("BiKhoa", biKhoa ? "1" : "0");

            // Lấy họ tên + mã định danh theo vai trò
            if (vaiTro == "SV")
            {
                var sv = Db.Query(@"SELECT MSSV, HoTen FROM SINHVIEN WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
                if (sv.Rows.Count > 0)
                {
                    HttpContext.Session.SetString("MSSV", sv.Rows[0]["MSSV"].ToString()!);
                    HttpContext.Session.SetString("HoTen", sv.Rows[0]["HoTen"].ToString()!);
                }
            }
            else if (vaiTro == "QL")
            {
                var ql = Db.Query(@"SELECT MaNV, HoTen FROM QUANLY WHERE MaTK = @MaTK;", Db.P("@MaTK", maTK));
                if (ql.Rows.Count > 0)
                {
                    HttpContext.Session.SetString("MaNV", ql.Rows[0]["MaNV"].ToString()!);
                    HttpContext.Session.SetString("HoTen", ql.Rows[0]["HoTen"].ToString()!);
                }
            }
            else
            {
                HttpContext.Session.SetString("HoTen", "Quản trị viên");
            }

            return ChuyenTrangTheoVaiTro(vaiTro);
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
        public IActionResult QuenMatKhau(string hoTen, string email)
        {
            var dt = Db.Query(@"SELECT MaTK FROM TAIKHOAN WHERE Email = @Email;", Db.P("@Email", (email ?? "").Trim()));
            if (dt.Rows.Count == 0)
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
        public IActionResult DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
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

            int maTK = HttpContext.Session.GetInt32("MaTK").Value;
            string mkCuHash = AuthHelper.Sha256(matKhauCu);

            // Kiểm tra mật khẩu cũ
            var dt = Db.Query("SELECT MaTK FROM TAIKHOAN WHERE MaTK = @MaTK AND MatKhau = @MatKhau",
                Db.P("@MaTK", maTK), Db.P("@MatKhau", mkCuHash));

            if (dt.Rows.Count == 0)
            {
                ViewBag.Loi = "Mật khẩu hiện tại không chính xác.";
                return View();
            }

            // Mã hóa mật khẩu mới và lưu vào CSDL
            string mkMoiHash = AuthHelper.Sha256(matKhauMoi);
            Db.Exec("UPDATE TAIKHOAN SET MatKhau = @MatKhauMoi WHERE MaTK = @MaTK",
                Db.P("@MatKhauMoi", mkMoiHash), Db.P("@MaTK", maTK));

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
