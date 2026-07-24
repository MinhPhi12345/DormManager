using System.Data;
using Microsoft.Data.SqlClient;

namespace DormManager.Helpers
{
    /// <summary>
    /// Lớp truy cập dữ liệu dùng chung (ADO.NET) - kết nối SQL Server.
    /// Dự án dùng kiến trúc HYBRID:
    ///  - CRUD / truy vấn 1 câu đơn giản: viết SQL tham số hóa trực tiếp trong
    ///    Controller, gọi qua Query / Exec / Scalar (CommandType.Text).
    ///  - Nghiệp vụ phức tạp (giao dịch nhiều bảng, nhiều result set, MERGE,
    ///    job quét theo tập hợp): giữ ở Stored Procedure (Database/StoredProcedures.sql)
    ///    và gọi qua QueryProc / QuerySetProc / ExecProc / ScalarProc
    ///    (CommandType.StoredProcedure).
    /// Cả hai nhánh đều tham số hóa để chống SQL injection.
    /// </summary>
    public static class Db
    {
        private static string _connStr = "";

        public static void Init(string connStr) => _connStr = connStr;

        // ================= Gọi câu SQL tham số hóa trực tiếp (CRUD / truy vấn đơn giản) =================
        public static DataTable Query(string sql, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            var dt = new DataTable();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public static int Exec(string sql, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        public static object? Scalar(string sql, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn);
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        // ================= Gọi Stored Procedure theo từng BM =================

        /// <summary>Gọi SP trả về 1 result set (SELECT) -> DataTable.</summary>
        public static DataTable QueryProc(string procName, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(procName, conn) { CommandType = CommandType.StoredProcedure };
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            var dt = new DataTable();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        /// <summary>Gọi SP trả về NHIỀU result set (nhiều câu SELECT trong 1 SP) -> DataSet.
        /// Dùng cho các trang tổng hợp (..) để giảm số lượt round-trip DB.</summary>
        public static DataSet QuerySetProc(string procName, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(procName, conn) { CommandType = CommandType.StoredProcedure };
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            var ds = new DataSet();
            using var da = new SqlDataAdapter(cmd);
            da.Fill(ds);
            return ds;
        }

        /// <summary>Gọi SP thực thi INSERT/UPDATE/DELETE, trả về số dòng bị ảnh hưởng.</summary>
        public static int ExecProc(string procName, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(procName, conn) { CommandType = CommandType.StoredProcedure };
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        /// <summary>Gọi SP trả về 1 giá trị đơn (SELECT 1 cột 1 dòng, hoặc SCOPE_IDENTITY()).</summary>
        public static object? ScalarProc(string procName, params SqlParameter[] ps)
        {
            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(procName, conn) { CommandType = CommandType.StoredProcedure };
            if (ps.Length > 0) cmd.Parameters.AddRange(ps);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        public static SqlParameter P(string name, object? value)
            => new(name, value ?? DBNull.Value);
    }
}
