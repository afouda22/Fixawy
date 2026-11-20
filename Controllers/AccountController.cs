using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Fixawyy.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly Context.AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly Services.IEmailService _emailService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        Context.AppDbContext context,
        IWebHostEnvironment webHostEnvironment,
        Services.IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && user.EmailConfirmed && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme);
            await _emailService.SendEmailAsync(user.Email!, "Fixawyy - Reset your password",
                $"<p>Hello {HtmlEncoder.Default.Encode(user.FullName)},</p><p><a href=\"{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}\">Reset your password</a></p>");
        }

        TempData["SuccessMessage"] = "If an active account exists for that email, password reset instructions have been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)) return BadRequest();
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Your password has been reset. You can now sign in.";
            return RedirectToAction(nameof(Login));
        }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new UserProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            SecondaryPhoneNumber = user.SecondaryPhoneNumber,
            Country = user.Country,
            NationalId = user.NationalId,
            DateOfBirth = user.DateOfBirth
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.SecondaryPhoneNumber = model.SecondaryPhoneNumber;
        user.Country = model.Country.Trim();
        user.NationalId = model.NationalId;
        user.DateOfBirth = model.DateOfBirth;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        TempData["SuccessMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
            return View(model);
        }

        if (!user.EmailConfirmed)
        {
            // Email not verified yet
            TempData["ErrorMessage"] = "Please verify your email address to activate your account.";
            return RedirectToAction(nameof(VerifyOtp), new { email = user.Email });
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            return View(model);
        }

        // Profile picture upload handling
        string? profilePictureUrl = null;
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "Only image files (.jpg, .jpeg, .png, .webp) are allowed.");
                return View(model);
            }

            if (model.ProfilePicture.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "Image size cannot exceed 5MB.");
                return View(model);
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.ProfilePicture.CopyToAsync(fileStream);
            }
            profilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
        }

        // Generate 6-digit OTP code
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            ProfilePictureUrl = profilePictureUrl,
            PhoneNumber = model.PhoneNumber,
            SecondaryPhoneNumber = model.SecondaryPhoneNumber,
            Country = model.Country,
            NationalId = model.NationalId,
            DateOfBirth = model.DateOfBirth,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false, // Must be verified via OTP
            VerificationCode = otpCode,
            VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // Role assignment
        // Public registration must never grant operational roles.
        const string targetRole = AppRoles.Customer;

        await _userManager.AddToRoleAsync(user, targetRole);

        // Send OTP verification email from info.eu.yo@gmail.com
        var emailSubject = "Fixawyy - Your Email Verification Code";
        var emailBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
            <div style='text-align: center; margin-bottom: 20px;'>
                <h2 style='color: #2563eb; margin: 0; font-size: 26px;'>Fixawyy</h2>
                <p style='color: #64748b; font-size: 14px; margin-top: 4px;'>Smart Maintenance & Booking Platform</p>
            </div>
            <p style='color: #1e293b; font-size: 16px;'>Hello <strong>{model.FullName}</strong>,</p>
            <p style='color: #475569; font-size: 15px; line-height: 1.5;'>Thank you for registering on Fixawyy. Please use the following 6-digit verification code to activate your account:</p>
            <div style='text-align: center; margin: 28px 0;'>
                <span style='display: inline-block; background-color: #eff6ff; color: #1d4ed8; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 12px 30px; border-radius: 8px; border: 1px dashed #3b82f6;'>
                    {otpCode}
                </span>
            </div>
            <p style='color: #64748b; font-size: 13px; line-height: 1.4;'>This code will expire in 15 minutes. If you did not create an account on Fixawyy, please ignore this message.</p>
            <hr style='border: none; border-top: 1px solid #f1f5f9; margin: 24px 0;' />
            <p style='color: #94a3b8; font-size: 12px; text-align: center;'>Fixawyy Platform &bull; Sent from info.eu.yo@gmail.com</p>
        </div>";

        await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

        TempData["OtpSentMessage"] = $"A verification code has been sent to {model.Email}. Please check your inbox.";

        return RedirectToAction(nameof(VerifyOtp), new { email = user.Email });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(Register));

        var model = new VerifyOtpViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User account not found.");
            return View(model);
        }

        if (user.VerificationCode != model.OtpCode)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "Invalid verification code. Please check and try again.");
            return View(model);
        }

        if (user.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "Verification code has expired. Please click 'Resend Code'.");
            return View(model);
        }

        // Verification succeeded!
        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        await _userManager.UpdateAsync(user);

        // Sign in user
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["SuccessMessage"] = "Your email has been verified successfully! Welcome to Fixawyy.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var newOtp = Random.Shared.Next(100000, 999999).ToString();
            user.VerificationCode = newOtp;
            user.VerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
            await _userManager.UpdateAsync(user);

            var emailSubject = "Fixawyy - Your New Verification Code";
            var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #2563eb; margin: 0; font-size: 26px;'>Fixawyy</h2>
                    <p style='color: #64748b; font-size: 14px; margin-top: 4px;'>Smart Maintenance & Booking Platform</p>
                </div>
                <p style='color: #1e293b; font-size: 16px;'>Hello <strong>{user.FullName}</strong>,</p>
                <p style='color: #475569; font-size: 15px; line-height: 1.5;'>Here is your new 6-digit verification code to activate your account:</p>
                <div style='text-align: center; margin: 28px 0;'>
                    <span style='display: inline-block; background-color: #eff6ff; color: #1d4ed8; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 12px 30px; border-radius: 8px; border: 1px dashed #3b82f6;'>
                        {newOtp}
                    </span>
                </div>
                <p style='color: #64748b; font-size: 13px; line-height: 1.4;'>This code will expire in 15 minutes.</p>
                <hr style='border: none; border-top: 1px solid #f1f5f9; margin: 24px 0;' />
                <p style='color: #94a3b8; font-size: 12px; text-align: center;'>Fixawyy Platform &bull; Sent from info.eu.yo@gmail.com</p>
            </div>";

            await _emailService.SendEmailAsync(user.Email!, emailSubject, emailBody);

            TempData["OtpSentMessage"] = $"A new verification code has been sent to {email}. Please check your inbox.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
