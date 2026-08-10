using DormManager.Data;
using DormManager.Helpers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DormManager")));

var app = builder.Build();

// Khởi tạo chuỗi kết nối cho lớp truy cập dữ liệu
Db.Init(app.Configuration.GetConnectionString("DormManager")!);

// Khởi tạo cấu hình tài khoản ngân hàng để sinh mã QR chuyển khoản (VietQR)
CauHinhThanhToan.Init(app.Configuration);

// Khởi tạo cấu hình SMTP để gửi email thông báo hóa đơn thật cho sinh viên
CauHinhEmail.Init(app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/TaiKhoan/DangNhap");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TaiKhoan}/{action=DangNhap}/{id?}");

app.Run();
