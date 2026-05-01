using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using PBL3.Services;
using PBL3.Services.Interfaces;
using PBL3.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Read from User Secrets, environment variables, or appsettings fallback.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing database connection string. Configure ConnectionStrings:DefaultConnection with User Secrets or the ConnectionStrings__DefaultConnection environment variable.");
}

builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// Add services to the container.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

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

_ = Task.Run(() => WarmUpDatabaseAsync(app.Services));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin-dashboard",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Home" });

app.MapAreaControllerRoute(
    name: "source-crud",
    areaName: "Admin",
    pattern: "Source/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Booking}/{action=Index}/{id?}")
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
        using var warmUpTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await context.Database.CanConnectAsync(warmUpTimeout.Token);
        await context.LoaiPhongs.AsNoTracking().AnyAsync(warmUpTimeout.Token);
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("Database warm-up timed out. The first database request may be slower.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database warm-up failed. The first database request may be slower.");
    }
}
