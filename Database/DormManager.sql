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
   LoaiDon có thêm 'TraPhong' (yêu cầu trả phòng do SV gửi, QL xác nhận).
   MaPhieu liên kết đơn với hợp đồng - chỉ dùng khi LoaiDon='TraPhong'. */
CREATE TABLE DONYEUCAU (
    MaDon     INT IDENTITY(1,1) PRIMARY KEY,
    MSSV      VARCHAR(10)  NOT NULL FOREIGN KEY REFERENCES SINHVIEN(MSSV),
    MaNV      VARCHAR(10)  NULL FOREIGN KEY REFERENCES QUANLY(MaNV),
    MaPhieu   INT          NULL FOREIGN KEY REFERENCES PHIEUDANGKY(MaPhieu),
    LoaiDon   VARCHAR(10)  NOT NULL CHECK (LoaiDon IN ('PhanHoi','DeXuat','TraPhong')),
    TieuDe    NVARCHAR(200) NOT NULL,
    NoiDung   NVARCHAR(MAX) NOT NULL,
    MucUuTien VARCHAR(10)  NOT NULL DEFAULT 'TrungBinh' CHECK (MucUuTien IN ('Cao','TrungBinh','Thap')),
    TrangThai VARCHAR(10)  NOT NULL DEFAULT 'ChoXuLy' CHECK (TrangThai IN ('ChoXuLy','DangXuLy','DaXuLy','TuChoi')),
    PhanHoi   NVARCHAR(MAX) NULL,
    NgayTao   DATETIME     NOT NULL DEFAULT GETDATE()
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
   GiaPhong × số người đã ở TRỌN VẸN từ đầu tháng đó (SV mới dọn vào giữa
   tháng chưa tính vào tháng này, bắt đầu tính từ tháng kế tiếp). */
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
    Kenh        VARCHAR(5)   NOT NULL CHECK (Kenh IN ('Email','SMS')),
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

/* =====================================================================
   DỮ LIỆU MẪU
   Mật khẩu mặc định của MỌI tài khoản là: 123456
   (SHA-256 = 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92)
   ===================================================================== */
DECLARE @mk VARCHAR(255) = '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92';

-- Tham số hệ thống ( )
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
('TS10','2', N'Số tháng của đợt lưu trú Học kỳ hè');

-- Tài khoản
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, SDT, VaiTro) VALUES
('admin',      @mk, 'admin@ktx.edu.vn',            '0900000001', 'QTV'),
('ql01',       @mk, 'ql01@ktx.edu.vn',             '0900000002', 'QL'),
('ql02',       @mk, 'ql02@ktx.edu.vn',             '0900000003', 'QL'),
('24DH113343',  @mk, '24dh113343@st.huflit.edu.vn', '0911000005', 'SV'),
('25DH113344',  @mk, '25dh113344@st.huflit.edu.vn', '0911000003', 'SV'),
('25DH113345',  @mk, '25dh113345@st.huflit.edu.vn', '0911000004', 'SV'),
('26DH113346',  @mk, '26dh113346@st.huflit.edu.vn', '0911000001', 'SV'),
('26DH113347',  @mk, '26dh113347@st.huflit.edu.vn', '0911000002', 'SV');

-- Tòa nhà
INSERT INTO TOANHA (MaToa, TenToa, DiaChi, SoTang) VALUES
('A1', N'Tòa A1', N'Khu A, KTX Đại học', 5),
('B1', N'Tòa B1', N'Khu B, KTX Đại học', 4);

-- Quản lý
INSERT INTO QUANLY (MaNV, MaTK, HoTen, MaToa) VALUES
('NV001', 2, N'Trần Văn Quản',  'A1'),
('NV002', 3, N'Lê Thị Lý',      'B1');

-- Sinh viên
INSERT INTO SINHVIEN (MSSV, MaTK, HoTen, NgaySinh, GioiTinh, KhoaHoc, DoiTuong) VALUES
('24DH113343', 4, N'Võ Quốc Dũng',    '2006-05-30', 'Nam', 'K2024', 'BinhThuong'),
('25DH113344', 5, N'Đặng Hoàng Minh', '2007-01-10', 'Nam', 'K2025', 'BinhThuong'),
('25DH113345', 6, N'Trần Thu Cúc',    '2007-11-05', 'Nu',  'K2025', 'BinhThuong'),
('26DH113346', 7, N'Nguyễn Văn An',   '2008-03-15', 'Nam', 'K2026', 'BinhThuong'),
('26DH113347', 8, N'Phạm Thị Bình',   '2008-07-22', 'Nu',  'K2026', 'ChinhSach');

-- Phòng
INSERT INTO PHONG (MaPhong, MaToa, Tang, LoaiPhong, SoGiuong, GiaPhong, TrangThai, SoGiuongTrong) VALUES
('A1-101', 'A1', 1, '4', 4,  600000, 'HoatDong', 4),
('A1-201', 'A1', 2, '6', 6,  450000, 'HoatDong', 4),
('A1-202', 'A1', 2, '6', 6,  450000, 'HoatDong', 6),
('A1-301', 'A1', 3, '8', 8,  350000, 'HoatDong', 8),
('B1-101', 'B1', 1, '4', 4,  650000, 'HoatDong', 3),
('B1-201', 'B1', 2, '8', 8,  350000, 'BaoTri',   8);

-- Giường (tự sinh theo phòng)
INSERT INTO GIUONG (MaGiuong, MaPhong, TrangThai)
SELECT p.MaPhong + '-G' + CAST(n.So AS VARCHAR), p.MaPhong, 'Trong'
FROM PHONG p
JOIN (SELECT 1 So UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
      UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8) n
  ON n.So <= p.SoGiuong;

-- Đánh dấu giường đang sử dụng cho các hợp đồng mẫu
UPDATE GIUONG SET TrangThai='DaSuDung' WHERE MaGiuong IN ('A1-201-G1','A1-201-G2','B1-101-G1');

-- Đợt đăng ký
INSERT INTO DOTDANGKY (TenDot, LoaiDot, HocKy, NgayMo, NgayDong, TrangThai) VALUES
(N'Ưu tiên Tân sinh viên HK1 2026', 'UuTien', 'HK1-2026', '2026-06-20', '2026-07-04', 'DaDong'),
(N'Đăng ký đại trà HK1 2026',       'DaiTra', 'HK1-2026', '2026-07-05', '2026-08-15', 'DangMo'),
(N'Học kỳ hè 2026',                 'KyHe',   'HKHe-2026','2026-05-15', '2026-06-01', 'DaDong');

-- Phiếu đăng ký / hợp đồng
INSERT INTO PHIEUDANGKY (MSSV, MaGiuong, MaDot, NgayDangKy, NgayBatDau, NgayKetThuc, TrangThai) VALUES
('25DH113344', 'A1-201-G1', 2, '2026-07-06', '2026-08-15', '2027-01-15', 'DangO'),
('25DH113345', 'A1-201-G2', 2, '2026-07-06', '2026-08-15', '2027-01-15', 'DangO'),
('24DH113343', 'B1-101-G1', 2, '2026-07-07', '2026-08-15', '2027-01-15', 'DangO'),
('26DH113346', 'A1-202-G1', 2, GETDATE(),    '2026-08-15', '2027-01-15', 'ChoDoiChieu');

-- Giữ chỗ cho phiếu ChoDoiChieu
UPDATE GIUONG SET TrangThai='DaSuDung' WHERE MaGiuong='A1-202-G1';
UPDATE PHONG SET SoGiuongTrong = 5 WHERE MaPhong='A1-202';

-- Đơn giá điện nước
INSERT INTO DONGIA (GiaDien, GiaNuoc, PhiDichVu, NgayApDung, TrangThai) VALUES
(2500, 15000, 20000, '2025-09-01', 'HetHieuLuc'),
(2800, 18000, 20000, '2026-01-01', 'HieuLuc');

-- Chỉ số điện nước tháng 05 & 06/2026 cho phòng đang có người ở
INSERT INTO CHISODIENNUOC (MaPhong, Thang, DienDauKy, DienCuoiKy, NuocDauKy, NuocCuoiKy, TrangThai) VALUES
('A1-201', '05/2026', 1200.0, 1350.5, 300.0, 315.2, 'DaChot'),
('B1-101', '05/2026',  800.0,  905.0, 210.0, 221.5, 'DaChot'),
('A1-201', '06/2026', 1350.5, 1502.0, 315.2, 331.0, 'DaChot'),
('B1-101', '06/2026',  905.0, 1012.5, 221.5, 232.8, 'DaChot');

-- Hóa đơn (TienPhong = GiaPhong × SoNguoiO + PhiDichVu; TienDien = kWh × GiaDien; TienNuoc = m³ × GiaNuoc)
-- A1-201 có 2 SV (25DH113344, 25DH113345): 450.000×2 + 20.000 = 920.000đ
-- B1-101 có 1 SV (24DH113343): 650.000×1 + 20.000 = 670.000đ
INSERT INTO HOADON (MaPhong, MaChiSo, MaDonGia, Thang, TienPhong, TienDien, TienNuoc, TongTien, NgayPhatHanh, HanThanhToan, TrangThai) VALUES
('A1-201', 1, 2, '05/2026', 920000, 421400, 273600, 1615000, '2026-06-01', '2026-06-15', 'DaThanhToan'),
('B1-101', 2, 2, '05/2026', 670000, 294000, 207000, 1171000, '2026-06-01', '2026-06-15', 'QuaHan'),
('A1-201', 3, 2, '06/2026', 920000, 424200, 284400, 1628600, '2026-07-01', '2026-07-20', 'ChoThanhToan'),
('B1-101', 4, 2, '06/2026', 670000, 301000, 203400, 1174400, '2026-07-01', '2026-07-20', 'ChoThanhToan');

-- Thanh toán
INSERT INTO THANHTOAN (MaHD, MSSV, PhuongThuc, SoTien, MaGDCong, ThoiGian, KetQua) VALUES
(1, '25DH113344', 'VNPay', 1615000, 'VNP20260610001', '2026-06-10', 'ThanhCong');

-- Vi phạm (hóa đơn B1-101 tháng 05 quá hạn > 14 ngày)
INSERT INTO VIPHAM (MSSV, MaHD, NgayGhiNhan, SoDiem, LyDo) VALUES
('24DH113343', 2, '2026-06-30', 1, N'Hóa đơn 05/2026 quá hạn thanh toán trên 14 ngày');
UPDATE SINHVIEN SET DiemViPham = 1 WHERE MSSV = '24DH113343';

-- Đơn phản hồi / đề xuất
INSERT INTO DONYEUCAU (MSSV, MaNV, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, PhanHoi, NgayTao) VALUES
('25DH113344', 'NV001', 'PhanHoi', N'Hỏng bóng đèn phòng A1-201', N'Bóng đèn trần giữa phòng bị chập chờn, xin sửa giúp.', 'Cao', 'DangXuLy', N'Đã tiếp nhận, kỹ thuật sẽ đến trong tuần.', '2026-07-05'),
('25DH113345', NULL,   'DeXuat',  N'Lắp thêm quạt trần',          N'Phòng nóng vào buổi trưa, đề xuất lắp thêm 1 quạt trần.', 'TrungBinh', 'ChoXuLy', NULL, '2026-07-08'),
('24DH113343', NULL,   'PhanHoi', N'Vòi nước rò rỉ',              N'Vòi nước khu vệ sinh phòng B1-101 bị rò rỉ.', 'TrungBinh', 'ChoXuLy', NULL, '2026-07-10');

-- Thông báo
INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh, ThoiGianGui, TrangThaiGui) VALUES
('25DH113344', 3, N'Hóa đơn tháng 06/2026 phòng A1-201: 1.628.600đ. Hạn thanh toán 20/07/2026.', 'Email', '2026-07-01', 'ThanhCong'),
('25DH113345', 3, N'Hóa đơn tháng 06/2026 phòng A1-201: 1.628.600đ. Hạn thanh toán 20/07/2026.', 'Email', '2026-07-01', 'ThanhCong'),
('24DH113343', 4, N'Hóa đơn tháng 06/2026 phòng B1-101: 1.174.400đ. Hạn thanh toán 20/07/2026.', 'SMS',   '2026-07-01', 'ThanhCong');
GO

PRINT N'>>> Tạo CSDL DormManager thành công! Mật khẩu mọi tài khoản: 123456';
PRINT N'>>> QTV: admin | QL: ql01, ql02 | SV: 26DH113346, 26DH113347, 25DH113344, 25DH113345, 24DH113343';
GO
