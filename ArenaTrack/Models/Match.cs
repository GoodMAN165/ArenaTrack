using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaTrack.Models;

public class Match : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Game { get; set; } = "";
    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    public string? ExternalId { get; set; }
    public string Team1 { get; set; } = "";
    public string Team2 { get; set; } = "";
    public int? Score1 { get; set; }
    public int? Score2 { get; set; }
    public string? ResultText { get; set; }

    public DateTime StartTimeUtc { get; set; }
    public string Status { get; set; } = "Upcoming";


    public string LocalTime => StartTimeUtc.ToLocalTime().ToString("dd.MM HH:mm");
    public string ScoreText => ResultText ?? (Score1 == null ? "vs" : $"{Score1} : {Score2}");
    public string StatusText => Status switch
    {
        "Live" => "Идёт",
        "Finished" => "Завершён",
        _ => "Предстоит"
    };
    public string TournamentName => Tournament?.Name ?? "";


    private bool _isFavorite;

    [NotMapped]
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value) return;
            _isFavorite = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}