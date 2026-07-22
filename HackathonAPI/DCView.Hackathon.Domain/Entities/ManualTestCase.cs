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

    /// <summary>S.No (row number in the test case table)</summary>
    public int SNo { get; set; } = 1;

    /// <summary>Scenario ID reference (e.g., "SC_E-Nach Integration_001")</summary>
    [MaxLength(200)]
    public string? ScenarioId { get; set; }

    /// <summary>Test Case ID (e.g., "TC-F-E-Nach Integration-001")</summary>
    [Required, MaxLength(200)]
    public string TestCaseId { get; set; } = string.Empty;

    /// <summary>Test Case Description</summary>
    public string? TestCaseDescription { get; set; }

    /// <summary>Step number (e.g., "Step 1", "Step 2")</summary>
    [Required, MaxLength(20)]
    public string StepNo { get; set; } = string.Empty;

    /// <summary>Test Step / Input Specification</summary>
    public string? InputSpecification { get; set; }

    /// <summary>Input / Test Data</summary>
    public string? InputTestData { get; set; }

    /// <summary>Expected Result</summary>
    public string? ExpectedResult { get; set; }

    /// <summary>Display order within the test case</summary>
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(ScenarioDbId))]
    public virtual ManualTestScenario Scenario { get; set; } = null!;
}
