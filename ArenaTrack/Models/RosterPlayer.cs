namespace ArenaTrack.Models;

public class RosterPlayer
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public string Nickname { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Country { get; set; } = "";
}