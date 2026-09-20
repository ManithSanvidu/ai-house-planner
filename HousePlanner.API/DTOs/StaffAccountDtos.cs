using System.ComponentModel.DataAnnotations;

namespace HousePlanner.API.DTOs;

public sealed class CreateStaffRequestDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public sealed record StaffAccountDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status);

public sealed class UpdateStaffStatusDto
{
    public bool Disabled { get; set; }
}

public sealed class UpdateStaffRequestDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
