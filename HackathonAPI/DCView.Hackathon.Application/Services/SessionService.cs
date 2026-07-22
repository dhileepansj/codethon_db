using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using DCView.Hackathon.Application.DTOs.Admin;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Enums;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.Application.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IUserRepository _userRepo;
    private readonly IHackathonConfigRepository _configRepo;
    private readonly IExecutionHistoryRepository _historyRepo;
    private readonly IConfiguration _config;

    public SessionService(
        ISessionRepository sessionRepo,
        IUserRepository userRepo,
        IHackathonConfigRepository configRepo,
        IExecutionHistoryRepository historyRepo,
        IConfiguration config)
    {
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
        _configRepo = configRepo;
        _historyRepo = historyRepo;
        _config = config;
    }

    public async Task<bool> ActivateSessionAsync(string userId, int? durationMinutes, string activatedBy)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        var session = await _sessionRepo.GetByUserIdAsync(user.Id);
        if (session == null)
        {
            session = new HackathonSession
            {
                UserId = user.Id,
                IsActive = true,
                StartedAt = DateTimeHelper.Now,
                ExpiresAt = durationMinutes.HasValue && durationMinutes.Value > 0
                    ? DateTimeHelper.Now.AddMinutes(durationMinutes.Value)
                    : null,
                CreatedBy = activatedBy
            };
            await _sessionRepo.CreateAsync(session);
        }
        else
        {
            session.IsActive = true;
            session.StartedAt = DateTimeHelper.Now;
            session.ExpiresAt = durationMinutes.HasValue && durationMinutes.Value > 0
                ? DateTimeHelper.Now.AddMinutes(durationMinutes.Value)
                : null;
            await _sessionRepo.UpdateAsync(session);
        }

        return true;
    }

    public async Task<bool> DeactivateSessionAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        var session = await _sessionRepo.GetByUserIdAsync(user.Id);
        if (session == null) return false;

        session.IsActive = false;
        await _sessionRepo.UpdateAsync(session);
        return true;
    }

    public async Task<bool> ExtendSessionAsync(string userId, int additionalMinutes)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        var session = await _sessionRepo.GetByUserIdAsync(user.Id);
        if (session == null) return false;

        if (session.ExpiresAt.HasValue)
            session.ExpiresAt = session.ExpiresAt.Value.AddMinutes(additionalMinutes);
        else
            session.ExpiresAt = DateTimeHelper.Now.AddMinutes(additionalMinutes);

        await _sessionRepo.UpdateAsync(session);
        return true;
    }

    public async Task<bool> ResetDatabaseAsync(string userId)
    {
        var user = await _userRepo.GetByUserIDAsync(userId);
        if (user == null) return false;

        var session = await _sessionRepo.GetByUserIdAsync(user.Id);
        if (session == null || !session.DatabaseCreated) return false;

        var hackConfig = await _configRepo.GetActiveConfigAsync(user.DbEnginePreference);
        if (hackConfig == null) return false;

        string encKey = _config["Encryption:Key"]!;
        string adminPassword = EncryptionHelper.Decrypt(hackConfig.AdminPasswordEncrypted, encKey);

        string dbName = session.DatabaseName!;
        string loginName = $"hack_{user.UserID}";

        if (hackConfig.DbEngineType == DbEngineType.Oracle)
        {
            // Oracle: DROP USER CASCADE
            int port = hackConfig.Port ?? 1521;
            string serviceName = hackConfig.OracleServiceName ?? "XEPDB1";
            string oracleConnStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={hackConfig.ServerName})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));User Id=\"{hackConfig.AdminUserId}\";Password=\"{adminPassword}\";Connection Timeout=30;";

            using var oraConn = new OracleConnection(oracleConnStr);
            await oraConn.OpenAsync();

            // Kill active sessions for the user
            try
            {
                using var killCmd = oraConn.CreateCommand();
                killCmd.CommandText = $@"
                    BEGIN
                        FOR s IN (SELECT SID, SERIAL# FROM V$SESSION WHERE USERNAME = '{dbName}')
                        LOOP
                            EXECUTE IMMEDIATE 'ALTER SYSTEM KILL SESSION ''' || s.SID || ',' || s.SERIAL# || ''' IMMEDIATE';
                        END LOOP;
                    END;";
                await killCmd.ExecuteNonQueryAsync();
            }
            catch { /* Best effort - may not have V$ access or no sessions */ }

            using var dropCmd = oraConn.CreateCommand();
            dropCmd.CommandText = $"DROP USER \"{dbName}\" CASCADE";
            dropCmd.CommandTimeout = 60;
            await dropCmd.ExecuteNonQueryAsync();
        }
        else
        {
            // SQL Server: DROP DATABASE + DROP LOGIN
            string adminConnStr = $"Server={hackConfig.ServerName};Database=master;User Id={hackConfig.AdminUserId};Password={adminPassword};TrustServerCertificate=True;Connection Timeout=30;";

            using var conn = new SqlConnection(adminConnStr);
            await conn.OpenAsync();

            using var dropCmd = conn.CreateCommand();
            dropCmd.CommandText = $@"
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                BEGIN
                    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{dbName}];
                END";
            dropCmd.CommandTimeout = 60;
            await dropCmd.ExecuteNonQueryAsync();

            using var dropLoginCmd = conn.CreateCommand();
            dropLoginCmd.CommandText = $@"
                IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{loginName}')
                    DROP LOGIN [{loginName}];";
            await dropLoginCmd.ExecuteNonQueryAsync();
        }

        // Reset session state
        session.DatabaseCreated = false;
        session.DatabaseName = null;
        session.DbLoginPassword = null;
        await _sessionRepo.UpdateAsync(session);

        return true;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var users = await _userRepo.GetAllParticipantsAsync();
        var sessions = await _sessionRepo.GetAllActiveSessionsAsync();
        var queriesToday = await _historyRepo.GetTodayCountAsync();

        return new DashboardStatsDto
        {
            TotalUsers = users.Count(),
            ActiveSessions = sessions.Count(),
            DatabasesCreated = sessions.Count(s => s.DatabaseCreated),
            QueriesToday = queriesToday
        };
    }
}

