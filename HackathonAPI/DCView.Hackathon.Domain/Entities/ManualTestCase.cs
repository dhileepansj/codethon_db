using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DCView.Hackathon.Domain.Entities;

/// <summary>
/// A test case step written by a participant, belonging to a scenario.
/// </summary>
[Table("Hackathon_ManualTestCases")]
public class ManualTestCase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ScenarioDbId { get; set; }

    /// <summary>Test Case ID (e.g., "TC-C-StudentInfo-001")</summary>
    [Required, MaxLength(100)]
    public string TestCaseId { get; set; } = string.Empty;

    /// <summary>Step number within the test case (e.g., "001", "002")</summary>
    [Required, MaxLength(20)]
    public string StepNo { get; set; } = string.Empty;

    /// <summary>What is being tested (input specification)</summary>
    public string? InputSpecification { get; set; }

    /// <summary>Help / remarks for this step</summary>
    public string? HelpRemarks { get; set; }

    /// <summary>The test data used</summary>
    public string? InputTestData { get; set; }

    /// <summary>Expected result description</summary>
    public string? ExpectedResult { get; set; }

    /// <summary>Actual result (if filled)</summary>
    public string? ActualResult { get; set; }

    /// <summary>Pass/Fail for this step</summary>
    [MaxLength(20)]
    public string? StepResult { get; set; }

    /// <summary>Display order within the test case</summary>
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(ScenarioDbId))]
    public virtual ManualTestScenario Scenario { get; set; } = null!;
}
