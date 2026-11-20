using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Sims1Pkg.Gui;

/// <summary>
/// Sims1Pkg's one window: a sidebar of what's open and installed, and a main area that shows a package, an install, the
/// installed packages, or the packing form.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const string Ok = "ok";
    private const string InstallFlag = "--install";

    private readonly InterfaceText _text = InterfaceText.Load();
    private readonly Settings _settings = Settings.ForUser();
    private readonly Installer _installer = new();
    private readonly PackForm _form = new(DateOnly.FromDateTime(DateTime.Now));
    private readonly Control[] _views;

    private GameInstall? _game;
    private Package? _package;
    private ReceiptListing _installed = new([], []);
    private string? _packFolder;
    private PackPreview? _preview;
    private Page _page = Page.Installed;
    private bool _merged;
    private bool _syncingSidebar;

    /// <summary>
    /// Creates the window with nothing open.
    /// </summary>
    public MainWindow()
        : this([])
    {
    }

    /// <summary>
    /// Creates the window. The first argument that isn't a flag is a package to open. With <c>--install</c>, the
    /// package's install starts as soon as the window opens, as it does after restarting with administrator rights.
    /// </summary>
    /// <param name="args">The app's arguments.</param>
    public MainWindow(IReadOnlyList<string> args)
    {
        InitializeComponent();
        _views = [PackageView, ProgressView, SummaryView, InstalledView, PackView];

        ApplyText();
        MergeTitleBar(TitleBars.Merges());
        _game = GameLocator.Find(_settings.LoadGameFolder());
        ShowGame();
        RefreshInstalled();
        ShowPage(Page.Installed);

        string? packagePath = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal));
        bool installNow = args.Contains(InstallFlag);
        Opened += async (_, _) => await StartAsync(packagePath, installNow);
    }

    private async Task StartAsync(string? packagePath, bool installNow)
    {
        if (packagePath is not null)
        {
            await OpenPackageAsync(packagePath);
        }

        if (_game is null)
        {
            await AskForGameAsync();
        }

        if (installNow && _package is not null)
        {
            await InstallOpenedAsync();
        }
    }

    // The text

    private void ApplyText()
    {
        Title = _text.Get(TextKeys.AppTitle);
        TitleText.Text = Title;
        ToolTip.SetTip(MinimizeButton, _text.Get(TextKeys.WindowMinimize));
        ToolTip.SetTip(CloseButton, _text.Get(TextKeys.WindowClose));
        GameLabel.Text = _text.Get(TextKeys.SidebarGameLabel);

        PackageFrame.Header = _text.Get(TextKeys.FrameContents);
        SummaryFrame.Header = _text.Get(TextKeys.FrameContents);
        InstalledFrame.Header = _text.Get(TextKeys.FrameContents);
        PackFrame.Header = _text.Get(TextKeys.FrameContents);
        DetailsFrame.Header = _text.Get(TextKeys.FrameDetails);
        ClosePackageButton.Content = _text.Get(TextKeys.ButtonCancel);
        DoneButton.Content = _text.Get(TextKeys.ButtonDone);
        SummaryHint.Text = _text.Get(TextKeys.SummaryHint);
        InstalledTitle.Text = _text.Get(TextKeys.InstalledTitle);
        InstalledEmpty.Text = _text.Get(TextKeys.InstalledEmpty);
        UninstallButton.Content = _text.Get(TextKeys.ButtonUninstall);

        PackTitle.Text = _text.Get(TextKeys.PackTitle);
        PackIntro.Text = _text.Get(TextKeys.PackIntro);
        ChooseFolderButton.Content = _text.Get(TextKeys.PackChooseFolder);
        SavePackageButton.Content = _text.Get(TextKeys.ButtonSavePackage);
        NameLabel.Text = _text.Get(TextKeys.FieldName);
        AuthorLabel.Text = _text.Get(TextKeys.FieldAuthor);
        DateLabel.Text = _text.Get(TextKeys.FieldDate);
        IdLabel.Text = _text.Get(TextKeys.FieldId);
        VersionLabel.Text = _text.Get(TextKeys.FieldVersion);
        DescriptionLabel.Text = _text.Get(TextKeys.FieldDescription);
        LicenseLabel.Text = _text.Get(TextKeys.FieldLicense);
        SourceUrlLabel.Text = _text.Get(TextKeys.FieldSourceUrl);
        DateBox.Text = _form.Date;
    }

    // The title bar, adapted from Roundhouse

    private bool DrawsWindowButtons => _merged && OperatingSystem.IsLinux() && WindowState != WindowState.FullScreen;

    /// <summary>
    /// Makes the navy bar the window's title bar where the desktop allows. On Linux, Avalonia draws the frame and
    /// Sims1Pkg the buttons; Windows and macOS keep their own buttons over the bar. Elsewhere the window manager's
    /// title bar is used and the navy bar is hidden.
    /// </summary>
    private void MergeTitleBar(bool merged)
    {
        _merged = merged;
        ExtendClientAreaToDecorationsHint = merged;
        ExtendClientAreaTitleBarHeightHint = merged ? TitleBar.Height : -1;
        TitleBar.IsVisible = merged;

        // The bar drags the window; its buttons take clicks.
        WindowDecorationProperties.SetElementRole(TitleBar, merged ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None);
        foreach (Button button in WindowButtons.Children.OfType<Button>())
        {
            WindowDecorationProperties.SetElementRole(button, merged ? WindowDecorationsElementRole.User : WindowDecorationsElementRole.None);
        }

        if (merged && OperatingSystem.IsLinux())
        {
            WindowDecorationsTheme = (ControlTheme)Resources["FrameOnly"]!;
        }

        ShowWindowButtons();
    }

    private void ShowWindowButtons()
    {
        WindowButtons.IsVisible = DrawsWindowButtons;
        ToolTip.SetTip(MaximizeButton, _text.Get(WindowState == WindowState.Maximized ? TextKeys.WindowRestore : TextKeys.WindowMaximize));
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty && MaximizeButton is not null)
        {
            ShowWindowButtons();
        }
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClicked(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Double-clicking the bar itself, not a button on it, maximizes or restores, as a title bar does.
        if (_merged && e.Source is Visual source && source.FindAncestorOfType<Button>(includeSelf: true) is null)
        {
            OnMaximizeClicked(sender, e);
        }
    }

    // The sidebar

    /// <summary>
    /// The sidebar's entries. <see cref="Open"/> opens a file picker rather than a page.
    /// </summary>
    private enum Page
    {
        Package,
        Installed,
        Open,
        New,
    }

    // The sidebar's four entries are made once and only updated, since Avalonia's list doesn't allow its items to be
    // replaced from inside its own selection handler.
    private void BuildSidebar()
    {
        if (SidebarList.Items.Count == 0)
        {
            foreach (Page page in Enum.GetValues<Page>())
            {
                SidebarList.Items.Add(new ListBoxItem { Tag = page });
            }
        }

        _syncingSidebar = true;
        foreach (ListBoxItem item in SidebarList.Items.OfType<ListBoxItem>())
        {
            Page page = (Page)item.Tag!;
            item.Content = page switch
            {
                Page.Package => _package?.Manifest.Details.Name,
                Page.Installed => _text.Get(TextKeys.SidebarInstalled, _installed.Receipts.Count),
                Page.Open => _text.Get(TextKeys.SidebarOpen),
                _ => _text.Get(TextKeys.SidebarNew),
            };
            item.IsVisible = page != Page.Package || _package is not null;
            if (page == _page)
            {
                SidebarList.SelectedItem = item;
            }
        }

        _syncingSidebar = false;
    }

    private async void OnSidebarSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSidebar || (SidebarList.SelectedItem as ListBoxItem)?.Tag is not Page page || page == _page)
        {
            return;
        }

        // Changes to the selection wait until this handler is done.
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        if (page == Page.Open)
        {
            BuildSidebar();
            if (await PickFileAsync() is { } path)
            {
                await OpenPackageAsync(path);
            }

            return;
        }

        ShowPage(page);
    }

    /// <summary>
    /// Shows a sidebar page: the open package, Installed, or New package.
    /// </summary>
    private void ShowPage(Page page)
    {
        _page = page;
        ShowView(page switch
        {
            Page.Package => PackageView,
            Page.New => PackView,
            _ => InstalledView,
        });
        BuildSidebar();
    }

    private void ShowView(Control view)
    {
        foreach (Control candidate in _views)
        {
            candidate.IsVisible = candidate == view;
        }
    }

    // The game folder

    private void ShowGame()
    {
        GameName.Text = _game is null ? _text.Get(TextKeys.SidebarNoGame) : Path.GetFileName(_game.RootDirectory);
        ToolTip.SetTip(GameButton, _game?.RootDirectory);
    }

    private async void OnGameClicked(object? sender, RoutedEventArgs e) => await ChooseGameAsync();

    private async Task<bool> AskForGameAsync()
    {
        string? choice = await Dialog.ShowAsync(
            this,
            _text.Get(TextKeys.GameTitle),
            _text.Get(TextKeys.GameMessage),
            [],
            new DialogButton("cancel", _text.Get(TextKeys.ButtonCancel), IsCancel: true),
            new DialogButton(Ok, _text.Get(TextKeys.PackChooseFolder), IsDefault: true));

        return choice == Ok && await ChooseGameAsync();
    }

    private async Task<bool> ChooseGameAsync()
    {
        string? folder = await PickFolderAsync(_text.Get(TextKeys.GameTitle));
        if (folder is null)
        {
            return false;
        }

        if (!GameInstall.IsGameFolder(folder))
        {
            await ShowErrorAsync(_text.Get(TextKeys.GameNotAGame), folder);
            return false;
        }

        // The folder the player picks is used as picked, even when TS1_GAME_DIR names another.
        _game = new GameInstall(folder);
        _settings.SaveGameFolder(_game.RootDirectory);
        ShowGame();
        RefreshInstalled();
        ShowInstalled(null);
        if (_package is not null)
        {
            ShowPackage();
        }

        BuildSidebar();
        return true;
    }

    // A package before installing

    private async Task OpenPackageAsync(string path)
    {
        try
        {
            Package package = Package.Open(path);
            _package?.Dispose();
            _package = package;
            ShowPackage();
            ShowPage(Page.Package);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(_text.Get(TextKeys.OpenFailed, Path.GetFileName(path)), exception.Message);
        }
    }

    private void ShowPackage()
    {
        if (_package is not { } package)
        {
            return;
        }

        PackageDetails details = package.Manifest.Details;
        string id = details.GetEffectiveId();

        PackageName.Text = details.Name;
        PackageMeta.Text = string.Join(" · ", new[]
        {
            _text.Get(TextKeys.PackageAuthor, details.Author),
            details.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            details.Version is null ? null : _text.Get(TextKeys.PackageVersion, details.Version),
            _text.Get(TextKeys.PackageId, id),
        }.OfType<string>());
        PackageDescription.Text = details.Description;
        PackageDescription.IsVisible = details.Description is not null;

        bool unknownKinds = package.Manifest.Files.Any(file => file.Kind is null);
        PackageWarning.Text = unknownKinds ? _text.Get(TextKeys.PackageUnknownKinds) : null;
        PackageWarning.IsVisible = unknownKinds;

        FillRows(PackageRows, "2*,90,3*", [_text.Get(TextKeys.ColumnFile), _text.Get(TextKeys.ColumnKind), _text.Get(TextKeys.ColumnDestination)],
            package.Manifest.Files.Select(file => file.Kind is { } kind
                ? new Row([file.Path, _text.Get(TextKeys.ForKind(kind)), GameLayout.GetDestination(file.Path, kind, id)])
                : new Row([file.Path, _text.Get(TextKeys.KindUnknown, file.KindName), _text.Get(TextKeys.DestinationUnknown)], Warn: true)));

        Receipt? installed = null;
        try
        {
            installed = _game is null ? null : Receipts.Get(_game, id);
        }
        catch (InstallException)
        {
            // A damaged receipt shows here as a fresh install; planning the install reports it.
        }

        InstallButton.Content = _text.Get(installed is null ? TextKeys.ButtonInstall
            : installed.Version == details.Version ? TextKeys.ButtonReinstall
            : TextKeys.ButtonUpdate);
        InstallButton.IsEnabled = !unknownKinds;
    }

    private void OnClosePackageClicked(object? sender, RoutedEventArgs e) => ClosePackage();

    private void ClosePackage()
    {
        _package?.Dispose();
        _package = null;
        ShowPage(Page.Installed);
    }

    // Installing

    private async void OnInstallClicked(object? sender, RoutedEventArgs e) => await InstallOpenedAsync();

    private async Task InstallOpenedAsync()
    {
        if (_package is not { } package || (_game is null && !await AskForGameAsync()) || _game is not { } game)
        {
            return;
        }

        string name = package.Manifest.Details.Name;
        await RunBusyAsync(async () =>
        {
            InstallPlan plan;
            try
            {
                plan = await _installer.PlanInstallAsync(package, game);
            }
            catch (GameFolderNotWritableException exception)
            {
                await HandleNotWritableAsync(exception);
                return;
            }

            if (plan.Kind == InstallKind.Update && !await ConfirmAsync(
                _text.Get(TextKeys.UpdateTitle, name),
                _text.Get(TextKeys.UpdateMessage, plan.Previous?.Version ?? _text.Get(TextKeys.NoVersion), package.Manifest.Details.Version ?? _text.Get(TextKeys.NoVersion)),
                [],
                TextKeys.ButtonUpdate))
            {
                return;
            }

            if (plan.Kind == InstallKind.Reinstall && !await ConfirmAsync(
                _text.Get(TextKeys.ReinstallTitle, name), _text.Get(TextKeys.ReinstallMessage), [], TextKeys.ButtonReinstall))
            {
                return;
            }

            bool replace = plan.Clashes.Count > 0;
            if (replace && !await ConfirmAsync(
                _text.Get(TextKeys.ClashTitle),
                _text.Get(TextKeys.ClashMessage, name),
                [.. plan.Clashes.Select(item => item.Destination)],
                TextKeys.ButtonReplace))
            {
                return;
            }

            ProgressTitle.Text = _text.Get(TextKeys.Installing, name);
            ProgressBar.Maximum = plan.Items.Count;
            ProgressBar.Value = 0;
            ProgressFile.Text = null;
            ShowView(ProgressView);

            var progress = new Progress<InstallProgress>(step =>
            {
                ProgressBar.Value = step.Done;
                ProgressFile.Text = step.Destination;
            });

            Receipt receipt = await _installer.InstallAsync(plan, replace, progress);
            ShowSummary(receipt);
        });
    }

    private void ShowSummary(Receipt receipt)
    {
        SummaryTitle.Text = _text.Get(TextKeys.SummaryInstalled, receipt.Name);
        FillRows(SummaryRows, "*", [_text.Get(TextKeys.ColumnDestination)], receipt.Files.Select(file => new Row([file.Path])));
        ShowView(SummaryView);
    }

    private void OnDoneClicked(object? sender, RoutedEventArgs e)
    {
        string? id = _package?.Manifest.Details.GetEffectiveId();
        ClosePackage();
        ShowInstalled(id);
    }

    // Installed packages

    private void RefreshInstalled()
    {
        try
        {
            _installed = _game is null ? new ReceiptListing([], []) : Receipts.List(_game);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _installed = new ReceiptListing([], [exception.Message]);
        }

        ShowInstalled(null);
    }

    private void ShowInstalled(string? selectId)
    {
        string? keep = selectId ?? ((InstalledList.SelectedItem as ListBoxItem)?.Tag as Receipt)?.Id;

        InstalledList.Items.Clear();
        foreach (Receipt receipt in _installed.Receipts)
        {
            var item = new ListBoxItem { Content = receipt.Name, Tag = receipt };
            InstalledList.Items.Add(item);
            if (receipt.Id == keep)
            {
                InstalledList.SelectedItem = item;
            }
        }

        bool empty = _installed.Receipts.Count == 0;
        InstalledEmpty.IsVisible = empty;
        InstalledList.IsVisible = !empty;
        InstalledProblems.Text = _installed.Problems.Count == 0 ? null
            : _text.Get(TextKeys.InstalledProblems) + Environment.NewLine + string.Join(Environment.NewLine, _installed.Problems);
        InstalledProblems.IsVisible = _installed.Problems.Count > 0;
        ShowInstalledDetails();
    }

    private void OnInstalledSelectionChanged(object? sender, SelectionChangedEventArgs e) => ShowInstalledDetails();

    private void ShowInstalledDetails()
    {
        if ((InstalledList.SelectedItem as ListBoxItem)?.Tag is not Receipt receipt)
        {
            InstalledDetails.IsVisible = false;
            return;
        }

        InstalledDetails.IsVisible = true;
        InstalledName.Text = receipt.Name;
        InstalledMeta.Text = string.Join(" · ", new[]
        {
            _text.Get(TextKeys.PackageAuthor, receipt.Author),
            receipt.Version is null ? null : _text.Get(TextKeys.PackageVersion, receipt.Version),
            _text.Get(TextKeys.InstalledOn, receipt.InstalledAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)),
        }.OfType<string>());

        var replaced = new HashSet<string>(receipt.Backups.Select(backup => backup.OriginalPath), StringComparer.OrdinalIgnoreCase);
        FillRows(InstalledRows, "*", [_text.Get(TextKeys.ColumnDestination)], receipt.Files.Select(file => new Row(
            [replaced.Contains(file.Path) ? $"{file.Path}  ({_text.Get(TextKeys.InstalledReplaced)})" : file.Path])));
    }

    private async void OnUninstallClicked(object? sender, RoutedEventArgs e)
    {
        if ((InstalledList.SelectedItem as ListBoxItem)?.Tag is not Receipt receipt || _game is not { } game)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            UninstallPlan plan;
            try
            {
                plan = await _installer.PlanUninstallAsync(game, receipt.Id);
            }
            catch (GameFolderNotWritableException exception)
            {
                await HandleNotWritableAsync(exception);
                return;
            }

            string message = _text.Get(TextKeys.UninstallMessage);
            if (plan.Missing.Count > 0)
            {
                message += "\n\n" + _text.Get(TextKeys.UninstallMissing) + "\n" + string.Join("\n", plan.Missing);
            }

            if (plan.Changed.Count > 0)
            {
                message += "\n\n" + _text.Get(TextKeys.UninstallChanged);
            }

            if (!await ConfirmAsync(_text.Get(TextKeys.UninstallTitle, receipt.Name), message, plan.Changed, TextKeys.ButtonUninstall))
            {
                return;
            }

            await Task.Run(() => _installer.Uninstall(plan));
            InstalledStatus.Text = _text.Get(TextKeys.Uninstalled, receipt.Name);
        });
    }

    // Making a package

    private async void OnChooseFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (await PickFolderAsync(_text.Get(TextKeys.PickerFolderTitle)) is not { } folder)
        {
            return;
        }

        try
        {
            _preview = PackageWriter.Preview(folder);
            _packFolder = folder;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(_text.Get(TextKeys.OpenFailed, folder), exception.Message);
            return;
        }

        PackStatus.Text = null;
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            NameBox.Text = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        }

        UpdatePackForm();
    }

    private void OnFormChanged(object? sender, TextChangedEventArgs e) => UpdatePackForm();

    private void UpdatePackForm()
    {
        _form.Name = NameBox.Text ?? "";
        _form.Author = AuthorBox.Text ?? "";
        _form.Date = DateBox.Text ?? "";
        _form.Id = IdBox.Text ?? "";
        _form.Version = VersionBox.Text ?? "";
        _form.Description = DescriptionBox.Text ?? "";
        _form.License = LicenseBox.Text ?? "";
        _form.SourceUrl = SourceUrlBox.Text ?? "";

        IReadOnlyList<string> problems = _form.GetProblems();
        FormProblems.Text = string.Join(Environment.NewLine, problems.Select(key => _text.Get(key)));
        FormProblems.IsVisible = problems.Count > 0;

        PackFolder.Text = _packFolder is null ? null : _text.Get(TextKeys.PackFolder, _packFolder);
        PackIntro.IsVisible = _packFolder is null;
        PackLeftOut.Text = _preview is { LeftOut.Count: > 0 } ? _text.Get(TextKeys.PackLeftOut, string.Join(", ", _preview.LeftOut)) : null;

        string id = _form.GetEffectiveId() ?? "…";
        if (_preview is { } preview)
        {
            FillRows(PackRows, "2*,90,3*", [_text.Get(TextKeys.ColumnFile), _text.Get(TextKeys.ColumnKind), _text.Get(TextKeys.ColumnDestination)],
                preview.Items.Select(item => item.Problem is { } problem || item.Kind is not { } kind
                    ? new Row([item.ArchivePath, _text.Get(TextKeys.ForProblem(item.Problem ?? PackProblem.NoKind)), _text.Get(TextKeys.DestinationUnknown)], Warn: true)
                    : new Row([item.ArchivePath, _text.Get(TextKeys.ForKind(kind)), Destination(item.ArchivePath, kind, id)])));

            if (preview.Items.Count == 0)
            {
                PackStatus.Text = _text.Get(TextKeys.PackEmpty);
            }
        }

        SavePackageButton.IsEnabled = _preview?.IsReady == true && problems.Count == 0;
    }

    private static string Destination(string archivePath, ContentKind kind, string id) =>
        PackageIds.IsValid(id) ? GameLayout.GetDestination(archivePath, kind, id) : $"{GameLayout.GetFolder(kind)}/…";

    private async void OnSavePackageClicked(object? sender, RoutedEventArgs e)
    {
        if (_packFolder is not { } folder || _form.ToDetails() is not { } details)
        {
            return;
        }

        if (await PickSaveFileAsync(details.GetEffectiveId() + PackageFormat.FileExtension) is not { } path)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await PackageWriter.PackAsync(folder, path, details, overwrite: true);
            PackStatus.Text = _text.Get(TextKeys.PackSaved, Path.GetFileName(path));
        });
    }

    // Rows in a frame

    private sealed record Row(string[] Cells, bool Warn = false);

    private static void FillRows(StackPanel rows, string columns, string[] headers, IEnumerable<Row> items)
    {
        rows.Children.Clear();
        rows.Children.Add(MakeRow(columns, headers, header: true, warn: false, shaded: false));

        int index = 0;
        foreach (Row row in items)
        {
            rows.Children.Add(MakeRow(columns, row.Cells, header: false, row.Warn, shaded: index++ % 2 == 0));
        }
    }

    private static Border MakeRow(string columns, string[] cells, bool header, bool warn, bool shaded)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(columns), ColumnSpacing = 12 };
        for (int column = 0; column < cells.Length; column++)
        {
            var cell = new TextBlock
            {
                Text = cells[column],
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                FontWeight = header ? FontWeight.Bold : FontWeight.Normal,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
            };

            if (warn && column > 0)
            {
                cell.Classes.Add("warning");
            }

            ToolTip.SetTip(cell, cells[column]);
            Grid.SetColumn(cell, column);
            grid.Children.Add(cell);
        }

        return new Border
        {
            Child = grid,
            Padding = new Thickness(6, 3),
            Background = shaded ? (IBrush?)Application.Current!.FindResource("RowBrush") : null,
        };
    }

    // Popups and pickers

    private async Task<bool> ConfirmAsync(string title, string message, IReadOnlyList<string> lines, string confirmKey) =>
        await Dialog.ShowAsync(
            this,
            title,
            message,
            lines,
            new DialogButton("cancel", _text.Get(TextKeys.ButtonCancel), IsCancel: true),
            new DialogButton(Ok, _text.Get(confirmKey), IsDefault: true)) == Ok;

    private async Task ShowErrorAsync(string message, string? details) =>
        await Dialog.ShowAsync(
            this,
            _text.Get(TextKeys.ErrorTitle),
            message,
            details is null ? [] : [details],
            new DialogButton(Ok, _text.Get(TextKeys.ButtonOk), IsDefault: true, IsCancel: true));

    private async Task HandleNotWritableAsync(GameFolderNotWritableException exception)
    {
        if (OperatingSystem.IsWindows() && !Environment.IsPrivilegedProcess)
        {
            if (await ConfirmAsync(_text.Get(TextKeys.AdminTitle), _text.Get(TextKeys.AdminMessage), [], TextKeys.ButtonContinue))
            {
                RestartAsAdministrator();
            }

            return;
        }

        await ShowErrorAsync(_text.Get(TextKeys.NotWritable, exception.Folder), exception.InnerException?.Message);
    }

    /// <summary>
    /// Restarts Sims1Pkg with administrator rights, using Windows' usual prompt, and installs the open package there.
    /// If the player says no to the prompt, this window carries on.
    /// </summary>
    private void RestartAsAdministrator()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = _package is null ? "" : $"\"{_package.FilePath}\" {InstallFlag}",
        };

        try
        {
            Process.Start(start);
            Close();
        }
        catch (Win32Exception)
        {
            // The player declined the prompt.
        }
    }

    private async Task<string?> PickFileAsync()
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = _text.Get(TextKeys.PickerOpenTitle),
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(_text.Get(TextKeys.PickerPackages)) { Patterns = ["*" + PackageFormat.FileExtension] }],
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
        });

        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    private async Task<string?> PickSaveFileAsync(string suggestedName)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = _text.Get(TextKeys.PickerSaveTitle),
            SuggestedFileName = suggestedName,
            DefaultExtension = PackageFormat.FileExtension.TrimStart('.'),
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType(_text.Get(TextKeys.PickerPackages)) { Patterns = ["*" + PackageFormat.FileExtension] }],
        });

        return file?.TryGetLocalPath();
    }

    /// <summary>
    /// Runs an install, uninstall, or pack with the sidebar locked, and reports any failure in a popup. Afterwards the
    /// installed list is read again, and a failed install goes back to the package.
    /// </summary>
    private async Task RunBusyAsync(Func<Task> work)
    {
        SidebarList.IsEnabled = false;
        GameButton.IsEnabled = false;
        try
        {
            await work();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await ShowErrorAsync(exception.Message, null);
        }
        finally
        {
            SidebarList.IsEnabled = true;
            GameButton.IsEnabled = true;
            RefreshInstalled();
            if (ProgressView.IsVisible)
            {
                ShowView(PackageView);
            }

            if (_package is not null)
            {
                ShowPackage();
            }

            BuildSidebar();
        }
    }
}
