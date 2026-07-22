using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Application.DTOs.Mcq;
using DCView.Hackathon.Application.Interfaces;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Shared.Helpers;

namespace DCView.Hackathon.Application.Services;

public class McqService : IMcqService
{
    private readonly DbContext _db;
    private readonly IUserRepository _userRepo;

    public McqService(DbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    // ─── Admin: Assessment CRUD ──────────────────────────────────

    public async Task<IEnumerable<AssessmentDto>> GetAllAssessmentsAsync()
    {
        var assessments = await _db.Set<Assessment>()
            .Include(a => a.Questions)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        return assessments.Select(a => MapAssessmentDto(a, a.Questions.Count(q => q.IsActive)));
    }

    public async Task<AssessmentDto?> GetAssessmentByIdAsync(int id)
    {
        var a = await _db.Set<Assessment>()
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id);
        return a != null ? MapAssessmentDto(a, a.Questions.Count(q => q.IsActive)) : null;
    }

    public async Task<AssessmentDto> CreateAssessmentAsync(CreateAssessmentDto dto, string createdBy)
    {
        var assessment = new Assessment
        {
            Title = dto.Title.Trim(),
            Type = dto.Type,
            SubType = dto.SubType.Trim(),
            DurationMinutes = dto.DurationMinutes,
            TotalQuestions = dto.TotalQuestions,
            MaxMarks = dto.MaxMarks,
            SimplePercentage = dto.SimplePercentage,
            MediumPercentage = dto.MediumPercentage,
            ComplexPercentage = dto.ComplexPercentage,
            SimpleMarks = dto.SimpleMarks,
            MediumMarks = dto.MediumMarks,
            ComplexMarks = dto.ComplexMarks,
            NegativeMarking = dto.NegativeMarking,
            NegativeMarkValue = dto.NegativeMarkValue,
            ShuffleQuestions = dto.ShuffleQuestions,
            ShuffleOptions = dto.ShuffleOptions,
            ShowResultImmediately = dto.ShowResultImmediately,
            PassPercentage = dto.PassPercentage,
            AllowNavigation = dto.AllowNavigation,
            AllowReview = dto.AllowReview,
            AutoSubmitOnTimeout = dto.AutoSubmitOnTimeout,
            OneQuestionPerPage = dto.OneQuestionPerPage,
            ShowComplexity = dto.ShowComplexity,
            ShowMarks = dto.ShowMarks,
            IsActive = true,
            CreatedDate = DateTimeHelper.Now,
            CreatedBy = createdBy
        };

        _db.Set<Assessment>().Add(assessment);
        await _db.SaveChangesAsync();
        return MapAssessmentDto(assessment, 0);
    }

    public async Task<bool> UpdateAssessmentAsync(int id, UpdateAssessmentDto dto, string modifiedBy)
    {
        var a = await _db.Set<Assessment>().FindAsync(id);
        if (a == null) return false;

        a.Title = dto.Title.Trim();
        a.Type = dto.Type;
        a.SubType = dto.SubType.Trim();
        a.DurationMinutes = dto.DurationMinutes;
        a.TotalQuestions = dto.TotalQuestions;
        a.MaxMarks = dto.MaxMarks;
        a.SimplePercentage = dto.SimplePercentage;
        a.MediumPercentage = dto.MediumPercentage;
        a.ComplexPercentage = dto.ComplexPercentage;
        a.SimpleMarks = dto.SimpleMarks;
        a.MediumMarks = dto.MediumMarks;
        a.ComplexMarks = dto.ComplexMarks;
        a.NegativeMarking = dto.NegativeMarking;
        a.NegativeMarkValue = dto.NegativeMarkValue;
        a.ShuffleQuestions = dto.ShuffleQuestions;
        a.ShuffleOptions = dto.ShuffleOptions;
        a.ShowResultImmediately = dto.ShowResultImmediately;
        a.PassPercentage = dto.PassPercentage;
        a.AllowNavigation = dto.AllowNavigation;
        a.AllowReview = dto.AllowReview;
        a.AutoSubmitOnTimeout = dto.AutoSubmitOnTimeout;
        a.OneQuestionPerPage = dto.OneQuestionPerPage;
        a.ShowComplexity = dto.ShowComplexity;
        a.ShowMarks = dto.ShowMarks;
        a.IsActive = dto.IsActive;
        a.ModifiedDate = DateTimeHelper.Now;
        a.ModifiedBy = modifiedBy;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAssessmentAsync(int id)
    {
        var a = await _db.Set<Assessment>().FindAsync(id);
        if (a == null) return false;
        _db.Set<Assessment>().Remove(a);
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── Admin: Question Bank ────────────────────────────────────

    public async Task<IEnumerable<McqQuestionDto>> GetQuestionsByAssessmentAsync(int assessmentId)
    {
        return await _db.Set<McqQuestion>()
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.SNo)
            .Select(q => new McqQuestionDto
            {
                Id = q.Id,
                SNo = q.SNo,
                Question = q.Question,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                CorrectAnswer = q.CorrectAnswer,
                Complexity = q.Complexity,
                Marks = q.Marks,
                Category = q.Category,
                Explanation = q.Explanation,
                IsActive = q.IsActive
            })
            .ToListAsync();
    }

    public async Task<McqQuestionDto> AddQuestionAsync(int assessmentId, CreateMcqQuestionDto dto)
    {
        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId)
            ?? throw new InvalidOperationException("Assessment not found.");

        var marks = dto.Marks ?? GetDefaultMarks(dto.Complexity, assessment);

        var question = new McqQuestion
        {
            AssessmentId = assessmentId,
            SNo = dto.SNo,
            Question = dto.Question.Trim(),
            OptionA = dto.OptionA.Trim(),
            OptionB = dto.OptionB.Trim(),
            OptionC = dto.OptionC.Trim(),
            OptionD = dto.OptionD.Trim(),
            CorrectAnswer = dto.CorrectAnswer.Trim().ToUpper(),
            Complexity = dto.Complexity.Trim(),
            Marks = marks,
            Category = dto.Category?.Trim(),
            Explanation = dto.Explanation?.Trim(),
            IsActive = true,
            CreatedDate = DateTimeHelper.Now
        };

        _db.Set<McqQuestion>().Add(question);
        await _db.SaveChangesAsync();

        return new McqQuestionDto
        {
            Id = question.Id, SNo = question.SNo, Question = question.Question,
            OptionA = question.OptionA, OptionB = question.OptionB,
            OptionC = question.OptionC, OptionD = question.OptionD,
            CorrectAnswer = question.CorrectAnswer, Complexity = question.Complexity,
            Marks = question.Marks, Category = question.Category,
            Explanation = question.Explanation, IsActive = question.IsActive
        };
    }

    public async Task<int> BulkUploadQuestionsAsync(int assessmentId, IEnumerable<CreateMcqQuestionDto> questions)
    {
        var assessment = await _db.Set<Assessment>().FindAsync(assessmentId)
            ?? throw new InvalidOperationException("Assessment not found.");

        int count = 0;
        foreach (var dto in questions)
        {
            var marks = dto.Marks ?? GetDefaultMarks(dto.Complexity, assessment);
            _db.Set<McqQuestion>().Add(new McqQuestion
            {
                AssessmentId = assessmentId,
                SNo = dto.SNo > 0 ? dto.SNo : count + 1,
                Question = dto.Question.Trim(),
                OptionA = dto.OptionA.Trim(),
                OptionB = dto.OptionB.Trim(),
                OptionC = dto.OptionC.Trim(),
                OptionD = dto.OptionD.Trim(),
                CorrectAnswer = dto.CorrectAnswer.Trim().ToUpper(),
                Complexity = dto.Complexity.Trim(),
                Marks = marks,
                Category = dto.Category?.Trim(),
                Explanation = dto.Explanation?.Trim(),
                IsActive = true,
                CreatedDate = DateTimeHelper.Now
            });
            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    public async Task<bool> UpdateQuestionAsync(int questionId, CreateMcqQuestionDto dto)
    {
        var q = await _db.Set<McqQuestion>().Include(x => x.Assessment).FirstOrDefaultAsync(x => x.Id == questionId);
        if (q == null) return false;

        q.SNo = dto.SNo;
        q.Question = dto.Question.Trim();
        q.OptionA = dto.OptionA.Trim();
        q.OptionB = dto.OptionB.Trim();
        q.OptionC = dto.OptionC.Trim();
        q.OptionD = dto.OptionD.Trim();
        q.CorrectAnswer = dto.CorrectAnswer.Trim().ToUpper();
        q.Complexity = dto.Complexity.Trim();
        q.Marks = dto.Marks ?? GetDefaultMarks(dto.Complexity, q.Assessment);
        q.Category = dto.Category?.Trim();
        q.Explanation = dto.Explanation?.Trim();

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var q = await _db.Set<McqQuestion>().FindAsync(questionId);
        if (q == null) return false;
        _db.Set<McqQuestion>().Remove(q);
        await _db.SaveChangesAsync();
        return true;
    }

    // ─── Admin: Results ──────────────────────────────────────────

    public async Task<IEnumerable<McqTestResultDto>> GetAllTestResultsAsync(int assessmentId)
    {
        return await _db.Set<McqTest>()
            .Include(t => t.User)
            .Include(t => t.Assessment)
            .Where(t => t.AssessmentId == assessmentId && t.IsSubmitted)
            .OrderByDescending(t => t.Score)
            .Select(t => new McqTestResultDto
            {
                TestId = t.Id,
                UserID = t.User.UserID,
                FullName = t.User.FullName,
                AssessmentTitle = t.Assessment.Title,
                StartedAt = t.StartedAt,
                SubmittedAt = t.SubmittedAt,
                TotalQuestions = t.TotalQuestions,
                Attempted = t.Attempted,
                Correct = t.Correct,
                Wrong = t.Wrong,
                Skipped = t.Skipped,
                Score = t.Score,
                MaxScore = t.MaxScore,
                Percentage = t.Percentage,
                Passed = t.Passed,
                IsAutoSubmitted = t.IsAutoSubmitted,
                TimeSpentSeconds = t.TimeSpentSeconds
            })
            .ToListAsync();
    }

    public async Task<McqTestResultDto?> GetUserTestResultAsync(int userId, int assessmentId)
    {
        var t = await _db.Set<McqTest>()
            .Include(x => x.User)
            .Include(x => x.Assessment)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AssessmentId == assessmentId && x.IsSubmitted);

        if (t == null) return null;

        return new McqTestResultDto
        {
            TestId = t.Id, UserID = t.User.UserID, FullName = t.User.FullName,
            AssessmentTitle = t.Assessment.Title, StartedAt = t.StartedAt,
            SubmittedAt = t.SubmittedAt, TotalQuestions = t.TotalQuestions,
            Attempted = t.Attempted, Correct = t.Correct, Wrong = t.Wrong,
            Skipped = t.Skipped, Score = t.Score, MaxScore = t.MaxScore,
            Percentage = t.Percentage, Passed = t.Passed,
            IsAutoSubmitted = t.IsAutoSubmitted, TimeSpentSeconds = t.TimeSpentSeconds
        };
    }

    // ─── Participant: Test Flow ──────────────────────────────────

    public async Task<McqTestInfoDto> GetTestInfoAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.AssessmentId == null)
            throw new InvalidOperationException("No assessment assigned to you.");

        var assessment = await _db.Set<Assessment>().FindAsync(user.AssessmentId.Value)
            ?? throw new InvalidOperationException("Assessment not found.");

        if (assessment.Type != "MCQ")
            throw new InvalidOperationException("Your assigned assessment is not an MCQ test.");

        int simpleCount = (int)Math.Round(assessment.TotalQuestions * assessment.SimplePercentage / 100.0);
        int mediumCount = (int)Math.Round(assessment.TotalQuestions * assessment.MediumPercentage / 100.0);
        int complexCount = assessment.TotalQuestions - simpleCount - mediumCount;

        // Check for existing test (in-progress or submitted)
        var existingTest = await _db.Set<McqTest>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.AssessmentId == assessment.Id && !t.IsSubmitted && t.IsInProgress);

        var submittedTest = await _db.Set<McqTest>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.AssessmentId == assessment.Id && t.IsSubmitted);

        return new McqTestInfoDto
        {
            AssessmentId = assessment.Id,
            Title = assessment.Title,
            TotalQuestions = assessment.TotalQuestions,
            DurationMinutes = assessment.DurationMinutes ?? 0,
            MaxMarks = assessment.MaxMarks,
            NegativeMarking = assessment.NegativeMarking,
            NegativeMarkValue = assessment.NegativeMarkValue,
            AllowNavigation = assessment.AllowNavigation,
            AllowReview = assessment.AllowReview,
            OneQuestionPerPage = assessment.OneQuestionPerPage,
            ShowComplexity = assessment.ShowComplexity,
            ShowMarks = assessment.ShowMarks,
            SimpleCount = simpleCount,
            MediumCount = mediumCount,
            ComplexCount = complexCount,
            SimpleMarks = assessment.SimpleMarks,
            MediumMarks = assessment.MediumMarks,
            ComplexMarks = assessment.ComplexMarks,
            HasExistingTest = existingTest != null,
            ExistingTestId = existingTest?.Id,
            IsAlreadySubmitted = submittedTest != null,
            // Only expose scores if ShowResultImmediately is enabled
            SubmittedScore = assessment.ShowResultImmediately ? submittedTest?.Score : null,
            SubmittedMaxScore = assessment.ShowResultImmediately ? submittedTest?.MaxScore : null,
            SubmittedPercentage = assessment.ShowResultImmediately ? submittedTest?.Percentage : null,
            SubmittedPassed = assessment.ShowResultImmediately ? submittedTest?.Passed : null
        };
    }

    public async Task<StartTestResultDto> StartTestAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        if (user.AssessmentId == null)
            throw new InvalidOperationException("No assessment assigned.");

        var assessment = await _db.Set<Assessment>()
            .Include(a => a.Questions.Where(q => q.IsActive))
            .FirstOrDefaultAsync(a => a.Id == user.AssessmentId.Value)
            ?? throw new InvalidOperationException("Assessment not found.");

        // Block if already submitted
        var submitted = await _db.Set<McqTest>()
            .AnyAsync(t => t.UserId == userId && t.AssessmentId == assessment.Id && t.IsSubmitted);
        if (submitted)
            throw new InvalidOperationException("You have already submitted this test. You cannot retake it.");

        // Check if already has an in-progress test
        var existing = await _db.Set<McqTest>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.AssessmentId == assessment.Id && t.IsInProgress && !t.IsSubmitted);

        if (existing != null)
        {
            return new StartTestResultDto
            {
                TestId = existing.Id,
                TotalQuestions = existing.TotalQuestions,
                StartedAt = existing.StartedAt!.Value,
                ExpiresAt = existing.StartedAt!.Value.AddMinutes(assessment.DurationMinutes ?? 9999),
                DurationMinutes = assessment.DurationMinutes ?? 0
            };
        }

        // Select questions per complexity distribution
        int simpleCount = (int)Math.Round(assessment.TotalQuestions * assessment.SimplePercentage / 100.0);
        int mediumCount = (int)Math.Round(assessment.TotalQuestions * assessment.MediumPercentage / 100.0);
        int complexCount = assessment.TotalQuestions - simpleCount - mediumCount;

        var rng = new Random();
        var simpleQuestions = assessment.Questions.Where(q => q.Complexity == "Simple").OrderBy(_ => rng.Next()).Take(simpleCount).ToList();
        var mediumQuestions = assessment.Questions.Where(q => q.Complexity == "Medium").OrderBy(_ => rng.Next()).Take(mediumCount).ToList();
        var complexQuestions = assessment.Questions.Where(q => q.Complexity == "Complex").OrderBy(_ => rng.Next()).Take(complexCount).ToList();

        var selectedQuestions = simpleQuestions.Concat(mediumQuestions).Concat(complexQuestions).ToList();

        // Shuffle the combined list if configured
        if (assessment.ShuffleQuestions)
            selectedQuestions = selectedQuestions.OrderBy(_ => rng.Next()).ToList();

        var questionOrder = selectedQuestions.Select(q => q.Id).ToList();

        // Generate option order if shuffle enabled
        string? optionOrder = null;
        if (assessment.ShuffleOptions)
        {
            var optionMap = new Dictionary<int, List<string>>();
            foreach (var q in selectedQuestions)
            {
                var opts = new List<string> { "A", "B", "C", "D" };
                optionMap[q.Id] = opts.OrderBy(_ => rng.Next()).ToList();
            }
            optionOrder = JsonSerializer.Serialize(optionMap);
        }

        // Calculate max score
        decimal maxScore = simpleQuestions.Sum(q => q.Marks)
            + mediumQuestions.Sum(q => q.Marks)
            + complexQuestions.Sum(q => q.Marks);

        var now = DateTimeHelper.Now;
        var test = new McqTest
        {
            UserId = userId,
            AssessmentId = assessment.Id,
            StartedAt = now,
            TotalQuestions = selectedQuestions.Count,
            MaxScore = maxScore,
            IsInProgress = true,
            QuestionOrder = JsonSerializer.Serialize(questionOrder),
            OptionOrder = optionOrder
        };

        _db.Set<McqTest>().Add(test);
        await _db.SaveChangesAsync();

        // Pre-create answer rows
        for (int i = 0; i < selectedQuestions.Count; i++)
        {
            _db.Set<McqAnswer>().Add(new McqAnswer
            {
                TestId = test.Id,
                QuestionId = selectedQuestions[i].Id,
                QuestionIndex = i + 1
            });
        }
        await _db.SaveChangesAsync();

        return new StartTestResultDto
        {
            TestId = test.Id,
            TotalQuestions = test.TotalQuestions,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(assessment.DurationMinutes ?? 9999),
            DurationMinutes = assessment.DurationMinutes ?? 0
        };
    }

    public async Task<McqTestStatusDto> GetTestStatusAsync(int userId)
    {
        var test = await GetActiveTestAsync(userId);
        var assessment = await _db.Set<Assessment>().FindAsync(test.AssessmentId);
        var answers = await _db.Set<McqAnswer>().Where(a => a.TestId == test.Id).OrderBy(a => a.QuestionIndex).ToListAsync();

        DateTime? expiresAt = assessment?.DurationMinutes != null && test.StartedAt.HasValue
            ? test.StartedAt.Value.AddMinutes(assessment.DurationMinutes.Value)
            : null;

        int? remainingSeconds = expiresAt.HasValue
            ? Math.Max(0, (int)(expiresAt.Value - DateTimeHelper.Now).TotalSeconds)
            : null;

        // If time has expired, auto-submit now (immediate catch before background job)
        if (remainingSeconds == 0 && expiresAt.HasValue && DateTimeHelper.Now > expiresAt.Value)
        {
            await SubmitTestAsync(userId, true);
            return new McqTestStatusDto
            {
                TestId = test.Id,
                IsInProgress = false,
                IsSubmitted = true,
                StartedAt = test.StartedAt,
                ExpiresAt = expiresAt,
                RemainingSeconds = 0,
                TotalQuestions = test.TotalQuestions,
                Answered = answers.Count(a => a.SelectedAnswer != null),
                Flagged = 0,
                NavigationPanel = new()
            };
        }

        return new McqTestStatusDto
        {
            TestId = test.Id,
            IsInProgress = test.IsInProgress,
            IsSubmitted = test.IsSubmitted,
            StartedAt = test.StartedAt,
            ExpiresAt = expiresAt,
            RemainingSeconds = remainingSeconds,
            TotalQuestions = test.TotalQuestions,
            Answered = answers.Count(a => a.SelectedAnswer != null),
            Flagged = answers.Count(a => a.IsFlagged),
            NavigationPanel = answers.Select(a => new QuestionNavItem
            {
                QuestionIndex = a.QuestionIndex,
                QuestionId = a.QuestionId,
                IsAnswered = a.SelectedAnswer != null,
                IsFlagged = a.IsFlagged
            }).ToList()
        };
    }

    public async Task<McqQuestionForTestDto?> GetQuestionAsync(int userId, int questionIndex)
    {
        var test = await GetActiveTestAsync(userId);
        var assessment = await _db.Set<Assessment>().FindAsync(test.AssessmentId);
        var answer = await _db.Set<McqAnswer>()
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.TestId == test.Id && a.QuestionIndex == questionIndex);

        if (answer == null) return null;

        return MapQuestionForTest(answer, test, assessment);
    }

    public async Task<IEnumerable<McqQuestionForTestDto>> GetAllQuestionsAsync(int userId)
    {
        var test = await GetActiveTestAsync(userId);
        var assessment = await _db.Set<Assessment>().FindAsync(test.AssessmentId);
        var answers = await _db.Set<McqAnswer>()
            .Include(a => a.Question)
            .Where(a => a.TestId == test.Id)
            .OrderBy(a => a.QuestionIndex)
            .ToListAsync();

        return answers.Select(a => MapQuestionForTest(a, test, assessment));
    }

    public async Task<bool> SaveAnswerAsync(int userId, SaveAnswerDto dto)
    {
        var test = await GetActiveTestAsync(userId);

        // Check if test time has expired
        var assessment = await _db.Set<Assessment>().FindAsync(test.AssessmentId);
        if (assessment?.DurationMinutes != null && test.StartedAt.HasValue)
        {
            var expiresAt = test.StartedAt.Value.AddMinutes(assessment.DurationMinutes.Value);
            if (DateTimeHelper.Now > expiresAt)
                throw new InvalidOperationException("Test time has expired. Your test will be auto-submitted.");
        }

        var answer = await _db.Set<McqAnswer>()
            .FirstOrDefaultAsync(a => a.TestId == test.Id && a.QuestionId == dto.QuestionId);

        if (answer == null) return false;

        if (dto.SelectedAnswer != null)
        {
            answer.SelectedAnswer = dto.SelectedAnswer.Trim().ToUpper();
            answer.AnsweredAt = DateTimeHelper.Now;
        }
        else
        {
            answer.SelectedAnswer = null;
            answer.AnsweredAt = null;
        }

        if (dto.TimeTakenSeconds.HasValue)
            answer.TimeTakenSeconds = (answer.TimeTakenSeconds ?? 0) + dto.TimeTakenSeconds.Value;

        if (dto.IsFlagged.HasValue)
            answer.IsFlagged = dto.IsFlagged.Value;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FlagQuestionAsync(int userId, int questionId, bool flagged)
    {
        var test = await GetActiveTestAsync(userId);
        var answer = await _db.Set<McqAnswer>()
            .FirstOrDefaultAsync(a => a.TestId == test.Id && a.QuestionId == questionId);
        if (answer == null) return false;

        answer.IsFlagged = flagged;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<McqSubmitResultDto> SubmitTestAsync(int userId, bool isAutoSubmit = false)
    {
        var test = await _db.Set<McqTest>()
            .Include(t => t.Answers).ThenInclude(a => a.Question)
            .Include(t => t.Assessment)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsInProgress && !t.IsSubmitted)
            ?? throw new InvalidOperationException("No active test found.");

        var assessment = test.Assessment;
        var now = DateTimeHelper.Now;

        // Calculate results
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

        int timeSpent = test.StartedAt.HasValue ? (int)(now - test.StartedAt.Value).TotalSeconds : 0;

        // Update test record
        test.IsInProgress = false;
        test.IsSubmitted = true;
        test.SubmittedAt = now;
        test.IsAutoSubmitted = isAutoSubmit;
        test.Attempted = correct + wrong;
        test.Correct = correct;
        test.Wrong = wrong;
        test.Skipped = skipped;
        test.Score = totalScore;
        test.Percentage = percentage;
        test.Passed = passed;
        test.TimeSpentSeconds = timeSpent;

        await _db.SaveChangesAsync();

        var result = new McqSubmitResultDto
        {
            TotalQuestions = test.TotalQuestions,
            TimeSpentSeconds = timeSpent,
            Message = "Test submitted successfully. Thank you!"
        };

        // Only expose scores if ShowResultImmediately is enabled
        if (assessment.ShowResultImmediately)
        {
            result.Score = totalScore;
            result.MaxScore = test.MaxScore;
            result.Percentage = percentage;
            result.Correct = correct;
            result.Wrong = wrong;
            result.Skipped = skipped;
            result.Passed = passed;
            result.ShowScores = true;
            result.Message = passed == true ? "Congratulations! You passed." :
                             passed == false ? "You did not meet the pass percentage." :
                             "Test submitted successfully.";

            result.DetailedResults = test.Answers.OrderBy(a => a.QuestionIndex).Select(a => new McqAnswerReviewDto
            {
                QuestionIndex = a.QuestionIndex,
                Question = a.Question.Question,
                OptionA = a.Question.OptionA,
                OptionB = a.Question.OptionB,
                OptionC = a.Question.OptionC,
                OptionD = a.Question.OptionD,
                CorrectAnswer = a.Question.CorrectAnswer,
                SelectedAnswer = a.SelectedAnswer,
                IsCorrect = a.IsCorrect == true,
                MarksAwarded = a.MarksAwarded,
                Explanation = a.Question.Explanation
            }).ToList();
        }

        return result;
    }

    // ─── Private Helpers ─────────────────────────────────────────

    private async Task<McqTest> GetActiveTestAsync(int userId)
    {
        return await _db.Set<McqTest>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsInProgress && !t.IsSubmitted)
            ?? throw new InvalidOperationException("No active test found. Please start your test first.");
    }

    private McqQuestionForTestDto MapQuestionForTest(McqAnswer answer, McqTest test, Assessment? assessment)
    {
        var q = answer.Question;
        string optA = q.OptionA, optB = q.OptionB, optC = q.OptionC, optD = q.OptionD;

        // Apply option shuffling if configured
        if (!string.IsNullOrEmpty(test.OptionOrder))
        {
            try
            {
                var optionMap = JsonSerializer.Deserialize<Dictionary<int, List<string>>>(test.OptionOrder);
                if (optionMap != null && optionMap.TryGetValue(q.Id, out var order) && order.Count == 4)
                {
                    var originalOptions = new Dictionary<string, string>
                    {
                        ["A"] = q.OptionA, ["B"] = q.OptionB, ["C"] = q.OptionC, ["D"] = q.OptionD
                    };
                    optA = originalOptions[order[0]];
                    optB = originalOptions[order[1]];
                    optC = originalOptions[order[2]];
                    optD = originalOptions[order[3]];
                }
            }
            catch { /* If deserialization fails, show original order */ }
        }

        return new McqQuestionForTestDto
        {
            QuestionId = q.Id,
            QuestionIndex = answer.QuestionIndex,
            Question = q.Question,
            OptionA = optA,
            OptionB = optB,
            OptionC = optC,
            OptionD = optD,
            Complexity = assessment?.ShowComplexity == true ? q.Complexity : "",
            Marks = assessment?.ShowMarks == true ? q.Marks : 0,
            Category = q.Category,
            SelectedAnswer = answer.SelectedAnswer,
            IsFlagged = answer.IsFlagged
        };
    }

    private static int GetDefaultMarks(string complexity, Assessment assessment)
    {
        return complexity.ToLower() switch
        {
            "simple" => assessment.SimpleMarks,
            "medium" => assessment.MediumMarks,
            "complex" => assessment.ComplexMarks,
            _ => 1
        };
    }

    private static AssessmentDto MapAssessmentDto(Assessment a, int questionCount)
    {
        return new AssessmentDto
        {
            Id = a.Id, Title = a.Title, Type = a.Type, SubType = a.SubType,
            DurationMinutes = a.DurationMinutes, TotalQuestions = a.TotalQuestions,
            MaxMarks = a.MaxMarks, SimplePercentage = a.SimplePercentage,
            MediumPercentage = a.MediumPercentage, ComplexPercentage = a.ComplexPercentage,
            SimpleMarks = a.SimpleMarks, MediumMarks = a.MediumMarks, ComplexMarks = a.ComplexMarks,
            NegativeMarking = a.NegativeMarking, NegativeMarkValue = a.NegativeMarkValue,
            ShuffleQuestions = a.ShuffleQuestions, ShuffleOptions = a.ShuffleOptions,
            ShowResultImmediately = a.ShowResultImmediately, PassPercentage = a.PassPercentage,
            AllowNavigation = a.AllowNavigation, AllowReview = a.AllowReview,
            AutoSubmitOnTimeout = a.AutoSubmitOnTimeout, OneQuestionPerPage = a.OneQuestionPerPage,
            ShowComplexity = a.ShowComplexity, ShowMarks = a.ShowMarks,
            IsActive = a.IsActive, QuestionBankCount = questionCount, CreatedDate = a.CreatedDate
        };
    }
}
