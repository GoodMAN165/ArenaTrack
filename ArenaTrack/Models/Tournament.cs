namespace ArenaTrack.Models;

public class Tournament
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string? PrizePool { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string PrizeText => string.IsNullOrWhiteSpace(PrizePool) ? "не указан" : PrizePool;
    public string DatesText => $"{StartDate.ToLocalTime():dd.MM.yyyy} — {EndDate.ToLocalTime():dd.MM.yyyy}";
}