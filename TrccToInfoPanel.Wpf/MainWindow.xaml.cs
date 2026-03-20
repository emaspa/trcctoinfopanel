using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TrccToInfoPanel.Wpf.ViewModels;

namespace TrccToInfoPanel.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadIcon();

        // Auto-scroll log to bottom
        if (DataContext is MainViewModel vm)
        {
            ((INotifyCollectionChanged)vm.LogMessages).CollectionChanged += (_, _) =>
            {
                if (LogListBox.Items.Count > 0)
                    LogListBox.ScrollIntoView(LogListBox.Items[^1]);
            };
        }
    }

    private void LoadIcon()
    {
        var exeDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(exeDir, "app.ico"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"),
            Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location) ?? "", "..", "..", "..", "app.ico"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                // Use BitmapDecoder to pick the largest frame from the .ico
                using var stream = File.OpenRead(Path.GetFullPath(path));
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var largest = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
                Icon = largest;
                return;
            }
            catch { /* try next */ }
        }
    }

    private void BrowseFolderInput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select a TRCC theme folder" };
        if (dlg.ShowDialog() == true && DataContext is MainViewModel vm)
            vm.InputPath = dlg.FolderName;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0
            && DataContext is MainViewModel vm)
        {
            vm.InputPath = files[0];
        }
    }
}
