using System.Data;

namespace DormManager.Helpers
{
    /// <summary>
    /// Dựng System.Data.DataTable/DataRow từ kết quả truy vấn LINQ/EF Core.
    /// Dùng khi Controller đã chuyển sang "code bình thường" (LINQ, không còn
    /// câu SQL) nhưng View Razor hiện có vẫn đang đọc dữ liệu theo kiểu
    /// DataRow (vd @d["TenCot"]) - giữ nguyên View, chỉ đổi cách Controller
    /// lấy dữ liệu.
    /// </summary>
    public static class DataTableHelper
    {
        /// <summary>Dựng bảng nhiều dòng. Mỗi phần tử của <paramref name="rows"/> là 1 mảng
        /// giá trị theo đúng thứ tự tên cột trong <paramref name="cols"/>.</summary>
        public static DataTable Build(string[] cols, IEnumerable<object?[]> rows)
        {
            var dt = new DataTable();
            foreach (var c in cols) dt.Columns.Add(c, typeof(object));
            foreach (var r in rows)
            {
                var dr = dt.NewRow();
                for (int i = 0; i < cols.Length; i++) dr[i] = r[i] ?? DBNull.Value;
                dt.Rows.Add(dr);
            }
            return dt;
        }

        /// <summary>Dựng 1 DataRow đơn (dùng cho trang chi tiết).</summary>
        public static DataRow BuildRow(string[] cols, object?[] values)
            => Build(cols, new[] { values }).Rows[0];
    }
}
