using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Sekkati.ViewModels;

namespace Sekkati.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType[] FileTypes =
    [
        new FilePickerFileType("テキストファイル") { Patterns = ["*.txt", "*.md", "*.log"] },
        new FilePickerFileType("すべてのファイル") { Patterns = ["*"] },
    ];

    private MainWindowViewModel Vm => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Deactivated += OnDeactivated;

        // 起動時またはウィンドウ作成時に現在のテーマをメニューに反映させる
        UpdateThemeMenu(Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default);
    }

    private async Task<string> GetSekkatiFolderAsync()
    {
        var docs = await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        var docsPath = docs?.Path.LocalPath
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folder = Path.Combine(docsPath, "Sekkati");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private bool _isHeld = false;

    private void OnHold(object? sender, RoutedEventArgs e)
    {
        _isHeld = !_isHeld;
        MenuHold.Header = _isHeld ? "保持解除(_H)" : "保持(_H)";
    }

    private async void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isHeld) return;
        if (OwnedWindows.Count > 0) return;

        if (!string.IsNullOrEmpty(Vm.Content))
        {
            if (Vm.FilePath is null)
            {
                var folder = await GetSekkatiFolderAsync();
                var path = Path.Combine(folder, DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
                await File.WriteAllTextAsync(path, Vm.Content);
                Vm.MarkSaved(path);
            }
            else if (Vm.IsModified)
            {
                await File.WriteAllTextAsync(Vm.FilePath, Vm.Content);
                Vm.MarkSaved(Vm.FilePath);
            }
        }

        Hide();
    }

    private async void OnBrowse1Hour(object? sender, RoutedEventArgs e) =>
        await OpenBrowseWindow("1時間以内", DateTime.Now.AddHours(-1), DateTime.Now);

    private async void OnBrowseToday(object? sender, RoutedEventArgs e) =>
        await OpenBrowseWindow("今日", DateTime.Today, DateTime.Today.AddDays(1));

    private async void OnBrowseYesterday(object? sender, RoutedEventArgs e) =>
        await OpenBrowseWindow("昨日", DateTime.Today.AddDays(-1), DateTime.Today);

    private async void OnBrowseThisWeek(object? sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek;
        var monday = today.AddDays(dow == 0 ? -6 : -(dow - 1));
        await OpenBrowseWindow("今週", monday, DateTime.Now);
    }

    private async void OnBrowse1Month(object? sender, RoutedEventArgs e) =>
        await OpenBrowseWindow("1ヶ月以内", DateTime.Now.AddMonths(-1), DateTime.Now);

    private async void OnBrowse1Year(object? sender, RoutedEventArgs e) =>
        await OpenBrowseWindow("1年", DateTime.Now.AddYears(-1), DateTime.Now);

    private async void OnDelete1Hour(object? sender, RoutedEventArgs e) =>
        await DeleteFiles("1時間以内", DateTime.Now.AddHours(-1), DateTime.Now);

    private async void OnDeleteToday(object? sender, RoutedEventArgs e) =>
        await DeleteFiles("今日", DateTime.Today, DateTime.Today.AddDays(1));

    private async void OnDeleteYesterday(object? sender, RoutedEventArgs e) =>
        await DeleteFiles("昨日", DateTime.Today.AddDays(-1), DateTime.Today);

    private async void OnDeleteThisWeek(object? sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek;
        var monday = today.AddDays(dow == 0 ? -6 : -(dow - 1));
        await DeleteFiles("今週", monday, DateTime.Now);
    }

    private async void OnDelete1Month(object? sender, RoutedEventArgs e) =>
        await DeleteFiles("1ヶ月以内", DateTime.Now.AddMonths(-1), DateTime.Now);

    private async void OnDelete1Year(object? sender, RoutedEventArgs e) =>
        await DeleteFiles("1年", DateTime.Now.AddYears(-1), DateTime.Now);

    private async Task DeleteFiles(string label, DateTime from, DateTime to)
    {
        var folder = await GetSekkatiFolderAsync();
        var files = Directory.GetFiles(folder, "*.txt")
            .Select(path => (path, name: Path.GetFileNameWithoutExtension(path)))
            .Where(f => DateTime.TryParseExact(f.name, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                && dt >= from && dt < to)
            .Select(f => f.path)
            .ToList();

        if (files.Count == 0)
        {
            await new MessageBox($"削除対象なし", "該当するメモはありません。").ShowDialog(this);
            return;
        }

        var confirmed = await new MessageBox(
            $"{label}のメモを削除",
            $"{files.Count} 件のメモを削除します。よろしいですか？",
            confirm: true).ShowDialog<bool>(this);

        if (!confirmed) return;

        foreach (var path in files)
            File.Delete(path);
    }

    private async Task OpenBrowseWindow(string title, DateTime from, DateTime to)
    {
        var folder = await GetSekkatiFolderAsync();
        var entries = Directory.GetFiles(folder, "*.txt")
            .Select(path => (path, name: Path.GetFileNameWithoutExtension(path)))
            .Where(f => DateTime.TryParseExact(f.name, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                && dt >= from && dt < to)
            .Select(f => (f.path, dt: DateTime.ParseExact(f.name, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture)))
            .OrderByDescending(f => f.dt)
            .ToList();

        if (entries.Count == 0)
        {
            new BrowseWindow(title, "該当するメモはありません。").Show(this);
            return;
        }

        var sb = new StringBuilder();
        foreach (var (path, dt) in entries)
        {
            sb.AppendLine($"── {dt:yyyy/MM/dd HH:mm} ──────────────────────────");
            sb.AppendLine(await File.ReadAllTextAsync(path));
        }

        new BrowseWindow(title, sb.ToString().TrimEnd()).Show(this);
    }

    private void OnNewWindow(object? sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
    }

    private async void OnOpen(object? sender, RoutedEventArgs e)
    {
        var sekkatiFolder = await StorageProvider.TryGetFolderFromPathAsync(
            await GetSekkatiFolderAsync());

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "ファイルを開く",
            AllowMultiple = true,
            FileTypeFilter = FileTypes,
            SuggestedStartLocation = sekkatiFolder,
        });

        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // 空の無題ウィンドウなら再利用、そうでなければ新しいウィンドウで開く
            if (Vm.FilePath is null && !Vm.IsModified && string.IsNullOrEmpty(Vm.Content))
            {
                Vm.Load(content, file.Path.LocalPath);
            }
            else
            {
                var win = new MainWindow();
                win.Show();
                ((MainWindowViewModel)win.DataContext!).Load(content, file.Path.LocalPath);
            }
        }
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        if (Vm.FilePath is not null)
        {
            await File.WriteAllTextAsync(Vm.FilePath, Vm.Content);
            Vm.MarkSaved(Vm.FilePath);
        }
        else
        {
            await SaveAs();
        }
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e) => await SaveAs();

    private async Task SaveAs()
    {
        var sekkatiFolder = await StorageProvider.TryGetFolderFromPathAsync(
            await GetSekkatiFolderAsync());

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "名前を付けて保存",
            DefaultExtension = "txt",
            SuggestedFileName = DateTime.Now.ToString("yyyyMMddHHmmss"),
            SuggestedStartLocation = sekkatiFolder,
            FileTypeChoices = FileTypes,
        });
        if (file is null) return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(Vm.Content);
        Vm.MarkSaved(file.Path.LocalPath);
    }

    // Show() のたびにマウス位置へ移動（Hide() 後の再表示も含む）
    protected override void OnOpened(EventArgs e)
    {
        PositionAtCursor();
        base.OnOpened(e);
    }

    private void PositionAtCursor()
    {
        var cursor = CursorHelper.TryGetPosition();
        if (cursor is null) return;

        var (cx, cy) = cursor.Value;
        var pixelPoint = new PixelPoint(cx, cy);
        var screen = Screens.ScreenFromPoint(pixelPoint);

        if (screen != null)
        {
            var area = screen.WorkingArea;
            var scale = screen.Scaling;
            var ww = (int)(Width * scale);
            var wh = (int)(Height * scale);
            var x = Math.Clamp(cx, area.X, area.Right - ww);
            var y = Math.Clamp(cy, area.Y, area.Bottom - wh);
            Position = new PixelPoint(x, y);
        }
        else
        {
            Position = pixelPoint;
        }
    }

    // X ボタンや「閉じる」はウィンドウを隠す（アプリはバックグラウンドで継続）
    // シャットダウン中はキャンセルしない（OS 終了をブロックしないため）
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!App.IsShuttingDown)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }

    private void OnThemeLight(object? sender, RoutedEventArgs e) => ApplyTheme(ThemeVariant.Light);
    private void OnThemeDark(object? sender, RoutedEventArgs e) => ApplyTheme(ThemeVariant.Dark);
    private void OnThemeSystem(object? sender, RoutedEventArgs e) => ApplyTheme(ThemeVariant.Default);

    private void ApplyTheme(ThemeVariant variant)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = variant;
            // 全ての開いているウィンドウのメニューを更新する
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows.OfType<MainWindow>())
                {
                    window.UpdateThemeMenu(variant);
                }
            }
        }
    }

    public void UpdateThemeMenu(ThemeVariant variant)
    {
        MenuThemeLight.Icon = null;
        MenuThemeDark.Icon = null;
        MenuThemeSystem.Icon = null;

        var target = variant == ThemeVariant.Light ? MenuThemeLight
            : variant == ThemeVariant.Dark ? MenuThemeDark
            : MenuThemeSystem;

        if (target != null)
        {
            target.Icon = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 8, Height = 8,
                Fill = Avalonia.Media.Brushes.Gray,
            };
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Hide();

    private void OnQuit(object? sender, RoutedEventArgs e)
    {
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.Shutdown();
    }
}
