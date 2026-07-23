using DownloadPilot.Core.Models;

namespace DownloadPilot.App.ViewModels;

public sealed class MailSpamItemViewModel(MailSpamCandidate candidate) : ObservableObject
{
    private bool _isSelected = candidate.SpamScore >= 80;

    public MailSpamCandidate Candidate { get; } = candidate;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Subject => Candidate.Subject;

    public string From => Candidate.From;

    public string DateReadable => Candidate.Date?.LocalDateTime.ToString("dd-MM-yyyy HH:mm") ?? "-";

    public string Snippet => Candidate.Snippet;

    public int SpamScore => Candidate.SpamScore;

    public string Reason => Candidate.Reason;

    public string FolderName => Candidate.FolderName;

    public string AttachmentLabel => Candidate.HasAttachments ? "Ja" : "Nee";
}
