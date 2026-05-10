using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sekkati.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isModified;

    [ObservableProperty]
    private string _content = string.Empty;

    public string Title =>
        (FilePath is null ? "無題" : Path.GetFileName(FilePath))
        + (IsModified ? " *" : "")
        + " — SekkaTi";

    private bool _loading;

    partial void OnContentChanged(string value)
    {
        if (!_loading)
            IsModified = true;
    }

    public void Load(string content, string? path)
    {
        _loading = true;
        Content = content;
        _loading = false;
        FilePath = path;
        IsModified = false;
    }

    public void MarkSaved(string path)
    {
        FilePath = path;
        IsModified = false;
    }
}
