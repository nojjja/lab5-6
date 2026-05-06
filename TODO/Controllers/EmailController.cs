using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using TODO;

[Route("api/[controller]")]
[ApiController]
public class EmailController : ControllerBase
{
    private readonly MailSettings _mailSettings;

    public EmailController(IOptions<MailSettings> mailSettings)
    {
        _mailSettings = mailSettings.Value;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_mailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(request.ToEmail));
            message.Subject = "Напоминание о задаче";
            message.Body = new TextPart("plain")
            {
                Text = $"Привет! Не забудь выполнить задачу: {request.TaskTitle}"
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            var smtpSecurity = _mailSettings.SmtpUseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_mailSettings.SmtpHost, _mailSettings.SmtpPort, smtpSecurity);
            await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return Ok(new { protocol = "SMTP", message = "Письмо успешно отправлено." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { protocol = "SMTP", error = ex.Message });
        }
    }

    [HttpGet("check-imap")]
    public async Task<IActionResult> CheckImapInbox()
    {
        try
        {
            using var client = new ImapClient();
            await client.ConnectAsync(_mailSettings.ImapHost, _mailSettings.ImapPort, _mailSettings.ImapUseSsl);
            await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);

            await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
            var messageCount = client.Inbox.Count;

            string? latestSubject = null;
            if (messageCount > 0)
            {
                var latestMessage = await client.Inbox.GetMessageAsync(messageCount - 1);
                latestSubject = latestMessage.Subject;
            }

            await client.DisconnectAsync(true);

            return Ok(new
            {
                protocol = "IMAP",
                message = "Подключение успешно.",
                inboxCount = messageCount,
                latestSubject
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { protocol = "IMAP", error = ex.Message });
        }
    }

    [HttpGet("check-pop3")]
    public async Task<IActionResult> CheckPop3Inbox()
    {
        try
        {
            using var client = new Pop3Client();
            await client.ConnectAsync(_mailSettings.Pop3Host, _mailSettings.Pop3Port, _mailSettings.Pop3UseSsl);
            await client.AuthenticateAsync(_mailSettings.Username, _mailSettings.Password);

            var messageCount = client.Count;
            string? latestSubject = null;

            if (messageCount > 0)
            {
                var latestMessage = await client.GetMessageAsync(messageCount - 1);
                latestSubject = latestMessage.Subject;
            }

            await client.DisconnectAsync(true);

            return Ok(new
            {
                protocol = "POP3",
                message = "Подключение успешно.",
                inboxCount = messageCount,
                latestSubject
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { protocol = "POP3", error = ex.Message });
        }
    }
}

public class EmailRequest
{
    public required string ToEmail { get; set; }
    public required string TaskTitle { get; set; }
}
