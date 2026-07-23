namespace DCView.Hackathon.Domain.Enums;

public enum SurveyEmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Bounced = 3
}

/// <summary>
/// How survey invitation emails are sent.
/// </summary>
public enum SurveyEmailMode
{
    /// <summary>
    /// One single email with all participants in TO, all unique RMs/VHs in CC.
    /// </summary>
    SingleBulk = 0,

    /// <summary>
    /// Individual email per participant (only TO them). Separate summary email to RMs/VHs.
    /// </summary>
    IndividualWithSummary = 1,

    /// <summary>
    /// Individual email per participant with their RM/VH in CC on each email.
    /// </summary>
    IndividualWithCc = 2
}
