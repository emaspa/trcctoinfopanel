using System.Collections.Specialized;
using System.Windows;
using Microsoft.Win32;
using TrccToInfoPanel.Wpf.ViewModels;

namespace TrccToInfoPanel.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

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

    private void BrowseFolderInput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Select a TRCC theme folder" };
        if (dlg.ShowDialog() == true && DataContext is MainViewModel vm)
            vm.InputPath = dlg.FolderName;
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
