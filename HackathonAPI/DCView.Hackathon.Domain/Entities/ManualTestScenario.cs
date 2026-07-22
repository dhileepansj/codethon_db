using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// A test scenario written by a participant during a Manual Testing hackathon.
/// </summary>
[Table("Hackathon_ManualTestScenarios")]
public class ManualTestScenario
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int AssessmentId { get; set; }

    /// <summary>Scenario ID entered by participant (e.g., "SC_Login_001")</summary>
    [Required, MaxLength(100)]
    public string ScenarioId { get; set; } = string.Empty;

    /// <summary>Short scenario title</summary>
    [MaxLength(500)]
    public string? Scenario { get; set; }

    /// <summary>Detailed description of the scenario</summary>
    public string? Description { get; set; }

    /// <summary>Must Test: "Yes" or "No"</summary>
    [MaxLength(10)]
    public string? MustTest { get; set; }

    /// <summary>Pass / Fail status</summary>
    [MaxLength(20)]
    public string? PassFail { get; set; }

    /// <summary>Display order</summary>
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AssessmentId))]
    public virtual Assessment Assessment { get; set; } = null!;

    public virtual ICollection<ManualTestCase> TestCases { get; set; } = new List<ManualTestCase>();
}
