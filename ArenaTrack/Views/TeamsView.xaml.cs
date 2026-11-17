using System.Windows;
using System.Windows.Controls;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class TeamsView : UserControl, ISearchable
{
    private readonly string _game;
    private readonly List<string> _all;

    public TeamsView(string game)
    {
        _game = game;
        InitializeComponent();

        Placeholder.Text = game == "Chess"
            ? "Выберите игрока слева"
            : "Выберите команду слева";

        _all = StatsService.GetNames(game);
        NameList.ItemsSource = _all;

        if (_all.Count == 0)
            Placeholder.Text = "Данных пока нет. Нажмите «Обновить данные» на странице «Матчи».";
    }

    public void ApplySearch(string text)
    {
        var s = text.Trim();
        NameList.ItemsSource = s.Length == 0
            ? _all
            : _all.Where(n => n.Contains(s, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    private void NameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NameList.SelectedItem is not string name) return;

        var profile = StatsService.GetProfile(name, _game);

        Placeholder.Visibility = Visibility.Collapsed;
        Card.Visibility = Visibility.Visible;

        CardName.Text = name;
        CardGame.Text = profile.Subtitle;
        StatsList.ItemsSource = profile.Stats;
        RowsList.ItemsSource = profile.Rows;
        RosterList.ItemsSource = profile.Roster;
        RosterPanel.Visibility = profile.Roster.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}