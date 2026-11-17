namespace ArenaTrack.Models;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime TimeUtc { get; set; }
    public string Source { get; set; } = "";
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = "";

    public string LocalTime => TimeUtc.ToLocalTime().ToString("dd.MM HH:mm:ss");
}