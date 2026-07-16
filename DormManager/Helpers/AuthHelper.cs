using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DormManager.Helpers
{
    public static class AuthHelper
    {
        /// <summary>Băm mật khẩu SHA-256 (theo thiết kế bảng TAIKHOAN).</summary>
        public static string Sha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>
    /// Attribute phân quyền theo Session["VaiTro"]: SV, QL, QTV.
    /// Ví dụ: [PhanQuyen("QL", "QTV")]
    /// </summary>
    public class PhanQuyenAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;
        public PhanQuyenAttribute(params string[] roles) => _roles = roles;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var vaiTro = context.HttpContext.Session.GetString("VaiTro");
            if (string.IsNullOrEmpty(vaiTro))
            {
                context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", null);
                return;
            }
            if (_roles.Length > 0 && !_roles.Contains(vaiTro))
            {
                context.Result = new RedirectToActionResult("KhongCoQuyen", "TaiKhoan", null);
            }
        }
    }
}
