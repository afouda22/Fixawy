using Microsoft.AspNetCore.Identity;

namespace Fixawyy.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Additional User Info
    public string? SecondaryPhoneNumber { get; set; }
    public string Country { get; set; } = "Egypt";
    public string? NationalId { get; set; } // 14 digits Egyptian National ID
    public DateTime? DateOfBirth { get; set; }

    // Email OTP Verification
    public string? VerificationCode { get; set; }
    public DateTime? VerificationCodeExpiresAt { get; set; }

    public virtual TechnicianProfile? TechnicianProfile { get; set; }
}
