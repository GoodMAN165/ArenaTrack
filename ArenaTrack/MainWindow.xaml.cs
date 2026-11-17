using ArenaTrack.Services;
using ArenaTrack.Views;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ArenaTrack;

public partial class MainWindow : Window
{

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        //Тёмная системная рамка окна
        try
        {
            int on = 1;
            var hwnd = new WindowInteropHelper(this).Handle;
            DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int));
        }
        catch { /*на старых системах остаётся обычный заголовок */ }
    }

    public MainWindow()
    {
        InitializeComponent();
        SearchBox.TextChanged += SearchBox_TextChanged;
        PageHost.Content = new MatchesView();

        UpdateSyncText();
        SyncStatus.Changed += UpdateSyncText;

        NetworkChange.NetworkAvailabilityChanged += (_, _) => Dispatcher.Invoke(UpdateSyncText);
    }

    private void UpdateSyncText()
    {
        SyncText.Text = SyncStatus.GetText();
        SyncText.Foreground = SyncStatus.NeedsAttention
            ? new SolidColorBrush(Color.FromRgb(0xF5, 0xB3, 0x01))
            : (Brush)FindResource("MutedBrush");
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (PageHost == null || sender is not RadioButton rb) return;

        var title = rb.Tag?.ToString() ?? "";
        PageTitle.Text = title;
        SearchBox.Text = "";

        PageHost.Content = title switch
        {
            "Матчи" => new MatchesView(),
            "Турниры" => new TournamentsView(),
            "Избранное" => new FavoritesView(),
            "Команды" => new TeamsView("Valorant"),
            "Игроки" => new TeamsView("Chess"),
            "Настройки" => new SettingsView(),
            "Администрирование" => new AdminView(),
            _ => new TextBlock
            {
                Text = $"Раздел «{title}» в разработке",
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private UIElement? _pageBeforeSearch;

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var text = SearchBox.Text.Trim();

        //Страницы Матчи и списки команд фильтруют себя сами
        if (PageHost.Content is ISearchable page)
        {
            page.ApplySearch(text);
            return;
        }

        //остальные страницы показываем общий результат поиска
        if (text.Length > 0)
        {
            if (PageHost.Content is not SearchResultsView)
                _pageBeforeSearch = PageHost.Content as UIElement;
            PageHost.Content = new SearchResultsView(text);
        }
        else if (PageHost.Content is SearchResultsView && _pageBeforeSearch != null)
        {
            PageHost.Content = _pageBeforeSearch;
        }
    }

}