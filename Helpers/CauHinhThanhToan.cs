namespace DormManager.Helpers
{
    /// <summary>
    /// Cấu hình tài khoản ngân hàng của KTX để sinh mã QR chuyển khoản (chuẩn VietQR) -
    /// đọc 1 lần từ appsettings.json (mục "ThanhToanNganHang") khi khởi động, giống cách
    /// Db.Init đọc chuỗi kết nối. VietQR (img.vietqr.io) là dịch vụ sinh ảnh QR động miễn phí,
    /// không cần API key - các app ngân hàng Việt Nam quét được để tự điền số tài khoản,
    /// số tiền và nội dung chuyển khoản.
    /// </summary>
    public static class CauHinhThanhToan
    {
        public static string MaNganHang { get; private set; } = "";
        public static string SoTaiKhoan { get; private set; } = "";
        public static string ChuTaiKhoan { get; private set; } = "";
        public static string TenNganHang { get; private set; } = "";

        public static void Init(IConfiguration config)
        {
            var s = config.GetSection("ThanhToanNganHang");
            MaNganHang = s["MaNganHang"] ?? "";
            SoTaiKhoan = s["SoTaiKhoan"] ?? "";
            ChuTaiKhoan = s["ChuTaiKhoan"] ?? "";
            TenNganHang = s["TenNganHang"] ?? "";
        }

        /// <summary>Sinh URL ảnh mã QR VietQR cho đúng số tiền + nội dung chuyển khoản của 1 hóa đơn cụ thể
        /// (mã hóa đơn - dùng để đối soát thủ công khi tiền về tài khoản).</summary>
        public static string TaoUrlQr(decimal soTien, string noiDung)
        {
            string info = Uri.EscapeDataString(noiDung);
            string ten = Uri.EscapeDataString(ChuTaiKhoan);
            return $"https://img.vietqr.io/image/{MaNganHang}-{SoTaiKhoan}-qr_only.png?amount={(long)soTien}&addInfo={info}&accountName={ten}";
        }
    }
}
