namespace ArenaTrack.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Game { get; set; } = "";
    public string? ExternalId { get; set; }
    public DateTime? RosterUpdatedUtc { get; set; }
}