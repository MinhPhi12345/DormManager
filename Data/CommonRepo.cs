using DormManager.Helpers;

namespace DormManager.Data
{
    /// <summary>
    /// Wrapper cho các Stored Procedure tiện ích dùng chung nhiều nơi
    /// (tra tham số hệ thống, ghi thông báo). Chỉ chứa lời gọi SP.
    /// </summary>
    public static class CommonRepo
    {
        /// <summary>sp_Chung_LayThamSo: tra giá trị 1 tham số hệ thống (TS1..TS10).</summary>
        public static string? LayThamSo(string maThamSo)
            => Db.ScalarProc("sp_Chung_LayThamSo", Db.P("@MaThamSo", maThamSo))?.ToString();

        /// <summary>Tra tham số hệ thống dạng số nguyên.</summary>
        public static int LayThamSoInt(string maThamSo)
            => int.Parse(LayThamSo(maThamSo)!);

        /// <summary>sp_Chung_ThemThongBao: ghi 1 thông báo gửi sinh viên (Email/SMS).</summary>
        public static void ThemThongBao(string mssv, int? maHD, string noiDung, string kenh = "Email")
            => Db.ExecProc("sp_Chung_ThemThongBao",
                Db.P("@MSSV", mssv), Db.P("@MaHD", (object?)maHD), Db.P("@NoiDung", noiDung), Db.P("@Kenh", kenh));
    }
}
