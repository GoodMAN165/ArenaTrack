using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class FavoritesView : UserControl
{
    public FavoritesView()
    {
        InitializeComponent();
        Load();

        Loaded += (_, _) => FavoriteService.Changed += Load;
        Unloaded += (_, _) => FavoriteService.Changed -= Load;
    }

    private void Load()
    {
        using var db = new AppDbContext();
        var ids = FavoriteService.GetIds();

        var list = db.Matches.Include(m => m.Tournament)
            .Where(m => ids.Contains(m.Id))
            .AsEnumerable()
            .OrderBy(m => m.StartTimeUtc)
            .ToList();

        foreach (var m in list) m.IsFavorite = true;

        FavList.ItemsSource = list;
        EmptyText.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}