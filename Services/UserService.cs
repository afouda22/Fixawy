using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public interface IUserService
{
    Task<UserManagementViewModel> GetUsersAsync(string? roleFilter, string? search);
    Task<bool> AssignRoleAsync(string userId, string newRole, string? facility = null);
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
                Facility = u.Facility,
                Roles = roles
            });
        }

        var facilities = await _context.MaintenanceTickets
            .AsNoTracking()
            .Select(t => t.Department)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        return new UserManagementViewModel
        {
            Users = userItemList,
            Search = search,
            RoleFilter = roleFilter,
            AvailableRoles = AppRoles.All.ToList(),
            AvailableFacilities = facilities
        };
    }

    public async Task<bool> AssignRoleAsync(string userId, string newRole, string? facility = null)
    {
        if (!AppRoles.All.Contains(newRole)) return false;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var addRes = await _userManager.AddToRoleAsync(user, newRole);

        if (!string.IsNullOrWhiteSpace(facility))
        {
            user.Facility = facility;
            await _userManager.UpdateAsync(user);
        }

        return addRes.Succeeded;
    }

    public async Task<bool> ToggleUserStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.Admin))
        {
            var adminCount = (await _userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count;
            if (adminCount <= 1)
            {
                return (false, "Cannot delete the only remaining Administrator account.");
            }
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded ? (true, "User deleted successfully.") : (false, "Failed to delete user.");
    }
}
