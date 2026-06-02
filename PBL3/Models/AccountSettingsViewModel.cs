using System.ComponentModel.DataAnnotations;

namespace PBL3.Models;

public class AccountSettingsViewModel
{
    public string AccountId { get; set; } = string.Empty;

    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(10)]
    public string? Gender { get; set; }

    public DateOnly? BirthDate { get; set; }

    [StringLength(15)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }
}
