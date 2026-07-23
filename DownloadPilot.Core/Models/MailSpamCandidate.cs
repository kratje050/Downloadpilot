namespace DownloadPilot.Core.Models;

public sealed class MailSpamCandidate
{
    public uint Uid { get; init; }

    public required string FolderName { get; init; }

    public required string Subject { get; init; }

    public required string From { get; init; }

    public string? ReplyTo { get; init; }

    public DateTimeOffset? Date { get; init; }

    public required string Snippet { get; init; }

    public int SpamScore { get; init; }

    public required string Reason { get; init; }

    public bool HasAttachments { get; init; }
}
