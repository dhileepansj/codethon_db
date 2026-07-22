namespace DCView.Hackathon.Application.DTOs.Admin;

public class AdminUserDto
{
    public int Id { get; set; }
    public string UserID { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public AdminPermissionsDto Permissions { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminPermissionsDto
{
    public bool CanManageUsers { get; set; }
    public bool CanManageSessions { get; set; }
    public bool CanViewMonitoring { get; set; }
    public bool CanManageAssessments { get; set; }
    public bool CanViewResults { get; set; }
    public bool CanManageHackathonSetup { get; set; }
    public bool CanManageServerConfig { get; set; }
    public bool CanManageScaffoldScripts { get; set; }
    public bool CanManageSecuritySettings { get; set; }
    public bool CanManageAiDetection { get; set; }
    public bool CanExportData { get; set; }
    public bool CanResetDatabase { get; set; }
    public bool CanDeleteUsers { get; set; }
}

public class CreateAdminUserDto
{
    public string UserID { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public AdminPermissionsDto Permissions { get; set; } = new();
}

public class UpdateAdminUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public AdminPermissionsDto? Permissions { get; set; }
}
