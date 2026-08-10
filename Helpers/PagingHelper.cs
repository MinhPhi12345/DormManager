using Microsoft.AspNetCore.Http;

namespace DormManager.Helpers
{
    /// <summary>
    /// Tiện ích phân trang dùng chung cho các trang danh sách lớn (Hóa đơn hệ thống, Báo cáo công nợ,
    /// Lịch sử thông báo, Nhật ký hệ thống...) - tránh tải và render toàn bộ dữ liệu cùng lúc khi số
    /// dòng tăng dần theo thời gian.
    /// </summary>
    public static class PagingHelper
    {
        public const int KichThuocTrangMacDinh = 20;

        /// <summary>Chuẩn hóa số trang yêu cầu về khoảng hợp lệ [1, tổng số trang] dựa trên tổng số dòng.</summary>
        public static (int Trang, int TongSoTrang) Chuan(int trangYeuCau, int tongSoDong, int kichThuocTrang = KichThuocTrangMacDinh)
        {
            int tongSoTrang = Math.Max(1, (int)Math.Ceiling(tongSoDong / (double)kichThuocTrang));
            int trang = Math.Clamp(trangYeuCau < 1 ? 1 : trangYeuCau, 1, tongSoTrang);
            return (trang, tongSoTrang);
        }

        /// <summary>Lấy lại toàn bộ query string hiện tại, bỏ tham số "trang", để pager giữ nguyên bộ lọc khi đổi trang.</summary>
        public static Dictionary<string, string?> LayRouteHienTai(IQueryCollection query)
        {
            var d = new Dictionary<string, string?>();
            foreach (var kv in query)
                if (!string.Equals(kv.Key, "trang", StringComparison.OrdinalIgnoreCase))
                    d[kv.Key] = kv.Value.ToString();
            return d;
        }

        public static PagerViewModel TaoPager(int trang, int tongSoTrang, int tongSoDong, IQueryCollection query)
            => new PagerViewModel
            {
                Trang = trang,
                TongSoTrang = tongSoTrang,
                TongSoDong = tongSoDong,
                BaseRoute = LayRouteHienTai(query)
            };
    }
}
