namespace DownloadPilot.Core.Models;

public sealed class MailConnectionSettings
{
    public required string Provider { get; init; }

    public required string EmailAddress { get; init; }

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public required string ImapHost { get; init; }

    public int ImapPort { get; init; } = 993;

    public bool UseSsl { get; init; } = true;

    public string FolderName { get; init; } = "INBOX";

    public string SpamFolderName { get; init; } = string.Empty;

    public int MaxMessages { get; init; } = 50;
}
