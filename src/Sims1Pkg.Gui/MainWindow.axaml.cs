using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sims1Pkg;

namespace Sims1Pkg.Gui;

public partial class MainWindow : Window
{
    private ProgressBar? _progressBar;
    private TextBlock? _statusText;
    private TextBlock? _readyText;
    private ListBox? _fileList;
    private TextBlock? _gameDirText;
    private Button? _refreshDirsButton;

    public MainWindow()
    {
        InitializeComponent();
        
        // Get controls from XAML
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _statusText = this.FindControl<TextBlock>("StatusText");
        _readyText = this.FindControl<TextBlock>("ReadyText");
        _fileList = this.FindControl<ListBox>("FileList");
        _gameDirText = this.FindControl<TextBlock>("GameDirText");
        _refreshDirsButton = this.FindControl<Button>("RefreshDirsButton");
        
        // Wire up event handlers
        if (_refreshDirsButton != null) _refreshDirsButton.Click += OnRefreshDirsClick;
        
        // Initialize GameDirectories display
        RefreshGameDirectories();
        
        UpdateStatus("Ready");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RefreshGameDirectories()
    {
        string gameDir = GameDirectories.GameDirectory;
        string downloadsDir = GameDirectories.DownloadsDirectory;
        
        // Validate game directory
        bool isValid = false;
        if (!string.IsNullOrEmpty(gameDir))
        {
            string simsExe = Path.Combine(gameDir, "Sims.exe");
            isValid = File.Exists(simsExe);
        }
        
        if (_gameDirText != null)
        {
            if (string.IsNullOrEmpty(gameDir))
            {
                _gameDirText.Text = "Game: Not detected - Set TS1_GAME_DIR or install The Sims 1";
            }
            else
            {
                _gameDirText.Text = "Game: " + gameDir + (isValid ? " (Valid)" : " (Warning: Sims.exe not found)");
            }
            
            if (!string.IsNullOrEmpty(downloadsDir))
            {
                _gameDirText.Text += " | Downloads: " + downloadsDir;
            }
        }
        
        UpdateStatus("Game directories: " + (isValid ? "Valid" : "Not found"));
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

    private void OnRefreshDirsClick(object? sender, RoutedEventArgs e)
    {
        // Reset and rediscover
        GameDirectories.Reset();
        RefreshGameDirectories();
    }

    private void UpdateStatus(string message)
    {
        if (_statusText != null) _statusText.Text = message;
        if (_readyText != null) _readyText.Text = message;
    }
}
