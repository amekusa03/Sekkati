using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sekkati.Views;

public partial class MessageBox : Window
{
    private readonly bool _confirm;

    public MessageBox(string title, string message, bool confirm = false)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        _confirm = confirm;
        CancelButton.IsVisible = confirm;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(_confirm ? true : null);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
