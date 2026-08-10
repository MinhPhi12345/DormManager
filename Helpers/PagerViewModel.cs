namespace DormManager.Helpers
{
    /// <summary>Dữ liệu truyền cho partial view _Pager.cshtml - dùng chung cho mọi trang danh sách có phân trang.</summary>
    public class PagerViewModel
    {
        public int Trang { get; set; } = 1;
        public int TongSoTrang { get; set; } = 1;
        public int TongSoDong { get; set; } = 0;

        /// <summary>Toàn bộ query string hiện tại (trừ "trang") - dùng để giữ nguyên bộ lọc khi chuyển trang.</summary>
        public Dictionary<string, string?> BaseRoute { get; set; } = new();
    }
}
