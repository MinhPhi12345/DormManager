using DormManager.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Khởi tạo chuỗi kết nối cho lớp truy cập dữ liệu
Db.Init(app.Configuration.GetConnectionString("DormManager")!);

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
