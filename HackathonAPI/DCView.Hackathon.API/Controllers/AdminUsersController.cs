using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.Admin;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Infrastructure.Data;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.API.Controllers;

/// <summary>
/// Manage admin/panel-member accounts. SuperAdmin only.
/// </summary>
[Route("api/admin-users")]
[ApiController]
[Authorize(Roles = "SuperAdmin")]
public class AdminUsersController : ControllerBase
{
    private readonly HackathonDbContext _db;

    public AdminUsersController(HackathonDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var admins = await _db.AdminUsers
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        return Ok(admins.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin not found" });
        return Ok(MapToDto(admin));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserID) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "UserID and Password are required" });

        // Check uniqueness across both admin and participant tables
        if (await _db.AdminUsers.AnyAsync(a => a.UserID == request.UserID))
            return BadRequest(new { message = $"Admin '{request.UserID}' already exists" });

        if (await _db.Users.AnyAsync(u => u.UserID == request.UserID))
            return BadRequest(new { message = $"A participant with ID '{request.UserID}' already exists. Choose a different ID." });

        var admin = new AdminUser
        {
            UserID = request.UserID.Trim().ToUpper(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            FullName = request.FullName?.Trim(),
            Email = request.Email?.Trim(),
            Role = "Admin",
            IsActive = true,
            CanManageUsers = request.Permissions.CanManageUsers,
            CanManageSessions = request.Permissions.CanManageSessions,
            CanViewMonitoring = request.Permissions.CanViewMonitoring,
            CanManageAssessments = request.Permissions.CanManageAssessments,
            CanViewResults = request.Permissions.CanViewResults,
            CanManageHackathonSetup = request.Permissions.CanManageHackathonSetup,
            CanManageServerConfig = request.Permissions.CanManageServerConfig,
            CanManageScaffoldScripts = request.Permissions.CanManageScaffoldScripts,
            CanManageSecuritySettings = request.Permissions.CanManageSecuritySettings,
            CanManageAiDetection = request.Permissions.CanManageAiDetection,
            CanManageManualTesting = request.Permissions.CanManageManualTesting,
            CanExportData = request.Permissions.CanExportData,
            CanResetDatabase = request.Permissions.CanResetDatabase,
            CanDeleteUsers = request.Permissions.CanDeleteUsers,
            CreatedDate = DateTimeHelper.Now,
            CreatedBy = User.FindFirst(ClaimTypes.Name)?.Value
        };

        _db.AdminUsers.Add(admin);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(admin));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAdminUserDto request)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin not found" });

        if (request.FullName != null) admin.FullName = request.FullName.Trim();
        if (request.Email != null) admin.Email = request.Email.Trim();
        if (request.IsActive.HasValue) admin.IsActive = request.IsActive.Value;

        if (request.Permissions != null)
        {
            admin.CanManageUsers = request.Permissions.CanManageUsers;
            admin.CanManageSessions = request.Permissions.CanManageSessions;
            admin.CanViewMonitoring = request.Permissions.CanViewMonitoring;
            admin.CanManageAssessments = request.Permissions.CanManageAssessments;
            admin.CanViewResults = request.Permissions.CanViewResults;
            admin.CanManageHackathonSetup = request.Permissions.CanManageHackathonSetup;
            admin.CanManageServerConfig = request.Permissions.CanManageServerConfig;
            admin.CanManageScaffoldScripts = request.Permissions.CanManageScaffoldScripts;
            admin.CanManageSecuritySettings = request.Permissions.CanManageSecuritySettings;
            admin.CanManageAiDetection = request.Permissions.CanManageAiDetection;
            admin.CanManageManualTesting = request.Permissions.CanManageManualTesting;
            admin.CanExportData = request.Permissions.CanExportData;
            admin.CanResetDatabase = request.Permissions.CanResetDatabase;
            admin.CanDeleteUsers = request.Permissions.CanDeleteUsers;
        }

        admin.ModifiedDate = DateTimeHelper.Now;
        admin.ModifiedBy = User.FindFirst(ClaimTypes.Name)?.Value;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Admin updated", data = MapToDto(admin) });
    }

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] AdminChangePasswordDto request)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin not found" });

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        admin.ModifiedDate = DateTimeHelper.Now;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var admin = await _db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound(new { message = "Admin not found" });

        if (admin.Role == "SuperAdmin")
            return BadRequest(new { message = "Cannot delete SuperAdmin" });

        _db.AdminUsers.Remove(admin);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Admin '{admin.UserID}' deleted" });
    }

    private static AdminUserDto MapToDto(AdminUser a) => new()
    {
        Id = a.Id,
        UserID = a.UserID,
        FullName = a.FullName,
        Email = a.Email,
        Role = a.Role,
        IsActive = a.IsActive,
        CreatedDate = a.CreatedDate,
        LastLoginAt = a.LastLoginAt,
        Permissions = new AdminPermissionsDto
        {
            CanManageUsers = a.CanManageUsers,
            CanManageSessions = a.CanManageSessions,
            CanViewMonitoring = a.CanViewMonitoring,
            CanManageAssessments = a.CanManageAssessments,
            CanViewResults = a.CanViewResults,
            CanManageHackathonSetup = a.CanManageHackathonSetup,
            CanManageServerConfig = a.CanManageServerConfig,
            CanManageScaffoldScripts = a.CanManageScaffoldScripts,
            CanManageSecuritySettings = a.CanManageSecuritySettings,
            CanManageAiDetection = a.CanManageAiDetection,
            CanManageManualTesting = a.CanManageManualTesting,
            CanExportData = a.CanExportData,
            CanResetDatabase = a.CanResetDatabase,
            CanDeleteUsers = a.CanDeleteUsers
        }
    };
}
