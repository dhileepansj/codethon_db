using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;

namespace DCView.Hackathon.Infrastructure.Data;

public class HackathonDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<HackathonSession> Sessions { get; set; }
    public DbSet<HackathonConfig> HackathonConfigs { get; set; }
    public DbSet<ExecutionHistory> ExecutionHistories { get; set; }
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<UserFolder> UserFolders { get; set; }
    public DbSet<TabSwitchLog> TabSwitchLogs { get; set; }
    public DbSet<AiDetectionLog> AiDetectionLogs { get; set; }
    public DbSet<HackathonQuestionPaper> QuestionPapers { get; set; }
    public DbSet<UserSubmissionFile> SubmissionFiles { get; set; }
    public DbSet<AiDetectionSettings> AiDetectionSettings { get; set; }
    public DbSet<AiDetectionUserOverride> AiDetectionUserOverrides { get; set; }
    public DbSet<AiBlockedSave> AiBlockedSaves { get; set; }
    public DbSet<PasswordChangeLog> PasswordChangeLogs { get; set; }
    public DbSet<SecuritySettings> SecuritySettings { get; set; }
    public DbSet<ScaffoldScript> ScaffoldScripts { get; set; }
    public DbSet<HackathonSchedule> HackathonSchedules { get; set; }
    public DbSet<HackathonBreak> HackathonBreaks { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<McqQuestion> McqQuestions { get; set; }
    public DbSet<McqTest> McqTests { get; set; }
    public DbSet<McqAnswer> McqAnswers { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<ManualTestScenario> ManualTestScenarios { get; set; }
    public DbSet<ManualTestCase> ManualTestCases { get; set; }
    public DbSet<SubmissionAuditLog> SubmissionAuditLogs { get; set; }

    // ─── Survey Module ───────────────────────────────────────────
    public DbSet<Survey> Surveys { get; set; }
    public DbSet<SurveyField> SurveyFields { get; set; }
    public DbSet<SurveyFieldDependency> SurveyFieldDependencies { get; set; }
    public DbSet<SurveyParticipant> SurveyParticipants { get; set; }
    public DbSet<SurveyDistribution> SurveyDistributions { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<SurveyResponseAnswer> SurveyResponseAnswers { get; set; }
    public DbSet<SurveyEmailSettings> SurveyEmailSettings { get; set; }
    public DbSet<SurveyParticipantStatusLog> SurveyParticipantStatusLogs { get; set; }
    public DbSet<SurveyReminderLog> SurveyReminderLogs { get; set; }
    public DbSet<SurveyOtp> SurveyOtps { get; set; }

    public HackathonDbContext(DbContextOptions<HackathonDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.UserID).IsUnique();
            entity.HasOne(e => e.Session)
                  .WithOne(s => s.User)
                  .HasForeignKey<HackathonSession>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Session
        modelBuilder.Entity<HackathonSession>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // ExecutionHistory
        modelBuilder.Entity<ExecutionHistory>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExecutedAt);
        });

        // UserFolder — self-referencing
        modelBuilder.Entity<UserFolder>(entity =>
        {
            entity.HasOne(f => f.ParentFolder)
                  .WithMany(f => f.SubFolders)
                  .HasForeignKey(f => f.ParentFolderId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(f => f.Files)
                  .WithOne(f => f.Folder)
                  .HasForeignKey(f => f.FolderId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // UserFile
        modelBuilder.Entity<UserFile>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.FolderId });
        });

        // TabSwitchLog
        modelBuilder.Entity<TabSwitchLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventTime);
        });

        // AiDetectionLog
        modelBuilder.Entity<AiDetectionLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.ConfidenceScore);
            entity.HasIndex(e => e.AnalyzedDate);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.File)
                  .WithMany()
                  .HasForeignKey(e => e.FileId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // AiDetectionSettings — single row
        modelBuilder.Entity<AiDetectionSettings>(entity =>
        {
            entity.HasData(new AiDetectionSettings
            {
                Id = 1,
                Mode = "AllowAndMark",
                BlockThreshold = 70,
                ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // AiDetectionUserOverride
        modelBuilder.Entity<AiDetectionUserOverride>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // AiBlockedSave
        modelBuilder.Entity<AiBlockedSave>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.File)
                  .WithMany()
                  .HasForeignKey(e => e.FileId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // Seed SuperAdmin — password: Admin@123 (BCrypt cost 12)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            UserID = "superadmin",
            PasswordHash = "$2a$12$UokIogJtz0fLfUW7ubIzYOgNJZ8cZTo14YbiBB0ddKiL2m4Y2T/Vy",
            FullName = "Super Admin",
            Role = "SuperAdmin",
            IsActive = true,
            MustChangePassword = false,
            PasswordResetRequested = false,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "SYSTEM"
        });

        // PasswordChangeLog
        modelBuilder.Entity<PasswordChangeLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ChangedAt);
        });

        // SecuritySettings — single row with default seed
        modelBuilder.Entity<SecuritySettings>(entity =>
        {
            entity.HasData(new SecuritySettings
            {
                Id = 1,
                MinLength = 8,
                MaxLength = 64,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireSpecialChar = true,
                PasswordHistoryCount = 5,
                MaxFailedLoginAttempts = 5,
                LockoutDurationMinutes = 15,
                PasswordExpiryDays = 0,
                MaxConcurrentSessions = 1,
                ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // ─── MCQ Entities ────────────────────────────────────────────

        // Assessment
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Type);
        });

        // McqQuestion
        modelBuilder.Entity<McqQuestion>(entity =>
        {
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => new { e.AssessmentId, e.Complexity });

            entity.HasOne(e => e.Assessment)
                  .WithMany(a => a.Questions)
                  .HasForeignKey(e => e.AssessmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // McqTest
        modelBuilder.Entity<McqTest>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AssessmentId);
            entity.HasIndex(e => new { e.UserId, e.AssessmentId });

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Assessment)
                  .WithMany()
                  .HasForeignKey(e => e.AssessmentId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // McqAnswer
        modelBuilder.Entity<McqAnswer>(entity =>
        {
            entity.HasIndex(e => e.TestId);
            entity.HasIndex(e => new { e.TestId, e.QuestionId }).IsUnique();

            entity.HasOne(e => e.Test)
                  .WithMany(t => t.Answers)
                  .HasForeignKey(e => e.TestId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                  .WithMany()
                  .HasForeignKey(e => e.QuestionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // User → Assessment (optional FK)
        modelBuilder.Entity<User>(entity2 =>
        {
            entity2.HasOne(u => u.Assessment)
                   .WithMany()
                   .HasForeignKey(u => u.AssessmentId)
                   .OnDelete(DeleteBehavior.SetNull);
        });

        // AdminUser
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(e => e.UserID).IsUnique();
        });

        // ManualTestScenario
        modelBuilder.Entity<ManualTestScenario>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.AssessmentId });

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Assessment)
                  .WithMany()
                  .HasForeignKey(e => e.AssessmentId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ManualTestCase
        modelBuilder.Entity<ManualTestCase>(entity =>
        {
            entity.HasIndex(e => e.ScenarioDbId);

            entity.HasOne(e => e.Scenario)
                  .WithMany(s => s.TestCases)
                  .HasForeignKey(e => e.ScenarioDbId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Survey Module ──────────────────────────────────────────

        // Survey
        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.IsDeleted);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.EmailSettings)
                  .WithOne(s => s.Survey)
                  .HasForeignKey<SurveyEmailSettings>(s => s.SurveyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SurveyField
        modelBuilder.Entity<SurveyField>(entity =>
        {
            entity.HasIndex(e => new { e.SurveyId, e.SortOrder });

            entity.HasOne(e => e.Survey)
                  .WithMany(s => s.Fields)
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Dependencies)
                  .WithOne(d => d.Field)
                  .HasForeignKey(d => d.FieldId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.DependentFields)
                  .WithOne(d => d.DependsOnField)
                  .HasForeignKey(d => d.DependsOnFieldId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SurveyFieldDependency
        modelBuilder.Entity<SurveyFieldDependency>(entity =>
        {
            entity.HasIndex(e => e.FieldId);
            entity.HasIndex(e => e.DependsOnFieldId);
        });

        // SurveyParticipant
        modelBuilder.Entity<SurveyParticipant>(entity =>
        {
            entity.HasIndex(e => new { e.SurveyId, e.EmployeeEmail });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BatchId);

            entity.HasOne(e => e.Survey)
                  .WithMany(s => s.Participants)
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Distribution)
                  .WithOne(d => d.Participant)
                  .HasForeignKey<SurveyDistribution>(d => d.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.StatusLog)
                  .WithOne(l => l.Participant)
                  .HasForeignKey<SurveyParticipantStatusLog>(l => l.ParticipantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SurveyDistribution
        modelBuilder.Entity<SurveyDistribution>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.SurveyId, e.ParticipantId });
            entity.HasIndex(e => e.EmailStatus);

            entity.HasOne(e => e.Survey)
                  .WithMany(s => s.Distributions)
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SurveyResponse
        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasIndex(e => e.SurveyId);
            entity.HasIndex(e => new { e.ParticipantId, e.SurveyId });
            entity.HasIndex(e => e.SubmittedAt);

            entity.HasOne(e => e.Survey)
                  .WithMany(s => s.Responses)
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Participant)
                  .WithMany()
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Distribution)
                  .WithMany()
                  .HasForeignKey(e => e.DistributionId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SurveyResponseAnswer
        modelBuilder.Entity<SurveyResponseAnswer>(entity =>
        {
            entity.HasIndex(e => e.ResponseId);
            entity.HasIndex(e => new { e.ResponseId, e.FieldId });

            entity.HasOne(e => e.Response)
                  .WithMany(r => r.Answers)
                  .HasForeignKey(e => e.ResponseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Field)
                  .WithMany()
                  .HasForeignKey(e => e.FieldId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SurveyParticipantStatusLog
        modelBuilder.Entity<SurveyParticipantStatusLog>(entity =>
        {
            entity.HasIndex(e => e.ParticipantId);
            entity.HasIndex(e => e.SurveyId);

            entity.HasOne(e => e.Survey)
                  .WithMany()
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.MarkedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.MarkedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // SurveyReminderLog
        modelBuilder.Entity<SurveyReminderLog>(entity =>
        {
            entity.HasIndex(e => e.DistributionId);

            entity.HasOne(e => e.Distribution)
                  .WithMany(d => d.Reminders)
                  .HasForeignKey(e => e.DistributionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SurveyOtp
        modelBuilder.Entity<SurveyOtp>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipantId, e.SurveyId });
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.Participant)
                  .WithMany()
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Survey)
                  .WithMany()
                  .HasForeignKey(e => e.SurveyId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
