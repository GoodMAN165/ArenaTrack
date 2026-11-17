using ArenaTrack.Data;

namespace ArenaTrack.Services;

public record SearchHit(string Title, string Subtitle, string Section);

public static class SearchService
{
    public static List<SearchHit> Search(string text)
    {
        var s = text.Trim();
        var hits = new List<SearchHit>();
        if (s.Length == 0) return hits;

        bool Has(string? v) => v != null &&
            v.Contains(s, StringComparison.CurrentCultureIgnoreCase);

        using var db = new AppDbContext();

        //Игры
        foreach (var g in new[] { "Valorant", "Chess" })
            if (Has(g) || (g == "Chess" && Has("шахматы")))
                hits.Add(new SearchHit(g == "Chess" ? "Шахматы (Chess)" : g, "Дисциплина", "Игры"));

        //Турниры
        foreach (var t in db.Tournaments.AsEnumerable().Where(t => Has(t.Name)).OrderBy(t => t.Name).Take(30))
            hits.Add(new SearchHit(t.Name, $"{t.Game} · {t.DatesText}", "Турниры"));

        //Команды Valorant
        foreach (var n in StatsService.GetNames("Valorant").Where(n => Has(n)).Take(30))
            hits.Add(new SearchHit(n, "Valorant", "Команды"));

        //Шахматисты
        foreach (var n in StatsService.GetNames("Chess").Where(n => Has(n)).Take(30))
            hits.Add(new SearchHit(n, "Chess", "Игроки"));

        return hits;
    }
}