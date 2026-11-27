namespace ClinicSpace.Infrastructure.Email;

public enum EmailDeliveryMode
{
    /// <summary>Send through an SMTP server (Mailpit in Docker Compose, or a real relay).</summary>
    Smtp = 1,

    /// <summary>Write each email as an .eml file that any mail client can open. No server needed.</summary>
    File = 2,

    /// <summary>Write the email to the application log only.</summary>
    Log = 3,
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public EmailDeliveryMode Mode { get; set; } = EmailDeliveryMode.Log;

    public string FromAddress { get; set; } = "no-reply@clinicspace.test";

    public string FromName { get; set; } = "MPFT Clinic Space";

    public string SmtpHost { get; set; } = "localhost";

    public int SmtpPort { get; set; } = 1025;

    /// <summary>Auto, None, StartTls or SslOnConnect. Use StartTls or SslOnConnect for any real relay.</summary>
    public string SmtpSecurity { get; set; } = "Auto";

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public string FileDirectory { get; set; } = "mail-drop";

    /// <summary>Turns the background sender on or off (off in some tests).</summary>
    public bool DispatcherEnabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int MaxAttempts { get; set; } = 8;
}
