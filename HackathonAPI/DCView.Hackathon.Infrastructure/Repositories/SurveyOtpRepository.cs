using Microsoft.EntityFrameworkCore;
using DCView.Hackathon.Domain.Entities;
using DCView.Hackathon.Domain.Repositories;
using DCView.Hackathon.Infrastructure.Data;

namespace DCView.Hackathon.Infrastructure.Repositories;

public class SurveyOtpRepository : ISurveyOtpRepository
{
    private readonly HackathonDbContext _db;

    public SurveyOtpRepository(HackathonDbContext db) => _db = db;

    public async Task<SurveyOtp?> GetLatestByParticipantAndSurveyAsync(Guid participantId, Guid surveyId)
        => await _db.SurveyOtps
            .Where(o => o.ParticipantId == participantId && o.SurveyId == surveyId)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<SurveyOtp> CreateAsync(SurveyOtp otp)
    {
        _db.SurveyOtps.Add(otp);
        await _db.SaveChangesAsync();
        return otp;
    }

    public async Task UpdateAsync(SurveyOtp otp)
    {
        _db.SurveyOtps.Update(otp);
        await _db.SaveChangesAsync();
    }

    public async Task InvalidateAllForParticipantAsync(Guid participantId, Guid surveyId)
    {
        var otps = await _db.SurveyOtps
            .Where(o => o.ParticipantId == participantId && o.SurveyId == surveyId && !o.IsVerified)
            .ToListAsync();

        foreach (var otp in otps)
        {
            otp.ExpiresAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
