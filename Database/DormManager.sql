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
   DỮ LIỆU MẪU
   Mật khẩu mặc định của MỌI tài khoản là: 123456
   (SHA-256 = 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92)

   Toàn bộ ngày tháng dưới đây tính TƯƠNG ĐỐI theo GETDATE() (không ghi cứng
   ngày cụ thể) để dữ liệu luôn hợp lệ và đủ điều kiện demo MỌI nghiệp vụ hiện
   có (đăng ký/chuyển phòng/gia hạn/hóa đơn/khóa-mở khóa/nhật ký...) bất kể
   script này được chạy vào thời điểm nào. Các bảng có khối lượng dữ liệu lớn
   (Nhật ký, Hóa đơn, Lịch sử thông báo) được sinh theo tập hợp để đủ nhiều
   dòng kiểm thử phân trang và bộ lọc mặc định theo thời gian.
   ===================================================================== */
DECLARE @mk    VARCHAR(255) = '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92';
DECLARE @Today DATE = CAST(GETDATE() AS DATE);

-- Đợt đăng ký: 1 đã đóng (ưu tiên), 1 đang mở (đại trà), 1 đã đóng lâu (hè cũ), 1 chưa mở (dự kiến)
DECLARE @UT_Mo  DATE = DATEADD(DAY,-60, @Today), @UT_Dong  DATE = DATEADD(DAY,-46, @Today);  -- 14 ngày = TS1
DECLARE @DT_Mo  DATE = DATEADD(DAY,-45, @Today), @DT_Dong  DATE = DATEADD(DAY, 30, @Today);
DECLARE @KH_Mo  DATE = DATEADD(DAY,-200,@Today), @KH_Dong  DATE = DATEADD(DAY,-185,@Today);
DECLARE @DT2_Mo DATE = DATEADD(DAY, 60, @Today), @DT2_Dong DATE = DATEADD(DAY, 90, @Today);

-- Hợp đồng: nhóm "đã ở lâu" sắp hết hạn (còn ~27 ngày, trong TS13=30 -> ĐƯỢC gia hạn)
-- và nhóm "mới dọn vào" còn xa hạn (~130 ngày -> BỊ CHẶN gia hạn) để demo rõ 2 trạng thái.
DECLARE @BD_Lau DATE = DATEADD(DAY,-125,@Today);
DECLARE @KT_Lau DATE = DATEADD(MONTH,5,@BD_Lau);
DECLARE @BD_Moi DATE = DATEADD(DAY,-20, @Today);
DECLARE @KT_Moi DATE = DATEADD(MONTH,5,@BD_Moi);
DECLARE @BD_Cho DATE = DATEADD(DAY, 5,  @Today);
DECLARE @KT_Cho DATE = DATEADD(MONTH,5,@BD_Cho);
DECLARE @BD_Cu  DATE = DATEADD(DAY,-90, @Today);
DECLARE @KT_Cu  DATE = DATEADD(DAY,-20, @Today);

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

-- Tài khoản (thêm ql03 và 27DH113351 đã ở trạng thái BiKhoa sẵn để demo chức năng Mở khóa của admin)
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, SDT, VaiTro, TrangThai) VALUES
('admin',       @mk, 'admin@ktx.edu.vn',            '0900000001', 'QTV', 'HoatDong'),
('ql01',        @mk, 'ql01@ktx.edu.vn',             '0900000002', 'QL',  'HoatDong'),
('ql02',        @mk, 'ql02@ktx.edu.vn',             '0900000003', 'QL',  'HoatDong'),
('ql03',        @mk, 'ql03@ktx.edu.vn',             '0900000004', 'QL',  'BiKhoa'),
('24DH113343',  @mk, '24dh113343@st.huflit.edu.vn', '0911000005', 'SV',  'HoatDong'),
('25DH113344',  @mk, '25dh113344@st.huflit.edu.vn', '0911000003', 'SV',  'HoatDong'),
('25DH113345',  @mk, '25dh113345@st.huflit.edu.vn', '0911000004', 'SV',  'HoatDong'),
('26DH113346',  @mk, '26dh113346@st.huflit.edu.vn', '0911000001', 'SV',  'HoatDong'),
('26DH113347',  @mk, '26dh113347@st.huflit.edu.vn', '0911000002', 'SV',  'HoatDong'),
('26DH113349',  @mk, '26dh113349@st.huflit.edu.vn', '0911000006', 'SV',  'HoatDong'),
('26DH113350',  @mk, '26dh113350@st.huflit.edu.vn', '0911000007', 'SV',  'HoatDong'),
('27DH113351',  @mk, '27dh113351@st.huflit.edu.vn', '0911000008', 'SV',  'BiKhoa');

-- Tòa nhà
INSERT INTO TOANHA (MaToa, TenToa, DiaChi, SoTang) VALUES
('A1', N'Tòa A1', N'Khu A, KTX Đại học', 5),
('B1', N'Tòa B1', N'Khu B, KTX Đại học', 4);

-- Quản lý (tra MaTK theo TenDangNhap thay vì ghi cứng số ID - tránh sai lệch khi đổi thứ tự INSERT)
INSERT INTO QUANLY (MaNV, MaTK, HoTen, MaToa) VALUES
('NV001', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql01'), N'Trần Văn Quản', 'A1'),
('NV002', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql02'), N'Lê Thị Lý',     'B1'),
('NV003', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='ql03'), N'Hồ Văn Khóa',   'B1');   -- tài khoản đang Bị khóa

-- Sinh viên (thêm 26DH113349 - diện chính sách đang ở, 26DH113350 - thường mới dọn vào,
-- 27DH113351 - tài khoản mới tạo nhưng đã bị khóa, chưa có hợp đồng)
INSERT INTO SINHVIEN (MSSV, MaTK, HoTen, NgaySinh, GioiTinh, KhoaHoc, DoiTuong) VALUES
('24DH113343', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='24DH113343'), N'Đặng Hoàng Minh Phi', '2006-04-07', 'Nam', 'K2024', 'BinhThuong'),
('25DH113344', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113344'), N'Võ Quốc Dũng',         '2007-01-10', 'Nam', 'K2025', 'BinhThuong'),
('25DH113345', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='25DH113345'), N'Trần Thu Cúc',         '2007-11-05', 'Nu',  'K2025', 'BinhThuong'),
('26DH113346', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113346'), N'Nguyễn Văn An',        '2008-03-15', 'Nam', 'K2026', 'BinhThuong'),
('26DH113347', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113347'), N'Phạm Thị Bình',        '2008-07-22', 'Nu',  'K2026', 'ChinhSach'),
('26DH113349', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113349'), N'Ngô Anh Thư',          '2008-02-18', 'Nu',  'K2026', 'ChinhSach'),
('26DH113350', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='26DH113350'), N'Bùi Văn Khoa',         '2008-09-30', 'Nam', 'K2026', 'BinhThuong'),
('27DH113351', (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='27DH113351'), N'Đỗ Thị Hạnh',          '2009-05-12', 'Nu',  'K2027', 'BinhThuong');

-- Phòng (SoGiuongTrong được TÍNH LẠI tự động phía dưới sau khi có phiếu đăng ký, không ghi cứng)
INSERT INTO PHONG (MaPhong, MaToa, Tang, LoaiPhong, SoGiuong, GiaPhong, TrangThai, SoGiuongTrong) VALUES
('A1-101', 'A1', 1, '4', 4,  600000, 'HoatDong', 4),
('A1-201', 'A1', 2, '6', 6,  450000, 'HoatDong', 6),
('A1-202', 'A1', 2, '6', 6,  450000, 'HoatDong', 6),
('A1-301', 'A1', 3, '8', 8,  350000, 'HoatDong', 8),
('B1-101', 'B1', 1, '4', 4,  650000, 'HoatDong', 4),
('B1-201', 'B1', 2, '8', 8,  350000, 'BaoTri',   8);

-- Giường (tự sinh theo phòng)
INSERT INTO GIUONG (MaGiuong, MaPhong, TrangThai)
SELECT p.MaPhong + '-G' + CAST(n.So AS VARCHAR), p.MaPhong, 'Trong'
FROM PHONG p
JOIN (SELECT 1 So UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
      UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8) n
  ON n.So <= p.SoGiuong;

-- Đợt đăng ký
INSERT INTO DOTDANGKY (TenDot, LoaiDot, HocKy, NgayMo, NgayDong, TrangThai) VALUES
(N'Ưu tiên Tân sinh viên HK1', 'UuTien', 'HK1-2026',    @UT_Mo,  @UT_Dong,  'DaDong'),
(N'Đăng ký đại trà HK1',       'DaiTra', 'HK1-2026',    @DT_Mo,  @DT_Dong,  'DangMo'),
(N'Học kỳ hè',                 'KyHe',   'HKHe-truoc',  @KH_Mo,  @KH_Dong,  'DaDong'),
(N'Đăng ký đại trà HK2 (dự kiến)', 'DaiTra', 'HK2-2026', @DT2_Mo, @DT2_Dong, 'ChuaMo');

-- Phiếu đăng ký / hợp đồng
INSERT INTO PHIEUDANGKY (MSSV, MaGiuong, MaDot, NgayDangKy, NgayBatDau, NgayKetThuc, TrangThai) VALUES
-- Nhóm đã ở lâu (~4 tháng) - còn ~27 ngày là hết hạn -> ĐƯỢC gia hạn (trong TS13=30 ngày)
('25DH113344', 'A1-201-G1', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), DATEADD(DAY,-130,@Today), @BD_Lau, @KT_Lau, 'DangO'),
('25DH113345', 'A1-201-G2', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), DATEADD(DAY,-130,@Today), @BD_Lau, @KT_Lau, 'DangO'),
('24DH113343', 'B1-101-G1', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), DATEADD(DAY,-130,@Today), @BD_Lau, @KT_Lau, 'DangO'),
-- Nhóm mới dọn vào (~20 ngày) - còn ~130 ngày -> BỊ CHẶN gia hạn (demo TS13), gồm 1 SV diện chính sách để đối chứng
('26DH113349', 'A1-202-G1', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), DATEADD(DAY,-22,@Today), @BD_Moi, @KT_Moi, 'DangO'),
('26DH113350', 'A1-202-G2', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), DATEADD(DAY,-22,@Today), @BD_Moi, @KT_Moi, 'DangO'),
-- Phiếu chờ đối chiếu (chưa tới ngày bắt đầu ở)
('26DH113346', 'A1-202-G3', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='DaiTra' AND TrangThai='DangMo'), GETDATE(), @BD_Cho, @KT_Cho, 'ChoDoiChieu'),
-- Hợp đồng cũ đã trả phòng (diện chính sách, đợt học kỳ hè trước)
('26DH113347', 'A1-301-G1', (SELECT MaDot FROM DOTDANGKY WHERE LoaiDot='KyHe'), DATEADD(DAY,-95,@Today), @BD_Cu, @KT_Cu, 'DaTraPhong');

-- Cập nhật trạng thái giường + số giường trống THEO DỮ LIỆU THỰC TẾ (không ghi cứng số liệu)
UPDATE g SET TrangThai='DaSuDung'
FROM GIUONG g
WHERE g.MaGiuong IN (SELECT MaGiuong FROM PHIEUDANGKY WHERE TrangThai IN ('DangO','ChoDoiChieu'));

UPDATE p SET SoGiuongTrong = p.SoGiuong - ISNULL(occ.SoLuong, 0)
FROM PHONG p
OUTER APPLY (SELECT COUNT(*) SoLuong FROM GIUONG g WHERE g.MaPhong = p.MaPhong AND g.TrangThai='DaSuDung') occ;

-- Đơn giá điện nước
INSERT INTO DONGIA (GiaDien, GiaNuoc, PhiDichVu, NgayApDung, TrangThai) VALUES
(2500, 15000, 20000, '2025-09-01', 'HetHieuLuc'),
(2800, 18000, 20000, '2026-01-01', 'HieuLuc');

-- Chỉ số điện nước: 8 tháng lịch sử (đã chốt) + tháng hiện tại (Nhap - chưa lập hóa đơn,
-- sẵn sàng demo "Xem trước/Lập hóa đơn") cho 2 phòng dài hạn A1-201 và B1-101
;WITH NumsThang AS (
    SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8
),
PhongDaiHan AS (
    SELECT MaPhong FROM (VALUES ('A1-201'), ('B1-101')) x(MaPhong)
)
INSERT INTO CHISODIENNUOC (MaPhong, Thang, DienDauKy, DienCuoiKy, NuocDauKy, NuocCuoiKy, TrangThai)
SELECT
    ph.MaPhong,
    FORMAT(DATEADD(MONTH, -tn.n, GETDATE()), 'MM/yyyy'),
    1000.0,
    1000.0 + 140 + ((tn.n * 7) % 45),
    250.0,
    250.0 + 16 + ((tn.n * 3) % 14),
    CASE WHEN tn.n = 0 THEN 'Nhap' ELSE 'DaChot' END
FROM PhongDaiHan ph CROSS JOIN NumsThang tn;

-- Chỉ số điện nước tháng hiện tại cho phòng của nhóm "mới dọn vào" (A1-202) - cũng chưa lập hóa đơn
INSERT INTO CHISODIENNUOC (MaPhong, Thang, DienDauKy, DienCuoiKy, NuocDauKy, NuocCuoiKy, TrangThai) VALUES
('A1-202', FORMAT(GETDATE(), 'MM/yyyy'), 500.0, 560.0, 120.0, 130.0, 'Nhap');

-- Hóa đơn cho 8 tháng lịch sử đã chốt (tháng hiện tại chưa có hóa đơn - đúng ý đồ demo):
-- tháng gần nhất (n=1) -> ChoThanhToan, hạn còn 2 ngày (trong hạn TS5=3 ngày -> demo banner nhắc nhở QD07),
-- tháng kế (n=2, xa hơn) -> QuaHan (đã quá hạn > TS4=14 ngày, phát sinh vi phạm),
-- các tháng còn lại -> DaThanhToan.
-- Lưu ý thứ tự thời gian: tháng càng xa hiện tại thì ngày phát hành/hạn thanh toán càng phải
-- lùi xa hơn về quá khứ (không được để hóa đơn tháng xa hơn lại có hạn "trẻ" hơn hóa đơn tháng gần).
-- TienPhong giả định sĩ số hiện tại ổn định trong suốt lịch sử để đơn giản hoá dữ liệu mẫu.
;WITH Nums9 AS (
    SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8
),
DonGiaHL AS (SELECT TOP 1 MaDonGia, GiaDien, GiaNuoc FROM DONGIA WHERE TrangThai='HieuLuc')
INSERT INTO HOADON (MaPhong, MaChiSo, MaDonGia, Thang, TienPhong, TienDien, TienNuoc, TongTien, NgayPhatHanh, HanThanhToan, TrangThai)
SELECT
    cs.MaPhong, cs.MaChiSo, dg.MaDonGia, cs.Thang,
    CASE cs.MaPhong WHEN 'A1-201' THEN 920000 ELSE 670000 END,
    (cs.DienCuoiKy - cs.DienDauKy) * dg.GiaDien,
    (cs.NuocCuoiKy - cs.NuocDauKy) * dg.GiaNuoc,
    CASE cs.MaPhong WHEN 'A1-201' THEN 920000 ELSE 670000 END
        + (cs.DienCuoiKy - cs.DienDauKy) * dg.GiaDien
        + (cs.NuocCuoiKy - cs.NuocDauKy) * dg.GiaNuoc,
    CASE WHEN n.n = 1 THEN DATEADD(DAY,-13,GETDATE())
         WHEN n.n = 2 THEN DATEADD(DAY,-35,GETDATE())
         ELSE DATEADD(MONTH,-n.n, DATEADD(DAY,-15,GETDATE())) END,
    CASE WHEN n.n = 1 THEN DATEADD(DAY, 2,GETDATE())   -- trong hạn TS5=3 ngày -> demo ngay banner nhắc nhở (QD07)
         WHEN n.n = 2 THEN DATEADD(DAY,-20,GETDATE())  -- quá hạn > TS4=14 ngày -> demo vi phạm
         ELSE DATEADD(MONTH,-n.n, DATEADD(DAY,15,GETDATE())) END,
    CASE WHEN n.n = 1 THEN 'ChoThanhToan' WHEN n.n = 2 THEN 'QuaHan' ELSE 'DaThanhToan' END
FROM CHISODIENNUOC cs
CROSS JOIN DonGiaHL dg
JOIN Nums9 n ON FORMAT(DATEADD(MONTH, -n.n, GETDATE()), 'MM/yyyy') = cs.Thang
WHERE cs.TrangThai = 'DaChot' AND cs.MaPhong IN ('A1-201','B1-101');

-- Thanh toán cho các hóa đơn đã "Đã thanh toán"
INSERT INTO THANHTOAN (MaHD, MSSV, PhuongThuc, SoTien, MaGDCong, ThoiGian, KetQua)
SELECT
    h.MaHD,
    CASE h.MaPhong WHEN 'A1-201' THEN '25DH113344' ELSE '24DH113343' END,
    CASE h.MaHD % 3 WHEN 0 THEN 'VNPay' WHEN 1 THEN 'MoMo' ELSE 'NganHang' END,
    h.TongTien,
    'GD' + CAST(h.MaHD AS VARCHAR) + FORMAT(h.HanThanhToan,'yyyyMMdd'),
    h.HanThanhToan,
    'ThanhCong'
FROM HOADON h
WHERE h.TrangThai = 'DaThanhToan' AND h.MaPhong IN ('A1-201','B1-101');

-- Vi phạm: hóa đơn quá hạn > TS4 (14 ngày) của các phòng dài hạn
INSERT INTO VIPHAM (MSSV, MaHD, NgayGhiNhan, SoDiem, LyDo)
SELECT sv.MSSV, h.MaHD, DATEADD(DAY,-5,GETDATE()),
    CAST((SELECT GiaTri FROM THAMSO WHERE MaThamSo='TS6') AS INT),
    N'Hóa đơn ' + h.Thang + N' quá hạn thanh toán trên ' + (SELECT GiaTri FROM THAMSO WHERE MaThamSo='TS4') + N' ngày'
FROM HOADON h
CROSS APPLY (
    SELECT '25DH113344' AS MSSV WHERE h.MaPhong='A1-201'
    UNION ALL SELECT '25DH113345' WHERE h.MaPhong='A1-201'
    UNION ALL SELECT '24DH113343' WHERE h.MaPhong='B1-101'
) sv
WHERE h.TrangThai='QuaHan';

UPDATE sv SET DiemViPham = vp.SoLan
FROM SINHVIEN sv
JOIN (SELECT MSSV, COUNT(*) AS SoLan FROM VIPHAM GROUP BY MSSV) vp ON vp.MSSV = sv.MSSV;

-- Đơn phản hồi / đề xuất / chuyển phòng / trả phòng
INSERT INTO DONYEUCAU (MSSV, MaNV, MaPhieu, MaGiuongMoi, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, PhanHoi, NgayTao) VALUES
('25DH113344', 'NV001', NULL, NULL, 'PhanHoi', N'Hỏng bóng đèn phòng A1-201', N'Bóng đèn trần giữa phòng bị chập chờn, xin sửa giúp.', 'Cao', 'DangXuLy', N'Đã tiếp nhận, kỹ thuật sẽ đến trong tuần.', DATEADD(DAY,-14,GETDATE())),
('25DH113345', NULL,   NULL, NULL, 'DeXuat',  N'Lắp thêm quạt trần', N'Phòng nóng vào buổi trưa, đề xuất lắp thêm 1 quạt trần.', 'TrungBinh', 'ChoXuLy', NULL, DATEADD(DAY,-10,GETDATE())),
('24DH113343', NULL,   NULL, NULL, 'PhanHoi', N'Vòi nước rò rỉ', N'Vòi nước khu vệ sinh phòng B1-101 bị rò rỉ.', 'TrungBinh', 'ChoXuLy', NULL, DATEADD(DAY,-7,GETDATE())),
('26DH113350', NULL,
    (SELECT MaPhieu FROM PHIEUDANGKY WHERE MSSV='26DH113350' AND TrangThai='DangO'),
    'A1-101-G1', 'ChuyenPhong', N'Xin chuyển sang phòng A1-101',
    N'Phòng hiện tại khá ồn vào ban đêm, mong được chuyển sang phòng A1-101 còn trống giường.',
    'TrungBinh', 'ChoXuLy', NULL, DATEADD(DAY,-3,GETDATE())),
('24DH113343', NULL,
    (SELECT MaPhieu FROM PHIEUDANGKY WHERE MSSV='24DH113343' AND TrangThai='DangO'),
    NULL, 'TraPhong', N'Xin trả phòng trước hạn',
    N'Gia đình chuyển chỗ ở gần trường nên xin được trả phòng sớm trước khi hợp đồng kết thúc.',
    'Thap', 'ChoXuLy', NULL, DATEADD(DAY,-1,GETDATE()));

-- Lịch sử thông báo: 1 thông báo/hóa đơn lịch sử gửi cho SV đang ở phòng đó (đủ nhiều dòng để
-- kiểm thử phân trang + bộ lọc mặc định 30 ngày). Chỉ có kênh Email (QD12 chỉ yêu cầu gửi qua
-- email; hệ thống hiện chỉ gửi email thật qua SMTP) và gắn với đúng nghiệp vụ THẬT đang có
-- (gửi hóa đơn hàng tháng) - không tự tạo sẵn thông báo "nhắc nhở" ở đây vì QD07/TS5 được job quét
-- tự sinh khi sinh viên mở trang Tổng quan (banner + email thật), không cần chèn sẵn trong seed data.
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui)
SELECT sv.MSSV, h.MaHD,
    N'Hóa đơn tháng ' + h.Thang + N' phòng ' + h.MaPhong + N': ' + FORMAT(h.TongTien,'N0') + N'đ. Hạn thanh toán ' + FORMAT(h.HanThanhToan,'dd/MM/yyyy') + N'.',
    'Email',
    h.NgayPhatHanh,
    'ThanhCong'
FROM HOADON h
CROSS APPLY (
    SELECT '25DH113344' AS MSSV WHERE h.MaPhong='A1-201'
    UNION ALL SELECT '25DH113345' WHERE h.MaPhong='A1-201'
    UNION ALL SELECT '24DH113343' WHERE h.MaPhong='B1-101'
) sv
WHERE h.MaPhong IN ('A1-201','B1-101');

-- Nhật ký thao tác hệ thống: 30 dòng mẫu trải dài ~87 ngày (đủ để kiểm thử phân trang và
-- bộ lọc mặc định "30 ngày gần nhất") xoay vòng đủ 5 loại hành động Thêm/Sửa/Xóa/Khóa/Mở khóa
;WITH NumsNK AS (
    SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9
    UNION ALL SELECT 10 UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14
    UNION ALL SELECT 15 UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19
    UNION ALL SELECT 20 UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24
    UNION ALL SELECT 25 UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29
)
INSERT INTO NHATKY (MaTK, HoTenNguoiThucHien, VaiTro, HanhDong, DoiTuong, NoiDung, ThoiGian)
SELECT
    (SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin'),
    N'Quản trị viên', 'QTV',
    CASE n % 5 WHEN 0 THEN 'Them' WHEN 1 THEN 'Sua' WHEN 2 THEN 'Xoa' WHEN 3 THEN 'Khoa' ELSE 'MoKhoa' END,
    CASE n % 4 WHEN 0 THEN N'Phòng' WHEN 1 THEN N'Tài khoản' WHEN 2 THEN N'Đơn giá điện/nước' ELSE N'Đợt đăng ký' END,
    N'Thao tác mẫu #' + CAST(n AS NVARCHAR(10)) + N' để kiểm thử phân trang và bộ lọc thời gian của Nhật ký hệ thống.',
    DATEADD(DAY, -(n*3), GETDATE())
FROM NumsNK;

-- Vài dòng nhật ký "thật" gắn với đúng tình huống trong dữ liệu mẫu (tài khoản đang bị khóa ở trên)
INSERT INTO NHATKY (MaTK, HoTenNguoiThucHien, VaiTro, HanhDong, DoiTuong, NoiDung, ThoiGian) VALUES
((SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin'), N'Quản trị viên', 'QTV', 'Khoa', N'Tài khoản', N'Khóa tài khoản quản lý ql03 do vi phạm quy trình xử lý đơn từ - chờ xác minh.', DATEADD(DAY,-4,GETDATE())),
((SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin'), N'Quản trị viên', 'QTV', 'Khoa', N'Tài khoản', N'Khóa tài khoản sinh viên 27DH113351 do phát hiện thông tin đăng ký chưa xác thực.', DATEADD(DAY,-2,GETDATE())),
((SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin'), N'Quản trị viên', 'QTV', 'Sua', N'Đơn giá điện/nước', N'Cập nhật biểu giá điện/nước áp dụng từ 01/01/2026.', DATEADD(DAY,-40,GETDATE())),
((SELECT MaTK FROM TAIKHOAN WHERE TenDangNhap='admin'), N'Quản trị viên', 'QTV', 'Them', N'Phòng', N'Thêm phòng A1-301 (8 giường) vào tòa A1.', DATEADD(DAY,-50,GETDATE()));
GO

PRINT N'>>> Tạo CSDL DormManager thành công! Mật khẩu mọi tài khoản: 123456';
PRINT N'>>> QTV: admin';
PRINT N'>>> QL: ql01, ql02 (hoạt động) | ql03 (đang Bị khóa - demo Mở khóa)';
PRINT N'>>> SV đang ở: 25DH113344, 25DH113345, 24DH113343 (sắp hết hạn - được Gia hạn) | 26DH113349 (chính sách), 26DH113350 (thường) (mới ở - Gia hạn bị chặn)';
PRINT N'>>> SV khác: 26DH113346 (chờ đối chiếu) | 26DH113347 (chính sách, đã trả phòng) | 27DH113351 (đang Bị khóa - demo Mở khóa)';
GO
