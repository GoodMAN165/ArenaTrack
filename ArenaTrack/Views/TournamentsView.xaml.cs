using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Models;

namespace ArenaTrack.Views;

public partial class TournamentsView : UserControl
{
    public TournamentsView()
    {
        InitializeComponent();
        using var db = new AppDbContext();
        TournamentList.ItemsSource = db.Tournaments
            .OrderBy(t => t.Name).ToList();
    }

    private void TournamentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TournamentList.SelectedItem is not Tournament t) return;

        Placeholder.Visibility = Visibility.Collapsed;
        Card.Visibility = Visibility.Visible;

        CardName.Text = t.Name;
        CardGame.Text = t.Game;
        CardDates.Text = "Даты: " + t.DatesText;
        CardPrize.Text = "Призовой фонд: " + t.PrizeText;

        using var db = new AppDbContext();
        CardMatches.ItemsSource = db.Matches
            .Where(m => m.TournamentId == t.Id)
            .AsEnumerable()
            .OrderBy(m => m.StartTimeUtc)
            .ToList();
    }
}