using System.Globalization;
using System.Text.RegularExpressions;

namespace DownloadPilot.Infrastructure.Utilities;

public static partial class DocumentInsightsExtractor
{
    public static DocumentInsights Extract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new DocumentInsights();
        }

        return new DocumentInsights
        {
            DocumentType = DetectDocumentType(text),
            CompanyName = DetectCompanyName(text),
            Amount = DetectAmount(text),
            DocumentDate = DetectDate(text),
            Subject = DetectSubject(text)
        };
    }

    public static string? DetectDocumentType(string text)
    {
        if (ContainsAny(text, "factuur", "invoice", "btw", "totaalbedrag"))
        {
            return "Factuur";
        }

        if (ContainsAny(text, "bestelbevestiging", "orderbevestiging"))
        {
            return "Bestelbevestiging";
        }

        if (ContainsAny(text, "garantiebewijs", "garantie"))
        {
            return "Garantiebewijs";
        }

        if (ContainsAny(text, "handleiding", "gebruiksaanwijzing", "manual"))
        {
            return "Handleiding";
        }

        if (ContainsAny(text, "bankafschrift", "iban", "saldo"))
        {
            return "Bankafschrift";
        }

        if (ContainsAny(text, "verzekering", "polis", "premie"))
        {
            return "Verzekeringsdocument";
        }

        if (ContainsAny(text, "belastingdienst", "inkomstenbelasting", "aangifte"))
        {
            return "Belastingdocument";
        }

        if (ContainsAny(text, "school", "ouderavond", "klas"))
        {
            return "Schoolbrief";
        }

        if (ContainsAny(text, "dierenarts", "huisdier", "vaccinatie"))
        {
            return "Dierenartsfactuur";
        }

        if (ContainsAny(text, "energiecontract", "stroom", "gas", "leveringstarief"))
        {
            return "Energiecontract";
        }

        return null;
    }

    public static string? DetectCompanyName(string text)
    {
        var companyHints = new[]
        {
            "Coolblue", "Bol", "Amazon", "MediaMarkt", "Ziggo", "KPN", "Eneco", "Essent", "ING", "Rabobank", "ABN AMRO",
            "Albert Heijn", "Jumbo", "IKEA", "KLM", "NS", "Vodafone", "Odido", "Apple", "Microsoft", "Google"
        };

        var hinted = companyHints.FirstOrDefault(c => ContainsCompanyHint(text, c));
        if (!string.IsNullOrWhiteSpace(hinted))
        {
            return hinted;
        }

        var linePrefixes = new[] { "leverancier", "bedrijf", "verkoper", "merchant", "afzender", "winkel", "supplier", "seller" };
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var prefix in linePrefixes)
            {
                if (line.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
                {
                    var value = CleanCompanyCandidate(line[(prefix.Length + 1)..]);
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
        }

        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = CleanCompanyCandidate(line);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (CompanySuffixRegex().IsMatch(candidate) || LooksLikeCompanyName(candidate))
            {
                return candidate;
            }
        }

        var domainMatch = DomainRegex().Match(text);
        if (domainMatch.Success)
        {
            var domain = domainMatch.Groups[1].Value;
            var firstPart = domain.Split('.')[0];
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstPart.Replace('-', ' '));
        }

        return null;
    }

    public static decimal? DetectAmount(string text)
    {
        var match = AmountRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var normalized = match.Groups[1].Value.Replace(".", string.Empty).Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    public static DateTime? DetectDate(string text)
    {
        var datePatterns = new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy" };
        foreach (Match match in DateRegex().Matches(text))
        {
            var value = match.Value;
            foreach (var pattern in datePatterns)
            {
                if (DateTime.TryParseExact(value, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    public static string? DetectSubject(string text)
    {
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("Onderwerp:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Product:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = line.IndexOf(':');
                if (idx > -1)
                {
                    var value = line[(idx + 1)..].Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] candidates)
        => candidates.Any(c => text.Contains(c, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsCompanyHint(string text, string companyName)
    {
        var pattern = $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(companyName)}(?![\p{{L}}\p{{N}}])";
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    private static string? CleanCompanyCandidate(string value)
    {
        var candidate = value.Trim(' ', '\t', '-', ':', '|');
        candidate = Regex.Replace(candidate, @"\s+", " ");

        if (candidate.Length is < 2 or > 60)
        {
            return null;
        }

        if (ContainsAny(candidate, "factuur", "invoice", "datum", "bedrag", "totaal", "btw", "iban", "kvk", "pagina",
            "product:", "onderwerp:", "klant:", "adres:", "telefoon:", "email:", "e-mail:"))
        {
            return null;
        }

        if (DateRegex().IsMatch(candidate) || AmountRegex().IsMatch(candidate))
        {
            return null;
        }

        return candidate;
    }

    private static bool LooksLikeCompanyName(string candidate)
    {
        var words = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is < 1 or > 5)
        {
            return false;
        }

        if (!candidate.Any(char.IsLetter))
        {
            return false;
        }

        var letters = candidate.Where(char.IsLetter).ToArray();
        if (letters.Length < 3)
        {
            return false;
        }

        var uppercaseLetters = letters.Count(char.IsUpper);
        if (uppercaseLetters >= 2 && candidate.Length <= 12)
        {
            return true;
        }

        var titleCasedWords = words.Count(word =>
        {
            var firstLetter = word.FirstOrDefault(char.IsLetter);
            return firstLetter != default && char.IsUpper(firstLetter);
        });

        return words.Length >= 2 && titleCasedWords >= 2;
    }

    [GeneratedRegex(@"(?:EUR|€)?\s*([0-9]{1,3}(?:\.[0-9]{3})*(?:,[0-9]{2})|[0-9]+(?:,[0-9]{2}))", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b\d{1,4}[\-/]\d{1,2}[\-/]\d{2,4}\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\b(?:[A-Z0-9][A-Z0-9&.' -]{1,45})\s+(?:B\.?V\.?|N\.?V\.?|LLC|Ltd\.?|GmbH|Inc\.?|Holding)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CompanySuffixRegex();

    [GeneratedRegex(@"\b(?:https?://)?(?:www\.)?([a-z0-9-]+\.(?:nl|com|de|be|eu|org))\b", RegexOptions.IgnoreCase)]
    private static partial Regex DomainRegex();
}
