using System.Net;
using System.Net.Mail;

namespace DormManager.Helpers
{
    /// <summary>
    /// Cấu hình + gửi email thật (SMTP) khi hệ thống gửi thông báo cho sinh viên - đọc 1 lần
    /// từ appsettings.json (mục "EmailSMTP") khi khởi động, giống cách CauHinhThanhToan đọc
    /// cấu hình VietQR. Dùng System.Net.Mail.SmtpClient sẵn có của .NET, không cần cài thêm
    /// gói NuGet nào. Nếu chưa cấu hình (để trống) thì bỏ qua việc gửi thật, hệ thống vẫn hoạt
    /// động bình thường và chỉ lưu lịch sử thông báo trong bảng THONGBAO như trước.
    /// </summary>
    public static class CauHinhEmail
    {
        public static string Host { get; private set; } = "";
        public static int Port { get; private set; } = 587;
        public static string TenDangNhap { get; private set; } = "";
        public static string MatKhau { get; private set; } = "";
        public static string EmailNguoiGui { get; private set; } = "";
        public static string TenNguoiGui { get; private set; } = "Ký túc xá";
        public static bool EnableSsl { get; private set; } = true;

        /// <summary>Đã điền đủ cấu hình SMTP thật hay chưa (tránh gửi thử với thông tin rỗng gây lỗi).</summary>
        public static bool DaCauHinh =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(TenDangNhap) && !string.IsNullOrWhiteSpace(MatKhau);

        public static void Init(IConfiguration config)
        {
            var s = config.GetSection("EmailSMTP");
            Host = s["Host"] ?? "";
            Port = int.TryParse(s["Port"], out int p) ? p : 587;
            TenDangNhap = s["TenDangNhap"] ?? "";
            MatKhau = s["MatKhau"] ?? "";
            EmailNguoiGui = s["EmailNguoiGui"] ?? TenDangNhap;
            TenNguoiGui = s["TenNguoiGui"] ?? "Ký túc xá";
            EnableSsl = !bool.TryParse(s["EnableSsl"], out bool b) || b;
        }

        /// <summary>
        /// Gửi 1 email thật tới sinh viên. Trả về true nếu gửi thành công, false nếu thất bại
        /// hoặc chưa cấu hình SMTP - KHÔNG throw exception ra ngoài để 1 email lỗi (vd sai địa
        /// chỉ, mất mạng) không làm gián đoạn luồng gửi hóa đơn cho cả phòng.
        /// </summary>
        public static bool Gui(string toEmail, string tieuDe, string noiDungHtml)
        {
            if (!DaCauHinh || string.IsNullOrWhiteSpace(toEmail)) return false;
            try
            {
                using var mail = new MailMessage
                {
                    From = new MailAddress(EmailNguoiGui, TenNguoiGui, System.Text.Encoding.UTF8),
                    Subject = tieuDe,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    Body = noiDungHtml,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };
                mail.To.Add(toEmail);

                using var smtp = new SmtpClient(Host, Port)
                {
                    Credentials = new NetworkCredential(TenDangNhap, MatKhau),
                    EnableSsl = EnableSsl
                };
                smtp.Send(mail);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
