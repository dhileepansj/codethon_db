using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Infrastructure.Data;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.API.BackgroundServices;

/// <summary>
/// Background service that periodically checks for expired MCQ tests
/// and auto-submits them. Runs every 30 seconds.
/// </summary>
public class McqAutoSubmitService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<McqAutoSubmitService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public McqAutoSubmitService(IServiceScopeFactory scopeFactory, ILogger<McqAutoSubmitService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCQ Auto-Submit background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSubmitExpiredTests(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MCQ auto-submit check.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAndSubmitExpiredTests(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HackathonDbContext>();
        var now = DateTimeHelper.Now;

        // Find all in-progress tests that have expired
        var expiredTests = await db.Set<McqTest>()
            .Include(t => t.Assessment)
            .Include(t => t.Answers).ThenInclude(a => a.Question)
            .Where(t => t.IsInProgress && !t.IsSubmitted && t.StartedAt != null)
            .ToListAsync(ct);

        int submitted = 0;

        foreach (var test in expiredTests)
        {
            if (test.Assessment?.DurationMinutes == null) continue;

            var expiresAt = test.StartedAt!.Value.AddMinutes(test.Assessment.DurationMinutes.Value);
            if (now <= expiresAt) continue; // Not expired yet

            // Auto-submit this test
            try
            {
                var assessment = test.Assessment;
                int correct = 0, wrong = 0, skipped = 0;
                decimal totalScore = 0;

                foreach (var answer in test.Answers)
                {
                    if (answer.SelectedAnswer == null)
                    {
                        skipped++;
                        answer.IsCorrect = null;
                        answer.MarksAwarded = 0;
                    }
                    else if (answer.SelectedAnswer == answer.Question.CorrectAnswer)
                    {
                        correct++;
                        answer.IsCorrect = true;
                        answer.MarksAwarded = answer.Question.Marks;
                        totalScore += answer.Question.Marks;
                    }
                    else
                    {
                        wrong++;
                        answer.IsCorrect = false;
                        if (assessment.NegativeMarking)
                        {
                            answer.MarksAwarded = -assessment.NegativeMarkValue;
                            totalScore -= assessment.NegativeMarkValue;
                        }
                        else
                        {
                            answer.MarksAwarded = 0;
                        }
                    }
                }

                decimal maxScore = test.MaxScore > 0 ? test.MaxScore : 1;
                decimal percentage = Math.Round(totalScore / maxScore * 100, 2);
                bool? passed = assessment.PassPercentage > 0 ? percentage >= assessment.PassPercentage : null;
                int timeSpent = (int)(expiresAt - test.StartedAt!.Value).TotalSeconds;

                test.IsInProgress = false;
                test.IsSubmitted = true;
                test.SubmittedAt = expiresAt; // Use expiry time as submit time
                test.IsAutoSubmitted = true;
                test.Attempted = correct + wrong;
                test.Correct = correct;
                test.Wrong = wrong;
                test.Skipped = skipped;
                test.Score = totalScore;
                test.Percentage = percentage;
                test.Passed = passed;
                test.TimeSpentSeconds = timeSpent;

                submitted++;
                _logger.LogInformation("Auto-submitted expired MCQ test {TestId} for user {UserId}. Score: {Score}/{MaxScore}",
                    test.Id, test.UserId, totalScore, test.MaxScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-submit test {TestId}", test.Id);
            }
        }

        if (submitted > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Auto-submitted {Count} expired MCQ test(s).", submitted);
        }
    }
}
