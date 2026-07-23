using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DCView.Hackathon.Domain.Enums;

namespace DCView.Hackathon.Domain.Entities;

[Table("Survey_ReminderLogs")]
public class SurveyReminderLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid DistributionId { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public int ReminderNumber { get; set; }

    public SurveyEmailStatus EmailStatus { get; set; } = SurveyEmailStatus.Sent;

    // Navigation
    [ForeignKey(nameof(DistributionId))]
    public virtual SurveyDistribution? Distribution { get; set; }
}
