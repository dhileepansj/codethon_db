namespace DCView.Hackathon.Application.Services;

/// <summary>
/// Builds rendered email HTML from templates with variable substitution.
/// All templates use table-based layout with inline styles for Outlook desktop compatibility.
/// </summary>
public static class SurveyEmailTemplateBuilder
{
    private const string DEFAULT_INVITATION_SUBJECT = "You're invited to fill: {{SurveyTitle}}";

    private const string DEFAULT_INVITATION_BODY = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>Survey Invitation</title>
<!--[if mso]>
<style type=""text/css"">
table {border-collapse:collapse;border-spacing:0;margin:0;}
div, td {padding:0;}
div {margin:0 !important;}
</style>
<noscript>
<xml>
<o:OfficeDocumentSettings>
<o:PixelsPerInch>96</o:PixelsPerInch>
</o:OfficeDocumentSettings>
</xml>
</noscript>
<![endif]-->
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#f4f4f7;"">
<tr>
<td align=""center"" style=""padding:40px 20px;"">

<!-- Container -->
<table role=""presentation"" width=""560"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#ffffff;border:1px solid #e5e7eb;"">

<!-- Header -->
<tr>
<td style=""background-color:#1e40af;padding:28px 32px;"">
<h1 style=""margin:0;font-size:20px;font-weight:600;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">{{SurveyTitle}}</h1>
</td>
</tr>

<!-- Body -->
<tr>
<td style=""padding:32px;"">
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Hi {{EmployeeName}},</p>
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">You have been invited to participate in a survey. Please click the button below to begin.</p>

<!-- Button -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" align=""center"" style=""margin:24px auto;"">
<tr>
<td style=""background-color:#1e40af;padding:14px 32px;text-align:center;"">
<!--[if mso]>
<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{{SurveyLink}}"" style=""height:44px;v-text-anchor:middle;width:200px;"" arcsize=""14%"" strokecolor=""#1e40af"" fillcolor=""#1e40af"">
<w:anchorlock/>
<center style=""color:#ffffff;font-family:'Segoe UI',Tahoma,sans-serif;font-size:14px;font-weight:600;"">Open Survey</center>
</v:roundrect>
<![endif]-->
<!--[if !mso]><!-->
<a href=""{{SurveyLink}}"" target=""_blank"" style=""display:inline-block;background-color:#1e40af;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:14px;font-weight:600;text-decoration:none;padding:14px 32px;border:0;"">Open Survey</a>
<!--<![endif]-->
</td>
</tr>
</table>

<!-- Info Box -->
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:20px 0;"">
<tr>
<td style=""background-color:#f0f4ff;border:1px solid #dbeafe;padding:16px;"">
<p style=""margin:0 0 4px 0;font-size:13px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;""><strong>Survey:</strong> {{SurveyTitle}}</p>
<p style=""margin:0;font-size:13px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;""><strong>Deadline:</strong> {{Deadline}}</p>
</td>
</tr>
</table>

<p style=""margin:16px 0 4px 0;font-size:12px;line-height:18px;color:#6b7280;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">If the button doesn't work, copy and paste this link into your browser:</p>
<p style=""margin:0;font-size:12px;line-height:18px;color:#2563eb;word-break:break-all;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">{{SurveyLink}}</p>
</td>
</tr>

<!-- Footer -->
<tr>
<td style=""background-color:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;"">
<p style=""margin:0;font-size:11px;line-height:16px;color:#9ca3af;text-align:center;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">This is an automated message from NovacCodeLab. Please do not reply.</p>
</td>
</tr>

</table>
<!-- /Container -->

</td>
</tr>
</table>
</body>
</html>";

    private const string DEFAULT_REMINDER_SUBJECT = "Reminder: Please fill — {{SurveyTitle}}";

    private const string DEFAULT_REMINDER_BODY = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>Survey Reminder</title>
<!--[if mso]>
<style type=""text/css"">
table {border-collapse:collapse;border-spacing:0;margin:0;}
div, td {padding:0;}
div {margin:0 !important;}
</style>
<noscript>
<xml>
<o:OfficeDocumentSettings>
<o:PixelsPerInch>96</o:PixelsPerInch>
</o:OfficeDocumentSettings>
</xml>
</noscript>
<![endif]-->
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#f4f4f7;"">
<tr>
<td align=""center"" style=""padding:40px 20px;"">

<!-- Container -->
<table role=""presentation"" width=""560"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#ffffff;border:1px solid #e5e7eb;"">

<!-- Header (Amber for reminder) -->
<tr>
<td style=""background-color:#d97706;padding:28px 32px;"">
<h1 style=""margin:0;font-size:20px;font-weight:600;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Reminder: {{SurveyTitle}}</h1>
</td>
</tr>

<!-- Body -->
<tr>
<td style=""padding:32px;"">
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Hi {{EmployeeName}},</p>
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">This is a gentle reminder that you haven't submitted your response for <strong>{{SurveyTitle}}</strong> yet.</p>
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Please take a few minutes to complete the survey before the deadline.</p>

<!-- Button -->
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" align=""center"" style=""margin:24px auto;"">
<tr>
<td style=""background-color:#d97706;padding:14px 32px;text-align:center;"">
<!--[if mso]>
<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{{SurveyLink}}"" style=""height:44px;v-text-anchor:middle;width:200px;"" arcsize=""14%"" strokecolor=""#d97706"" fillcolor=""#d97706"">
<w:anchorlock/>
<center style=""color:#ffffff;font-family:'Segoe UI',Tahoma,sans-serif;font-size:14px;font-weight:600;"">Open Survey</center>
</v:roundrect>
<![endif]-->
<!--[if !mso]><!-->
<a href=""{{SurveyLink}}"" target=""_blank"" style=""display:inline-block;background-color:#d97706;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:14px;font-weight:600;text-decoration:none;padding:14px 32px;border:0;"">Open Survey</a>
<!--<![endif]-->
</td>
</tr>
</table>

<p style=""margin:16px 0 0 0;font-size:12px;line-height:18px;color:#6b7280;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;""><strong>Deadline:</strong> {{Deadline}}</p>
</td>
</tr>

<!-- Footer -->
<tr>
<td style=""background-color:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;"">
<p style=""margin:0;font-size:11px;line-height:16px;color:#9ca3af;text-align:center;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">This is an automated message from NovacCodeLab. Please do not reply.</p>
</td>
</tr>

</table>
<!-- /Container -->

</td>
</tr>
</table>
</body>
</html>";

    /// <summary>
    /// Renders subject with variables.
    /// </summary>
    public static string RenderSubject(string? template, Dictionary<string, string> variables)
    {
        var subject = string.IsNullOrWhiteSpace(template) ? DEFAULT_INVITATION_SUBJECT : template;
        return ReplaceVariables(subject, variables);
    }

    /// <summary>
    /// Renders reminder subject with variables.
    /// </summary>
    public static string RenderReminderSubject(string? template, Dictionary<string, string> variables)
    {
        var subject = string.IsNullOrWhiteSpace(template) ? DEFAULT_REMINDER_SUBJECT : template;
        return ReplaceVariables(subject, variables);
    }

    /// <summary>
    /// Renders invitation body with variables.
    /// </summary>
    public static string RenderInvitationBody(string? template, Dictionary<string, string> variables)
    {
        var body = string.IsNullOrWhiteSpace(template) ? DEFAULT_INVITATION_BODY : template;
        return ReplaceVariables(body, variables);
    }

    /// <summary>
    /// Renders reminder body with variables.
    /// </summary>
    public static string RenderReminderBody(string? template, Dictionary<string, string> variables)
    {
        var body = string.IsNullOrWhiteSpace(template) ? DEFAULT_REMINDER_BODY : template;
        return ReplaceVariables(body, variables);
    }

    private static string ReplaceVariables(string content, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            content = content.Replace($"{{{{{key}}}}}", value ?? "");
        }
        return content;
    }
}
