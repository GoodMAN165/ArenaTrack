using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using ArenaTrack.Data;
using ArenaTrack.Services;

namespace ArenaTrack.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
        ShowState();
    }

    private void ShowState()
    {
        bool unlocked = PasswordService.IsUnlocked;
        LoginPanel.Visibility = unlocked ? Visibility.Collapsed : Visibility.Visible;
        AdminPanel.Visibility = unlocked ? Visibility.Visible : Visibility.Collapsed;

        if (unlocked)
        {
            ChessBox.IsChecked = AppSettings.ChessEnabled;
            ValorantBox.IsChecked = AppSettings.ValorantEnabled;
            KeyStatus.Text = ApiKeyStore.Exists
                ? "Ключ сохранён (хранится в зашифрованном виде)."
                : "Ключ не задан.";
            LogList.ItemsSource = LogService.GetRecent();
            return;
        }

        bool has = PasswordService.HasPassword;
        LoginTitle.Text = has
            ? "Введите пароль администратора"
            : "Первый вход: задайте пароль администратора (не короче 4 символов) и повторите его";
        Pwd2Box.Visibility = has ? Visibility.Collapsed : Visibility.Visible;
        LoginButton.Content = has ? "Войти" : "Создать пароль";
        PwdBox.Clear();
        Pwd2Box.Clear();
        LoginStatus.Text = "";
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (!PasswordService.HasPassword)
        {
            if (PwdBox.Password.Length < 4)
            {
                LoginStatus.Text = "Пароль должен содержать не меньше 4 символов.";
                return;
            }
            if (PwdBox.Password != Pwd2Box.Password)
            {
                LoginStatus.Text = "Пароли не совпадают.";
                return;
            }
            PasswordService.SetPassword(PwdBox.Password);
            LogService.Write("Admin", "Info", "Задан пароль администратора");
        }
        else if (!PasswordService.Verify(PwdBox.Password))
        {
            LoginStatus.Text = "Неверный пароль.";
            LogService.Write("Admin", "Warning", "Неудачная попытка входа");
            return;
        }

        PasswordService.IsUnlocked = true;
        ShowState();
    }

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        PasswordService.IsUnlocked = false;
        ShowState();
    }

    private void Source_Click(object sender, RoutedEventArgs e)
    {
        AppSettings.ChessEnabled = ChessBox.IsChecked == true;
        AppSettings.ValorantEnabled = ValorantBox.IsChecked == true;
        AppSettings.Save();
    }

    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        var key = KeyBox.Password.Trim();
        if (key.Length < 10)
        {
            KeyStatus.Text = "Ключ слишком короткий, проверьте, что скопирован целиком.";
            return;
        }

        try
        {
            ApiKeyStore.Save(key);
            KeyBox.Clear();
            KeyStatus.Text = "Ключ сохранён (хранится в зашифрованном виде).";
            LogService.Write("Admin", "Info", "API-ключ PandaScore обновлён");   // сам ключ в журнал не пишем
            LogList.ItemsSource = LogService.GetRecent();
        }
        catch (Exception ex)
        {
            KeyStatus.Text = "Не удалось сохранить ключ: " + ex.Message;
        }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "База данных SQLite (*.db)|*.db",
            FileName = $"arenatrack-{DateTime.Now:yyyyMMdd-HHmm}.db"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            SqliteConnection.ClearAllPools();   // закрываем соединения, чтобы файл был целостным
            File.Copy(AppDbContext.DbPath, dlg.FileName, overwrite: true);
            BackupStatus.Text = "Копия создана: " + dlg.FileName;
            LogService.Write("Admin", "Info", "Создана резервная копия базы данных");
        }
        catch (Exception ex)
        {
            BackupStatus.Text = "Не удалось создать копию: " + ex.Message;
            LogService.Write("Admin", "Error", "Резервная копия: " + ex.Message);
        }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "База данных SQLite (*.db)|*.db" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var header = new byte[15];
            using (var fs = File.OpenRead(dlg.FileName))
                fs.ReadExactly(header, 0, 15);
            if (System.Text.Encoding.ASCII.GetString(header) != "SQLite format 3")
            {
                BackupStatus.Text = "Выбранный файл не является базой данных SQLite.";
                return;
            }

            var answer = MessageBox.Show(
                "Текущая база будет заменена данными из выбранной копии. Продолжить?",
                "Восстановление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            SqliteConnection.ClearAllPools();

            // Страховка: сохраняем текущую базу рядом, на случай ошибки
            var safety = Path.Combine(Path.GetDirectoryName(AppDbContext.DbPath)!,
                                      "arenatrack.before-restore.db");
            File.Copy(AppDbContext.DbPath, safety, overwrite: true);

            File.Copy(dlg.FileName, AppDbContext.DbPath, overwrite: true);
            File.Delete(AppDbContext.DbPath + "-wal");
            File.Delete(AppDbContext.DbPath + "-shm");

            BackupStatus.Text = "База восстановлена. Перезапустите программу, чтобы увидеть данные.";
            LogService.Write("Admin", "Info", "База данных восстановлена из копии");
        }
        catch (Exception ex)
        {
            BackupStatus.Text = "Не удалось восстановить: " + ex.Message;
            LogService.Write("Admin", "Error", "Восстановление: " + ex.Message);
        }
    }


    private void RefreshLog_Click(object sender, RoutedEventArgs e) =>
        LogList.ItemsSource = LogService.GetRecent();

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogService.Clear();
        LogList.ItemsSource = LogService.GetRecent();
    }
}