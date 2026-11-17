using ArenaTrack.Data;
using ArenaTrack.Models;

namespace ArenaTrack.Services;

public static class FavoriteService
{

    public static event Action? Changed;

    public static HashSet<int> GetIds()
    {
        using var db = new AppDbContext();
        return db.Favorites.Select(f => f.MatchId).ToHashSet();
    }


    public static bool Toggle(int matchId)
    {
        using var db = new AppDbContext();
        var existing = db.Favorites.FirstOrDefault(f => f.MatchId == matchId);

        bool nowFavorite;
        if (existing == null)
        {
            db.Favorites.Add(new Favorite { MatchId = matchId });
            nowFavorite = true;
        }
        else
        {
            db.Favorites.Remove(existing);
            nowFavorite = false;
        }

        db.SaveChanges();
        Changed?.Invoke();
        return nowFavorite;
    }
}