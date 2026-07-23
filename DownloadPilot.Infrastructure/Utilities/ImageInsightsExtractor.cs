using System.Text.RegularExpressions;

namespace DownloadPilot.Infrastructure.Utilities;

public static partial class ImageInsightsExtractor
{
    public static ImageInsights Extract(string filePath, string? extractedText, DocumentInsights documentInsights)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var textToInspect = $"{fileName} {extractedText}".Trim();

        if (!string.IsNullOrWhiteSpace(documentInsights.CompanyName))
        {
            return new ImageInsights
            {
                SuggestedFolderName = documentInsights.CompanyName,
                Reason = "Bedrijfsnaam herkend in afbeelding"
            };
        }

        var topic = DetectTopic(textToInspect);
        if (!string.IsNullOrWhiteSpace(topic))
        {
            return new ImageInsights
            {
                SuggestedFolderName = topic,
                Reason = "Onderwerp herkend uit bestandsnaam of OCR-tekst"
            };
        }

        return new ImageInsights();
    }

    public static string? DetectTopic(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (ContainsAny(text, "screenshot", "screen shot", "schermopname", "schermafbeelding"))
        {
            return "Screenshots";
        }

        if (ContainsAny(text, "boarding pass", "ticket", "e-ticket", "voucher", "qr", "barcode", "toegangsbewijs"))
        {
            return "Tickets";
        }

        if (ContainsAny(text, "bon", "receipt", "kassabon", "aankoopbewijs"))
        {
            return "Bonnen";
        }

        if (ContainsAny(text, "paspoort", "rijbewijs", "identiteitskaart", "id-kaart", "documentnummer"))
        {
            return "Identiteit";
        }

        if (ContainsAny(text, "school", "klas", "huiswerk", "ouderavond", "rapport"))
        {
            return "School";
        }

        if (ContainsAny(text, "werk", "project", "meeting", "presentatie", "whiteboard"))
        {
            return "Werk";
        }

        if (ContainsAny(text, "vakantie", "travel", "reis", "hotel", "strand", "beach", "airport", "vlucht"))
        {
            return "Reizen";
        }

        if (ContainsAny(text, "menu", "restaurant", "eten", "diner", "lunch", "food"))
        {
            return "Eten en drinken";
        }

        if (ContainsAny(text, "auto", "kenteken", "garage", "parking", "parkeren"))
        {
            return "Auto";
        }

        if (ContainsAny(text, "huis", "woning", "interieur", "kamer", "keuken", "tuin"))
        {
            return "Woning";
        }

        if (ContainsAny(text, "sport", "fitness", "wedstrijd", "training"))
        {
            return "Sport";
        }

        if (ContainsAny(text, "arts", "ziekenhuis", "apotheek", "medicatie", "zorgverzekering"))
        {
            return "Gezondheid";
        }

        if (CameraFileNameRegex().IsMatch(text))
        {
            return null;
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] candidates)
        => candidates.Any(c => text.Contains(c, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\b(?:img|dsc|dscn|photo|pxl|pxl_)\s*[_-]?\d{3,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex CameraFileNameRegex();
}
