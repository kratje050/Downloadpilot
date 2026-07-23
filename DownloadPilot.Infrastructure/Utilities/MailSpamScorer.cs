using System.Text.RegularExpressions;

namespace DownloadPilot.Infrastructure.Utilities;

public static partial class MailSpamScorer
{
    private static readonly string[] HighRiskPhrases =
    [
        "verify your account",
        "account suspended",
        "password expired",
        "payment failed",
        "urgent action",
        "act now",
        "limited time",
        "gift card",
        "crypto",
        "bitcoin",
        "casino",
        "lottery",
        "winner",
        "claim your prize",
        "klik hier",
        "verifieer uw account",
        "account geblokkeerd",
        "betaling mislukt",
        "wachtwoord verlopen",
        "gratis prijs",
        "u heeft gewonnen"
    ];

    private static readonly string[] MediumRiskPhrases =
    [
        "free",
        "bonus",
        "discount",
        "unsubscribe",
        "tracking",
        "invoice overdue",
        "final notice",
        "laatste waarschuwing",
        "korting",
        "aanbieding",
        "openstaande factuur",
        "incasso"
    ];

    public static (int Score, string Reason) Score(
        string? subject,
        string? from,
        string? replyTo,
        string? body,
        string? headers,
        bool hasAttachments)
    {
        var score = 0;
        var reasons = new List<string>();
        var text = $"{subject} {from} {replyTo} {body} {headers}";

        var highRiskHits = HighRiskPhrases
            .Where(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();
        if (highRiskHits.Count > 0)
        {
            score += 25 + highRiskHits.Count * 8;
            reasons.Add("spamwoorden: " + string.Join(", ", highRiskHits));
        }

        var mediumRiskHits = MediumRiskPhrases
            .Where(phrase => text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();
        if (mediumRiskHits.Count > 0)
        {
            score += mediumRiskHits.Count * 5;
            reasons.Add("marketing/phishing-termen: " + string.Join(", ", mediumRiskHits));
        }

        var linkCount = UrlRegex().Matches(body ?? string.Empty).Count;
        if (linkCount >= 5)
        {
            score += 20;
            reasons.Add($"{linkCount} links in de mail");
        }
        else if (linkCount >= 2)
        {
            score += 10;
            reasons.Add($"{linkCount} links in de mail");
        }

        if (LooksLikeSpoofedSender(from, replyTo))
        {
            score += 20;
            reasons.Add("afzender en reply-to lijken verschillend");
        }

        if (headers?.Contains("X-Spam-Flag: YES", StringComparison.OrdinalIgnoreCase) == true
            || headers?.Contains("X-Spam-Status: Yes", StringComparison.OrdinalIgnoreCase) == true)
        {
            score += 35;
            reasons.Add("server markeerde deze mail als spam");
        }

        if (hasAttachments && ContainsRiskyAttachmentHint(text))
        {
            score += 25;
            reasons.Add("bijlage met risicowoorden");
        }

        if (string.IsNullOrWhiteSpace(from))
        {
            score += 15;
            reasons.Add("geen afzender gevonden");
        }

        score = Math.Clamp(score, 0, 100);
        if (reasons.Count == 0)
        {
            reasons.Add(score >= 45 ? "verdacht patroon gevonden" : "geen sterke spam-indicator");
        }

        return (score, string.Join("; ", reasons));
    }

    private static bool LooksLikeSpoofedSender(string? from, string? replyTo)
    {
        var fromDomain = ExtractDomain(from);
        var replyToDomain = ExtractDomain(replyTo);
        return !string.IsNullOrWhiteSpace(fromDomain)
            && !string.IsNullOrWhiteSpace(replyToDomain)
            && !fromDomain.Equals(replyToDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractDomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = EmailDomainRegex().Match(value);
        return match.Success ? match.Groups["domain"].Value.Trim().TrimEnd('>') : null;
    }

    private static bool ContainsRiskyAttachmentHint(string text)
    {
        return text.Contains(".exe", StringComparison.OrdinalIgnoreCase)
            || text.Contains(".scr", StringComparison.OrdinalIgnoreCase)
            || text.Contains(".js", StringComparison.OrdinalIgnoreCase)
            || text.Contains(".vbs", StringComparison.OrdinalIgnoreCase)
            || text.Contains(".html", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"https?://", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"@(?<domain>[A-Z0-9.-]+\.[A-Z]{2,})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailDomainRegex();
}
