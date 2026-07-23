using DownloadPilot.Infrastructure.Utilities;

namespace DownloadPilot.Tests;

public sealed class MailSpamScorerTests
{
    [Fact]
    public void Score_ShouldMarkPhishingMailAsHighRisk()
    {
        var result = MailSpamScorer.Score(
            "Urgent action required: verify your account",
            "Security <security@example.com>",
            "Reply <reply@strange-domain.test>",
            "Klik hier om uw account te verifieeren: https://example.test https://example2.test",
            "X-Spam-Flag: YES",
            hasAttachments: false);

        Assert.True(result.Score >= 80);
        Assert.Contains("spamwoorden", result.Reason);
    }

    [Fact]
    public void Score_ShouldKeepNormalMailLowRisk()
    {
        var result = MailSpamScorer.Score(
            "Afspraak morgen",
            "Roy <roy@example.com>",
            "Roy <roy@example.com>",
            "Kun je morgen om 10:00 uur?",
            string.Empty,
            hasAttachments: false);

        Assert.True(result.Score < 45);
    }
}
