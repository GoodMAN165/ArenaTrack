using ArenaTrack.Data;
using ArenaTrack.Services;
using System.Windows;
using System.Windows.Controls;

namespace ArenaTrack.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        EnabledBox.IsChecked = AppSettings.NotificationsEnabled;
        MinutesBox.Text = AppSettings.NotifyMinutes.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MinutesBox.Text.Trim(), out int minutes) || minutes < 1 || minutes > 1440)
        {
            StatusText.Text = "Введите целое число от 1 до 1440.";
            return;
        }

        AppSettings.NotificationsEnabled = EnabledBox.IsChecked == true;
        AppSettings.NotifyMinutes = minutes;
        AppSettings.Save();
        StatusText.Text = "Настройки сохранены.";
    }

    private void Test_Click(object sender, RoutedEventArgs e)
    {
        NotificationService.ShowTest();
    }

    private void LoadDemo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DemoData.Load();
            DemoStatus.Text = "Демо-данные загружены. Откройте раздел «Матчи».";
        }
        catch (Exception ex)
        {
            DemoStatus.Text = "Не удалось загрузить демо-данные: " + ex.Message;
        }
    }

    private void RemoveDemo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DemoData.Remove();
            DemoStatus.Text = "Демо-данные удалены.";
        }
        catch (Exception ex)
        {
            DemoStatus.Text = "Не удалось удалить демо-данные: " + ex.Message;
        }
    }
}