namespace ArenaTrack.Models;

public class PlayerInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? FideId { get; set; }
    public string? Title { get; set; }
    public string? Federation { get; set; }
    public int? Standard { get; set; }
    public int? Rapid { get; set; }
    public int? Blitz { get; set; }
    public DateTime? RatingsUpdatedUtc { get; set; }
}