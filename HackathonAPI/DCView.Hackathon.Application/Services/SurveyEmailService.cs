using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using DCView.Hackathon.Application.Interfaces;

namespace DCView.Hackathon.Application.Services;

public class SurveyEmailService : ISurveyEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SurveyEmailService> _logger;

    private string SmtpServer => _config["SurveyEmail:SmtpServer"] ?? "smtp.falconide.com";
    private int SmtpPort => int.TryParse(_config["SurveyEmail:SmtpPort"], out var p) ? p : 587;
    private bool UseSsl => bool.TryParse(_config["SurveyEmail:UseSsl"], out var s) && s;
    private string SenderEmail => _config["SurveyEmail:SenderEmail"] ?? "ebs_los@novactech.in";
    private string SenderName => _config["SurveyEmail:SenderName"] ?? "NovacCodeLab Surveys";
    private string Username => _config["SurveyEmail:Username"] ?? "";
    private string Password => _config["SurveyEmail:Password"] ?? "";
    private bool Enabled => bool.TryParse(_config["SurveyEmail:Enabled"], out var e) && e;

    public SurveyEmailService(IConfiguration config, ILogger<SurveyEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendInvitationAsync(SurveyEmailMessage message)
    {
        if (!Enabled)
        {
            _logger.LogWarning("Survey email is disabled. Skipping invitation to {Email}", message.ToEmail);
            return true;
        }

        var mime = BuildMimeMessage(message);
        return await SendAsync(mime);
    }

    public async Task<bool> SendBulkInvitationAsync(BulkSurveyEmailMessage message)
    {
        if (!Enabled)
        {
            _logger.LogWarning("Survey email is disabled. Skipping bulk invitation to {Count} recipients", message.ToRecipients.Count);
            return true;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(SenderName, SenderEmail));

        // Add all participants to TO
        foreach (var (email, name) in message.ToRecipients)
        {
            mime.To.Add(new MailboxAddress(name, email));
        }

        // Add all CC emails
        foreach (var cc in message.CcEmails)
        {
            if (IsValidEmail(cc))
                mime.Cc.Add(new MailboxAddress("", cc));
        }

        mime.Subject = message.Subject;
        var body = new BodyBuilder { HtmlBody = message.HtmlBody };
        mime.Body = body.ToMessageBody();

        return await SendAsync(mime);
    }

    public async Task<bool> SendManagerSummaryAsync(string managerEmail, string managerName, string surveyTitle, List<string> reporteeNames)
    {
        if (!Enabled)
        {
            _logger.LogWarning("Survey email is disabled. Skipping manager summary to {Email}", managerEmail);
            return true;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(SenderName, SenderEmail));
        mime.To.Add(new MailboxAddress(managerName, managerEmail));
        mime.Subject = $"Survey Notification: {surveyTitle} — {reporteeNames.Count} team member(s) invited";

        var reporteeList = string.Join("", reporteeNames.Select(n => $"<tr><td style=\"padding:6px 12px;font-size:13px;color:#374151;border-bottom:1px solid #e5e7eb;font-family:'Segoe UI',Tahoma,sans-serif;\">{n}</td></tr>"));
        var html = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /></head>
<body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#f4f4f7;"">
<tr><td align=""center"" style=""padding:40px 20px;"">
<table role=""presentation"" width=""560"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#ffffff;border:1px solid #e5e7eb;"">
<tr><td style=""background-color:#1e40af;padding:24px 32px;""><h1 style=""margin:0;font-size:18px;font-weight:600;color:#ffffff;font-family:'Segoe UI',Tahoma,sans-serif;"">Survey Invitation Sent</h1></td></tr>
<tr><td style=""padding:32px;"">
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,sans-serif;"">Hi " + managerName + @",</p>
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,sans-serif;"">The following team member(s) have been invited to participate in <strong>" + surveyTitle + @"</strong>:</p>
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""border:1px solid #e5e7eb;margin:16px 0;"">" + reporteeList + @"</table>
<p style=""margin:16px 0 0 0;font-size:14px;line-height:22px;color:#6b7280;font-family:'Segoe UI',Tahoma,sans-serif;"">This is for your information. No action is required from you.</p>
</td></tr>
<tr><td style=""background-color:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;""><p style=""margin:0;font-size:11px;color:#9ca3af;text-align:center;font-family:'Segoe UI',Tahoma,sans-serif;"">Automated message from NovacCodeLab.</p></td></tr>
</table>
</td></tr></table>
</body></html>";

        var body = new BodyBuilder { HtmlBody = html };
        mime.Body = body.ToMessageBody();

        return await SendAsync(mime);
    }

    public async Task<bool> SendReminderAsync(SurveyEmailMessage message)
    {
        if (!Enabled)
        {
            _logger.LogWarning("Survey email is disabled. Skipping reminder to {Email}", message.ToEmail);
            return true;
        }

        var mime = BuildMimeMessage(message);
        return await SendAsync(mime);
    }

    public async Task<bool> SendOtpAsync(string toEmail, string toName, string otpCode, string surveyTitle)
    {
        if (!Enabled)
        {
            _logger.LogWarning("Survey email is disabled. OTP for {Email}: {Otp}", toEmail, otpCode);
            return true;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(SenderName, SenderEmail));
        mime.To.Add(new MailboxAddress(toName, toEmail));
        mime.Subject = $"Your OTP for {surveyTitle}";

        var body = new BodyBuilder
        {
            HtmlBody = BuildOtpEmailHtml(otpCode, surveyTitle, toName)
        };
        mime.Body = body.ToMessageBody();

        return await SendAsync(mime);
    }

    private MimeMessage BuildMimeMessage(SurveyEmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(SenderName, SenderEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));

        // CC: Reporting Manager
        if (message.IncludeRm && !string.IsNullOrWhiteSpace(message.RmEmail))
        {
            mime.Cc.Add(new MailboxAddress("", message.RmEmail));
        }

        // CC: Vertical Head
        if (message.IncludeVh && !string.IsNullOrWhiteSpace(message.VhEmail))
        {
            mime.Cc.Add(new MailboxAddress("", message.VhEmail));
        }

        // CC: Additional emails
        if (!string.IsNullOrWhiteSpace(message.AdditionalCcEmails))
        {
            var ccList = message.AdditionalCcEmails
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var cc in ccList)
            {
                if (IsValidEmail(cc))
                {
                    mime.Cc.Add(new MailboxAddress("", cc));
                }
            }
        }

        mime.Subject = message.Subject;

        var body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody
        };
        mime.Body = body.ToMessageBody();

        return mime;
    }

    private async Task<bool> SendAsync(MimeMessage message)
    {
        try
        {
            using var client = new SmtpClient();

            var secureOption = UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(SmtpServer, SmtpPort, secureOption);

            if (!string.IsNullOrWhiteSpace(Username))
            {
                await client.AuthenticateAsync(Username, Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {To}, subject: {Subject}",
                message.To.ToString(), message.Subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}, subject: {Subject}",
                message.To.ToString(), message.Subject);
            return false;
        }
    }

    private static bool IsValidEmail(string email)
        => System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static string BuildOtpEmailHtml(string otpCode, string surveyTitle, string name)
    {
        return @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
<title>OTP Verification</title>
<!--[if mso]>
<style type=""text/css"">
table {border-collapse:collapse;border-spacing:0;margin:0;}
div, td {padding:0;}
div {margin:0 !important;}
</style>
<![endif]-->
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f7;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#f4f4f7;"">
<tr>
<td align=""center"" style=""padding:40px 20px;"">

<table role=""presentation"" width=""480"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color:#ffffff;border:1px solid #e5e7eb;"">

<!-- Header -->
<tr>
<td style=""background-color:#1e40af;padding:24px 32px;text-align:center;"">
<h1 style=""margin:0;font-size:20px;font-weight:600;color:#ffffff;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Verification Code</h1>
</td>
</tr>

<!-- Body -->
<tr>
<td style=""padding:32px;"">
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">Hi " + name + @",</p>
<p style=""margin:0 0 16px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">You requested a verification code to access the survey <strong>" + surveyTitle + @"</strong>. Please use the code below:</p>

<!-- OTP Box -->
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin:24px 0;"">
<tr>
<td style=""background-color:#f0f4ff;border:2px solid #1e40af;padding:24px;text-align:center;"">
<span style=""font-size:36px;font-weight:700;letter-spacing:10px;color:#1e40af;font-family:'Courier New',Courier,monospace;"">" + otpCode + @"</span>
</td>
</tr>
</table>

<p style=""margin:0 0 12px 0;font-size:14px;line-height:22px;color:#374151;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">This code is valid for <strong>5 minutes</strong>. Do not share this code with anyone.</p>
<p style=""margin:0;font-size:14px;line-height:22px;color:#6b7280;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">If you did not request this, you can safely ignore this email.</p>
</td>
</tr>

<!-- Footer -->
<tr>
<td style=""background-color:#f9fafb;padding:16px 32px;border-top:1px solid #e5e7eb;"">
<p style=""margin:0;font-size:11px;line-height:16px;color:#9ca3af;text-align:center;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;"">This is an automated message from NovacCodeLab. Please do not reply.</p>
</td>
</tr>

</table>

</td>
</tr>
</table>
</body>
</html>";
    }
}
