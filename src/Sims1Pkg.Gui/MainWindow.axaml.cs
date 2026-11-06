using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sims1Pkg;

namespace Sims1Pkg.Gui;

public partial class MainWindow : Window
{
    private readonly TS1Package _packageService = new();
    private ProgressBar? _progressBar;
    private TextBlock? _statusText;
    private TextBlock? _readyText;
    private ListBox? _fileList;
    private Button? _extractButton;
    private Button? _compressButton;
    private Button? _inspectButton;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get controls from XAML
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _readyText = this.FindControl<TextBlock>("ReadyText");
        _fileList = this.FindControl<ListBox>("FileList");
        _extractButton = this.FindControl<Button>("ExtractButton");
        _compressButton = this.FindControl<Button>("CompressButton");
        _inspectButton = this.FindControl<Button>("InspectButton");
        
        // Wire up event handlers
        if (_extractButton != null) _extractButton.Click += OnExtractClick;
        if (_compressButton != null) _compressButton.Click += OnCompressClick;
        if (_inspectButton != null) _inspectButton.Click += OnInspectClick;
        
        // Initialize
        UpdateStatus("Ready");
        
        // Add supported extensions to file dialog filters
        _packageService.Progress += (sender, e) => 
        {
            SetProgress(e.PercentComplete, e.Message);
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SetProgress(int percent, string message)
    {
        if (_progressBar != null)
        {
            _progressBar.Value = percent;
            _progressBar.IsIndeterminate = percent < 0;
        }
        UpdateStatus(message);
    }

    private void SetIndeterminate(bool indeterminate)
    {
        if (_progressBar != null)
            _progressBar.IsIndeterminate = indeterminate;
    }

    private void SetFileList(IEnumerable<string> files)
    {
        if (_fileList != null)
            _fileList.ItemsSource = files;
    }

    private async void OnExtractClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Package File",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Sims1Pkg Files", Extensions = { "s1pk" } },
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                }
            };
            
            var result = await dialog.ShowAsync(this);
            if (result == null || result.Length == 0) return;
            
            string packagePath = result[0];
            string outputDir = Path.Combine(
                Path.GetDirectoryName(packagePath)!,
                Path.GetFileNameWithoutExtension(packagePath));
            
            Directory.CreateDirectory(outputDir);
            
            UpdateStatus("Extracting...");
            SetIndeterminate(true);
            
            await _packageService.ExtractAsync(packagePath, outputDir);
            
            UpdateStatus("Extraction complete!");
            SetIndeterminate(false);
            SetProgress(100, "Done");
            
            // Show extracted files
            var files = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(outputDir, f))
                .ToList();
            SetFileList(files);
        }
        catch (Exception ex)
        {
            UpdateStatus("Error: " + ex.Message);
            SetIndeterminate(false);
        }
    }

    private async void OnCompressClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Source Directory"
            };
            
            var sourceDir = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(sourceDir)) return;
            
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Package As",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Sims1Pkg Files", Extensions = { "s1pk" } }
                },
                InitialFileName = Path.GetFileName(sourceDir) + ".s1pk"
            };
            
            var packagePath = await saveDialog.ShowAsync(this);
            if (string.IsNullOrEmpty(packagePath)) return;
            
            UpdateStatus("Compressing...");
            SetIndeterminate(true);
            
            await _packageService.CompressAsync(sourceDir, packagePath);
            
            UpdateStatus("Compression complete!");
            SetIndeterminate(false);
            SetProgress(100, "Done");
        }
        catch (Exception ex)
        {
            UpdateStatus("Error: " + ex.Message);
            SetIndeterminate(false);
        }
    }

    private async void OnInspectClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Package File",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Sims1Pkg Files", Extensions = { "s1pk" } },
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                }
            };
            
            var result = await dialog.ShowAsync(this);
            if (result == null || result.Length == 0) return;
            
            string packagePath = result[0];
            
            UpdateStatus("Inspecting package...");
            SetIndeterminate(true);
            
            // For inspection, we extract to a temp directory and list files
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            
            await _packageService.ExtractAsync(packagePath, tempDir);
            
            // Get files
            var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(tempDir, f))
                .ToList();
            
            SetFileList(files);
            
            UpdateStatus("Package inspected");
            SetIndeterminate(false);
            SetProgress(100, "Done");
        }
        catch (Exception ex)
        {
            UpdateStatus("Error: " + ex.Message);
            SetIndeterminate(false);
        }
    }

    private void UpdateStatus(string message)
    {
        if (_statusText != null) _statusText.Text = message;
        if (_readyText != null) _readyText.Text = message;
    }
}
