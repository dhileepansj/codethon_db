using DCView.Hackathon.Shared.Helpers;
using System.Security.Claims;
using DCView.Hackathon.Domain.Repositories;

namespace DCView.Hackathon.API.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] ProtectedPaths = { "/api/hackathon", "/api/schema", "/api/files", "/api/history", "/api/activity", "/api/mcq/test" };

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionRepository sessionRepo)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Only validate for participant-protected routes
        bool isProtected = ProtectedPaths.Any(p => path.StartsWith(p));
        if (!isProtected)
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context); // Let [Authorize] handle it
            return;
        }

        // SuperAdmin and Admin bypass session validation
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "SuperAdmin" || role == "Admin")
        {
            await _next(context);
            return;
        }

        // Get user ID from token
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
            return;
        }

        var session = await sessionRepo.GetByUserIdAsync(userId);

        if (session == null || !session.IsActive)
        {
            // Allow status check even when inactive (so frontend can show proper screen)
            bool isStatusCheck2 = path.Contains("/status") || path.Contains("/info");
            if (isStatusCheck2)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Session is not active. Please wait for the admin to activate your session.", code = "SESSION_INACTIVE" });
            return;
        }

        // Activity logging (tab-switch, devtools) is ALWAYS allowed when session is active
        bool isActivityLog = path.StartsWith("/api/activity");
        if (isActivityLog)
        {
            await _next(context);
            return;
        }

        // MCQ test endpoints don't require database creation
        bool isMcqEndpoint = path.StartsWith("/api/mcq");
        if (isMcqEndpoint)
        {
            await _next(context);
            return;
        }

        // Check expiry
        bool isExpired = session.ExpiresAt.HasValue && session.ExpiresAt < DateTimeHelper.Now;

        // Determine what's allowed after submission/expiry
        bool isFileOperation = path.StartsWith("/api/files");
        bool isHistoryView = path.StartsWith("/api/history");
        bool isSchemaView = path.StartsWith("/api/schema");
        bool isStatusCheck = path.Contains("/status");
        bool isSubmissionFileOp = path.Contains("/submission-files");
        bool isSubmitAction = path.Contains("/submit");

        // Block everything except status check when submitted
        if (session.IsSubmitted && !isStatusCheck && !isSubmissionFileOp)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "You have already submitted your work. No further changes are allowed.", code = "SESSION_SUBMITTED" });
            return;
        }

        if (isExpired && !isFileOperation && !isHistoryView && !isSchemaView && !isStatusCheck && !isSubmissionFileOp && !isSubmitAction)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Session has expired. You can still save your scripts from the Files tab.", code = "SESSION_EXPIRED" });
            return;
        }

        // For execute/schema — require DB to be created (except create-database itself)
        bool requiresDb = !path.Contains("/create-database") && !isStatusCheck;
        if (requiresDb && !session.DatabaseCreated)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Please create your database first." });
            return;
        }

        await _next(context);
    }
}

