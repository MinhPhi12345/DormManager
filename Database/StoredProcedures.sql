/* =============================================================================
   STORED PROCEDURES - DormManager (Nhóm 19)   [KIẾN TRÚC HYBRID]

   Dự án áp dụng kiến trúc truy cập dữ liệu HYBRID: chỉ những nghiệp vụ THỰC SỰ
   hưởng lợi từ Stored Procedure mới giữ ở đây (26 SP), còn các thao tác
   CRUD / truy vấn 1 câu đơn giản được viết trực tiếp bằng SQL tham số hóa
   trong Controller (gọi qua Db.Query / Db.Exec / Db.Scalar).

   Tiêu chí giữ lại 1 SP:
     - Giao dịch nhiều bảng cần BEGIN TRAN...COMMIT/ROLLBACK để đảm bảo toàn vẹn
       (vd sp_ThanhToan, sp_XacNhanTraPhong, sp_TaoHoaDonDong, sp_XacNhanDangKy).
     - Trả về NHIỀU result set trong 1 lượt gọi, giảm round-trip
       (sp_TongQuan, sp_ThongKe, sp_DotDangMoChoSV).
     - Xử lý theo tập hợp / tổng hợp / MERGE (sp_LuuChiSo, sp_ThongKe).
     - Job tự động quét theo tập hợp (sp_CapNhatHoaDonQuaHan, sp_KhoaTaiKhoanQuaHan,
       sp_GhiNhanViPham, sp_CapNhatDiemViPham, sp_CapNhatTrangThaiDot).
     - Tiện ích dùng chung nhiều nơi (sp_Chung_LayThamSo, sp_Chung_ThemThongBao).

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

PRINT N'Đã tạo xong toàn bộ Stored Procedure.';
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
