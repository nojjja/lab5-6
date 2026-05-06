namespace TODO;

public class MailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; }
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; }
    public bool ImapUseSsl { get; set; }
    public string Pop3Host { get; set; } = string.Empty;
    public int Pop3Port { get; set; }
    public bool Pop3UseSsl { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}
