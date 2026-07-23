using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_Otps")]
public class SurveyOtp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid SurveyId { get; set; }

    /// <summary>
    /// BCrypt-hashed OTP code.
    /// </summary>
    [Required, MaxLength(200)]
    public string OtpHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Number of failed verification attempts.
    /// </summary>
    public int Attempts { get; set; } = 0;

    public bool IsVerified { get; set; } = false;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Number of OTP resend requests in this session.
    /// </summary>
    public int ResendCount { get; set; } = 0;

    /// <summary>
    /// If locked out due to too many failed attempts, this is the unlock time.
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    // Navigation
    [ForeignKey(nameof(ParticipantId))]
    public virtual SurveyParticipant? Participant { get; set; }

    [ForeignKey(nameof(SurveyId))]
    public virtual Survey? Survey { get; set; }
}
