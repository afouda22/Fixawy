using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public interface IUserService
{
    Task<UserManagementViewModel> GetUsersAsync(string? roleFilter, string? search);
    Task<bool> AssignRoleAsync(string userId, string newRole);
    Task<bool> ToggleUserStatusAsync(string userId);
    Task<(bool Success, string Message)> DeleteUserAsync(string userId);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Context.AppDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        Context.AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<UserManagementViewModel> GetUsersAsync(string? roleFilter, string? search)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userItemList = new List<UserItemViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);

            if (!string.IsNullOrWhiteSpace(roleFilter) && 
                !roles.Any(r => r.Equals(roleFilter, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            userItemList.Add(new UserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles = roles
            });
        }

        return new UserManagementViewModel
        {
            Users = userItemList,
            Search = search,
            RoleFilter = roleFilter,
            AvailableRoles = AppRoles.All.ToList()
        };
    }

    public async Task<bool> AssignRoleAsync(string userId, string newRole)
    {
        if (!AppRoles.All.Contains(newRole)) return false;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var res = await _userManager.AddToRoleAsync(user, newRole);

        if (newRole == AppRoles.Technician)
        {
            var hasProfile = await _context.TechnicianProfiles.AnyAsync(tp => tp.UserId == userId);
            if (!hasProfile)
            {
                var profile = new TechnicianProfile
                {
                    UserId = user.Id,
                    Bio = "Skilled technician ready to accept jobs.",
                    HourlyRate = 150m,
                    YearsOfExperience = 1,
                    IsAvailable = true
                };

                for (int day = 0; day <= 4; day++)
                {
                    profile.Schedules.Add(new TechnicianSchedule
                    {
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        IsWorkingDay = true
                    });
                }

                await _context.TechnicianProfiles.AddAsync(profile);
                await _context.SaveChangesAsync();
            }
        }

        return res.Succeeded;
    }

    public async Task<bool> ToggleUserStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        var res = await _userManager.UpdateAsync(user);
        return res.Succeeded;
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "User account not found.");

        if (user.Email?.Equals("admin@fixawyy.com", StringComparison.OrdinalIgnoreCase) == true)
        {
            return (false, "The primary system administrator account cannot be deleted.");
        }

        // Remove technician profile & related data if exists
        var techProfile = await _context.TechnicianProfiles
            .Include(tp => tp.TechnicianSpecialties)
            .Include(tp => tp.Schedules)
            .FirstOrDefaultAsync(tp => tp.UserId == userId);

        if (techProfile != null)
        {
            _context.TechnicianSpecialties.RemoveRange(techProfile.TechnicianSpecialties);
            _context.TechnicianSchedules.RemoveRange(techProfile.Schedules);
            _context.TechnicianProfiles.Remove(techProfile);
            await _context.SaveChangesAsync();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to delete user.";
            return (false, error);
        }

        return (true, $"User '{user.FullName}' ({user.Email}) has been deleted successfully.");
    }
}
