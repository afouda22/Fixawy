using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Fixawyy.Model_Views;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; } = false;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be at least 3 characters")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [Display(Name = "Primary Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid secondary phone format")]
    [Display(Name = "Secondary Phone Number (Optional)")]
    public string? SecondaryPhoneNumber { get; set; }

    [Required(ErrorMessage = "Please select your country")]
    [Display(Name = "Country")]
    public string Country { get; set; } = "Egypt";

    [Required(ErrorMessage = "National ID is required")]
    [RegularExpression(@"^[0-9]{14}$", ErrorMessage = "National ID must be exactly 14 numeric digits")]
    [Display(Name = "National ID (الرقم القومي)")]
    public string NationalId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of Birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Personal photo is required (الصورة الشخصية مطلوبة)")]
    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }

    [Required]
    [Display(Name = "Register As")]
    public string Role { get; set; } = "Customer";
}

public class VerifyOtpViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Code must be 6 numeric digits")]
    [Display(Name = "Verification Code")]
    public string OtpCode { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserProfileViewModel
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Phone]
    public string? SecondaryPhoneNumber { get; set; }

    [Required, StringLength(100)]
    public string Country { get; set; } = "Egypt";

    [RegularExpression(@"^[0-9]{14}$")]
    public string? NationalId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string? Email { get; set; }
}
