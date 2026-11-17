using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class MatchesView : UserControl, ISearchable
{
    private string _search = "";

    public MatchesView()
    {
        InitializeComponent();
        LoadMatches();
    }


    public void ApplySearch(string text)
    {
        _search = text.Trim();
        LoadMatches();
    }

    private void GameFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (MatchList != null) LoadMatches();
    }

    private void LoadMatches()
    {
        using var db = new AppDbContext();


        var matches = db.Matches.Include(m => m.Tournament).AsEnumerable();

        var game = (GameFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (game is "Valorant" or "Chess")
            matches = matches.Where(m => m.Game == game);

        if (_search.Length > 0)
        {
            matches = matches.Where(m =>
                m.Team1.Contains(_search, StringComparison.CurrentCultureIgnoreCase) ||
                m.Team2.Contains(_search, StringComparison.CurrentCultureIgnoreCase) ||
                (m.Tournament?.Name ?? "").Contains(_search, StringComparison.CurrentCultureIgnoreCase));
        }

        var list = matches.OrderBy(m => m.StartTimeUtc).ToList();

        var favs = FavoriteService.GetIds();
        foreach (var m in list)
            m.IsFavorite = favs.Contains(m.Id);

        MatchList.ItemsSource = list;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (!SyncStatus.IsOnline)
        {
            StatusText.Text = "Нет подключения к Интернету. Показаны сохранённые данные.";
            LogService.Write("Sync", "Warning", "Обновление пропущено: нет подключения к Интернету");
            SyncStatus.Refresh();
            return;
        }

        RefreshButton.IsEnabled = false;
        StatusText.Text = "Загрузка...";

        int total = 0, succeeded = 0;
        var errors = new List<string>();

        if (AppSettings.ChessEnabled)
        {
            try
            {
                int n = await new ChessService().SyncAsync();
                total += n; succeeded++;
                LogService.Write("Chess", "Info", $"Загружено записей: {n}");
            }
            catch (Exception ex)
            {
                errors.Add("Chess: " + ex.Message);
                LogService.Write("Chess", "Error", ex.Message);
            }
        }

        if (AppSettings.ValorantEnabled)
        {
            try
            {
                int n = await new ValorantService().SyncAsync();
                total += n; succeeded++;
                LogService.Write("Valorant", "Info", $"Загружено записей: {n}");
            }
            catch (Exception ex)
            {
                errors.Add("Valorant: " + ex.Message);
                LogService.Write("Valorant", "Error", ex.Message);
            }
        }

        LoadMatches();

        if (succeeded > 0) SyncStatus.MarkSuccess();
        else SyncStatus.Refresh();

        StatusText.Text = errors.Count == 0
            ? $"Обновлено в {DateTime.Now:HH:mm:ss}, записей: {total}"
            : $"Записей: {total}. Ошибки: " + string.Join(" | ", errors);

        RefreshButton.IsEnabled = true;
    }
}