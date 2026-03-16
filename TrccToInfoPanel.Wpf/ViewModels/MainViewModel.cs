using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TrccToInfoPanel.Wpf.ViewModels;

public enum DetectedMode { Unknown, SingleFile, SingleDirectory, AllInResolution, Batch }

public class MainViewModel : ObservableObject
{
    private string _inputPath = "";
    private string _outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InfoPanel");
    private DetectedMode _mode = DetectedMode.Unknown;
    private string _modeDescription = "Select a config1.dc file or TRCC theme folder";
    private bool _isConverting;
    private int _canvasWidth = 800;
    private int _canvasHeight = 480;
    private BitmapImage? _backgroundImage;
    private TrccTheme? _parsedTheme;
    private List<TrccTheme>? _parsedThemes;

    public string InputPath
    {
        get => _inputPath;
        set { if (SetProperty(ref _inputPath, value)) DetectMode(); }
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public DetectedMode Mode
    {
        get => _mode;
        private set => SetProperty(ref _mode, value);
    }

    public string ModeDescription
    {
        get => _modeDescription;
        private set => SetProperty(ref _modeDescription, value);
    }

    public bool IsConverting
    {
        get => _isConverting;
        private set { SetProperty(ref _isConverting, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public int CanvasWidth
    {
        get => _canvasWidth;
        private set => SetProperty(ref _canvasWidth, value);
    }

    public int CanvasHeight
    {
        get => _canvasHeight;
        private set => SetProperty(ref _canvasHeight, value);
    }

    public BitmapImage? BackgroundImage
    {
        get => _backgroundImage;
        private set => SetProperty(ref _backgroundImage, value);
    }

    public ObservableCollection<PreviewElement> PreviewElements { get; } = [];
    public ObservableCollection<ElementInfo> ElementInfos { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public ICommand BrowseInputCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand ConvertCommand { get; }

    public MainViewModel()
    {
        BrowseInputCommand = new RelayCommand(_ => BrowseInput());
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        ConvertCommand = new RelayCommand(async _ => await ConvertAsync(), _ => CanConvert());
    }

    private bool CanConvert() => !IsConverting && Mode != DetectedMode.Unknown
        && !string.IsNullOrWhiteSpace(InputPath) && !string.IsNullOrWhiteSpace(OutputPath);

    private void BrowseInput()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select config1.dc or any file in a theme folder",
            Filter = "TRCC Theme (config1.dc)|config1.dc|All files (*.*)|*.*",
            CheckFileExists = false,
        };

        if (dlg.ShowDialog() == true)
        {
            InputPath = dlg.FileName;
            return;
        }

        // Try folder picker
        var folderDlg = new OpenFolderDialog
        {
            Title = "Select a TRCC theme folder",
        };

        if (folderDlg.ShowDialog() == true)
            InputPath = folderDlg.FolderName;
    }

    private void BrowseOutput()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select output directory",
            InitialDirectory = OutputPath,
        };

        if (dlg.ShowDialog() == true)
            OutputPath = dlg.FolderName;
    }

    private void DetectMode()
    {
        _parsedTheme = null;
        _parsedThemes = null;
        PreviewElements.Clear();
        ElementInfos.Clear();
        BackgroundImage = null;

        var path = InputPath.Trim();
        if (string.IsNullOrEmpty(path))
        {
            Mode = DetectedMode.Unknown;
            ModeDescription = "Select a config1.dc file or TRCC theme folder";
            return;
        }

        // Case 1: Direct config1.dc file
        if (path.EndsWith("config1.dc", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
        {
            TryParseSingle(path);
            return;
        }

        // Case 2: Directory containing config1.dc
        if (Directory.Exists(path))
        {
            var configInDir = Path.Combine(path, "config1.dc");
            if (File.Exists(configInDir))
            {
                TryParseSingle(configInDir);
                return;
            }

            // Case 3: Directory containing Theme* subdirs with config1.dc
            var themeDirs = Directory.GetDirectories(path, "Theme*")
                .Where(d => File.Exists(Path.Combine(d, "config1.dc")))
                .ToList();

            if (themeDirs.Count > 0)
            {
                Mode = DetectedMode.AllInResolution;
                ModeDescription = $"Resolution folder: {themeDirs.Count} themes found";
                TryParseMultiple(themeDirs);
                return;
            }

            // Case 4: Directory containing resolution dirs (batch mode)
            var resDirs = Directory.GetDirectories(path, "Theme*")
                .Where(d => Directory.GetDirectories(d, "Theme*").Any(td => File.Exists(Path.Combine(td, "config1.dc"))))
                .ToList();

            if (resDirs.Count > 0)
            {
                var totalThemes = resDirs.Sum(rd =>
                    Directory.GetDirectories(rd, "Theme*")
                        .Count(td => File.Exists(Path.Combine(td, "config1.dc"))));

                Mode = DetectedMode.Batch;
                ModeDescription = $"Batch: {resDirs.Count} resolutions, {totalThemes} themes total";
                return;
            }
        }

        Mode = DetectedMode.Unknown;
        ModeDescription = "No TRCC theme found at this path";
    }

    private void TryParseSingle(string configPath)
    {
        try
        {
            _parsedTheme = TrccThemeReader.Read(configPath);
            Mode = File.Exists(configPath) && configPath.EndsWith("config1.dc")
                ? DetectedMode.SingleFile : DetectedMode.SingleDirectory;
            ModeDescription = $"Theme: {_parsedTheme.CanvasWidth}x{_parsedTheme.CanvasHeight} — {_parsedTheme.Elements.Count} elements";
            CanvasWidth = _parsedTheme.CanvasWidth > 0 ? _parsedTheme.CanvasWidth : 800;
            CanvasHeight = _parsedTheme.CanvasHeight > 0 ? _parsedTheme.CanvasHeight : 480;
            PopulatePreview(_parsedTheme);
        }
        catch (Exception ex)
        {
            Mode = DetectedMode.Unknown;
            ModeDescription = $"Error reading theme: {ex.Message}";
        }
    }

    private void TryParseMultiple(List<string> themeDirs)
    {
        _parsedThemes = [];
        foreach (var dir in themeDirs.OrderBy(d => d))
        {
            try
            {
                var theme = TrccThemeReader.Read(Path.Combine(dir, "config1.dc"));
                _parsedThemes.Add(theme);
            }
            catch { /* skip broken themes */ }
        }

        // Preview first theme
        if (_parsedThemes.Count > 0)
        {
            _parsedTheme = _parsedThemes[0];
            CanvasWidth = _parsedTheme.CanvasWidth > 0 ? _parsedTheme.CanvasWidth : 800;
            CanvasHeight = _parsedTheme.CanvasHeight > 0 ? _parsedTheme.CanvasHeight : 480;
            PopulatePreview(_parsedTheme);
        }
    }

    private void PopulatePreview(TrccTheme theme)
    {
        PreviewElements.Clear();
        ElementInfos.Clear();

        // Load background image
        LoadBackgroundImage(theme.ThemeDirectory);

        for (int i = 0; i < theme.Elements.Count; i++)
        {
            var el = theme.Elements[i];
            PreviewElements.Add(PreviewElement.FromTrccElement(el));
            ElementInfos.Add(ElementInfo.FromTrccElement(el, i));
        }
    }

    private void LoadBackgroundImage(string themeDir)
    {
        foreach (var name in new[] { "00.png", "01.png" })
        {
            var path = Path.Combine(themeDir, name);
            if (!File.Exists(path)) continue;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                BackgroundImage = bmp;
                return;
            }
            catch { /* skip broken image */ }
        }
        BackgroundImage = null;
    }

    private async Task ConvertAsync()
    {
        IsConverting = true;
        LogMessages.Clear();
        Log($"Output: {OutputPath}");

        try
        {
            await Task.Run(() =>
            {
                switch (Mode)
                {
                    case DetectedMode.SingleFile or DetectedMode.SingleDirectory:
                        ConvertSingleTheme();
                        break;
                    case DetectedMode.AllInResolution:
                        ConvertAllThemes();
                        break;
                    case DetectedMode.Batch:
                        ConvertBatch();
                        break;
                }
            });
            Log("All done!");
        }
        catch (Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
        finally
        {
            IsConverting = false;
        }
    }

    private void ConvertSingleTheme()
    {
        var configPath = InputPath.Trim();
        if (Directory.Exists(configPath))
            configPath = Path.Combine(configPath, "config1.dc");

        Log($"Reading: {configPath}");
        var theme = TrccThemeReader.Read(configPath);
        Log($"Converting: {theme.CanvasWidth}x{theme.CanvasHeight}, {theme.Elements.Count} elements");
        var (profile, items) = ThemeConverter.Convert(theme);
        InfoPanelWriter.WriteProfile(OutputPath, profile, items, theme.ThemeDirectory, msg => Log(msg));
    }

    private void ConvertAllThemes()
    {
        var path = InputPath.Trim();
        var themeDirs = Directory.GetDirectories(path, "Theme*")
            .Where(d => File.Exists(Path.Combine(d, "config1.dc")))
            .OrderBy(d => d).ToList();

        Log($"Converting {themeDirs.Count} themes...");
        int done = 0;

        foreach (var dir in themeDirs)
        {
            try
            {
                var theme = TrccThemeReader.Read(Path.Combine(dir, "config1.dc"));
                Log($"[{++done}/{themeDirs.Count}] {Path.GetFileName(dir)}: {theme.CanvasWidth}x{theme.CanvasHeight}");
                var (profile, items) = ThemeConverter.Convert(theme);
                InfoPanelWriter.WriteProfile(OutputPath, profile, items, theme.ThemeDirectory, msg => Log(msg));
            }
            catch (Exception ex)
            {
                Log($"  Error: {Path.GetFileName(dir)}: {ex.Message}");
            }
        }
    }

    private void ConvertBatch()
    {
        var path = InputPath.Trim();
        var resDirs = Directory.GetDirectories(path, "Theme*").OrderBy(d => d).ToList();

        int total = 0, errors = 0;

        foreach (var resDir in resDirs)
        {
            var themeDirs = Directory.GetDirectories(resDir, "Theme*")
                .Where(d => File.Exists(Path.Combine(d, "config1.dc")))
                .OrderBy(d => d).ToList();

            if (themeDirs.Count == 0) continue;
            Log($"{Path.GetFileName(resDir)}: {themeDirs.Count} themes");

            foreach (var dir in themeDirs)
            {
                try
                {
                    var theme = TrccThemeReader.Read(Path.Combine(dir, "config1.dc"));
                    var (profile, items) = ThemeConverter.Convert(theme);
                    InfoPanelWriter.WriteProfile(OutputPath, profile, items, theme.ThemeDirectory, msg => Log(msg));
                    total++;
                }
                catch (Exception ex)
                {
                    Log($"  Error: {dir}: {ex.Message}");
                    errors++;
                }
            }
        }

        Log($"Converted {total} themes ({errors} errors)");
    }

    private void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessages.Add(message);
        });
    }
}
