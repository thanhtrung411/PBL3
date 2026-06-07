using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using PBL3.Services;
using PBL3.Services.Interfaces;
using PBL3.Data;
using PBL3.Models;

const string AdminPolicyName = "AdminOnly";

LoadDotEnv(AppContext.BaseDirectory);
LoadDotEnv(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Read from User Secrets, environment variables, or appsettings fallback.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing database connection string. Configure ConnectionStrings:DefaultConnection with User Secrets, .env, or the ConnectionStrings__DefaultConnection environment variable.");
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
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var adminRoleKeys = GetAdminRoleKeys(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.FindAll(ClaimTypes.Role)
                .Any(claim => adminRoleKeys.Contains(NormalizeRoleKey(claim.Value))));
    });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
    options.Conventions.Add(new AdminAuthorizationConvention(AdminPolicyName));
});

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("PBL3");

builder.Services.Configure<VnPayOptions>(builder.Configuration.GetSection("Payment:VnPay"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

//Add services for DI
builder.Services.AddScoped<ILoaiPhongService, LoaiPhongService>();

builder.Services.AddScoped<IPhongService, PhongService>();

builder.Services.AddScoped<IVaiTroService, VaiTroService>();

builder.Services.AddScoped<INhanVienService, NhanVienService>();

builder.Services.AddScoped<IKhachHangService, KhachHangService>();
builder.Services.AddScoped<IDichVuService, DichVuService>();
builder.Services.AddScoped<IMaGiamGiaService, MaGiamGiaService>();
builder.Services.AddScoped<ITaiKhoanService, TaiKhoanService>();
builder.Services.AddScoped<IPasswordHasher<TaiKhoan>, PasswordHasher<TaiKhoan>>();
builder.Services.AddScoped<IBangGiaPhongService, BangGiaPhongService>();
builder.Services.AddScoped<ILinkAnhService, LinkAnhService>();
builder.Services.AddScoped<IDatPhongService, DatPhongService>();
builder.Services.AddScoped<IHoaDonService, HoaDonService>();
builder.Services.AddScoped<IInvoicePromotionService, InvoicePromotionService>();
builder.Services.AddScoped<IChiTietHoaDonService, ChiTietHoaDonService>();
builder.Services.AddScoped<IPublicBookingService, PublicBookingService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<IReceptionistCheckInService, ReceptionistCheckInService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IBookingEmailService, BookingEmailService>();
builder.Services.AddScoped<IExpiredBookingCleanupService, ExpiredBookingCleanupService>();
builder.Services.AddHostedService<ExpiredBookingCleanupHostedService>();

var app = builder.Build();

_ = Task.Run(() => WarmUpDatabaseAsync(app.Services));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin-dashboard",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { controller = "Home" })
    .RequireAuthorization(AdminPolicyName);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Booking}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static HashSet<string> GetAdminRoleKeys(IConfiguration configuration)
{
    var configuredRoles = configuration
        .GetSection("Authorization:AdminRoles")
        .Get<string[]>();
    var roles = configuredRoles is { Length: > 0 }
        ? configuredRoles
        : new[] { "Admin", "VT001" };

    return roles
        .Select(NormalizeRoleKey)
        .Where(role => role is "admin" or "vt001")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

static string NormalizeRoleKey(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    var normalized = value.Trim().Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);
    foreach (var character in normalized)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(character);
        if (category != UnicodeCategory.NonSpacingMark)
        {
            builder.Append(character);
        }
    }

    return builder
        .ToString()
        .Normalize(NormalizationForm.FormC)
        .ToLowerInvariant();
}

static async Task WarmUpDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseWarmUp");
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>();

    try
    {
        using var warmUpTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await context.Database.CanConnectAsync(warmUpTimeout.Token);
        await context.LoaiPhongs.AsNoTracking().AnyAsync(warmUpTimeout.Token);
        await MigratePlainTextPasswordsAsync(context, passwordHasher, logger, warmUpTimeout.Token);
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

static async Task MigratePlainTextPasswordsAsync(
    ApplicationDbContext context,
    IPasswordHasher<TaiKhoan> passwordHasher,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var accounts = await context.TaiKhoans
        .Where(account => account.MatKhau != "" &&
                          !account.MatKhau.StartsWith("AQAAAA"))
        .ToListAsync(cancellationToken);
    if (accounts.Count == 0)
    {
        return;
    }

    foreach (var account in accounts)
    {
        account.MatKhau = passwordHasher.HashPassword(account, account.MatKhau);
    }

    await context.SaveChangesAsync(cancellationToken);
    logger.LogInformation("Migrated {Count} plain-text account passwords to password hashes.", accounts.Count);
}

static void LoadDotEnv(string directory)
{
    var envPath = Path.Combine(directory, ".env");
    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(envPath))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        if (key.Length == 0)
        {
            continue;
        }

        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}

sealed class AdminAuthorizationConvention : IControllerModelConvention
{
    private static readonly HashSet<string> RootAdminControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "BookingManagement",
        "Customer",
        "Home",
        "Invoice",
        "LoaiPhongs",
        "Promotion",
        "Report",
        "Room",
        "Service"
    };

    private readonly string _policyName;

    public AdminAuthorizationConvention(string policyName)
    {
        _policyName = policyName;
    }

    public void Apply(ControllerModel controller)
    {
        var controllerNamespace = controller.ControllerType.Namespace ?? string.Empty;
        var isSourceCrudController = controllerNamespace.StartsWith(
            "PBL3.Areas.Admin.Controllers",
            StringComparison.Ordinal);
        var isRootAdminController = RootAdminControllers.Contains(controller.ControllerName);

        if (isSourceCrudController || isRootAdminController)
        {
            controller.Filters.Add(new AuthorizeFilter(_policyName));
        }
    }
}
