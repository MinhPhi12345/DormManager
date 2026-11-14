/* =====================================================================
   DORMMANAGER - Phần mềm quản lý Ký túc xá và Hóa đơn điện nước
   Nhóm 19 - CNPM - Script tạo CSDL SQL Server (theo LAB 3)
   Chạy toàn bộ file này trong SSMS hoặc Visual Studio (SQL Server Object Explorer)

   Đây là bản gộp đầy đủ - chỉ cần chạy file này, sau đó chạy
   StoredProcedures.sql là đủ, không cần chạy thêm file ALTER nào khác.
   ===================================================================== */
USE master;
GO
IF DB_ID('DormManager') IS NOT NULL
BEGIN
    ALTER DATABASE DormManager SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DormManager;
END
GO
CREATE DATABASE DormManager;
GO
USE DormManager;
GO

/* ============================ 1. TAIKHOAN ============================ */
CREATE TABLE TAIKHOAN (
    MaTK        INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap VARCHAR(50)  NOT NULL UNIQUE,
    MatKhau     VARCHAR(255) NOT NULL,                -- SHA-256
    Email       VARCHAR(100) NOT NULL UNIQUE,
    SDT         VARCHAR(15)  NULL,
    VaiTro      VARCHAR(10)  NOT NULL CHECK (VaiTro IN ('SV','QL','QTV')),
    TrangThai   VARCHAR(15)  NOT NULL DEFAULT 'HoatDong' CHECK (TrangThai IN ('HoatDong','BiKhoa')),
    NgayTao     DATETIME     NOT NULL DEFAULT GETDATE()
);

/* ============================ 2. SINHVIEN ============================ */
CREATE TABLE SINHVIEN (
    MSSV       VARCHAR(10)  PRIMARY KEY,
    MaTK       INT          NOT NULL UNIQUE FOREIGN KEY REFERENCES TAIKHOAN(MaTK),
    HoTen      NVARCHAR(100) NOT NULL,
    NgaySinh   DATE         NULL,
    GioiTinh   VARCHAR(5)   NULL CHECK (GioiTinh IN ('Nam','Nu')),
    KhoaHoc    VARCHAR(10)  NOT NULL,                 -- VD 'K2026'
    DoiTuong   VARCHAR(15)  NOT NULL DEFAULT 'BinhThuong' CHECK (DoiTuong IN ('BinhThuong','ChinhSach')),
    DiemViPham INT          NOT NULL DEFAULT 0 CHECK (DiemViPham >= 0)
);

/* ============================ 3. TOANHA ============================= */
CREATE TABLE TOANHA (
    MaToa   VARCHAR(10)   PRIMARY KEY,
    TenToa  NVARCHAR(50)  NOT NULL,
    DiaChi  NVARCHAR(200) NULL,
    SoTang  INT           NOT NULL CHECK (SoTang > 0)
);

/* ============================ 4. QUANLY ============================= */
CREATE TABLE QUANLY (
    MaNV  VARCHAR(10)   PRIMARY KEY,
    MaTK  INT           NOT NULL UNIQUE FOREIGN KEY REFERENCES TAIKHOAN(MaTK),
    HoTen NVARCHAR(100) NOT NULL,
    MaToa VARCHAR(10)   NULL FOREIGN KEY REFERENCES TOANHA(MaToa)
);

/* ============================ 5. PHONG ============================== */
CREATE TABLE PHONG (
    MaPhong       VARCHAR(10) PRIMARY KEY,             -- VD 'A1-201'
    MaToa         VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES TOANHA(MaToa),
    Tang          INT         NOT NULL CHECK (Tang > 0),
    LoaiPhong     VARCHAR(2)  NOT NULL CHECK (LoaiPhong IN ('4','6','8')),
    SoGiuong      INT         NOT NULL CHECK (SoGiuong > 0),
    GiaPhong      DECIMAL(10,0) NOT NULL CHECK (GiaPhong >= 0), -- VNĐ/người/tháng
    TrangThai     VARCHAR(15) NOT NULL DEFAULT 'HoatDong' CHECK (TrangThai IN ('HoatDong','BaoTri','NgungSuDung')),
    SoGiuongTrong INT         NOT NULL CHECK (SoGiuongTrong >= 0)
);

/* ========================== 5b. ANHPHONG =============================
   Ảnh minh họa của phòng (1 phòng - nhiều ảnh) - Admin (QTV) thêm/xóa,
   sinh viên xem khi tra cứu/xem chi tiết phòng để yên tâm hơn khi đăng ký. */
CREATE TABLE ANHPHONG (
    MaAnh    INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong  VARCHAR(10)   NOT NULL FOREIGN KEY REFERENCES PHONG(MaPhong),
    DuongDan NVARCHAR(255) NOT NULL,             -- đường dẫn tương đối trong wwwroot, VD /uploads/phong/A1-101/xxx.jpg
    ThuTu    INT           NOT NULL DEFAULT 0,   -- thứ tự hiển thị, ảnh đầu tiên = ảnh đại diện
    NgayTao  DATETIME      NOT NULL DEFAULT GETDATE()
);

/* ============================ 6. GIUONG ============================= */
CREATE TABLE GIUONG (
    MaGiuong  VARCHAR(15) PRIMARY KEY,                 -- VD 'A1-201-G1'
    MaPhong   VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES PHONG(MaPhong),
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'Trong' CHECK (TrangThai IN ('Trong','DaSuDung','BaoTri'))
);

/* =========================== 7. DOTDANGKY =========================== */
CREATE TABLE DOTDANGKY (
    MaDot     INT IDENTITY(1,1) PRIMARY KEY,
    TenDot    NVARCHAR(100) NOT NULL,
    LoaiDot   VARCHAR(10)   NOT NULL CHECK (LoaiDot IN ('UuTien','DaiTra','KyHe')),
    HocKy     VARCHAR(20)   NOT NULL,                  -- VD 'HK1-2026'
    NgayMo    DATETIME      NOT NULL,
    NgayDong  DATETIME      NOT NULL,
    TrangThai VARCHAR(10)   NOT NULL DEFAULT 'ChuaMo' CHECK (TrangThai IN ('ChuaMo','DangMo','DaDong')),
    CHECK (NgayDong > NgayMo)
);

/* ========================== 8. PHIEUDANGKY ========================== */
CREATE TABLE PHIEUDANGKY (
    MaPhieu     INT IDENTITY(1,1) PRIMARY KEY,
    MSSV        VARCHAR(10) NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    MaGiuong    VARCHAR(15) NOT NULL FOREIGN KEY REFERENCES GIUONG(MaGiuong),
    MaDot       INT         NOT NULL FOREIGN KEY REFERENCES DOTDANGKY(MaDot),
    NgayDangKy  DATETIME    NOT NULL DEFAULT GETDATE(),
    NgayBatDau  DATE        NOT NULL,
    NgayKetThuc DATE        NOT NULL,
    TrangThai   VARCHAR(15) NOT NULL DEFAULT 'ChoDoiChieu' CHECK (TrangThai IN ('ChoDoiChieu','DangO','DaTraPhong','DaHuy'))
);

/* =========================== 9. DONYEUCAU =============================
   LoaiDon có thêm 'TraPhong' (yêu cầu trả phòng do SV gửi, QL xác nhận) và
   'ChuyenPhong' (yêu cầu chuyển phòng do SV gửi, QL xác nhận).
   MaPhieu liên kết đơn với hợp đồng hiện tại - dùng khi LoaiDon='TraPhong'
   hoặc 'ChuyenPhong'. MaGiuongMoi là giường SV muốn chuyển đến - chỉ dùng
   khi LoaiDon='ChuyenPhong'. */
CREATE TABLE DONYEUCAU (
    MaDon       INT IDENTITY(1,1) PRIMARY KEY,
    MSSV        VARCHAR(10)  NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    MaNV        VARCHAR(10)  NULL FOREIGN KEY REFERENCES QUANLY(MaNV),
    MaPhieu     INT          NULL FOREIGN KEY REFERENCES PHIEUDANGKY(MaPhieu),
    MaGiuongMoi VARCHAR(15)  NULL FOREIGN KEY REFERENCES GIUONG(MaGiuong),
    LoaiDon     VARCHAR(12)  NOT NULL CHECK (LoaiDon IN ('PhanHoi','DeXuat','TraPhong','ChuyenPhong')),
    TieuDe      NVARCHAR(200) NOT NULL,
    NoiDung     NVARCHAR(MAX) NOT NULL,
    MucUuTien   VARCHAR(10)  NOT NULL DEFAULT 'TrungBinh' CHECK (MucUuTien IN ('Cao','TrungBinh','Thap')),
    TrangThai   VARCHAR(10)  NOT NULL DEFAULT 'ChoXuLy' CHECK (TrangThai IN ('ChoXuLy','DangXuLy','DaXuLy','TuChoi')),
    PhanHoi     NVARCHAR(MAX) NULL,
    NgayTao     DATETIME     NOT NULL DEFAULT GETDATE()
);

/* ======================== 10. CHISODIENNUOC ========================= */
CREATE TABLE CHISODIENNUOC (
    MaChiSo   INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong   VARCHAR(10)  NOT NULL FOREIGN KEY REFERENCES PHONG(MaPhong),
    Thang     CHAR(7)      NOT NULL,                   -- 'MM/YYYY'
    DienDauKy  DECIMAL(10,1) NOT NULL CHECK (DienDauKy >= 0),
    DienCuoiKy DECIMAL(10,1) NOT NULL,
    NuocDauKy  DECIMAL(10,1) NOT NULL CHECK (NuocDauKy >= 0),
    NuocCuoiKy DECIMAL(10,1) NOT NULL,
    TrangThai VARCHAR(10)  NOT NULL DEFAULT 'Nhap' CHECK (TrangThai IN ('Nhap','DaChot')),
    CONSTRAINT UQ_ChiSo UNIQUE (MaPhong, Thang),
 CHECK (DienCuoiKy >= DienDauKy), --
    CHECK (NuocCuoiKy >= NuocDauKy)
);

/* ============================ 11. DONGIA ============================ */
CREATE TABLE DONGIA (
    MaDonGia   INT IDENTITY(1,1) PRIMARY KEY,
    GiaDien    DECIMAL(10,0) NOT NULL CHECK (GiaDien > 0),
    GiaNuoc    DECIMAL(10,0) NOT NULL CHECK (GiaNuoc > 0),
    PhiDichVu  DECIMAL(10,0) NOT NULL DEFAULT 0 CHECK (PhiDichVu >= 0),
    NgayApDung DATE          NOT NULL,
    TrangThai  VARCHAR(15)   NOT NULL DEFAULT 'HieuLuc' CHECK (TrangThai IN ('HieuLuc','HetHieuLuc'))
);

/* ============================ 12. HOADON ==============================
   Hóa đơn điện/nước/tiền phòng - lập CHUNG theo phòng mỗi tháng, chia sẻ
   cho cả phòng (ai trả trước thì hóa đơn đóng cho cả phòng). TienPhong =
   GiaPhong × số người dọn vào từ ngày TS12 của tháng đó trở về trước
   (SV dọn vào SAU ngày TS12 được miễn tiền phòng tháng này, tính từ
   tháng kế tiếp - tránh tính đủ 1 tháng tiền phòng cho SV mới ở vài ngày). */
CREATE TABLE HOADON (
    MaHD         INT IDENTITY(1,1) PRIMARY KEY,
    MaPhong      VARCHAR(10)   NOT NULL FOREIGN KEY REFERENCES PHONG(MaPhong),
    MaChiSo      INT           NOT NULL UNIQUE FOREIGN KEY REFERENCES CHISODIENNUOC(MaChiSo),
    MaDonGia     INT           NOT NULL FOREIGN KEY REFERENCES DONGIA(MaDonGia),
    Thang        CHAR(7)       NOT NULL,
    TienPhong    DECIMAL(12,0) NOT NULL CHECK (TienPhong >= 0),  -- = GiaPhong × SoNguoiO(đủ tháng) + PhiDichVu
    TienDien     DECIMAL(12,0) NOT NULL,               -- = kWh × GiaDien
    TienNuoc     DECIMAL(12,0) NOT NULL,               -- = m³ × GiaNuoc
    TongTien     DECIMAL(12,0) NOT NULL,               -- = TienPhong + TienDien + TienNuoc
    NgayPhatHanh DATETIME      NULL,
    HanThanhToan DATE          NOT NULL,
    TrangThai    VARCHAR(15)   NOT NULL DEFAULT 'Nhap' CHECK (TrangThai IN ('Nhap','ChoThanhToan','QuaHan','DaThanhToan'))
);

/* =========================== 13. THANHTOAN ============================ */
CREATE TABLE THANHTOAN (
    MaGD       INT IDENTITY(1,1) PRIMARY KEY,
    MaHD       INT           NOT NULL FOREIGN KEY REFERENCES HOADON(MaHD),
    MSSV       VARCHAR(10)   NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    PhuongThuc VARCHAR(10)   NOT NULL CHECK (PhuongThuc IN ('VNPay','MoMo','NganHang')),
    SoTien     DECIMAL(12,0) NOT NULL CHECK (SoTien > 0),
    MaGDCong   VARCHAR(50)   NULL,
    ThoiGian   DATETIME      NOT NULL DEFAULT GETDATE(),
    KetQua     VARCHAR(10)   NOT NULL CHECK (KetQua IN ('ThanhCong','ThatBai'))
);

/* ============================ 14. THONGBAO ============================ */
CREATE TABLE THONGBAO (
    MaTB        INT IDENTITY(1,1) PRIMARY KEY,
    MSSV        VARCHAR(10)  NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    MaHD        INT          NULL FOREIGN KEY REFERENCES HOADON(MaHD),
    NoiDung     NVARCHAR(MAX) NOT NULL,
    Kenh        VARCHAR(5)   NOT NULL CHECK (Kenh IN ('Email')),   -- QD12 chỉ yêu cầu gửi qua email; hệ thống hiện chỉ gửi email thật (SMTP)
    ThoiGianGui DATETIME     NOT NULL DEFAULT GETDATE(),
    TrangThaiGui VARCHAR(10) NOT NULL DEFAULT 'ThanhCong' CHECK (TrangThaiGui IN ('ThanhCong','ThatBai','ChoGuiLai'))
);

/* ============================ 15. VIPHAM =============================== */
CREATE TABLE VIPHAM (
    MaVP        INT IDENTITY(1,1) PRIMARY KEY,
    MSSV        VARCHAR(10)   NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    MaHD        INT           NULL FOREIGN KEY REFERENCES HOADON(MaHD),
    NgayGhiNhan DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    SoDiem      INT           NOT NULL DEFAULT 1,
    LyDo        NVARCHAR(200) NOT NULL
);

/* ============================ 16. THAMSO ============================= */
CREATE TABLE THAMSO (
    MaThamSo VARCHAR(10)  PRIMARY KEY,
    GiaTri   VARCHAR(20)  NOT NULL,
    GhiChu   NVARCHAR(200) NULL
);
GO

/* ============================ 17. NHATKY ==============================
   Nhật ký thao tác hệ thống (audit log) - ghi lại ai thêm/sửa/xóa phòng,
   tài khoản, đơn giá... Chỉ Quản trị viên xem được (trang Nhật ký hệ thống). */
CREATE TABLE NHATKY (
    MaNhatKy           INT IDENTITY(1,1) PRIMARY KEY,
    MaTK               INT NULL FOREIGN KEY REFERENCES TAIKHOAN(MaTK),
    HoTenNguoiThucHien NVARCHAR(100) NULL,
    VaiTro             VARCHAR(10) NULL,
    HanhDong           VARCHAR(20) NOT NULL CHECK (HanhDong IN ('Them','Sua','Xoa','Khoa','MoKhoa')),
    DoiTuong           NVARCHAR(50) NOT NULL,
    NoiDung            NVARCHAR(500) NOT NULL,
    ThoiGian           DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* =====================================================================
   DỮ LIỆU MẪU (bản chuẩn hóa 15/10/2026)
   Mật khẩu mặc định của MỌI tài khoản là: 123456
   (SHA-256 = 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92)

   Nguyên tắc của bộ dữ liệu này:
   - Mọi ngày tháng tính TƯƠNG ĐỐI theo GETDATE() để dữ liệu luôn hợp lệ dù
     chạy script vào thời điểm nào.
   - Dữ liệu nhất quán với nghiệp vụ và với cách ứng dụng tự sinh dữ liệu:
     + Hợp đồng chỉ thuộc đợt đăng ký đang mở tại ngày đăng ký; thời hạn theo
       TS2/TS10, hợp đồng dài hơn TS2 tháng là do đã gia hạn (có thông báo gia hạn).
     + Trạng thái giường và số giường trống khớp với hợp đồng (SoGiuongTrong =
       số giường Trống, giường bảo trì không tính là trống).
     + Chỉ số điện/nước cộng dồn (chỉ số đầu kỳ tháng này = chỉ số cuối kỳ tháng
       trước), chỉ có ở những tháng phòng có người ở.
     + Hóa đơn tính đúng công thức của ứng dụng: TienPhong = GiaPhong x số người
       dọn vào trước/đúng ngày TS12 + PhiDichVu; TienDien = kWh x GiaDien;
       TienNuoc = m3 x GiaNuoc; đơn giá lấy theo biểu giá hiệu lực của tháng.
     + Chỉ người ở trong tháng mới nhận thông báo/thanh toán hóa đơn tháng đó.
     + Khóa tài khoản (QD05), vi phạm lũy tiến (QD06) và nhắc hạn (QD07) chỉ
       xuất hiện khi có hóa đơn tương ứng; DiemViPham = tổng SoDiem trong VIPHAM.
     + Đơn trả/chuyển phòng có tiêu đề, nội dung, mức ưu tiên giống hệt đơn do
       ứng dụng tạo, và chỉ tồn tại khi sinh viên đủ điều kiện gửi đơn.
   ===================================================================== */
DECLARE @mk    VARCHAR(255) = '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92';
DECLARE @Today DATE = CAST(GETDATE() AS DATE);

-- Đợt đăng ký (theo thứ tự thời gian):
--   HK2-2025 đại trà (đã đóng lâu) -> Học kỳ hè (đã đóng) -> Ưu tiên HK1 (đã đóng)
--   -> Đại trà HK1 (ĐANG MỞ) -> Đại trà HK2 (dự kiến, chưa mở)
DECLARE @DTc_Mo DATE = DATEADD(DAY,-300,@Today), @DTc_Dong DATE = DATEADD(DAY,-270,@Today);
DECLARE @KH_Mo  DATE = DATEADD(DAY,-150,@Today), @KH_Dong  DATE = DATEADD(DAY,-130,@Today);
DECLARE @UT_Mo  DATE = DATEADD(DAY,-60, @Today), @UT_Dong  DATE = DATEADD(DAY,-46, @Today);  -- 14 ngày = TS1
DECLARE @DT_Mo  DATE = DATEADD(DAY,-45, @Today), @DT_Dong  DATE = DATEADD(DAY, 30, @Today);
DECLARE @DT2_Mo DATE = DATEADD(DAY, 60, @Today), @DT2_Dong DATE = DATEADD(DAY, 90, @Today);

-- Hợp đồng:
--   Nhóm "ở lâu": đăng ký ở đợt HK2-2025, hợp đồng 5 tháng rồi gia hạn thêm 5 tháng
--   (khi đợt Học kỳ hè mở) -> hiện còn 27 ngày là hết hạn (trong TS13 = 30 -> ĐƯỢC gia hạn tiếp).
DECLARE @KT_Lau  DATE = DATEADD(DAY, 27, @Today);
DECLARE @BD_Lau  DATE = DATEADD(MONTH,-10,@KT_Lau);
DECLARE @NgayGiaHanLau DATETIME = DATEADD(HOUR, 9, CAST(DATEADD(DAY,-140,@Today) AS DATETIME));
--   Nhóm "mới dọn vào" (đợt đại trà HK1): còn khoảng 130 ngày -> BỊ CHẶN gia hạn (TS13).
DECLARE @BD_Moi  DATE = DATEADD(DAY,-20, @Today);
DECLARE @KT_Moi  DATE = DATEADD(MONTH,5,@BD_Moi);
--   Phiếu chờ đối chiếu: vừa đăng ký 2 ngày trước (ứng dụng đặt NgayBatDau = ngày đăng ký).
DECLARE @BD_Cho  DATE = DATEADD(DAY,-2,  @Today);
DECLARE @KT_Cho  DATE = DATEADD(MONTH,5,@BD_Cho);
--   Hợp đồng học kỳ hè đã kết thúc và đã trả phòng (TS10 = 2 tháng).
DECLARE @BD_He   DATE = DATEADD(DAY,-145,@Today);
DECLARE @KT_He   DATE = DATEADD(MONTH,2,@BD_He);

-- Tham số hệ thống
INSERT INTO THAMSO (MaThamSo, GiaTri, GhiChu) VALUES
('TS1','14',N'Số ngày của đợt đăng ký ưu tiên Tân sinh viên'),
('TS2','5', N'Số tháng tối đa của hợp đồng lưu trú trong một học kỳ chính'),
('TS3','7', N'Số ngày quá hạn thanh toán để khóa quyền đăng ký dịch vụ tiện ích'),
('TS4','14',N'Số ngày quá hạn thanh toán để ghi nhận vi phạm nội quy'),
('TS5','3', N'Số ngày trước hạn thanh toán để gửi nhắc nhở lần hai'),
('TS6','1', N'Số điểm vi phạm ghi nhận cho mỗi lần quá hạn trên 14 ngày'),
('TS7','3500', N'Đơn giá điện trần theo quy định pháp luật (VNĐ/kWh)'),
('TS8','25000',N'Đơn giá nước trần theo quy định pháp luật (VNĐ/m³)'),
('TS9','6-8', N'Loại phòng tiêu chuẩn dành cho sinh viên diện chính sách'),
('TS10','2', N'Số tháng của đợt lưu trú Học kỳ hè'),
('TS11','15',N'Số ngày tối thiểu phải ở tại phòng hiện tại trước khi được đăng ký chuyển phòng'),
('TS12','10',N'Ngày cuối trong tháng còn tính tiền phòng nếu dọn vào - dọn vào từ ngày này trở về trước vẫn tính tiền phòng tháng đó, dọn vào sau ngày này được miễn, tính từ tháng kế tiếp'),
('TS13','30',N'Số ngày trước khi hợp đồng hết hạn thì sinh viên mới được phép gia hạn (tránh gia hạn chồng chất nhiều lần cùng lúc)');

DECLARE @TS4  INT = (SELECT CAST(GiaTri AS INT) FROM THAMSO WHERE MaThamSo='TS4');
DECLARE @TS6  INT = (SELECT CAST(GiaTri AS INT) FROM THAMSO WHERE MaThamSo='TS6');
DECLARE @TS12 INT = (SELECT CAST(GiaTri AS INT) FROM THAMSO WHERE MaThamSo='TS12');

-- Tài khoản. Chỉ có 3 tài khoản bị khóa và đều có lý do rõ ràng:
--   ql03      : Quản trị viên khóa thủ công (xem Nhật ký hệ thống).
--   25DH113345: đang ở A1-102, bị khóa theo QD05 vì hóa đơn tháng trước nữa của phòng quá hạn > TS3 ngày.
--   25DH113354: từng ở A1-102 (hợp đồng đã kết thúc), cũng đứng tên phòng lúc hóa đơn đó phát sinh
--               nên bị khóa theo QD05 -> sinh viên bị khóa quyền đăng ký mà KHÔNG có hợp đồng hiệu lực.
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, SDT, VaiTro, TrangThai, NgayTao) VALUES
('admin',       @mk, 'admin@ktx.edu.vn',            '0900000001', 'QTV', 'HoatDong', DATEADD(DAY,-500,GETDATE())),
('ql01',        @mk, 'ql01@ktx.edu.vn',             '0900000002', 'QL',  'HoatDong', DATEADD(DAY,-500,GETDATE())),
('ql02',        @mk, 'ql02@ktx.edu.vn',             '0900000003', 'QL',  'HoatDong', DATEADD(DAY,-500,GETDATE())),
('ql03',        @mk, 'ql03@ktx.edu.vn',             '0900000004', 'QL',  'BiKhoa',   DATEADD(DAY,-400,GETDATE())),
('24DH113343',  @mk, '24dh113343@st.huflit.edu.vn', '0911000005', 'SV',  'HoatDong', DATEADD(DAY,-320,GETDATE())),
('25DH113344',  @mk, '25dh113344@st.huflit.edu.vn', '0911000003', 'SV',  'HoatDong', DATEADD(DAY,-320,GETDATE())),
('25DH113345',  @mk, '25dh113345@st.huflit.edu.vn', '0911000004', 'SV',  'BiKhoa',   DATEADD(DAY,-320,GETDATE())),
('25DH113352',  @mk, '25dh113352@st.huflit.edu.vn', '0911000009', 'SV',  'HoatDong', DATEADD(DAY,-320,GETDATE())),
('25DH113354',  @mk, '25dh113354@st.huflit.edu.vn', '0911000011', 'SV',  'BiKhoa',   DATEADD(DAY,-320,GETDATE())),
('26DH113346',  @mk, '26dh113346@st.huflit.edu.vn', '0911000001', 'SV',  'HoatDong', DATEADD(DAY,-60, GETDATE())),
('26DH113347',  @mk, '26dh113347@st.huflit.edu.vn', '0911000002', 'SV',  'HoatDong', DATEADD(DAY,-160,GETDATE())),
('26DH113349',  @mk, '26dh113349@st.huflit.edu.vn', '0911000006', 'SV',  'HoatDong', DATEADD(DAY,-60, GETDATE())),
('26DH113350',  @mk, '26dh113350@st.huflit.edu.vn', '0911000007', 'SV',  'HoatDong', DATEADD(DAY,-60, GETDATE())),
('26DH113353',  @mk, '26dh113353@st.huflit.edu.vn', '0911000010', 'SV',  'HoatDong', DATEADD(DAY,-60, GETDATE())),
('27DH113351',  @mk, '27dh113351@st.huflit.edu.vn', '0911000008', 'SV',  'HoatDong', DATEADD(DAY,-55, GETDATE()));

-- Tòa nhà
INSERT INTO TOANHA (MaToa, TenToa, DiaChi, SoTang) VALUES
('A1', N'Tòa A1', N'Khu A, KTX Đại học', 5),
('B1', N'Tòa B1', N'Khu B, KTX Đại học', 4);

-- Quản lý (tra MaTK theo TenDangNhap thay vì ghi cứng số ID)
INSERT INTO QUANLY (MaNV, MaTK, HoTen, MaToa) VALUES
('NV001', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql01'), N'Trần Văn Quản', 'A1'),
('NV002', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql02'), N'Lê Thị Lý',     'B1'),
('NV003', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql03'), N'Hồ Văn Khóa',   'B1');   -- tài khoản đang Bị khóa

-- Sinh viên
INSERT INTO SINHVIEN (MSSV, MaTK, HoTen, NgaySinh, GioiTinh, KhoaHoc, DoiTuong) VALUES
('24DH113343', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='24DH113343'), N'Đặng Hoàng Minh Phi', '2006-04-07', 'Nam', 'K2024', 'BinhThuong'),
('25DH113344', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113344'), N'Võ Quốc Dũng',        '2007-01-10', 'Nam', 'K2025', 'BinhThuong'),
('25DH113345', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113345'), N'Trần Thu Cúc',        '2007-11-05', 'Nu',  'K2025', 'BinhThuong'),
('25DH113352', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113352'), N'Lý Minh Tâm',         '2007-06-21', 'Nam', 'K2025', 'BinhThuong'),
('25DH113354', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113354'), N'Phan Đức Lộc',        '2007-08-14', 'Nam', 'K2025', 'BinhThuong'),
('26DH113346', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113346'), N'Nguyễn Văn An',       '2008-03-15', 'Nam', 'K2026', 'BinhThuong'),
('26DH113347', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113347'), N'Phạm Thị Bình',       '2008-07-22', 'Nu',  'K2026', 'ChinhSach'),
('26DH113349', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113349'), N'Ngô Anh Thư',         '2008-02-18', 'Nu',  'K2026', 'ChinhSach'),
('26DH113350', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113350'), N'Bùi Văn Khoa',        '2008-09-30', 'Nam', 'K2026', 'BinhThuong'),
('26DH113353', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113353'), N'Trương Gia Huy',      '2008-12-02', 'Nam', 'K2026', 'BinhThuong'),
('27DH113351', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='27DH113351'), N'Đỗ Thị Hạnh',         '2009-05-12', 'Nu',  'K2027', 'BinhThuong');

-- Phòng (SoGiuongTrong được TÍNH LẠI phía dưới theo trạng thái giường thực tế)
INSERT INTO PHONG (MaPhong, MaToa, Tang, LoaiPhong, SoGiuong, GiaPhong, TrangThai, SoGiuongTrong) VALUES
('A1-101', 'A1', 1, '4', 4, 600000, 'HoatDong', 4),   -- để trống hoàn toàn: dùng cho kiểm thử đăng ký
('A1-102', 'A1', 1, '4', 4, 600000, 'HoatDong', 4),   -- phòng có hóa đơn quá hạn (QD05, QD06)
('A1-201', 'A1', 2, '6', 6, 450000, 'HoatDong', 6),
('A1-202', 'A1', 2, '6', 6, 450000, 'HoatDong', 6),
('A1-301', 'A1', 3, '8', 8, 350000, 'HoatDong', 8),
('B1-101', 'B1', 1, '4', 4, 650000, 'HoatDong', 4),
('B1-201', 'B1', 2, '8', 8, 350000, 'BaoTri',   8);   -- cả phòng đang bảo trì

-- Giường (tự sinh theo số giường của phòng)
INSERT INTO GIUONG (MaGiuong, MaPhong, TrangThai)
SELECT p.MaPhong + '-G' + CAST(n.So AS VARCHAR), p.MaPhong, 'Trong'
FROM PHONG p
JOIN (SELECT 1 So UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
      UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8) n
  ON n.So <= p.SoGiuong;

-- Đợt đăng ký
INSERT INTO DOTDANGKY (TenDot, LoaiDot, HocKy, NgayMo, NgayDong, TrangThai) VALUES
(N'Đăng ký đại trà HK2-2025',      'DaiTra', 'HK2-2025',  @DTc_Mo, @DTc_Dong, 'DaDong'),
(N'Học kỳ hè 2026',                'KyHe',   'HKHe-2026', @KH_Mo,  @KH_Dong,  'DaDong'),
(N'Ưu tiên Tân sinh viên HK1',     'UuTien', 'HK1-2026',  @UT_Mo,  @UT_Dong,  'DaDong'),
(N'Đăng ký đại trà HK1',           'DaiTra', 'HK1-2026',  @DT_Mo,  @DT_Dong,  'DangMo'),
(N'Đăng ký đại trà HK2 (dự kiến)', 'DaiTra', 'HK2-2026',  @DT2_Mo, @DT2_Dong, 'ChuaMo');

DECLARE @DotCu   INT = (SELECT MaDot FROM DOTDANGKY WHERE HocKy='HK2-2025');
DECLARE @DotHe   INT = (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='KyHe');
DECLARE @DotHK1  INT = (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND HocKy='HK1-2026');

-- Phiếu đăng ký / hợp đồng (NgayDangKy = NgayBatDau như cách ứng dụng ghi phiếu)
INSERT INTO PHIEUDANGKY (MSSV, MaGiuong, MaDot, NgayDangKy, NgayBatDau, NgayKetThuc, TrangThai) VALUES
-- Nhóm ở lâu (đã gia hạn 1 lần) - còn 27 ngày -> ĐƯỢC gia hạn
('25DH113344', 'A1-201-G1', @DotCu,  DATEADD(HOUR,9, CAST(@BD_Lau AS DATETIME)), @BD_Lau, @KT_Lau, 'DangO'),
('25DH113352', 'A1-201-G3', @DotCu,  DATEADD(HOUR,10,CAST(@BD_Lau AS DATETIME)), @BD_Lau, @KT_Lau, 'DangO'),
('24DH113343', 'B1-101-G1', @DotCu,  DATEADD(HOUR,14,CAST(@BD_Lau AS DATETIME)), @BD_Lau, @KT_Lau, 'DangO'),
('25DH113345', 'A1-102-G1', @DotCu,  DATEADD(HOUR,15,CAST(@BD_Lau AS DATETIME)), @BD_Lau, @KT_Lau, 'DangO'),
-- Đã ở A1-102 cùng 25DH113345 (gia hạn 3 tháng), hợp đồng kết thúc khoảng 1 tháng trước, còn nợ hóa đơn quá hạn
('25DH113354', 'A1-102-G2', @DotCu,  DATEADD(HOUR,16,CAST(@BD_Lau AS DATETIME)), @BD_Lau, DATEADD(MONTH,8,@BD_Lau), 'DaTraPhong'),
-- Nhóm mới dọn vào - còn khoảng 130 ngày -> BỊ CHẶN gia hạn (có 1 SV diện chính sách để đối chứng)
('26DH113349', 'A1-202-G1', @DotHK1, DATEADD(HOUR,9, CAST(@BD_Moi AS DATETIME)), @BD_Moi, @KT_Moi, 'DangO'),
('26DH113350', 'A1-202-G2', @DotHK1, DATEADD(HOUR,10,CAST(@BD_Moi AS DATETIME)), @BD_Moi, @KT_Moi, 'DangO'),
('26DH113353', 'A1-202-G5', @DotHK1, DATEADD(HOUR,11,CAST(@BD_Moi AS DATETIME)), @BD_Moi, @KT_Moi, 'DangO'),
-- Phiếu chờ đối chiếu (đăng ký 2 ngày trước, chưa đến văn phòng nhận phòng)
('26DH113346', 'A1-202-G3', @DotHK1, DATEADD(HOUR,16,CAST(@BD_Cho AS DATETIME)), @BD_Cho, @KT_Cho, 'ChoDoiChieu'),
-- Hợp đồng Học kỳ hè đã kết thúc, đã trả phòng (diện chính sách, phòng 8 giường đúng TS9)
('26DH113347', 'A1-301-G1', @DotHe,  DATEADD(HOUR,9, CAST(@BD_He  AS DATETIME)), @BD_He,  @KT_He,  'DaTraPhong');

-- Trạng thái giường + số giường trống THEO DỮ LIỆU THỰC TẾ
UPDATE GIUONG SET TrangThai='DaSuDung'
WHERE MaGiuong IN (SELECT MaGiuong FROM PHIEUDANGKY WHERE TrangThai IN ('DangO','ChoDoiChieu'));

UPDATE GIUONG SET TrangThai='BaoTri'
WHERE MaPhong = 'B1-201'        -- cả phòng đang bảo trì
   OR MaGiuong = 'A1-202-G6';   -- 1 giường hỏng trong phòng đang hoạt động

UPDATE p SET SoGiuongTrong = (SELECT COUNT(*) FROM GIUONG g WHERE g.MaPhong = p.MaPhong AND g.TrangThai = 'Trong')
FROM PHONG p;

-- Đơn giá điện nước (biểu giá cũ hết hiệu lực khi có biểu giá mới)
INSERT INTO DONGIA (GiaDien, GiaNuoc, PhiDichVu, NgayApDung, TrangThai) VALUES
(2500, 15000, 20000, '2025-09-01', 'HetHieuLuc'),
(2800, 18000, 20000, '2026-01-01', 'HieuLuc');

/* ---------------------------------------------------------------------
   Lịch cư trú theo tháng: 8 tháng đã qua (n = 1..8) + tháng hiện tại (n = 0).
   Dùng để sinh chỉ số điện nước, hóa đơn, thông báo, thanh toán nhất quán.
   --------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#Thang') IS NOT NULL DROP TABLE #Thang;
IF OBJECT_ID('tempdb..#CuTru') IS NOT NULL DROP TABLE #CuTru;
IF OBJECT_ID('tempdb..#Tieuthu') IS NOT NULL DROP TABLE #Tieuthu;

CREATE TABLE #Thang (n INT PRIMARY KEY, Thang CHAR(7), DauThang DATE, CuoiThang DATE, NguongTS12 DATE);
INSERT INTO #Thang (n, Thang, DauThang, CuoiThang, NguongTS12)
SELECT v.n, FORMAT(x.d, 'MM/yyyy'), x.d, EOMONTH(x.d), DATEFROMPARTS(YEAR(x.d), MONTH(x.d), @TS12)
FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8)) v(n)
CROSS APPLY (SELECT DATEFROMPARTS(YEAR(DATEADD(MONTH,-v.n,@Today)), MONTH(DATEADD(MONTH,-v.n,@Today)), 1) AS d) x;

-- Ai ở phòng nào trong từng tháng; TinhTien = 1 nếu dọn vào trước/đúng ngày TS12 (QD18)
CREATE TABLE #CuTru (MaPhong VARCHAR(10), n INT, MSSV VARCHAR(10), MaPhieu INT, TinhTien INT);
INSERT INTO #CuTru (MaPhong, n, MSSV, MaPhieu, TinhTien)
SELECT g.MaPhong, t.n, pd.MSSV, pd.MaPhieu,
       CASE WHEN pd.NgayBatDau <= t.NguongTS12 THEN 1 ELSE 0 END
FROM PHIEUDANGKY pd
JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
CROSS JOIN #Thang t
WHERE pd.TrangThai IN ('DangO','DaTraPhong')
  AND pd.NgayBatDau  <= t.CuoiThang
  AND pd.NgayKetThuc >= t.DauThang
  AND pd.NgayBatDau  <= @Today
  AND (t.n > 0 OR pd.TrangThai = 'DangO');

-- Lượng tiêu thụ mỗi tháng (phụ thuộc số người ở) + chỉ số gốc của đồng hồ từng phòng
CREATE TABLE #Tieuthu (MaPhong VARCHAR(10), n INT, Kwh DECIMAL(10,1), M3 DECIMAL(10,1), DienGoc DECIMAL(10,1), NuocGoc DECIMAL(10,1));
INSERT INTO #Tieuthu (MaPhong, n, Kwh, M3, DienGoc, NuocGoc)
SELECT c.MaPhong, c.n,
       45 * COUNT(*) + 20 + ((c.n * 7 + ASCII(RIGHT(c.MaPhong,1))) % 25),
       5  * COUNT(*) + 2  + ((c.n * 3 + ASCII(RIGHT(c.MaPhong,1))) % 4),
       CASE c.MaPhong WHEN 'A1-102' THEN 1200 WHEN 'A1-201' THEN 1000 WHEN 'A1-202' THEN 500
                      WHEN 'A1-301' THEN 300  WHEN 'B1-101' THEN 800  ELSE 100 END,
       CASE c.MaPhong WHEN 'A1-102' THEN 300  WHEN 'A1-201' THEN 250  WHEN 'A1-202' THEN 120
                      WHEN 'A1-301' THEN 80   WHEN 'B1-101' THEN 200  ELSE 50 END
FROM #CuTru c
GROUP BY c.MaPhong, c.n;

-- Chỉ số điện nước CỘNG DỒN: đầu kỳ = cuối kỳ tháng có chỉ số liền trước.
-- Tháng hiện tại: B1-101 đã lập hóa đơn nháp (DaChot); các phòng còn lại chưa lập (Nhap) -> demo "Lập hóa đơn".
INSERT INTO CHISODIENNUOC (MaPhong, Thang, DienDauKy, DienCuoiKy, NuocDauKy, NuocCuoiKy, TrangThai)
SELECT tt.MaPhong, t.Thang,
       tt.DienGoc + ISNULL(SUM(tt.Kwh) OVER (PARTITION BY tt.MaPhong ORDER BY tt.n DESC ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0),
       tt.DienGoc + SUM(tt.Kwh) OVER (PARTITION BY tt.MaPhong ORDER BY tt.n DESC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
       tt.NuocGoc + ISNULL(SUM(tt.M3) OVER (PARTITION BY tt.MaPhong ORDER BY tt.n DESC ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0),
       tt.NuocGoc + SUM(tt.M3) OVER (PARTITION BY tt.MaPhong ORDER BY tt.n DESC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
       CASE WHEN tt.n > 0 OR tt.MaPhong = 'B1-101' THEN 'DaChot' ELSE 'Nhap' END
FROM #Tieuthu tt
JOIN #Thang t ON t.n = tt.n;

/* Hóa đơn: phát hành khoảng ngày 2 tháng sau, hạn thanh toán 15 ngày sau khi phát hành.
   - Tháng gần nhất (n = 1) của A1-201, A1-102, B1-101: Chờ thanh toán, còn 2 ngày đến hạn
     (trong TS5 = 3 ngày -> sinh viên mở Tổng quan sẽ nhận nhắc nhở QD07).
   - Tháng n = 2 của A1-102: Quá hạn khoảng 28 ngày (> TS3, > TS4) -> khóa TK + vi phạm.
   - Tháng hiện tại của B1-101: hóa đơn Nháp (demo Gửi hóa đơn / Hủy hóa đơn nháp).
   - Còn lại: Đã thanh toán. */
INSERT INTO HOADON (MaPhong, MaChiSo, MaDonGia, Thang, TienPhong, TienDien, TienNuoc, TongTien, NgayPhatHanh, HanThanhToan, TrangThai)
SELECT cs.MaPhong, cs.MaChiSo, dg.MaDonGia, cs.Thang,
       p.GiaPhong * ISNULL(sn.SoTinhTien, 0) + dg.PhiDichVu,
       (cs.DienCuoiKy - cs.DienDauKy) * dg.GiaDien,
       (cs.NuocCuoiKy - cs.NuocDauKy) * dg.GiaNuoc,
       p.GiaPhong * ISNULL(sn.SoTinhTien, 0) + dg.PhiDichVu
         + (cs.DienCuoiKy - cs.DienDauKy) * dg.GiaDien
         + (cs.NuocCuoiKy - cs.NuocDauKy) * dg.GiaNuoc,
       ph.NgayPhatHanh,
       CASE WHEN t.n = 0 THEN DATEADD(DAY, 15, @Today) ELSE CAST(DATEADD(DAY, 15, ph.NgayPhatHanh) AS DATE) END,
       CASE WHEN t.n = 0 THEN 'Nhap'
            WHEN t.n = 1 AND cs.MaPhong IN ('A1-201','A1-102','B1-101') THEN 'ChoThanhToan'
            WHEN t.n = 2 AND cs.MaPhong = 'A1-102' THEN 'QuaHan'
            ELSE 'DaThanhToan' END
FROM CHISODIENNUOC cs
JOIN #Thang t ON t.Thang = cs.Thang
JOIN PHONG p ON p.MaPhong = cs.MaPhong
OUTER APPLY (SELECT SUM(c.TinhTien) AS SoTinhTien FROM #CuTru c WHERE c.MaPhong = cs.MaPhong AND c.n = t.n) sn
CROSS APPLY (SELECT TOP 1 d.MaDonGia, d.GiaDien, d.GiaNuoc, d.PhiDichVu FROM DONGIA d
             ORDER BY CASE WHEN d.NgayApDung <= t.DauThang THEN 0 ELSE 1 END, d.NgayApDung DESC) dg
CROSS APPLY (SELECT CASE WHEN t.n = 0 THEN NULL
                         ELSE DATEADD(MONTH, -(t.n - 1), DATEADD(DAY, -13, GETDATE())) END AS NgayPhatHanh) ph
WHERE cs.TrangThai = 'DaChot';

-- Thanh toán: 1 người ở phòng trong tháng đó thanh toán hộ cả phòng qua chuyển khoản (NganHang),
-- trong hạn. Riêng hóa đơn tháng n = 5 của A1-102 bị trả trễ 16 ngày, Quản lý thu tiền mặt tại văn phòng.
INSERT INTO THANHTOAN (MaHD, MSSV, PhuongThuc, SoTien, MaGDCong, ThoiGian, KetQua)
SELECT h.MaHD, nguoi.MSSV, 'NganHang', h.TongTien,
       CASE WHEN tg.Tre = 1 THEN 'TIENMAT' + FORMAT(tg.ThoiGian, 'yyyyMMddHHmmss') + CAST(h.MaHD AS VARCHAR(10))
            ELSE 'NGANHANG' + FORMAT(tg.ThoiGian, 'yyyyMMddHHmmss') END,
       tg.ThoiGian, 'ThanhCong'
FROM HOADON h
JOIN #Thang t ON t.Thang = h.Thang
CROSS APPLY (SELECT CASE WHEN h.MaPhong = 'A1-102' AND t.n = 5 THEN 1 ELSE 0 END AS Tre) x
CROSS APPLY (SELECT x.Tre AS Tre,
                    CASE WHEN x.Tre = 1 THEN DATEADD(HOUR, 10, CAST(DATEADD(DAY, @TS4 + 2, h.HanThanhToan) AS DATETIME))
                         WHEN t.n = 1   THEN DATEADD(DAY, 2, h.NgayPhatHanh)
                         ELSE DATEADD(DAY, 2 + h.MaHD % 9, h.NgayPhatHanh) END AS ThoiGian) tg
CROSS APPLY (SELECT TOP 1 c.MSSV FROM #CuTru c
             WHERE c.MaPhong = h.MaPhong AND c.n = t.n
             ORDER BY ABS(CHECKSUM(c.MSSV, h.MaHD))) nguoi
WHERE h.TrangThai = 'DaThanhToan';

-- Vi phạm (QD06, chế tài lũy tiến: lần thứ N = TS6 x N điểm), ghi nhận vào ngày quá hạn thứ TS4 + 1:
--   lần 1: hóa đơn n = 5 của A1-102 (trả trễ 16 ngày); lần 2: hóa đơn n = 2 của A1-102 (đang quá hạn).
;WITH DsViPham AS (
    SELECT c.MSSV, h.MaHD, h.Thang, h.HanThanhToan,
           ROW_NUMBER() OVER (PARTITION BY c.MSSV ORDER BY h.HanThanhToan) AS LanThu
    FROM HOADON h
    JOIN #Thang t ON t.Thang = h.Thang
    JOIN #CuTru c ON c.MaPhong = h.MaPhong AND c.n = t.n
    WHERE (h.TrangThai = 'QuaHan' AND DATEDIFF(DAY, h.HanThanhToan, @Today) > @TS4)
       OR (h.MaPhong = 'A1-102' AND t.n = 5)
)
INSERT INTO VIPHAM (MSSV, MaHD, NgayGhiNhan, SoDiem, LyDo)
SELECT MSSV, MaHD, DATEADD(DAY, @TS4 + 1, HanThanhToan), @TS6 * LanThu,
       N'Hóa đơn ' + Thang + N' quá hạn thanh toán trên ' + CAST(@TS4 AS NVARCHAR(5)) + N' ngày (vi phạm lần ' +
       CAST(LanThu AS NVARCHAR(5)) + N' - chế tài lũy tiến: ' + CAST(@TS6 * LanThu AS NVARCHAR(5)) + N' điểm)'
FROM DsViPham;

-- DiemViPham là thuộc tính dẫn xuất = tổng điểm vi phạm (giống sp_CapNhatDiemViPham)
UPDATE sv SET DiemViPham = ISNULL((SELECT SUM(v.SoDiem) FROM VIPHAM v WHERE v.MSSV = sv.MSSV), 0)
FROM SINHVIEN sv;

-- Đơn phản hồi / đề xuất / chuyển phòng / trả phòng
DECLARE @PhieuHe     INT = (SELECT MaPhieu FROM PHIEUDANGKY WHERE MSSV='26DH113347' AND TrangThai='DaTraPhong');
DECLARE @Phieu113350 INT = (SELECT MaPhieu FROM PHIEUDANGKY WHERE MSSV='26DH113350' AND TrangThai='DangO');
DECLARE @Phieu113353 INT = (SELECT MaPhieu FROM PHIEUDANGKY WHERE MSSV='26DH113353' AND TrangThai='DangO');

INSERT INTO DONYEUCAU (MSSV, MaNV, MaPhieu, MaGiuongMoi, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, PhanHoi, NgayTao) VALUES
-- Đơn trả phòng đã xử lý của hợp đồng học kỳ hè (QL xác nhận đúng ngày hết hạn hợp đồng)
('26DH113347', 'NV001', @PhieuHe, NULL, 'TraPhong', N'Yêu cầu trả phòng A1-301',
    N'Sinh viên yêu cầu trả phòng A1-301 (hợp đồng #' + CAST(@PhieuHe AS NVARCHAR(10)) + N'). Vui lòng đối chiếu và xác nhận.',
    'Cao', 'DaXuLy', N'Đã xác nhận trả phòng. Cảm ơn bạn đã bàn giao phòng đúng quy định.', DATEADD(DAY,-3, CAST(@KT_He AS DATETIME))),
-- Phản hồi đã xử lý xong
('25DH113352', 'NV001', NULL, NULL, 'PhanHoi', N'Wifi tầng 2 tòa A1 chập chờn',
    N'Wifi khu vực tầng 2 tòa A1 thường xuyên mất kết nối vào buổi tối.', 'TrungBinh', 'DaXuLy',
    N'Đã thay bộ phát wifi tầng 2, bạn vui lòng kiểm tra lại.', DATEADD(DAY,-30,GETDATE())),
-- Phản hồi đang xử lý (QL đã nâng mức ưu tiên)
('25DH113344', 'NV001', NULL, NULL, 'PhanHoi', N'Hỏng bóng đèn phòng A1-201',
    N'Bóng đèn trần giữa phòng bị chập chờn, xin sửa giúp.', 'Cao', 'DangXuLy',
    N'Đã tiếp nhận, kỹ thuật sẽ đến trong tuần.', DATEADD(DAY,-14,GETDATE())),
-- Đề xuất bị từ chối
('26DH113349', 'NV001', NULL, NULL, 'DeXuat', N'Kéo dài giờ mở phòng tự học',
    N'Đề xuất mở phòng tự học tới 24h trong mùa thi.', 'Thap', 'TuChoi',
    N'Phòng tự học mở tới 23h theo nội quy chung, chưa thể kéo dài thêm.', DATEADD(DAY,-12,GETDATE())),
-- Chờ xử lý
('25DH113345', NULL, NULL, NULL, 'DeXuat', N'Lắp thêm quạt trần',
    N'Phòng A1-102 nóng vào buổi trưa, đề xuất lắp thêm 1 quạt trần.', 'TrungBinh', 'ChoXuLy', NULL, DATEADD(DAY,-10,GETDATE())),
('24DH113343', NULL, NULL, NULL, 'PhanHoi', N'Vòi nước rò rỉ',
    N'Vòi nước khu vệ sinh phòng B1-101 bị rò rỉ.', 'TrungBinh', 'ChoXuLy', NULL, DATEADD(DAY,-7,GETDATE())),
-- Đơn chuyển phòng chờ xử lý (đủ điều kiện QD16: ở >= TS11 ngày, phòng không nợ, giường đích còn trống)
('26DH113350', NULL, @Phieu113350, 'A1-301-G8', 'ChuyenPhong', N'Yêu cầu chuyển phòng A1-202 → A1-301',
    N'Sinh viên yêu cầu chuyển từ phòng A1-202 sang phòng A1-301 (giường A1-301-G8). Vui lòng đối chiếu và xác nhận.',
    'Cao', 'ChoXuLy', NULL, DATEADD(DAY,-3,GETDATE())),
-- Đơn trả phòng chờ xử lý (phòng A1-202 không còn hóa đơn chưa thanh toán)
('26DH113353', NULL, @Phieu113353, NULL, 'TraPhong', N'Yêu cầu trả phòng A1-202',
    N'Sinh viên yêu cầu trả phòng A1-202 (hợp đồng #' + CAST(@Phieu113353 AS NVARCHAR(10)) + N'). Vui lòng đối chiếu và xác nhận.',
    'Cao', 'ChoXuLy', NULL, DATEADD(DAY,-1,GETDATE()));

/* Lịch sử thông báo (chỉ kênh Email - QD12), nội dung giống hệt thông báo ứng dụng tự sinh */
-- 1. Hoàn tất nhận phòng (sp_XacNhanNhanPhong) cho các hợp đồng đã nhận phòng
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT MSSV, NULL,
       N'Phiếu đăng ký #' + CAST(MaPhieu AS NVARCHAR(10)) + N': đã hoàn tất thủ tục nhận phòng. Chào mừng bạn đến với KTX!',
       'Email', DATEADD(HOUR, 6, NgayDangKy), 'ThanhCong'
FROM PHIEUDANGKY WHERE TrangThai IN ('DangO','DaTraPhong');

-- 2. Gia hạn hợp đồng (sp_GiaHan) của nhóm ở lâu, thực hiện khi đợt Học kỳ hè đang mở
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT MSSV, NULL, N'Hợp đồng #' + CAST(MaPhieu AS NVARCHAR(10)) + N' đã được gia hạn thêm 5 tháng.',
       'Email', DATEADD(MINUTE, MaPhieu * 7, @NgayGiaHanLau), 'ThanhCong'
FROM PHIEUDANGKY WHERE TrangThai = 'DangO' AND NgayBatDau = @BD_Lau;

INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT MSSV, NULL, N'Hợp đồng #' + CAST(MaPhieu AS NVARCHAR(10)) + N' đã được gia hạn thêm 3 tháng.',
       'Email', DATEADD(MINUTE, MaPhieu * 7, @NgayGiaHanLau), 'ThanhCong'
FROM PHIEUDANGKY WHERE MSSV = '25DH113354';

-- 3. Gửi hóa đơn hàng tháng tới từng sinh viên ở phòng trong tháng đó
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT c.MSSV, h.MaHD,
       N'Hóa đơn tháng ' + h.Thang + N' phòng ' + h.MaPhong + N': ' + FORMAT(h.TongTien, 'N0', 'vi-VN') +
       N'đ. Hạn thanh toán ' + FORMAT(h.HanThanhToan, 'dd/MM/yyyy') + N'.',
       'Email', h.NgayPhatHanh, 'ThanhCong'
FROM HOADON h
JOIN #Thang t ON t.Thang = h.Thang
JOIN #CuTru c ON c.MaPhong = h.MaPhong AND c.n = t.n
WHERE h.NgayPhatHanh IS NOT NULL;

-- 4. Nhắc nhở lần hai (QD07) cho hóa đơn chưa trả khi còn TS5 = 3 ngày đến hạn.
--    Hóa đơn đang Chờ thanh toán (n = 1) KHÔNG chèn sẵn: job sẽ tự gửi khi sinh viên mở Tổng quan.
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT c.MSSV, h.MaHD,
       N'Nhắc nhở thanh toán: Hóa đơn tháng ' + h.Thang + N' phòng ' + h.MaPhong + N' (' + FORMAT(h.TongTien, 'N0', 'vi-VN') +
       N'đ) còn 3 ngày nữa là đến hạn (' + FORMAT(h.HanThanhToan, 'dd/MM/yyyy') +
       N'). Vui lòng thanh toán sớm để tránh bị khóa quyền dịch vụ hoặc ghi vi phạm.',
       'Email', DATEADD(HOUR, 8, CAST(DATEADD(DAY, -3, h.HanThanhToan) AS DATETIME)), 'ThanhCong'
FROM HOADON h
JOIN #Thang t ON t.Thang = h.Thang
JOIN #CuTru c ON c.MaPhong = h.MaPhong AND c.n = t.n
WHERE h.TrangThai = 'QuaHan'
   OR EXISTS (SELECT 1 FROM THANHTOAN tt WHERE tt.MaHD = h.MaHD
              AND tt.ThoiGian > DATEADD(DAY, -3, CAST(h.HanThanhToan AS DATETIME)));

-- 5. Thanh toán thành công (sp_ThanhToan) - gửi cho người thanh toán
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT tt.MSSV, tt.MaHD,
       CASE WHEN tt.MaGDCong LIKE 'TIENMAT%'
            THEN N'Quản lý đã xác nhận thu tiền mặt hóa đơn #' + CAST(tt.MaHD AS NVARCHAR(10)) + N' tại văn phòng.'
            ELSE N'Thanh toán hóa đơn #' + CAST(tt.MaHD AS NVARCHAR(10)) + N' thành công qua NganHang. Mã giao dịch: ' + tt.MaGDCong + N'.' END,
       'Email', tt.ThoiGian, 'ThanhCong'
FROM THANHTOAN tt;

-- 6. Mở khóa tài khoản (sp_MoKhoa) cho các sinh viên của phòng sau khi Quản lý thu tiền hóa đơn trả trễ
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT c.MSSV, NULL, N'Tài khoản của bạn đã được mở khóa.', 'Email', DATEADD(MINUTE, 1, tt.ThoiGian), 'ThanhCong'
FROM THANHTOAN tt
JOIN HOADON h ON h.MaHD = tt.MaHD
JOIN #Thang t ON t.Thang = h.Thang
JOIN #CuTru c ON c.MaPhong = h.MaPhong AND c.n = t.n
WHERE tt.MaGDCong LIKE 'TIENMAT%';

-- 7. Xử lý đơn (sp_XuLyDon) và xác nhận trả phòng (sp_XacNhanTraPhong)
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT d.MSSV, NULL,
       CASE WHEN d.LoaiDon = 'TraPhong'
            THEN N'Yêu cầu trả phòng #' + CAST(d.MaDon AS NVARCHAR(10)) + N' đã được xác nhận. Phòng đã được giải phóng.'
            ELSE N'Đơn #' + CAST(d.MaDon AS NVARCHAR(10)) + N' của bạn đã được cập nhật trạng thái: ' + d.TrangThai +
                 CASE WHEN d.PhanHoi IS NULL THEN N'' ELSE N' Phản hồi: ' + d.PhanHoi END END,
       'Email',
       CASE WHEN d.LoaiDon = 'TraPhong' THEN CAST(@KT_He AS DATETIME) ELSE DATEADD(DAY, 1, d.NgayTao) END,
       'ThanhCong'
FROM DONYEUCAU d
WHERE d.TrangThai <> 'ChoXuLy';

/* Nhật ký thao tác hệ thống - các thao tác của Quản trị viên khớp với dữ liệu ở trên,
   trải dài nhiều tháng (đủ để kiểm thử phân trang và bộ lọc mặc định "30 ngày gần nhất"). */
DECLARE @MaTKAdmin INT = (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin');
INSERT INTO NHATKY (MaTK, HoTenNguoiThucHien, VaiTro, HanhDong, DoiTuong, NoiDung, ThoiGian)
SELECT @MaTKAdmin, N'Quản trị viên', 'QTV', v.HanhDong, v.DoiTuong, v.NoiDung,
       DATEADD(MINUTE, v.Phut, CAST(DATEADD(DAY, -v.SoNgay, @Today) AS DATETIME))
FROM (VALUES
    (500, 480, 'Them',   N'Tài khoản',         N'Tạo tài khoản quản lý ql01, ql02 và quản trị hệ thống.'),
    (400, 500, 'Them',   N'Tài khoản',         N'Tạo tài khoản quản lý ql03 phụ trách tòa B1.'),
    (330, 540, 'Them',   N'Phòng',             N'Thêm phòng A1-102 (4 giường) vào tòa A1.'),
    (320, 600, 'Them',   N'Tài khoản',         N'Tạo tài khoản sinh viên 24DH113343, 25DH113344, 25DH113345, 25DH113352, 25DH113354.'),
    (301, 480, 'Them',   N'Đợt đăng ký',       N'Tạo đợt "Đăng ký đại trà HK2-2025".'),
    (270, 1020,'Sua',    N'Đợt đăng ký',       N'Đóng đợt "Đăng ký đại trà HK2-2025".'),
    (250, 600, 'Xoa',    N'Tài khoản',         N'Xóa tài khoản thử nghiệm sv_test01 tạo nhầm.'),
    (160, 540, 'Them',   N'Tài khoản',         N'Tạo tài khoản sinh viên 26DH113347.'),
    (151, 480, 'Them',   N'Đợt đăng ký',       N'Tạo đợt "Học kỳ hè 2026" (thời hạn lưu trú TS10 = 2 tháng).'),
    (149, 600, 'Xoa',    N'Đợt đăng ký',       N'Xóa đợt đăng ký tạo trùng "Học kỳ hè 2026 (bản nháp)".'),
    (130, 1020,'Sua',    N'Đợt đăng ký',       N'Đóng đợt "Học kỳ hè 2026".'),
    (95,  540, 'Khoa',   N'Tài khoản',         N'Khóa tài khoản quản lý ql02 do đăng nhập bất thường - chờ xác minh.'),
    (93,  600, 'MoKhoa', N'Tài khoản',         N'Mở khóa tài khoản quản lý ql02 sau khi xác minh.'),
    (75,  480, 'Them',   N'Đợt đăng ký',       N'Tạo đợt "Ưu tiên Tân sinh viên HK1" (TS1 = 14 ngày).'),
    (61,  480, 'Them',   N'Đợt đăng ký',       N'Tạo đợt "Đăng ký đại trà HK1".'),
    (60,  540, 'Them',   N'Tài khoản',         N'Tạo tài khoản sinh viên 26DH113346, 26DH113349, 26DH113350, 26DH113353.'),
    (55,  540, 'Them',   N'Tài khoản',         N'Tạo tài khoản tân sinh viên 27DH113351.'),
    (46,  1020,'Sua',    N'Đợt đăng ký',       N'Đóng đợt "Ưu tiên Tân sinh viên HK1".'),
    (40,  600, 'Them',   N'Đợt đăng ký',       N'Tạo đợt "Đăng ký đại trà HK2 (dự kiến)".'),
    (35,  660, 'Sua',    N'Tài khoản',         N'Cập nhật số điện thoại tài khoản ql02.'),
    (28,  540, 'Sua',    N'Phòng',             N'Chuyển phòng B1-201 sang trạng thái Bảo trì (sửa hệ thống điện).'),
    (21,  600, 'Sua',    N'Tài khoản',         N'Cập nhật họ tên hiển thị của quản lý NV002.'),
    (15,  540, 'Sua',    N'Đợt đăng ký',       N'Cập nhật hạn đóng cổng đợt "Đăng ký đại trà HK1".'),
    (9,   600, 'Sua',    N'Tài khoản',         N'Cập nhật email liên hệ tài khoản ql01.'),
    (6,   660, 'Sua',    N'Phòng',             N'Chuyển giường A1-202-G6 sang trạng thái Bảo trì (hỏng khung giường).'),
    (4,   540, 'Khoa',   N'Tài khoản',         N'Khóa tài khoản quản lý ql03 do vi phạm quy trình xử lý đơn từ - chờ xác minh.'),
    (2,   600, 'Sua',    N'Tài khoản',         N'Đặt lại mật khẩu mặc định cho tài khoản 26DH113346 theo yêu cầu.'),
    (1,   540, 'Sua',    N'Đợt đăng ký',       N'Kiểm tra cấu hình cổng đăng ký đợt "Đăng ký đại trà HK2 (dự kiến)".')
) v(SoNgay, Phut, HanhDong, DoiTuong, NoiDung);

INSERT INTO NHATKY (MaTK, HoTenNguoiThucHien, VaiTro, HanhDong, DoiTuong, NoiDung, ThoiGian) VALUES
(@MaTKAdmin, N'Quản trị viên', 'QTV', 'Them', N'Đơn giá điện/nước', N'Thêm biểu giá điện/nước 2.500đ/kWh, 15.000đ/m³, phí dịch vụ 20.000đ, áp dụng từ 01/09/2025.', '20250825 09:00'),
(@MaTKAdmin, N'Quản trị viên', 'QTV', 'Sua',  N'Đơn giá điện/nước', N'Cập nhật biểu giá điện/nước 2.800đ/kWh, 18.000đ/m³, áp dụng từ 01/01/2026.', '20251226 09:00');

DROP TABLE #Tieuthu;
DROP TABLE #CuTru;
DROP TABLE #Thang;
GO

PRINT N'>>> Tạo CSDL DormManager thành công! Mật khẩu mọi tài khoản: 123456';
PRINT N'>>> QTV: admin | QL: ql01 (tòa A1), ql02 (tòa B1) | ql03 (Bị khóa thủ công - demo Mở khóa)';
PRINT N'>>> SV ở lâu, còn 27 ngày (được Gia hạn): 25DH113344, 25DH113352 (A1-201), 24DH113343 (B1-101)';
PRINT N'>>> SV bị khóa theo QD05 + 3 điểm vi phạm: 25DH113345 (đang ở A1-102), 25DH113354 (đã hết hợp đồng, còn nợ hóa đơn A1-102)';
PRINT N'>>> SV mới ở (Gia hạn bị chặn): 26DH113349 (chính sách), 26DH113350 (có đơn chuyển phòng), 26DH113353 (có đơn trả phòng) - A1-202';
PRINT N'>>> SV khác: 26DH113346 (chờ đối chiếu) | 26DH113347 (chính sách, đã trả phòng) | 27DH113351 (tân sinh viên, chưa có hợp đồng)';
GO