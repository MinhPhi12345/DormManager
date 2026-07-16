/* =============================================================================
   STORED PROCEDURES - DormManager (Nhóm 19)
   Mỗi Stored Procedure được đặt tên theo đúng chức năng thực hiện (không còn
   tiền tố BMxx_). Với các chức năng bị trùng ý nghĩa tên giữa nhiều BM khác
   nhau (khác tham số/cột trả về), tên được phân biệt thêm theo ngữ cảnh sử
   dụng, ví dụ: sp_DsToaTraCuuPhong (SV), sp_DsToaQuanLy (QL)...
   Một vài SP tiện ích dùng chung nhiều BM (tra thông số hệ thống, gửi
   thông báo...) được đặt tên sp_Chung_... để dễ phân biệt.

   Chạy file này SAU KHI đã chạy DormManager.sql (tạo bảng + dữ liệu mẫu).
   ========================================================================== */

USE DormManager;
GO

/* ============================================================================
   SP DÙNG CHUNG NHIỀU BM
   ========================================================================== */

-- Tra giá trị 1 tham số hệ thống (TS1..TS10) - dùng trong rất nhiều quy định QDxx
IF OBJECT_ID('sp_Chung_LayThamSo', 'P') IS NOT NULL DROP PROCEDURE sp_Chung_LayThamSo;
GO
CREATE PROCEDURE sp_Chung_LayThamSo
    @MaThamSo VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT GiaTri FROM THAMSO WHERE MaThamSo = @MaThamSo;
END
GO

-- Ghi 1 thông báo cho sinh viên - dùng lại ở rất nhiều nghiệp vụ
IF OBJECT_ID('sp_Chung_ThemThongBao', 'P') IS NOT NULL DROP PROCEDURE sp_Chung_ThemThongBao;
GO
CREATE PROCEDURE sp_Chung_ThemThongBao
    @MSSV    VARCHAR(10),
    @MaHD    INT = NULL,
    @NoiDung NVARCHAR(500),
    @Kenh    VARCHAR(10) = 'Email'
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh)
    VALUES (@MSSV, @MaHD, @NoiDung, @Kenh);
END
GO


/* ============================================================================
 - Đăng nhập
   ========================================================================== */
IF OBJECT_ID('sp_DangNhap', 'P') IS NOT NULL DROP PROCEDURE sp_DangNhap;
GO
CREATE PROCEDURE sp_DangNhap
    @TenDangNhap VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaTK, TenDangNhap, MatKhau, VaiTro, TrangThai
    FROM TAIKHOAN
    WHERE TenDangNhap = @TenDangNhap OR Email = @TenDangNhap;
END
GO

IF OBJECT_ID('sp_LayHoTenSVTheoTK', 'P') IS NOT NULL DROP PROCEDURE sp_LayHoTenSVTheoTK;
GO
CREATE PROCEDURE sp_LayHoTenSVTheoTK
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MSSV, HoTen FROM SINHVIEN WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_ThongTinQL', 'P') IS NOT NULL DROP PROCEDURE sp_ThongTinQL;
GO
CREATE PROCEDURE sp_ThongTinQL
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaNV, HoTen FROM QUANLY WHERE MaTK = @MaTK;
END
GO

/* ============================================================================
 - Quên mật khẩu
   ========================================================================== */
IF OBJECT_ID('sp_KiemTraEmail', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraEmail;
GO
CREATE PROCEDURE sp_KiemTraEmail
    @Email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaTK FROM TAIKHOAN WHERE Email = @Email;
END
GO

/* ============================================================================
 - Thông tin tổng quan (sinh viên)
   Gộp 4 câu SELECT vào 1 SP, trả về 4 result set trong 1 lượt gọi.
   ========================================================================== */
IF OBJECT_ID('sp_TongQuan', 'P') IS NOT NULL DROP PROCEDURE sp_TongQuan;
GO
CREATE PROCEDURE sp_TongQuan
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: Phòng đang ở
    SELECT TOP 1 p.MaPhong, p.Tang, p.GiaPhong, p.LoaiPhong, t.TenToa,
           pd.MaPhieu, pd.NgayBatDau, pd.NgayKetThuc, g.MaGiuong
    FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN PHONG  p ON p.MaPhong  = g.MaPhong
    JOIN TOANHA t ON t.MaToa    = p.MaToa
    WHERE pd.MSSV = @MSSV AND pd.TrangThai = 'DangO'
    ORDER BY pd.NgayDangKy DESC;

    -- Result set 2: Bạn cùng phòng
    SELECT sv.MSSV, sv.HoTen, sv.KhoaHoc, sv.GioiTinh
    FROM PHIEUDANGKY pd
    JOIN GIUONG g   ON g.MaGiuong = pd.MaGiuong
    JOIN SINHVIEN sv ON sv.MSSV   = pd.MSSV
    WHERE pd.TrangThai = 'DangO' AND sv.MSSV <> @MSSV
      AND g.MaPhong = (SELECT TOP 1 g2.MaPhong FROM PHIEUDANGKY pd2
                       JOIN GIUONG g2 ON g2.MaGiuong = pd2.MaGiuong
                       WHERE pd2.MSSV = @MSSV AND pd2.TrangThai = 'DangO');

    -- Result set 3: Hóa đơn chưa thanh toán của phòng đang ở
    SELECT h.MaHD, h.Thang, h.TongTien, h.HanThanhToan, h.TrangThai
    FROM HOADON h
    WHERE h.TrangThai IN ('ChoThanhToan','QuaHan')
      AND h.MaPhong IN (SELECT g.MaPhong FROM PHIEUDANGKY pd
                        JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
                        WHERE pd.MSSV = @MSSV AND pd.TrangThai = 'DangO')
    ORDER BY h.HanThanhToan;

    -- Result set 4: Đơn phản hồi/đề xuất gần đây
    SELECT TOP 5 MaDon, TieuDe, LoaiDon, NgayTao, TrangThai
    FROM DONYEUCAU WHERE MSSV = @MSSV ORDER BY NgayTao DESC;
END
GO

/* ============================================================================
 - Tra cứu phòng
   ========================================================================== */
IF OBJECT_ID('sp_DoiTuongSV', 'P') IS NOT NULL DROP PROCEDURE sp_DoiTuongSV;
GO
CREATE PROCEDURE sp_DoiTuongSV
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;
END
GO

IF OBJECT_ID('sp_TraCuuPhong', 'P') IS NOT NULL DROP PROCEDURE sp_TraCuuPhong;
GO
CREATE PROCEDURE sp_TraCuuPhong
    @TuKhoa       NVARCHAR(100) = NULL,
    @MaToa        VARCHAR(10)   = NULL,
    @LoaiPhong    VARCHAR(2)    = NULL,
    @MucGia       VARCHAR(20)   = NULL,   -- 'duoi400' | '400den600' | 'tren600' | NULL
 @ChiTieuChuan BIT = 0 -- 1 = SV diện chính sách: chỉ phòng 6-8 giường
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.MaPhong, p.Tang, p.LoaiPhong, p.SoGiuong, p.SoGiuongTrong,
           p.GiaPhong, p.TrangThai, t.TenToa, t.MaToa
    FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE p.TrangThai = 'HoatDong'
      AND (@ChiTieuChuan = 0 OR p.LoaiPhong IN ('6','8'))
      AND (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%' OR t.TenToa LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
      AND (@LoaiPhong IS NULL OR p.LoaiPhong = @LoaiPhong)
      AND (@MucGia IS NULL
           OR (@MucGia = 'duoi400'    AND p.GiaPhong < 400000)
           OR (@MucGia = '400den600' AND p.GiaPhong BETWEEN 400000 AND 600000)
           OR (@MucGia = 'tren600'   AND p.GiaPhong > 600000))
    ORDER BY t.MaToa, p.Tang, p.MaPhong;
END
GO

IF OBJECT_ID('sp_DsToaTraCuuPhong', 'P') IS NOT NULL DROP PROCEDURE sp_DsToaTraCuuPhong;
GO
CREATE PROCEDURE sp_DsToaTraCuuPhong
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;
END
GO

/* ============================================================================
 - Chi tiết thông tin phòng
   ========================================================================== */
IF OBJECT_ID('sp_ChiTietPhong', 'P') IS NOT NULL DROP PROCEDURE sp_ChiTietPhong;
GO
CREATE PROCEDURE sp_ChiTietPhong
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, t.TenToa, t.DiaChi FROM PHONG p
    JOIN TOANHA t ON t.MaToa = p.MaToa WHERE p.MaPhong = @MaPhong;
END
GO

IF OBJECT_ID('sp_DsGiuongTrongPhong', 'P') IS NOT NULL DROP PROCEDURE sp_DsGiuongTrongPhong;
GO
CREATE PROCEDURE sp_DsGiuongTrongPhong
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaGiuong, TrangThai FROM GIUONG WHERE MaPhong = @MaPhong ORDER BY MaGiuong;
END
GO

IF OBJECT_ID('sp_ThanhVien', 'P') IS NOT NULL DROP PROCEDURE sp_ThanhVien;
GO
CREATE PROCEDURE sp_ThanhVien
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sv.HoTen, sv.MSSV, sv.KhoaHoc FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai = 'DangO';
END
GO

/* ============================================================================
 /07/08 - Đăng ký ở KTX (đại trà / ưu tiên tân SV / học kỳ hè)
   ========================================================================== */
IF OBJECT_ID('sp_ThongTinGiuong', 'P') IS NOT NULL DROP PROCEDURE sp_ThongTinGiuong;
GO
CREATE PROCEDURE sp_ThongTinGiuong
    @MaGiuong VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.MaGiuong, g.TrangThai, p.MaPhong, p.LoaiPhong, p.GiaPhong, p.Tang, t.TenToa
    FROM GIUONG g JOIN PHONG p ON p.MaPhong = g.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE g.MaGiuong = @MaGiuong AND p.TrangThai = 'HoatDong';
END
GO

IF OBJECT_ID('sp_KiemTraHopDongHieuLuc', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraHopDongHieuLuc;
GO
CREATE PROCEDURE sp_KiemTraHopDongHieuLuc
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM PHIEUDANGKY
    WHERE MSSV = @MSSV AND TrangThai IN ('ChoDoiChieu','DangO');
END
GO

--: đợt ưu tiên được xét trước đợt đại trà nếu cả 2 cùng đang mở
IF OBJECT_ID('sp_LayDotDangMo', 'P') IS NOT NULL DROP PROCEDURE sp_LayDotDangMo;
GO
CREATE PROCEDURE sp_LayDotDangMo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM DOTDANGKY
    WHERE GETDATE() BETWEEN NgayMo AND NgayDong
    ORDER BY CASE LoaiDot WHEN 'UuTien' THEN 0 ELSE 1 END;
END
GO

IF OBJECT_ID('sp_LayKhoaHocDoiTuongSV', 'P') IS NOT NULL DROP PROCEDURE sp_LayKhoaHocDoiTuongSV;
GO
CREATE PROCEDURE sp_LayKhoaHocDoiTuongSV
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT KhoaHoc, DoiTuong FROM SINHVIEN WHERE MSSV = @MSSV;
END
GO

--: đợt ưu tiên chỉ dành cho khóa mới nhất (tân sinh viên)
IF OBJECT_ID('sp_KhoaHocMoiNhat', 'P') IS NOT NULL DROP PROCEDURE sp_KhoaHocMoiNhat;
GO
CREATE PROCEDURE sp_KhoaHocMoiNhat
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MAX(KhoaHoc) FROM SINHVIEN;
END
GO

-- Ghi phiếu đăng ký + trừ giường trống - gói trong 1 transaction để đảm bảo toàn vẹn dữ liệu
IF OBJECT_ID('sp_XacNhanDangKy', 'P') IS NOT NULL DROP PROCEDURE sp_XacNhanDangKy;
GO
CREATE PROCEDURE sp_XacNhanDangKy
    @MSSV        VARCHAR(10),
    @MaGiuong    VARCHAR(20),
    @MaDot       INT,
    @NgayBatDau  DATE,
    @NgayKetThuc DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO PHIEUDANGKY (MSSV, MaGiuong, MaDot, NgayBatDau, NgayKetThuc, TrangThai)
        VALUES (@MSSV, @MaGiuong, @MaDot, @NgayBatDau, @NgayKetThuc, 'ChoDoiChieu');

        UPDATE GIUONG SET TrangThai = 'DaSuDung' WHERE MaGiuong = @MaGiuong;

        UPDATE PHONG SET SoGiuongTrong = SoGiuongTrong - 1
        WHERE MaPhong = (SELECT MaPhong FROM GIUONG WHERE MaGiuong = @MaGiuong);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 /10 - Hợp đồng đăng ký + Gia hạn lưu trú
   ========================================================================== */
IF OBJECT_ID('sp_DsHopDong', 'P') IS NOT NULL DROP PROCEDURE sp_DsHopDong;
GO
CREATE PROCEDURE sp_DsHopDong
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pd.MaPhieu, pd.MaGiuong, pd.NgayDangKy, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai,
           p.MaPhong, p.GiaPhong, t.TenToa, d.TenDot, d.HocKy,
           (SELECT TOP 1 MaDon FROM DONYEUCAU
            WHERE MaPhieu = pd.MaPhieu AND LoaiDon = 'TraPhong' AND TrangThai IN ('ChoXuLy','DangXuLy')) AS MaDonTraPhongChoXL
    FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN PHONG p  ON p.MaPhong  = g.MaPhong
    JOIN TOANHA t ON t.MaToa    = p.MaToa
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE pd.MSSV = @MSSV ORDER BY pd.NgayDangKy DESC;
END
GO

IF OBJECT_ID('sp_KiemTraHopDong', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraHopDong;
GO
CREATE PROCEDURE sp_KiemTraHopDong
    @MaPhieu INT,
    @MSSV    VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM PHIEUDANGKY WHERE MaPhieu = @MaPhieu AND MSSV = @MSSV AND TrangThai = 'DangO';
END
GO

IF OBJECT_ID('sp_DemDotDangMo', 'P') IS NOT NULL DROP PROCEDURE sp_DemDotDangMo;
GO
CREATE PROCEDURE sp_DemDotDangMo
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM DOTDANGKY WHERE GETDATE() BETWEEN NgayMo AND NgayDong;
END
GO

IF OBJECT_ID('sp_GiaHan', 'P') IS NOT NULL DROP PROCEDURE sp_GiaHan;
GO
CREATE PROCEDURE sp_GiaHan
    @MaPhieu INT,
    @SoThang INT,
    @MSSV    VARCHAR(10),
    @NoiDung NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE PHIEUDANGKY SET NgayKetThuc = DATEADD(MONTH, @SoThang, NgayKetThuc) WHERE MaPhieu = @MaPhieu;
        INSERT INTO THONGBAO (MSSV, NoiDung, Kenh) VALUES (@MSSV, @NoiDung, 'Email');
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 - Yêu cầu trả phòng
   ========================================================================== */
IF OBJECT_ID('sp_ThongTinHopDong', 'P') IS NOT NULL DROP PROCEDURE sp_ThongTinHopDong;
GO
CREATE PROCEDURE sp_ThongTinHopDong
    @MaPhieu INT,
    @MSSV    VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pd.MaGiuong, g.MaPhong FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE pd.MaPhieu = @MaPhieu AND pd.MSSV = @MSSV AND pd.TrangThai = 'DangO';
END
GO

IF OBJECT_ID('sp_KiemTraNoHoaDon', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraNoHoaDon;
GO
CREATE PROCEDURE sp_KiemTraNoHoaDon
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM HOADON WHERE MaPhong = @MaPhong AND TrangThai IN ('ChoThanhToan','QuaHan');
END
GO

/* ============================================================================
   Đơn phản hồi / đề xuất (sinh viên)
   ========================================================================== */
IF OBJECT_ID('sp_TaoDon', 'P') IS NOT NULL DROP PROCEDURE sp_TaoDon;
GO
CREATE PROCEDURE sp_TaoDon
    @MSSV    VARCHAR(10),
    @LoaiDon VARCHAR(10),
    @TieuDe  NVARCHAR(200),
    @NoiDung NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO DONYEUCAU (MSSV, LoaiDon, TieuDe, NoiDung) VALUES (@MSSV, @LoaiDon, @TieuDe, @NoiDung);
END
GO

IF OBJECT_ID('sp_DsDonSV', 'P') IS NOT NULL DROP PROCEDURE sp_DsDonSV;
GO
CREATE PROCEDURE sp_DsDonSV
    @MSSV    VARCHAR(10),
    @LoaiDon VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaDon, TieuDe, MucUuTien, TrangThai, NgayTao
    FROM DONYEUCAU WHERE MSSV = @MSSV AND LoaiDon = @LoaiDon
    ORDER BY NgayTao DESC;
END
GO

IF OBJECT_ID('sp_ChiTietDonSV', 'P') IS NOT NULL DROP PROCEDURE sp_ChiTietDonSV;
GO
CREATE PROCEDURE sp_ChiTietDonSV
    @MaDon INT,
    @MSSV  VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.*, q.HoTen AS TenNV FROM DONYEUCAU d
    LEFT JOIN QUANLY q ON q.MaNV = d.MaNV
    WHERE d.MaDon = @MaDon AND d.MSSV = @MSSV;
END
GO

/* ============================================================================
 /17 - Lịch sử + tra cứu hóa đơn (sinh viên)
   ========================================================================== */
IF OBJECT_ID('sp_DsHoaDonSV', 'P') IS NOT NULL DROP PROCEDURE sp_DsHoaDonSV;
GO
CREATE PROCEDURE sp_DsHoaDonSV
    @MSSV      VARCHAR(10),
    @Thang     VARCHAR(7)  = NULL,
    @TrangThai VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT h.MaHD, h.MaPhong, h.Thang, h.TongTien, h.NgayPhatHanh, h.HanThanhToan, h.TrangThai
    FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.MSSV = @MSSV
    WHERE h.TrangThai <> 'Nhap'
      AND (@Thang IS NULL OR h.Thang = @Thang)
      AND (@TrangThai IS NULL OR h.TrangThai = @TrangThai)
    ORDER BY h.MaHD DESC;
END
GO

/* ============================================================================
 - Chi tiết hóa đơn & thanh toán
   ========================================================================== */
IF OBJECT_ID('sp_ChiTietHoaDon', 'P') IS NOT NULL DROP PROCEDURE sp_ChiTietHoaDon;
GO
CREATE PROCEDURE sp_ChiTietHoaDon
    @MaHD INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.*, c.DienDauKy, c.DienCuoiKy, c.NuocDauKy, c.NuocCuoiKy,
           dg.GiaDien, dg.GiaNuoc, dg.PhiDichVu, t.TenToa
    FROM HOADON h
    JOIN CHISODIENNUOC c ON c.MaChiSo = h.MaChiSo
    JOIN DONGIA dg ON dg.MaDonGia = h.MaDonGia
    JOIN PHONG p ON p.MaPhong = h.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE h.MaHD = @MaHD;
END
GO

IF OBJECT_ID('sp_DsGiaoDich', 'P') IS NOT NULL DROP PROCEDURE sp_DsGiaoDich;
GO
CREATE PROCEDURE sp_DsGiaoDich
    @MaHD INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM THANHTOAN WHERE MaHD = @MaHD ORDER BY ThoiGian DESC;
END
GO

IF OBJECT_ID('sp_KiemTraHoaDon', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraHoaDon;
GO
CREATE PROCEDURE sp_KiemTraHoaDon
    @MaHD INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TongTien, TrangThai FROM HOADON WHERE MaHD = @MaHD;
END
GO

-- Mô phỏng cổng thanh toán trực tuyến - ghi giao dịch + cập nhật hóa đơn + gửi thông báo
IF OBJECT_ID('sp_ThanhToan', 'P') IS NOT NULL DROP PROCEDURE sp_ThanhToan;
GO
CREATE PROCEDURE sp_ThanhToan
    @MaHD       INT,
    @MSSV       VARCHAR(10),
    @PhuongThuc VARCHAR(20),
    @SoTien     DECIMAL(12,0),
    @MaGDCong   VARCHAR(30),
    @NoiDungTB  NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO THANHTOAN (MaHD, MSSV, PhuongThuc, SoTien, MaGDCong, KetQua)
        VALUES (@MaHD, @MSSV, @PhuongThuc, @SoTien, @MaGDCong, 'ThanhCong');

        UPDATE HOADON SET TrangThai = 'DaThanhToan' WHERE MaHD = @MaHD;

        INSERT INTO THONGBAO (MSSV, MaHD, NoiDung, Kenh) VALUES (@MSSV, @MaHD, @NoiDungTB, 'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 /20/21 - Sinh viên vi phạm + mở khóa tài khoản
   ========================================================================== */

-- Job mô phỏng: cập nhật hóa đơn quá hạn
IF OBJECT_ID('sp_CapNhatHoaDonQuaHan', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatHoaDonQuaHan;
GO
CREATE PROCEDURE sp_CapNhatHoaDonQuaHan
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HOADON SET TrangThai = 'QuaHan'
    WHERE TrangThai = 'ChoThanhToan' AND HanThanhToan < CAST(GETDATE() AS DATE);
END
GO

--: khóa tài khoản khi hóa đơn quá hạn >= TS3 ngày
IF OBJECT_ID('sp_KhoaTaiKhoanQuaHan', 'P') IS NOT NULL DROP PROCEDURE sp_KhoaTaiKhoanQuaHan;
GO
CREATE PROCEDURE sp_KhoaTaiKhoanQuaHan
    @TS3 INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE tk SET tk.TrangThai = 'BiKhoa'
    FROM TAIKHOAN tk
    JOIN SINHVIEN sv ON sv.MaTK = tk.MaTK
    JOIN PHIEUDANGKY pd ON pd.MSSV = sv.MSSV AND pd.TrangThai = 'DangO'
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN HOADON h ON h.MaPhong = g.MaPhong
    WHERE h.TrangThai = 'QuaHan'
      AND DATEDIFF(DAY, h.HanThanhToan, GETDATE()) >= @TS3
      AND tk.TrangThai = 'HoatDong';
END
GO

--: ghi nhận vi phạm khi quá hạn > TS4 ngày
IF OBJECT_ID('sp_GhiNhanViPham', 'P') IS NOT NULL DROP PROCEDURE sp_GhiNhanViPham;
GO
CREATE PROCEDURE sp_GhiNhanViPham
    @TS4 INT,
    @TS6 INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO VIPHAM (MSSV, MaHD, SoDiem, LyDo)
    SELECT sv.MSSV, h.MaHD, @TS6,
 N'Hóa đơn ' + h.Thang + N' quá hạn thanh toán trên ' + CAST(@TS4 AS NVARCHAR) + N' ngày'
    FROM HOADON h
    JOIN GIUONG g ON g.MaPhong = h.MaPhong
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.TrangThai = 'DangO'
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE h.TrangThai = 'QuaHan'
      AND DATEDIFF(DAY, h.HanThanhToan, GETDATE()) > @TS4
      AND NOT EXISTS (SELECT 1 FROM VIPHAM v WHERE v.MaHD = h.MaHD AND v.MSSV = sv.MSSV);
END
GO

-- Cập nhật thuộc tính dẫn xuất DiemViPham
IF OBJECT_ID('sp_CapNhatDiemViPham', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatDiemViPham;
GO
CREATE PROCEDURE sp_CapNhatDiemViPham
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sv SET sv.DiemViPham = ISNULL((SELECT SUM(SoDiem) FROM VIPHAM v WHERE v.MSSV = sv.MSSV), 0)
    FROM SINHVIEN sv;
END
GO

IF OBJECT_ID('sp_DsViPham', 'P') IS NOT NULL DROP PROCEDURE sp_DsViPham;
GO
CREATE PROCEDURE sp_DsViPham
    @TuKhoa NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT sv.MSSV, sv.HoTen, sv.KhoaHoc, sv.DiemViPham, tk.TrangThai AS TrangThaiTK, tk.MaTK
    FROM SINHVIEN sv JOIN TAIKHOAN tk ON tk.MaTK = sv.MaTK
    WHERE (sv.DiemViPham > 0 OR tk.TrangThai = 'BiKhoa')
      AND (@TuKhoa IS NULL OR sv.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%')
    ORDER BY sv.DiemViPham DESC;
END
GO

IF OBJECT_ID('sp_LichSuViPham', 'P') IS NOT NULL DROP PROCEDURE sp_LichSuViPham;
GO
CREATE PROCEDURE sp_LichSuViPham
AS
BEGIN
    SET NOCOUNT ON;
    SELECT vp.*, sv.HoTen FROM VIPHAM vp
    JOIN SINHVIEN sv ON sv.MSSV = vp.MSSV ORDER BY vp.NgayGhiNhan DESC;
END
GO

--: mở khóa tài khoản + gửi thông báo (dùng OUTPUT lấy MSSV ngay trong câu UPDATE)
IF OBJECT_ID('sp_MoKhoa', 'P') IS NOT NULL DROP PROCEDURE sp_MoKhoa;
GO
CREATE PROCEDURE sp_MoKhoa
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MSSV VARCHAR(10);
    SELECT @MSSV = MSSV FROM SINHVIEN WHERE MaTK = @MaTK;

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE TAIKHOAN SET TrangThai = 'HoatDong' WHERE MaTK = @MaTK;

        IF @MSSV IS NOT NULL
            INSERT INTO THONGBAO (MSSV, NoiDung, Kenh)
            VALUES (@MSSV, N'Tài khoản của bạn đã được mở khóa.', 'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 /23/24 - Danh sách, sắp xếp ưu tiên, xử lý đơn phản hồi/yêu cầu (QL)
   ========================================================================== */
IF OBJECT_ID('sp_DsDon', 'P') IS NOT NULL DROP PROCEDURE sp_DsDon;
GO
CREATE PROCEDURE sp_DsDon
    @TuKhoa    NVARCHAR(100) = NULL,
    @LoaiDon   VARCHAR(10)   = NULL,
    @TrangThai VARCHAR(20)   = NULL,
    @UuTien    VARCHAR(10)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.MaDon, d.TieuDe, d.LoaiDon, d.MucUuTien, d.TrangThai, d.NgayTao,
           sv.HoTen, sv.MSSV
    FROM DONYEUCAU d JOIN SINHVIEN sv ON sv.MSSV = d.MSSV
    WHERE (@TuKhoa IS NULL OR d.TieuDe LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR sv.MSSV LIKE '%' + @TuKhoa + '%')
      AND (@LoaiDon IS NULL OR d.LoaiDon = @LoaiDon)
      AND (@TrangThai IS NULL OR d.TrangThai = @TrangThai)
      AND (@UuTien IS NULL OR d.MucUuTien = @UuTien)
    ORDER BY CASE d.MucUuTien WHEN 'Cao' THEN 0 WHEN 'TrungBinh' THEN 1 ELSE 2 END,
             CASE d.TrangThai WHEN 'ChoXuLy' THEN 0 WHEN 'DangXuLy' THEN 1 ELSE 2 END, d.NgayTao DESC;
END
GO

IF OBJECT_ID('sp_ChiTietDon', 'P') IS NOT NULL DROP PROCEDURE sp_ChiTietDon;
GO
CREATE PROCEDURE sp_ChiTietDon
    @MaDon INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.*, sv.HoTen, q.HoTen AS TenNV FROM DONYEUCAU d
    JOIN SINHVIEN sv ON sv.MSSV = d.MSSV
    LEFT JOIN QUANLY q ON q.MaNV = d.MaNV
    WHERE d.MaDon = @MaDon;
END
GO

-- Xử lý đơn: đổi ưu tiên/trạng thái/phản hồi + tự động gửi thông báo
IF OBJECT_ID('sp_XuLyDon', 'P') IS NOT NULL DROP PROCEDURE sp_XuLyDon;
GO
CREATE PROCEDURE sp_XuLyDon
    @MaDon     INT,
    @MucUuTien VARCHAR(10),
    @TrangThai VARCHAR(20),
    @PhanHoi   NVARCHAR(1000) = NULL,
    @MaNV      VARCHAR(10)    = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MSSV VARCHAR(10), @LoaiDon VARCHAR(10);
    SELECT @MSSV = MSSV, @LoaiDon = LoaiDon FROM DONYEUCAU WHERE MaDon = @MaDon;
    IF @MSSV IS NULL RETURN;

    IF @LoaiDon = 'TraPhong' AND @TrangThai = 'DaXuLy'
    BEGIN
        RAISERROR(N'Đơn trả phòng phải dùng chức năng "Xác nhận trả phòng" để đảm bảo giường được giải phóng đúng cách.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE DONYEUCAU SET MucUuTien = @MucUuTien, TrangThai = @TrangThai, PhanHoi = @PhanHoi, MaNV = @MaNV
        WHERE MaDon = @MaDon;

        INSERT INTO THONGBAO (MSSV, NoiDung, Kenh)
        VALUES (@MSSV,
                N'Đơn #' + CAST(@MaDon AS NVARCHAR) + N' của bạn đã được cập nhật trạng thái: ' + @TrangThai +
                CASE WHEN @PhanHoi IS NULL OR @PhanHoi = '' THEN '' ELSE N' Phản hồi: ' + @PhanHoi END,
                'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 /26/27 - Danh sách, tra cứu, chi tiết đơn đăng ký ở KTX (QL)
   ========================================================================== */
IF OBJECT_ID('sp_DsPhieu', 'P') IS NOT NULL DROP PROCEDURE sp_DsPhieu;
GO
CREATE PROCEDURE sp_DsPhieu
    @TuKhoa    NVARCHAR(100) = NULL,
    @TrangThai VARCHAR(20)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pd.MaPhieu, pd.NgayDangKy, pd.NgayBatDau, pd.NgayKetThuc, pd.TrangThai,
           sv.MSSV, sv.HoTen, g.MaPhong, pd.MaGiuong, d.TenDot
    FROM PHIEUDANGKY pd
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE (@TuKhoa IS NULL OR sv.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR g.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@TrangThai IS NULL OR pd.TrangThai = @TrangThai)
    ORDER BY CASE pd.TrangThai WHEN 'ChoDoiChieu' THEN 0 ELSE 1 END, pd.NgayDangKy DESC;
END
GO

IF OBJECT_ID('sp_ChiTietPhieu', 'P') IS NOT NULL DROP PROCEDURE sp_ChiTietPhieu;
GO
CREATE PROCEDURE sp_ChiTietPhieu
    @MaPhieu INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pd.*, sv.HoTen, sv.KhoaHoc, sv.GioiTinh, sv.DoiTuong, sv.DiemViPham,
           g.MaPhong, p.GiaPhong, p.LoaiPhong, t.TenToa, d.TenDot, d.HocKy
    FROM PHIEUDANGKY pd
    JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    JOIN PHONG p ON p.MaPhong = g.MaPhong
    JOIN TOANHA t ON t.MaToa = p.MaToa
    JOIN DOTDANGKY d ON d.MaDot = pd.MaDot
    WHERE pd.MaPhieu = @MaPhieu;
END
GO

/* ============================================================================
 - Xác nhận hoàn tất nhận phòng / Từ chối - hủy phiếu
   ========================================================================== */
IF OBJECT_ID('sp_XacNhanNhanPhong', 'P') IS NOT NULL DROP PROCEDURE sp_XacNhanNhanPhong;
GO
CREATE PROCEDURE sp_XacNhanNhanPhong
    @MaPhieu INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MSSV VARCHAR(10);
    SELECT @MSSV = MSSV FROM PHIEUDANGKY WHERE MaPhieu = @MaPhieu AND TrangThai = 'ChoDoiChieu';
    IF @MSSV IS NULL RETURN;

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE PHIEUDANGKY SET TrangThai = 'DangO' WHERE MaPhieu = @MaPhieu;

        INSERT INTO THONGBAO (MSSV, NoiDung, Kenh)
        VALUES (@MSSV, N'Phiếu đăng ký #' + CAST(@MaPhieu AS NVARCHAR) +
                N': đã hoàn tất thủ tục nhận phòng. Chào mừng bạn đến với KTX!', 'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('sp_HuyPhieu', 'P') IS NOT NULL DROP PROCEDURE sp_HuyPhieu;
GO
CREATE PROCEDURE sp_HuyPhieu
    @MaPhieu INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MSSV VARCHAR(10), @MaGiuong VARCHAR(20), @MaPhong VARCHAR(15);
    SELECT @MSSV = pd.MSSV, @MaGiuong = pd.MaGiuong, @MaPhong = g.MaPhong
    FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE pd.MaPhieu = @MaPhieu AND pd.TrangThai = 'ChoDoiChieu';
    IF @MSSV IS NULL RETURN;

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE PHIEUDANGKY SET TrangThai = 'DaHuy' WHERE MaPhieu = @MaPhieu;
        UPDATE GIUONG SET TrangThai = 'Trong' WHERE MaGiuong = @MaGiuong;
        UPDATE PHONG SET SoGiuongTrong = SoGiuongTrong + 1 WHERE MaPhong = @MaPhong;

        INSERT INTO THONGBAO (MSSV, NoiDung, Kenh)
        VALUES (@MSSV, N'Phiếu đăng ký #' + CAST(@MaPhieu AS NVARCHAR) + N' đã bị hủy.', 'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 /29 - Danh sách + tra cứu phòng (Quản lý)
   ========================================================================== */
IF OBJECT_ID('sp_DsPhongQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_DsPhongQuanLy;
GO
CREATE PROCEDURE sp_DsPhongQuanLy
    @TuKhoa NVARCHAR(50) = NULL,
    @MaToa  VARCHAR(10)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, t.TenToa FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
    ORDER BY p.MaToa, p.Tang, p.MaPhong;
END
GO

IF OBJECT_ID('sp_DsToaQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_DsToaQuanLy;
GO
CREATE PROCEDURE sp_DsToaQuanLy
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;
END
GO

/* ============================================================================
 /33 - Danh sách giường + cập nhật trạng thái giường
   ========================================================================== */
IF OBJECT_ID('sp_DsGiuongChiTietQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_DsGiuongChiTietQuanLy;
GO
CREATE PROCEDURE sp_DsGiuongChiTietQuanLy
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.MaGiuong, g.TrangThai, sv.HoTen, sv.MSSV
    FROM GIUONG g
    LEFT JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong AND pd.TrangThai IN ('DangO','ChoDoiChieu')
    LEFT JOIN SINHVIEN sv ON sv.MSSV = pd.MSSV
    WHERE g.MaPhong = @MaPhong ORDER BY g.MaGiuong;
END
GO

IF OBJECT_ID('sp_KiemTraGiuongDangO', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraGiuongDangO;
GO
CREATE PROCEDURE sp_KiemTraGiuongDangO
    @MaGiuong VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM PHIEUDANGKY WHERE MaGiuong = @MaGiuong AND TrangThai IN ('DangO','ChoDoiChieu');
END
GO

IF OBJECT_ID('sp_CapNhatGiuong', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatGiuong;
GO
CREATE PROCEDURE sp_CapNhatGiuong
    @MaGiuong  VARCHAR(20),
    @MaPhong   VARCHAR(15),
    @TrangThai VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE GIUONG SET TrangThai = @TrangThai WHERE MaGiuong = @MaGiuong;
        UPDATE PHONG SET SoGiuongTrong = (SELECT COUNT(*) FROM GIUONG WHERE MaPhong = @MaPhong AND TrangThai = 'Trong')
        WHERE MaPhong = @MaPhong;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 - Nhập chỉ số điện/nước
   ========================================================================== */
IF OBJECT_ID('sp_DsPhongChiSo', 'P') IS NOT NULL DROP PROCEDURE sp_DsPhongChiSo;
GO
CREATE PROCEDURE sp_DsPhongChiSo
    @Thang VARCHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.MaPhong, p.SoGiuong - p.SoGiuongTrong AS SoNguoiO,
           cs.MaChiSo, cs.DienDauKy, cs.DienCuoiKy, cs.NuocDauKy, cs.NuocCuoiKy, cs.TrangThai,
           truoc.DienCuoiKy AS DienKyTruoc, truoc.NuocCuoiKy AS NuocKyTruoc
    FROM PHONG p
    LEFT JOIN CHISODIENNUOC cs ON cs.MaPhong = p.MaPhong AND cs.Thang = @Thang
    OUTER APPLY (SELECT TOP 1 DienCuoiKy, NuocCuoiKy FROM CHISODIENNUOC
                 WHERE MaPhong = p.MaPhong AND Thang <> @Thang
                 ORDER BY RIGHT(Thang,4) DESC, LEFT(Thang,2) DESC) truoc
    WHERE p.TrangThai = 'HoatDong'
      AND EXISTS (SELECT 1 FROM GIUONG g JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
                  WHERE g.MaPhong = p.MaPhong AND pd.TrangThai = 'DangO')
    ORDER BY p.MaPhong;
END
GO

IF OBJECT_ID('sp_KiemTraDaChot', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraDaChot;
GO
CREATE PROCEDURE sp_KiemTraDaChot
    @MaPhong VARCHAR(15),
    @Thang   VARCHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM CHISODIENNUOC WHERE MaPhong = @MaPhong AND Thang = @Thang AND TrangThai = 'DaChot';
END
GO

--: chỉ số cuối kỳ >= đầu kỳ được kiểm tra ở tầng ứng dụng trước khi gọi SP này
IF OBJECT_ID('sp_LuuChiSo', 'P') IS NOT NULL DROP PROCEDURE sp_LuuChiSo;
GO
CREATE PROCEDURE sp_LuuChiSo
    @MaPhong    VARCHAR(15),
    @Thang      VARCHAR(7),
    @DienDauKy  DECIMAL(10,1),
    @DienCuoiKy DECIMAL(10,1),
    @NuocDauKy  DECIMAL(10,1),
    @NuocCuoiKy DECIMAL(10,1)
AS
BEGIN
    SET NOCOUNT ON;
    MERGE CHISODIENNUOC AS target
    USING (SELECT @MaPhong AS MaPhong, @Thang AS Thang) AS src
    ON target.MaPhong = src.MaPhong AND target.Thang = src.Thang
    WHEN MATCHED THEN UPDATE SET DienDauKy=@DienDauKy, DienCuoiKy=@DienCuoiKy, NuocDauKy=@NuocDauKy, NuocCuoiKy=@NuocCuoiKy
    WHEN NOT MATCHED THEN INSERT (MaPhong, Thang, DienDauKy, DienCuoiKy, NuocDauKy, NuocCuoiKy)
                          VALUES (@MaPhong, @Thang, @DienDauKy, @DienCuoiKy, @NuocDauKy, @NuocCuoiKy);
END
GO

/* ============================================================================
 /36 - Lập & gửi hóa đơn
   ========================================================================== */
IF OBJECT_ID('sp_DonGiaHieuLuc', 'P') IS NOT NULL DROP PROCEDURE sp_DonGiaHieuLuc;
GO
CREATE PROCEDURE sp_DonGiaHieuLuc
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 * FROM DONGIA WHERE TrangThai = 'HieuLuc' ORDER BY NgayApDung DESC;
END
GO

-- Dùng chung cho cả bước "Xem trước" và bước "Tạo hóa đơn hàng loạt" (cùng 1 tập nguồn dữ liệu)
IF OBJECT_ID('sp_NguonTaoHoaDon', 'P') IS NOT NULL DROP PROCEDURE sp_NguonTaoHoaDon;
GO
CREATE PROCEDURE sp_NguonTaoHoaDon
    @Thang VARCHAR(7)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Nam INT = CAST(RIGHT(@Thang, 4) AS INT);
    DECLARE @ThangSo INT = CAST(LEFT(@Thang, CHARINDEX('/', @Thang) - 1) AS INT);
    DECLARE @DauThang DATE = DATEFROMPARTS(@Nam, @ThangSo, 1);

    -- SoNguoiO chỉ đếm SV đã ở TRỌN VẸN từ đầu tháng đó (dọn vào từ trước hoặc
    -- đúng ngày đầu tháng). SV mới dọn vào giữa/cuối tháng chưa tính vào tháng
    -- này, bắt đầu tính từ tháng kế tiếp (đợt lập hóa đơn đầu mỗi tháng).
    SELECT cs.MaChiSo, cs.MaPhong, cs.DienDauKy, cs.DienCuoiKy, cs.NuocDauKy, cs.NuocCuoiKy,
           cs.DienCuoiKy - cs.DienDauKy AS Kwh, cs.NuocCuoiKy - cs.NuocDauKy AS M3,
           p.GiaPhong,
           (SELECT COUNT(*) FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
            WHERE g.MaPhong = p.MaPhong AND pd.TrangThai = 'DangO' AND pd.NgayBatDau <= @DauThang) AS SoNguoiO
    FROM CHISODIENNUOC cs
    JOIN PHONG p ON p.MaPhong = cs.MaPhong
    WHERE cs.Thang = @Thang AND cs.TrangThai = 'Nhap'
      AND NOT EXISTS (SELECT 1 FROM HOADON h WHERE h.MaChiSo = cs.MaChiSo)
    ORDER BY cs.MaPhong;
END
GO

IF OBJECT_ID('sp_DsNhap', 'P') IS NOT NULL DROP PROCEDURE sp_DsNhap;
GO
CREATE PROCEDURE sp_DsNhap
AS
BEGIN
    SET NOCOUNT ON;
    SELECT h.MaHD, h.MaPhong, h.Thang, h.TongTien, h.HanThanhToan
    FROM HOADON h WHERE h.TrangThai = 'Nhap' ORDER BY h.MaPhong;
END
GO

-- Tạo 1 dòng hóa đơn nháp + chốt chỉ số - C# lặp qua từng phòng và gọi SP này
IF OBJECT_ID('sp_TaoHoaDonDong', 'P') IS NOT NULL DROP PROCEDURE sp_TaoHoaDonDong;
GO
CREATE PROCEDURE sp_TaoHoaDonDong
    @MaPhong       VARCHAR(15),
    @MaChiSo       INT,
    @MaDonGia      INT,
    @Thang         VARCHAR(7),
    @TienPhong     DECIMAL(12,0),
    @TienDien      DECIMAL(12,0),
    @TienNuoc      DECIMAL(12,0),
    @TongTien      DECIMAL(12,0),
    @HanThanhToan  DATE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO HOADON (MaPhong, MaChiSo, MaDonGia, Thang, TienPhong, TienDien, TienNuoc, TongTien, HanThanhToan, TrangThai)
        VALUES (@MaPhong, @MaChiSo, @MaDonGia, @Thang, @TienPhong, @TienDien, @TienNuoc, @TongTien, @HanThanhToan, 'Nhap');

        UPDATE CHISODIENNUOC SET TrangThai = 'DaChot' WHERE MaChiSo = @MaChiSo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

IF OBJECT_ID('sp_ThongTinHoaDonNhap', 'P') IS NOT NULL DROP PROCEDURE sp_ThongTinHoaDonNhap;
GO
CREATE PROCEDURE sp_ThongTinHoaDonNhap
    @MaHD INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaPhong, Thang, TongTien, HanThanhToan FROM HOADON WHERE MaHD = @MaHD AND TrangThai = 'Nhap';
END
GO

IF OBJECT_ID('sp_DsSVTrongPhong', 'P') IS NOT NULL DROP PROCEDURE sp_DsSVTrongPhong;
GO
CREATE PROCEDURE sp_DsSVTrongPhong
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT pd.MSSV FROM PHIEUDANGKY pd
    JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai = 'DangO';
END
GO

--: xác nhận gửi hóa đơn (chuyển trạng thái Nhap -> ChoThanhToan)
IF OBJECT_ID('sp_GuiHoaDon', 'P') IS NOT NULL DROP PROCEDURE sp_GuiHoaDon;
GO
CREATE PROCEDURE sp_GuiHoaDon
    @MaHD INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE HOADON SET TrangThai = 'ChoThanhToan', NgayPhatHanh = GETDATE() WHERE MaHD = @MaHD;
END
GO

/* ============================================================================
 /38 - Lịch sử thông báo hóa đơn + tra cứu
   ========================================================================== */
IF OBJECT_ID('sp_DsThongBao', 'P') IS NOT NULL DROP PROCEDURE sp_DsThongBao;
GO
CREATE PROCEDURE sp_DsThongBao
    @TuKhoa NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tb.MaTB, tb.MSSV, sv.HoTen, tb.MaHD, tb.NoiDung, tb.Kenh, tb.ThoiGianGui, tb.TrangThaiGui
    FROM THONGBAO tb JOIN SINHVIEN sv ON sv.MSSV = tb.MSSV
    WHERE (@TuKhoa IS NULL OR tb.MSSV LIKE '%' + @TuKhoa + '%' OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR tb.NoiDung LIKE '%' + @TuKhoa + '%')
    ORDER BY tb.ThoiGianGui DESC;
END
GO

/* ============================================================================
 - Thống kê tổng quan hệ thống
   Gộp 6 chỉ số + 3 bảng biểu đồ vào 1 SP, trả về 4 result set.
   ========================================================================== */
IF OBJECT_ID('sp_ThongKe', 'P') IS NOT NULL DROP PROCEDURE sp_ThongKe;
GO
CREATE PROCEDURE sp_ThongKe
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: 6 chỉ số tổng quan (1 dòng, 6 cột)
    SELECT
        (SELECT COUNT(*) FROM PHONG) AS TongPhong,
        (SELECT COUNT(*) FROM PHIEUDANGKY WHERE TrangThai = 'DangO') AS SVDangO,
        (SELECT COUNT(*) FROM HOADON WHERE TrangThai IN ('ChoThanhToan','QuaHan')) AS HDChuaTT,
        (SELECT COUNT(*) FROM DONYEUCAU WHERE TrangThai = 'ChoXuLy') AS DonChoXuLy,
        (SELECT ISNULL(SUM(SoGiuongTrong),0) FROM PHONG WHERE TrangThai = 'HoatDong') AS GiuongTrong,
        (SELECT ISNULL(SUM(SoTien),0) FROM THANHTOAN WHERE KetQua = 'ThanhCong') AS TongThu;

    -- Result set 2: Doanh thu theo tháng
    SELECT FORMAT(ThoiGian, 'MM/yyyy') AS Thang, SUM(SoTien) AS TongTien
    FROM THANHTOAN WHERE KetQua = 'ThanhCong'
    GROUP BY FORMAT(ThoiGian, 'MM/yyyy'), YEAR(ThoiGian), MONTH(ThoiGian)
    ORDER BY YEAR(ThoiGian), MONTH(ThoiGian);

    -- Result set 3: Sản lượng điện nước theo tháng
    SELECT Thang, SUM(DienCuoiKy - DienDauKy) AS Kwh, SUM(NuocCuoiKy - NuocDauKy) AS M3
    FROM CHISODIENNUOC
    GROUP BY Thang ORDER BY RIGHT(Thang,4), LEFT(Thang,2);

    -- Result set 4: Đơn yêu cầu theo trạng thái
    SELECT TrangThai, COUNT(*) AS SoLuong FROM DONYEUCAU GROUP BY TrangThai;
END
GO

/* ============================================================================
 /40 - Danh sách + tra cứu phòng (Quản trị viên)
   ========================================================================== */
IF OBJECT_ID('sp_DsPhongQuanTri', 'P') IS NOT NULL DROP PROCEDURE sp_DsPhongQuanTri;
GO
CREATE PROCEDURE sp_DsPhongQuanTri
    @TuKhoa    NVARCHAR(50) = NULL,
    @MaToa     VARCHAR(10)  = NULL,
    @TrangThai VARCHAR(20)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.*, t.TenToa,
           (SELECT COUNT(*) FROM GIUONG g JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
            WHERE g.MaPhong = p.MaPhong AND pd.TrangThai IN ('DangO','ChoDoiChieu')) AS SoSVO
    FROM PHONG p JOIN TOANHA t ON t.MaToa = p.MaToa
    WHERE (@TuKhoa IS NULL OR p.MaPhong LIKE '%' + @TuKhoa + '%')
      AND (@MaToa IS NULL OR p.MaToa = @MaToa)
      AND (@TrangThai IS NULL OR p.TrangThai = @TrangThai)
    ORDER BY p.MaToa, p.Tang, p.MaPhong;
END
GO

IF OBJECT_ID('sp_DsToaChiTietQuanTri', 'P') IS NOT NULL DROP PROCEDURE sp_DsToaChiTietQuanTri;
GO
CREATE PROCEDURE sp_DsToaChiTietQuanTri
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaToa, TenToa, SoTang FROM TOANHA ORDER BY MaToa;
END
GO

/* ============================================================================
 - Thêm phòng mới
   ========================================================================== */
IF OBJECT_ID('sp_ThongTinToa', 'P') IS NOT NULL DROP PROCEDURE sp_ThongTinToa;
GO
CREATE PROCEDURE sp_ThongTinToa
    @MaToa VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SoTang FROM TOANHA WHERE MaToa = @MaToa;
END
GO

IF OBJECT_ID('sp_KiemTraTrungMaPhong', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraTrungMaPhong;
GO
CREATE PROCEDURE sp_KiemTraTrungMaPhong
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM PHONG WHERE MaPhong = @MaPhong;
END
GO

IF OBJECT_ID('sp_ThemPhong', 'P') IS NOT NULL DROP PROCEDURE sp_ThemPhong;
GO
CREATE PROCEDURE sp_ThemPhong
    @MaPhong  VARCHAR(15),
    @MaToa    VARCHAR(10),
    @Tang     INT,
    @LoaiPhong VARCHAR(2),
    @SoGiuong INT,
    @GiaPhong DECIMAL(10,0)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO PHONG (MaPhong, MaToa, Tang, LoaiPhong, SoGiuong, GiaPhong, TrangThai, SoGiuongTrong)
    VALUES (@MaPhong, @MaToa, @Tang, @LoaiPhong, @SoGiuong, @GiaPhong, 'HoatDong', @SoGiuong);
END
GO

IF OBJECT_ID('sp_ThemGiuong', 'P') IS NOT NULL DROP PROCEDURE sp_ThemGiuong;
GO
CREATE PROCEDURE sp_ThemGiuong
    @MaGiuong VARCHAR(20),
    @MaPhong  VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO GIUONG (MaGiuong, MaPhong) VALUES (@MaGiuong, @MaPhong);
END
GO

/* ============================================================================
 - Cập nhật phòng
   ========================================================================== */
IF OBJECT_ID('sp_CapNhatPhong', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatPhong;
GO
CREATE PROCEDURE sp_CapNhatPhong
    @MaPhong   VARCHAR(15),
    @GiaPhong  DECIMAL(10,0),
    @TrangThai VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PHONG SET GiaPhong = @GiaPhong, TrangThai = @TrangThai WHERE MaPhong = @MaPhong;
END
GO

/* ============================================================================
 - Xóa phòng
   ========================================================================== */
IF OBJECT_ID('sp_KiemTraDangO', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraDangO;
GO
CREATE PROCEDURE sp_KiemTraDangO
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM GIUONG g
    JOIN PHIEUDANGKY pd ON pd.MaGiuong = g.MaGiuong
    WHERE g.MaPhong = @MaPhong AND pd.TrangThai IN ('DangO','ChoDoiChieu');
END
GO

-- Gộp 3 điều kiện kiểm tra dữ liệu lịch sử (hóa đơn, chỉ số, phiếu đăng ký) vào 1 SP
IF OBJECT_ID('sp_KiemTraLichSu', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraLichSu;
GO
CREATE PROCEDURE sp_KiemTraLichSu
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(*) FROM HOADON WHERE MaPhong = @MaPhong) +
        (SELECT COUNT(*) FROM CHISODIENNUOC WHERE MaPhong = @MaPhong) +
        (SELECT COUNT(*) FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong WHERE g.MaPhong = @MaPhong)
        AS TongLichSu;
END
GO

IF OBJECT_ID('sp_NgungSuDung', 'P') IS NOT NULL DROP PROCEDURE sp_NgungSuDung;
GO
CREATE PROCEDURE sp_NgungSuDung
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PHONG SET TrangThai = 'NgungSuDung' WHERE MaPhong = @MaPhong;
END
GO

IF OBJECT_ID('sp_XoaPhong', 'P') IS NOT NULL DROP PROCEDURE sp_XoaPhong;
GO
CREATE PROCEDURE sp_XoaPhong
    @MaPhong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        DELETE FROM GIUONG WHERE MaPhong = @MaPhong;
        DELETE FROM PHONG WHERE MaPhong = @MaPhong;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 - Cập nhật đơn giá điện, nước
   ========================================================================== */
IF OBJECT_ID('sp_DsDonGia', 'P') IS NOT NULL DROP PROCEDURE sp_DsDonGia;
GO
CREATE PROCEDURE sp_DsDonGia
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM DONGIA ORDER BY NgayApDung DESC;
END
GO

--: đơn giá không vượt trần TS7 (điện), TS8 (nước) - đảm bảo chỉ 1 biểu giá hiệu lực
IF OBJECT_ID('sp_CapNhatDonGia', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatDonGia;
GO
CREATE PROCEDURE sp_CapNhatDonGia
    @GiaDien    DECIMAL(10,0),
    @GiaNuoc    DECIMAL(10,0),
    @PhiDichVu  DECIMAL(10,0),
    @NgayApDung DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TS7 DECIMAL(10,0), @TS8 DECIMAL(10,0);
    SELECT @TS7 = CAST(GiaTri AS DECIMAL(10,0)) FROM THAMSO WHERE MaThamSo = 'TS7';
    SELECT @TS8 = CAST(GiaTri AS DECIMAL(10,0)) FROM THAMSO WHERE MaThamSo = 'TS8';

    IF @GiaDien <= 0 OR @GiaDien > @TS7
    BEGIN
 RAISERROR(N'Đơn giá điện phải > 0 và không vượt trần TS7.', 16, 1);
        RETURN;
    END
    IF @GiaNuoc <= 0 OR @GiaNuoc > @TS8
    BEGIN
 RAISERROR(N'Đơn giá nước phải > 0 và không vượt trần TS8.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE DONGIA SET TrangThai = 'HetHieuLuc' WHERE TrangThai = 'HieuLuc';
        INSERT INTO DONGIA (GiaDien, GiaNuoc, PhiDichVu, NgayApDung, TrangThai)
        VALUES (@GiaDien, @GiaNuoc, @PhiDichVu, @NgayApDung, 'HieuLuc');
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ============================================================================
 - Thiết lập thời gian mở cổng đăng ký
   ========================================================================== */
IF OBJECT_ID('sp_CapNhatTrangThaiDot', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatTrangThaiDot;
GO
CREATE PROCEDURE sp_CapNhatTrangThaiDot
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE DOTDANGKY SET TrangThai =
        CASE WHEN GETDATE() < NgayMo THEN 'ChuaMo'
             WHEN GETDATE() BETWEEN NgayMo AND NgayDong THEN 'DangMo'
             ELSE 'DaDong' END;
END
GO

IF OBJECT_ID('sp_DsDot', 'P') IS NOT NULL DROP PROCEDURE sp_DsDot;
GO
CREATE PROCEDURE sp_DsDot
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM DOTDANGKY ORDER BY NgayMo DESC;
END
GO

-- /: đợt ưu tiên tự động đóng sau TS1 ngày (tính toán ngayDong ở tầng ứng dụng trước khi gọi)
IF OBJECT_ID('sp_TaoDot', 'P') IS NOT NULL DROP PROCEDURE sp_TaoDot;
GO
CREATE PROCEDURE sp_TaoDot
    @TenDot   NVARCHAR(200),
    @LoaiDot  VARCHAR(10),
    @HocKy    NVARCHAR(50),
    @NgayMo   DATETIME,
    @NgayDong DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO DOTDANGKY (TenDot, LoaiDot, HocKy, NgayMo, NgayDong, TrangThai)
    VALUES (@TenDot, @LoaiDot, @HocKy, @NgayMo, @NgayDong,
            CASE WHEN GETDATE() < @NgayMo THEN 'ChuaMo'
                 WHEN GETDATE() BETWEEN @NgayMo AND @NgayDong THEN 'DangMo' ELSE 'DaDong' END);
END
GO

IF OBJECT_ID('sp_DongDot', 'P') IS NOT NULL DROP PROCEDURE sp_DongDot;
GO
CREATE PROCEDURE sp_DongDot
    @MaDot INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE DOTDANGKY SET NgayDong = GETDATE(), TrangThai = 'DaDong' WHERE MaDot = @MaDot;
END
GO

/* ============================================================================
 /48 - Danh sách + tra cứu tài khoản
   ========================================================================== */
IF OBJECT_ID('sp_DsTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_DsTaiKhoan;
GO
CREATE PROCEDURE sp_DsTaiKhoan
    @TuKhoa    NVARCHAR(100) = NULL,
    @VaiTro    VARCHAR(5)    = NULL,
    @TrangThai VARCHAR(20)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tk.MaTK, tk.TenDangNhap, tk.Email, tk.SDT, tk.VaiTro, tk.TrangThai, tk.NgayTao,
           COALESCE(sv.HoTen, ql.HoTen, N'Quản trị viên') AS HoTen,
           sv.MSSV, sv.KhoaHoc, sv.DoiTuong,
           ql.MaNV, ql.MaToa
    FROM TAIKHOAN tk
    LEFT JOIN SINHVIEN sv ON sv.MaTK = tk.MaTK
    LEFT JOIN QUANLY ql ON ql.MaTK = tk.MaTK
    WHERE (@TuKhoa IS NULL OR tk.TenDangNhap LIKE '%' + @TuKhoa + '%' OR tk.Email LIKE '%' + @TuKhoa + '%'
           OR sv.HoTen LIKE '%' + @TuKhoa + '%' OR ql.HoTen LIKE '%' + @TuKhoa + '%')
      AND (@VaiTro IS NULL OR tk.VaiTro = @VaiTro)
      AND (@TrangThai IS NULL OR tk.TrangThai = @TrangThai)
    ORDER BY tk.MaTK;
END
GO

IF OBJECT_ID('sp_DsToaTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_DsToaTaiKhoan;
GO
CREATE PROCEDURE sp_DsToaTaiKhoan
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaToa, TenToa FROM TOANHA ORDER BY MaToa;
END
GO

/* ============================================================================
 - Tạo tài khoản
   ========================================================================== */
IF OBJECT_ID('sp_KiemTraTrung', 'P') IS NOT NULL DROP PROCEDURE sp_KiemTraTrung;
GO
CREATE PROCEDURE sp_KiemTraTrung
    @TenDangNhap VARCHAR(50),
    @Email       VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM TAIKHOAN WHERE TenDangNhap = @TenDangNhap OR Email = @Email;
END
GO

-- Tạo tài khoản, trả về MaTK vừa sinh (SCOPE_IDENTITY) để C# dùng insert SINHVIEN/QUANLY
IF OBJECT_ID('sp_TaoTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_TaoTaiKhoan;
GO
CREATE PROCEDURE sp_TaoTaiKhoan
    @TenDangNhap VARCHAR(50),
    @MatKhau     VARCHAR(100),
    @Email       VARCHAR(100),
    @SDT         VARCHAR(15) = NULL,
    @VaiTro      VARCHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, Email, SDT, VaiTro)
    VALUES (@TenDangNhap, @MatKhau, @Email, @SDT, @VaiTro);
    SELECT SCOPE_IDENTITY();
END
GO

IF OBJECT_ID('sp_ThemSinhVien', 'P') IS NOT NULL DROP PROCEDURE sp_ThemSinhVien;
GO
CREATE PROCEDURE sp_ThemSinhVien
    @MSSV     VARCHAR(10),
    @MaTK     INT,
    @HoTen    NVARCHAR(100),
    @KhoaHoc  VARCHAR(10),
    @DoiTuong VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO SINHVIEN (MSSV, MaTK, HoTen, KhoaHoc, DoiTuong) VALUES (@MSSV, @MaTK, @HoTen, @KhoaHoc, @DoiTuong);
END
GO

IF OBJECT_ID('sp_DemQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_DemQuanLy;
GO
CREATE PROCEDURE sp_DemQuanLy
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM QUANLY;
END
GO

IF OBJECT_ID('sp_ThemQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_ThemQuanLy;
GO
CREATE PROCEDURE sp_ThemQuanLy
    @MaNV  VARCHAR(10),
    @MaTK  INT,
    @HoTen NVARCHAR(100),
    @MaToa VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO QUANLY (MaNV, MaTK, HoTen, MaToa) VALUES (@MaNV, @MaTK, @HoTen, @MaToa);
END
GO

/* ============================================================================
 - Cập nhật tài khoản
   ========================================================================== */
IF OBJECT_ID('sp_CapNhatTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatTaiKhoan;
GO
CREATE PROCEDURE sp_CapNhatTaiKhoan
    @MaTK  INT,
    @Email VARCHAR(100),
    @SDT   VARCHAR(15) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TAIKHOAN SET Email = @Email, SDT = @SDT WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_DoiMatKhau', 'P') IS NOT NULL DROP PROCEDURE sp_DoiMatKhau;
GO
CREATE PROCEDURE sp_DoiMatKhau
    @MaTK       INT,
    @MatKhauMoi VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TAIKHOAN SET MatKhau = @MatKhauMoi WHERE MaTK = @MaTK;
END
GO

/* ============================================================================
 - Xóa tài khoản
   ========================================================================== */
IF OBJECT_ID('sp_LayMSSV', 'P') IS NOT NULL DROP PROCEDURE sp_LayMSSV;
GO
CREATE PROCEDURE sp_LayMSSV
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MSSV FROM SINHVIEN WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_LayMaNV', 'P') IS NOT NULL DROP PROCEDURE sp_LayMaNV;
GO
CREATE PROCEDURE sp_LayMaNV
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MaNV FROM QUANLY WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_DemLienQuanSV', 'P') IS NOT NULL DROP PROCEDURE sp_DemLienQuanSV;
GO
CREATE PROCEDURE sp_DemLienQuanSV
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT (SELECT COUNT(*) FROM PHIEUDANGKY WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM DONYEUCAU WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM THANHTOAN WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM THONGBAO WHERE MSSV = @MSSV)
         + (SELECT COUNT(*) FROM VIPHAM WHERE MSSV = @MSSV) AS TongLienQuan;
END
GO

IF OBJECT_ID('sp_DemLienQuanQL', 'P') IS NOT NULL DROP PROCEDURE sp_DemLienQuanQL;
GO
CREATE PROCEDURE sp_DemLienQuanQL
    @MaNV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM DONYEUCAU WHERE MaNV = @MaNV;
END
GO

IF OBJECT_ID('sp_KhoaTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_KhoaTaiKhoan;
GO
CREATE PROCEDURE sp_KhoaTaiKhoan
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TAIKHOAN SET TrangThai = 'BiKhoa' WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_XoaSinhVien', 'P') IS NOT NULL DROP PROCEDURE sp_XoaSinhVien;
GO
CREATE PROCEDURE sp_XoaSinhVien
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM SINHVIEN WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_XoaQuanLy', 'P') IS NOT NULL DROP PROCEDURE sp_XoaQuanLy;
GO
CREATE PROCEDURE sp_XoaQuanLy
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM QUANLY WHERE MaTK = @MaTK;
END
GO

IF OBJECT_ID('sp_XoaTaiKhoan', 'P') IS NOT NULL DROP PROCEDURE sp_XoaTaiKhoan;
GO
CREATE PROCEDURE sp_XoaTaiKhoan
    @MaTK INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM TAIKHOAN WHERE MaTK = @MaTK;
END
GO

PRINT N'Đã tạo xong toàn bộ Stored Procedure.';
GO


/* Sinh viên tạo yêu cầu trả phòng - chỉ ghi đơn, KHÔNG đụng phòng/giường */
IF OBJECT_ID('sp_TaoYeuCauTraPhong', 'P') IS NOT NULL DROP PROCEDURE sp_TaoYeuCauTraPhong;
GO
CREATE PROCEDURE sp_TaoYeuCauTraPhong
    @MSSV    VARCHAR(10),
    @MaPhieu INT,
    @TieuDe  NVARCHAR(200),
    @NoiDung NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO DONYEUCAU (MSSV, LoaiDon, TieuDe, NoiDung, MucUuTien, TrangThai, MaPhieu)
    VALUES (@MSSV, 'TraPhong', @TieuDe, @NoiDung, 'TrungBinh', 'ChoXuLy', @MaPhieu);
END
GO

/* Quản lý xác nhận trả phòng - lúc này mới thực sự giải phóng giường + đóng đơn */
IF OBJECT_ID('sp_XacNhanTraPhong', 'P') IS NOT NULL DROP PROCEDURE sp_XacNhanTraPhong;
GO
CREATE PROCEDURE sp_XacNhanTraPhong
    @MaDon INT,
    @MaNV  VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MSSV VARCHAR(10), @MaPhieu INT, @MaGiuong VARCHAR(20), @MaPhong VARCHAR(15);

    SELECT @MSSV = MSSV, @MaPhieu = MaPhieu FROM DONYEUCAU
    WHERE MaDon = @MaDon AND LoaiDon = 'TraPhong' AND TrangThai IN ('ChoXuLy', 'DangXuLy');
    IF @MaPhieu IS NULL RETURN;

    SELECT @MaGiuong = pd.MaGiuong, @MaPhong = g.MaPhong
    FROM PHIEUDANGKY pd JOIN GIUONG g ON g.MaGiuong = pd.MaGiuong
    WHERE pd.MaPhieu = @MaPhieu AND pd.TrangThai = 'DangO';

    IF @MaGiuong IS NULL
    BEGIN
        UPDATE DONYEUCAU SET TrangThai = 'TuChoi',
            PhanHoi = N'Hợp đồng không còn ở trạng thái đang ở, không thể xác nhận trả phòng.', MaNV = @MaNV
        WHERE MaDon = @MaDon;
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE PHIEUDANGKY SET TrangThai = 'DaTraPhong' WHERE MaPhieu = @MaPhieu;
        UPDATE GIUONG SET TrangThai = 'Trong' WHERE MaGiuong = @MaGiuong;
        UPDATE PHONG SET SoGiuongTrong = SoGiuongTrong + 1 WHERE MaPhong = @MaPhong;

        UPDATE DONYEUCAU SET TrangThai = 'DaXuLy',
            PhanHoi = N'Đã xác nhận trả phòng. Cảm ơn bạn đã bàn giao phòng đúng quy định.', MaNV = @MaNV
        WHERE MaDon = @MaDon;

        INSERT INTO THONGBAO (MSSV, NoiDung, Kenh)
        VALUES (@MSSV, N'Yêu cầu trả phòng #' + CAST(@MaDon AS NVARCHAR) + N' đã được xác nhận. Phòng đã được giải phóng.', 'Email');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* Cập nhật thông tin cá nhân (Họ tên, Khóa học/Đối tượng, Tòa nhà phụ trách)
   khi Quản trị viên sửa tài khoản - dùng chung cho modal Cập nhật tài khoản.
   Thử cập nhật cả 2 bảng theo MaTK, không phụ thuộc @VaiTro (an toàn hơn
   nếu tham số truyền lên bị sai/rỗng - MaTK chỉ khớp đúng 1 trong 2 bảng). */
IF OBJECT_ID('sp_CapNhatThongTinCaNhan', 'P') IS NOT NULL DROP PROCEDURE sp_CapNhatThongTinCaNhan;
GO
CREATE PROCEDURE sp_CapNhatThongTinCaNhan
    @MaTK     INT,
    @VaiTro   VARCHAR(5)  = NULL,
    @HoTen    NVARCHAR(100),
    @KhoaHoc  VARCHAR(10) = NULL,
    @DoiTuong VARCHAR(15) = NULL,
    @MaToa    VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE SINHVIEN SET HoTen = @HoTen, KhoaHoc = @KhoaHoc, DoiTuong = @DoiTuong WHERE MaTK = @MaTK;
    UPDATE QUANLY SET HoTen = @HoTen, MaToa = @MaToa WHERE MaTK = @MaTK;
END
GO

/* Banner thông báo đợt đăng ký đang mở cho sinh viên (Tổng quan / Tra cứu phòng) */
IF OBJECT_ID('sp_DotDangMoChoSV', 'P') IS NOT NULL DROP PROCEDURE sp_DotDangMoChoSV;
GO
CREATE PROCEDURE sp_DotDangMoChoSV
    @MSSV VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @KhoaHoc VARCHAR(10), @KhoaMoiNhat VARCHAR(10);
    SELECT @KhoaHoc = KhoaHoc FROM SINHVIEN WHERE MSSV = @MSSV;
    SELECT @KhoaMoiNhat = MAX(KhoaHoc) FROM SINHVIEN;

    SELECT MaDot, TenDot, LoaiDot, HocKy, NgayMo, NgayDong,
           CASE WHEN LoaiDot = 'UuTien' AND (@KhoaHoc IS NULL OR @KhoaHoc <> @KhoaMoiNhat)
                THEN 0 ELSE 1 END AS PhuHop
    FROM DOTDANGKY
    WHERE GETDATE() BETWEEN NgayMo AND NgayDong
    ORDER BY CASE LoaiDot WHEN 'UuTien' THEN 0 ELSE 1 END;
END
GO
