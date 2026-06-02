using System.ComponentModel.DataAnnotations;

namespace PBL3.Models;

public class AdminStaffManagementViewModel
{
    public List<AdminStaffManagementItemViewModel> Staff { get; set; } = new();

    public List<string> Positions { get; set; } = new();

    public List<AdminStaffRoleOptionViewModel> Roles { get; set; } = new();

    public int TotalCount => Staff.Count;

    public int ActiveCount => Staff.Count(x => x.Status == "Đang làm");

    public int InactiveCount => Staff.Count(x => x.Status != "Đang làm");

    public int AccountCount => Staff.Count(x => x.HasAccount);
}

public class AdminStaffManagementItemViewModel
{
    public string EmployeeId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public string BirthDateLabel { get; set; } = "Chưa có";

    public string BirthDateInput { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;

    public string AccountRole { get; set; } = string.Empty;

    public string AccountStatus { get; set; } = string.Empty;

    public bool HasAccount { get; set; }

    public int BookingCount { get; set; }

    public string Initials { get; set; } = "NV";
}

public class AdminStaffRoleOptionViewModel
{
    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;
}

public class AdminStaffCreateRequest
{
    [Required]
    public string HoTen { get; set; } = string.Empty;

    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? ChucVu { get; set; }

    public string? TrangThai { get; set; }

    [Required]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required]
    public string MatKhau { get; set; } = string.Empty;

    [Required]
    public string MaVaiTro { get; set; } = string.Empty;
}

public class AdminStaffUpdateRequest
{
    [Required]
    public string MaNv { get; set; } = string.Empty;

    [Required]
    public string HoTen { get; set; } = string.Empty;

    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? DiaChi { get; set; }

    public string? ChucVu { get; set; }

    public string? TrangThai { get; set; }
}
