using DownloadPilot.Core.Abstractions;
using DownloadPilot.Core.Models;
using DownloadPilot.Infrastructure.Utilities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace DownloadPilot.Infrastructure.Services;

public sealed class MailSpamFilterService(ILogger<MailSpamFilterService> logger) : IMailSpamFilterService
{
    private const int CandidateThreshold = 45;

    public async Task<MailSpamScanResult> ScanAsync(
        MailConnectionSettings settings,
        CancellationToken cancellationToken)
    {
        ValidateSettings(settings);

        using var client = await ConnectAsync(settings, cancellationToken);
        var folder = await OpenFolderAsync(client, settings.FolderName, FolderAccess.ReadOnly, cancellationToken);

        var allUids = await folder.SearchAsync(SearchQuery.All, cancellationToken);
        var maxMessages = Math.Clamp(settings.MaxMessages, 1, 500);
        var uidsToScan = allUids
            .OrderBy(uid => uid.Id)
            .TakeLast(maxMessages)
            .ToList();

        var candidates = new List<MailSpamCandidate>();
        foreach (var uid in uidsToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var message = await folder.GetMessageAsync(uid, cancellationToken);
                var candidate = BuildCandidate(folder.FullName, uid, message);
                if (candidate.SpamScore >= CandidateThreshold)
                {
                    candidates.Add(candidate);
                }
            }
            catch (Exception ex) when (ex is IOException or ImapProtocolException or FormatException)
            {
                logger.LogWarning(ex, "Mail overgeslagen tijdens spam-scan: {Uid}", uid.Id);
            }
        }

        await client.DisconnectAsync(true, cancellationToken);

        return new MailSpamScanResult
        {
            ScannedCount = uidsToScan.Count,
            Candidates = candidates
                .OrderByDescending(candidate => candidate.SpamScore)
                .ThenByDescending(candidate => candidate.Date)
                .ToList()
        };
    }

    public async Task<int> MoveToSpamAsync(
        MailConnectionSettings settings,
        IReadOnlyList<MailSpamCandidate> candidates,
        CancellationToken cancellationToken)
    {
        ValidateSettings(settings);

        if (candidates.Count == 0)
        {
            return 0;
        }

        using var client = await ConnectAsync(settings, cancellationToken);
        var grouped = candidates.GroupBy(candidate => candidate.FolderName, StringComparer.OrdinalIgnoreCase);
        var moved = 0;

        foreach (var group in grouped)
        {
            var source = await OpenFolderAsync(client, group.Key, FolderAccess.ReadWrite, cancellationToken);
            var target = await ResolveSpamFolderAsync(client, settings.SpamFolderName, cancellationToken);
            var uids = group
                .Select(candidate => new UniqueId(candidate.Uid))
                .ToList();

            await source.MoveToAsync(uids, target, cancellationToken);
            moved += uids.Count;
        }

        await client.DisconnectAsync(true, cancellationToken);
        return moved;
    }

    private static MailSpamCandidate BuildCandidate(string folderName, UniqueId uid, MimeMessage message)
    {
        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "(geen onderwerp)" : message.Subject.Trim();
        var from = message.From.Mailboxes.FirstOrDefault()?.ToString()
            ?? message.From.ToString();
        var replyTo = message.ReplyTo.Mailboxes.FirstOrDefault()?.ToString()
            ?? message.ReplyTo.ToString();
        var body = ExtractBody(message);
        var headers = string.Join(Environment.NewLine, message.Headers.Select(header => $"{header.Field}: {header.Value}"));
        var assessment = MailSpamScorer.Score(
            subject,
            from,
            replyTo,
            body,
            headers,
            message.Attachments.Any());

        return new MailSpamCandidate
        {
            Uid = uid.Id,
            FolderName = folderName,
            Subject = subject,
            From = string.IsNullOrWhiteSpace(from) ? "(onbekend)" : from,
            ReplyTo = string.IsNullOrWhiteSpace(replyTo) ? null : replyTo,
            Date = message.Date,
            Snippet = BuildSnippet(body),
            SpamScore = assessment.Score,
            Reason = assessment.Reason,
            HasAttachments = message.Attachments.Any()
        };
    }

    private static string ExtractBody(MimeMessage message)
    {
        var body = message.TextBody;
        if (string.IsNullOrWhiteSpace(body))
        {
            body = message.HtmlBody;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        body = body.Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return body.Length > 6000 ? body[..6000] : body;
    }

    private static string BuildSnippet(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "Geen tekstpreview beschikbaar";
        }

        return body.Length > 220 ? body[..220] + "..." : body;
    }

    private static async Task<ImapClient> ConnectAsync(
        MailConnectionSettings settings,
        CancellationToken cancellationToken)
    {
        var client = new ImapClient();
        var secureSocketOptions = settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        try
        {
            await client.ConnectAsync(settings.ImapHost, settings.ImapPort, secureSocketOptions, cancellationToken);
            if (ShouldUseOAuth(settings))
            {
                await client.AuthenticateAsync(new SaslMechanismOAuth2(settings.UserName, settings.Password), cancellationToken);
            }
            else
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
            }

            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static bool ShouldUseOAuth(MailConnectionSettings settings)
    {
        return settings.Provider.Contains("Outlook", StringComparison.OrdinalIgnoreCase)
            || settings.Provider.Contains("Hotmail", StringComparison.OrdinalIgnoreCase)
            || settings.Password.StartsWith("ya29.", StringComparison.OrdinalIgnoreCase)
            || settings.Password.StartsWith("Ew", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IMailFolder> OpenFolderAsync(
        ImapClient client,
        string folderName,
        FolderAccess access,
        CancellationToken cancellationToken)
    {
        var folder = string.IsNullOrWhiteSpace(folderName) || folderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? client.Inbox
            : await client.GetFolderAsync(folderName, cancellationToken);

        await folder.OpenAsync(access, cancellationToken);
        return folder;
    }

    private static async Task<IMailFolder> ResolveSpamFolderAsync(
        ImapClient client,
        string configuredFolderName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(configuredFolderName))
        {
            var configured = await TryGetFolderByNameAsync(client, configuredFolderName, cancellationToken);
            if (configured is not null)
            {
                return configured;
            }
        }

        var specialFolder = client.GetFolder(SpecialFolder.Junk);
        if (specialFolder is not null)
        {
            return specialFolder;
        }

        foreach (var folderName in new[] { "[Gmail]/Spam", "Junk Email", "Junk", "Spam", "Ongewenste e-mail" })
        {
            var folder = await TryGetFolderByNameAsync(client, folderName, cancellationToken);
            if (folder is not null)
            {
                return folder;
            }
        }

        throw new InvalidOperationException("Geen spam- of junkmap gevonden in deze mailbox.");
    }

    private static async Task<IMailFolder?> TryGetFolderByNameAsync(
        ImapClient client,
        string folderName,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetFolderAsync(folderName, cancellationToken);
        }
        catch (FolderNotFoundException)
        {
            return null;
        }
    }

    private static void ValidateSettings(MailConnectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ImapHost)
            || string.IsNullOrWhiteSpace(settings.UserName)
            || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException("Vul e-mailadres, gebruikersnaam, app-wachtwoord en IMAP-server in.");
        }
    }
}
