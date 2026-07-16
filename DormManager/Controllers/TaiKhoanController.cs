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

            var dt = Db.QueryProc("sp_DangNhap", Db.P("@TenDangNhap", tenDangNhap.Trim()));

            if (dt.Rows.Count == 0 || dt.Rows[0]["MatKhau"].ToString() != AuthHelper.Sha256(matKhau))
            {
                ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            var row = dt.Rows[0];
            if (row["TrangThai"].ToString() == "BiKhoa")
            {
 ViewBag.Loi = "Tài khoản đang bị khóa do vi phạm quy định thanh toán. Vui lòng liên hệ Ban quản lý.";
                return View();
            }

            int maTK = (int)row["MaTK"];
            string vaiTro = row["VaiTro"].ToString()!;
            HttpContext.Session.SetInt32("MaTK", maTK);
            HttpContext.Session.SetString("VaiTro", vaiTro);
            HttpContext.Session.SetString("TenDangNhap", row["TenDangNhap"].ToString()!);

            // Lấy họ tên + mã định danh theo vai trò
            if (vaiTro == "SV")
            {
                var sv = Db.QueryProc("sp_LayHoTenSVTheoTK", Db.P("@MaTK", maTK));
                if (sv.Rows.Count > 0)
                {
                    HttpContext.Session.SetString("MSSV", sv.Rows[0]["MSSV"].ToString()!);
                    HttpContext.Session.SetString("HoTen", sv.Rows[0]["HoTen"].ToString()!);
                }
            }
            else if (vaiTro == "QL")
            {
                var ql = Db.QueryProc("sp_ThongTinQL", Db.P("@MaTK", maTK));
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
            var dt = Db.QueryProc("sp_KiemTraEmail", Db.P("@Email", (email ?? "").Trim()));
            if (dt.Rows.Count == 0)
            {
                ViewBag.Loi = "Email không tồn tại trong hệ thống. Vui lòng kiểm tra lại email được cấp.";
                return View();
            }

            // Mô phỏng gửi email đặt lại mật khẩu qua Dịch vụ Email/SMS
            ViewBag.ThanhCong = $"Yêu cầu đặt lại mật khẩu đã được gửi tới email {email}. Vui lòng kiểm tra hộp thư (mô phỏng).";
            return View();
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
