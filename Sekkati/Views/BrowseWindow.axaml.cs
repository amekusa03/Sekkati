using System;
using Avalonia.Controls;

namespace Sekkati.Views;

public partial class BrowseWindow : Window
{
    public BrowseWindow(string periodTitle, string content)
    {
        InitializeComponent();
        Title = $"{periodTitle} — SekkaTi";
        Viewer.Text = content;
    }
}
