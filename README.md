# DormManager - Website quản lý Ký túc xá & Hóa đơn điện nước (Nhóm 19)

Công nghệ: **ASP.NET Core MVC (.NET 8)** · **C#** · **SQL Server (LocalDB)** · **Bootstrap 5.3** · ADO.NET (SQL tham số hóa)

## 1. Yêu cầu môi trường
- Visual Studio 2022 (workload **ASP.NET and web development**)
- .NET 8 SDK
- SQL Server LocalDB (cài sẵn kèm Visual Studio) hoặc SQL Server Express

## 2. Tạo cơ sở dữ liệu
Chạy **LẦN LƯỢT 2 file SQL** theo đúng thứ tự (SQL Server Object Explorer trong Visual Studio hoặc SQL Server Management Studio, kết nối tới `(localdb)\MSSQLLocalDB`):

1. **`Database/DormManager.sql`** — tạo database `DormManager` với **16 bảng** theo LAB 3 + dữ liệu mẫu.
2. **`Database/StoredProcedures.sql`** — tạo **99 Stored Procedure**, mỗi SP phục vụ đúng 1 Biểu mẫu (BM01-BM51). Toàn bộ nghiệp vụ của website (truy vấn, thêm/sửa/xóa) đều đi qua các SP này thay vì viết SQL trực tiếp trong code C#.

> Phải chạy file (1) trước, vì file (2) tham chiếu tới các bảng vừa tạo. Nếu dùng SQL Server Express/instance khác, sửa `ConnectionStrings:DormManager` trong `DormManager/appsettings.json` cho khớp.

> Khi chỉnh sửa lại schema hoặc thêm nghiệp vụ mới, chỉ cần sửa/thêm SP tương ứng trong `StoredProcedures.sql` rồi chạy lại **riêng file này** (không cần chạy lại `DormManager.sql`, tránh mất dữ liệu) — mỗi SP đều có `IF OBJECT_ID(...) DROP PROCEDURE` ở đầu nên chạy lại an toàn, không lỗi trùng tên.

## 3. Chạy website
1. Mở **`DormManager.sln`** bằng Visual Studio.
2. Nhấn **F5** (hoặc Ctrl+F5) để chạy. Trang đăng nhập sẽ mở ra.

## 4. Tài khoản mẫu (mật khẩu tất cả: `123456`)
| Vai trò | Tên đăng nhập | Ghi chú |
|---|---|---|
| Quản trị viên | `admin` | Thống kê, phòng, đơn giá, cổng đăng ký, tài khoản |
| Quản lý | `ql01`, `ql02` | Đơn từ, đơn đăng ký, chỉ số, hóa đơn, vi phạm |
| Sinh viên | `SV2024001`, `SV2024002`, `SV2025001`, `SV2025002`, `SV2026001` | SV2026001 có phiếu **Chờ đối chiếu** để demo BM30 |

## 5. Luồng demo gợi ý
1. **SV** đăng nhập `SV2025002` → Tra cứu phòng → chọn giường trống → Đăng ký (BM04-08).
2. **QL** đăng nhập `ql01` → Đơn đăng ký KTX → xác nhận nhận phòng (BM25-27, BM30).
3. **QL** → Chỉ số điện/nước → nhập chỉ số tháng hiện tại (BM34) → Lập hóa đơn → tạo & gửi (BM35/36).
4. **SV** → Hóa đơn → chi tiết → thanh toán mô phỏng VNPay/MoMo (BM16-18).
5. **QTV** đăng nhập `admin` → Thống kê (BM46), thêm phòng (BM41), đổi đơn giá có trần QD14 (BM44), tạo đợt đăng ký ưu tiên tự đóng sau TS1 ngày (BM45), tạo/cập nhật tài khoản (BM47-51).
6. **QL** → Sinh viên vi phạm: trang này mô phỏng job tự động quét hóa đơn quá hạn → khóa tài khoản (QD05) và ghi vi phạm (QD06); nút **Mở khóa** = BM21.

## 6. Kiến trúc truy cập dữ liệu: Stored Procedure theo từng BM
Toàn bộ nghiệp vụ được cài đặt dưới dạng **Stored Procedure**, đặt tên theo quy ước `sp_BMxx_TenChucNang` (ví dụ `sp_BM34_LuuChiSo`, `sp_BM46_ThongKe`...) trong file `Database/StoredProcedures.sql`. Controller (C#) chỉ gọi SP qua lớp `Helpers/Db.cs` (`QueryProc`, `ExecProc`, `ScalarProc`, `QuerySetProc`) bằng `CommandType.StoredProcedure`, không còn nhúng câu SQL trực tiếp trong code.

Một vài SP dùng chung nhiều BM (đặt tên `sp_Chung_...`) cho các thao tác lặp lại ở nhiều nơi:
- `sp_Chung_LayThamSo` - tra giá trị tham số hệ thống (TS1-TS10) dùng trong các quy định QD01-QD15.
- `sp_Chung_ThemThongBao` - ghi thông báo gửi sinh viên (QD08, QD12).

Một số trang tổng hợp nhiều số liệu (BM03 - Tổng quan sinh viên, BM46 - Thống kê QTV) dùng 1 SP trả về **nhiều result set** trong 1 lượt gọi (qua `Db.QuerySetProc` → `DataSet`), giảm số lượt round-trip tới SQL Server thay vì gọi nhiều query riêng lẻ.

Các thao tác ghi dữ liệu liên quan nhiều bảng (ví dụ BM06 - xác nhận đăng ký giường, BM18 - thanh toán hóa đơn, BM23 - xử lý đơn kèm gửi thông báo) được gói trong `BEGIN TRANSACTION ... COMMIT/ROLLBACK` ngay trong SP để đảm bảo toàn vẹn dữ liệu.

## 7. Chiến lược tổ chức CSS (theo yêu cầu đề bài)
Không dồn toàn bộ style vào 1 file duy nhất:
- **`wwwroot/css/site.css`**: CHỈ chứa khung sườn dùng chung (sidebar, topbar, biến màu, tiện ích `.btn-dm`).
- **View đơn giản → style Inline**: khối `<style>` đặt ngay trong view qua `@section Styles` (VD: `DangNhap`, `HopDong`, `Giuong`, `DonGia`, `DotDangKy`, `ViPham`...).
- **View phức tạp → style External**: file riêng trong **`wwwroot/css/views/`** (VD: `tongquan.css`, `tracuuphong.css`, `hoadon.css`, `hoadon-chitiet.css`, `chiso.css`, `taohoadon.css`, `thongke.css`, `phong-admin.css`, `taikhoan-admin.css`, `donyeucau-ql.css`, `dondangky-ql.css`).

Giao diện responsive: sidebar chuyển thành **offcanvas** trên màn hình nhỏ, bảng cuộn ngang bằng `.table-responsive`, thẻ/lưới co giãn theo breakpoint Bootstrap.

## 8. Quy định nghiệp vụ đã cài đặt
- QD01/TS1: đợt ưu tiên tân SV tự đóng sau 14 ngày; chỉ khóa mới nhất được đăng ký đợt ưu tiên.
- QD02: SV diện chính sách chỉ thấy phòng 6-8 giường, được gia hạn mọi lúc.
- QD04: `TongTien = TienPhong + TienDien + TienNuoc (+ phí dịch vụ)`; thanh toán online mô phỏng.
- QD05/TS3: khóa tài khoản khi hóa đơn quá hạn ≥ 7 ngày (quét khi mở trang Vi phạm).
- QD06/TS4: ghi vi phạm khi quá hạn > 14 ngày.
- QD08: mọi cập nhật đơn từ/hóa đơn đều sinh bản ghi THONGBAO.
- QD10: chỉ số cuối kỳ ≥ đầu kỳ, đã chốt thì không sửa được.
- QD14/TS7-TS8: đơn giá điện ≤ 3.500đ/kWh, nước ≤ 25.000đ/m³.
- Mật khẩu băm **SHA-256**, phân quyền session qua attribute `[PhanQuyen]`.
