using Microsoft.EntityFrameworkCore;
using PBL3.Services;
using PBL3.Services.Interfaces;
using PBL3.Data;

var builder = WebApplication.CreateBuilder(args);

// Read from User Secrets, environment variables, or appsettings fallback.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing database connection string. Configure ConnectionStrings:DefaultConnection with User Secrets or the ConnectionStrings__DefaultConnection environment variable.");
}

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

await WarmUpDatabaseAsync(app.Services);

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
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Guest}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static async Task WarmUpDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseWarmUp");
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.CanConnectAsync();
        await context.LoaiPhongs.AsNoTracking().AnyAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database warm-up failed. The first database request may be slower.");
    }
}
