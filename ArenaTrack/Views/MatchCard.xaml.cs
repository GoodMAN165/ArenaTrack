using System.Windows;
using System.Windows.Controls;
using ArenaTrack.Models;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class MatchCard : UserControl
{
    public MatchCard()
    {
        InitializeComponent();
    }

    private void Star_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Match m)
            m.IsFavorite = FavoriteService.Toggle(m.Id);
    }
}