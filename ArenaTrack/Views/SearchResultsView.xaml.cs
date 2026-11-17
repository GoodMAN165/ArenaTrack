using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class SearchResultsView : UserControl
{
    public SearchResultsView(string query)
    {
        InitializeComponent();

        var hits = SearchService.Search(query);

        var view = new ListCollectionView(hits);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SearchHit.Section)));
        ResultList.ItemsSource = view;

        if (hits.Count == 0)
        {
            EmptyText.Text = $"По запросу «{query}» ничего не найдено";
            EmptyText.Visibility = Visibility.Visible;
        }
    }
}