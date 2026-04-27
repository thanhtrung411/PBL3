using Microsoft.EntityFrameworkCore;
using PBL3.Services;
using PBL3.Services.Interfaces;
using PBL3.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối từ file appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Đăng ký DbContext vào hệ thống (Dependency Injection)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

//Add services for DI
builder.Services.AddScoped<ILoaiPhongService, LoaiPhongService>();

builder.Services.AddScoped<IPhongService, PhongService>();

builder.Services.AddScoped<IVaiTroService, VaiTroService>();

builder.Services.AddScoped<INhanVienService, NhanVienService>();

builder.Services.AddScoped<IKhachHangService, KhachHangService>();
builder.Services.AddScoped<IDichVuService, DichVuService>();
builder.Services.AddScoped<IMaGiamGiaService, MaGiamGiaService>();
builder.Services.AddScoped<ITaiKhoanService, TaiKhoanService>();
builder.Services.AddScoped<IBangGiaPhongService, BangGiaPhongService>();
builder.Services.AddScoped<IDatPhongService, DatPhongService>();
builder.Services.AddScoped<IHoaDonService, HoaDonService>();
builder.Services.AddScoped<IChiTietHoaDonService, ChiTietHoaDonService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();