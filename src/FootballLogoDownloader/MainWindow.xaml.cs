using FootballLogoDownloader.Models;
using FootballLogoDownloader.Services;
using FootballLogoDownloader.Security;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace FootballLogoDownloader;

public partial class MainWindow : Window
{
    private readonly FootyLogosService _footy = new();
    private readonly SettingsService _settingsService = new();
    private readonly AppSettings _settings;
    private readonly ObservableCollection<CountryItem> _countries = [];
    private readonly ObservableCollection<CompetitionItem> _competitions = [];
    private bool _initializing = true;
    private bool _busy;
    private string _lastDownloadFolder = string.Empty;

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["tr"] = new()
        {
            ["Subtitle"]="Futbol logolarını ülke ve lig seçerek SVG formatında indir.",
            ["Competition"]="Ligini seç", ["Country"]="Ülke", ["League"]="Lig / turnuva",
            ["Save"]="Kayıt konumunu seç", ["Folder"]="Kayıt klasörü", ["Browse"]="Seç...",
            ["Download"]="SVG LOGOLARINI İNDİR", ["Downloading"]="İNDİRİLİYOR...", ["Open"]="Klasörü Aç",
            ["Status"]="Durum", ["LoadingCountries"]="Ülkeler yükleniyor...", ["LoadingLeagues"]="Ligler yükleniyor...",
            ["CountriesReady"]="{0} ülke/seçenek hazır.", ["LeaguesReady"]="{0} lig/turnuva bulundu.",
            ["NoLeagues"]="Bu ülke için lig bulunamadı.", ["CountriesError"]="Ülke listesi alınamadı.",
            ["LeaguesError"]="Lig listesi alınamadı.", ["TeamsLoading"]="Takım listesi alınıyor...",
            ["NoTeams"]="Bu turnuva için logo bulunamadı.", ["DownloadingItem"]="İndiriliyor: {0} ({1}/{2})",
            ["Complete"]="Tamamlandı: {0} indirildi, {1} zaten vardı, {2} SVG bulunamadı, {3} hata.",
            ["FolderRequired"]="Önce bir kayıt klasörü seçin.", ["FolderInvalid"]="Kayıt klasörü geçersiz veya erişilemiyor.", ["SelectionRequired"]="Önce ülke ve lig seçin.",
            ["Details"]="İndirme ayrıntıları", ["Source"]="Kaynak: ", ["Rights"]="Logo kullanım hakları",
            ["International"]="Uluslararası", ["System"]="Sistem", ["Dark"]="Koyu", ["Light"]="Açık",
            ["FetchError"]="Veriler alınırken hata oluştu. İnternet bağlantınızı kontrol edin.",
            ["DownloadError"]="İndirme sırasında beklenmeyen bir hata oluştu."
        },
        ["en"] = new()
        {
            ["Subtitle"]="Download football logos by country and competition in SVG format.",
            ["Competition"]="Choose competition", ["Country"]="Country", ["League"]="League / competition",
            ["Save"]="Choose save location", ["Folder"]="Download folder", ["Browse"]="Browse...",
            ["Download"]="DOWNLOAD SVG LOGOS", ["Downloading"]="DOWNLOADING...", ["Open"]="Open Folder",
            ["Status"]="Status", ["LoadingCountries"]="Loading countries...", ["LoadingLeagues"]="Loading competitions...",
            ["CountriesReady"]="{0} countries/options ready.", ["LeaguesReady"]="{0} leagues/competitions found.",
            ["NoLeagues"]="No competition found for this country.", ["CountriesError"]="Could not load country list.",
            ["LeaguesError"]="Could not load competition list.", ["TeamsLoading"]="Loading team list...",
            ["NoTeams"]="No logo entries found for this competition.", ["DownloadingItem"]="Downloading: {0} ({1}/{2})",
            ["Complete"]="Complete: {0} downloaded, {1} already existed, {2} SVG unavailable, {3} errors.",
            ["FolderRequired"]="Please choose a download folder first.", ["FolderInvalid"]="The download folder is invalid or cannot be accessed.", ["SelectionRequired"]="Please choose a country and competition first.",
            ["Details"]="Download details", ["Source"]="Source: ", ["Rights"]="Logo usage rights",
            ["International"]="International", ["System"]="System", ["Dark"]="Dark", ["Light"]="Light",
            ["FetchError"]="Could not retrieve data. Please check your internet connection.",
            ["DownloadError"]="An unexpected error occurred while downloading."
        }
    };

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsService.Load();
        CountryCombo.ItemsSource = _countries;
        LeagueCombo.ItemsSource = _competitions;
        OutputFolderTextBox.Text = _settings.OutputFolder;
        Loaded += MainWindow_Loaded;
        StateChanged += (_, _) => MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        Closing += (_, _) => SaveSettings();
    }

    private string T(string key) => Strings[_settings.Language].TryGetValue(key, out var value) ? value : key;

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SelectComboItemByTag(LanguageCombo, _settings.Language);
        SelectComboItemByTag(ThemeCombo, _settings.Theme);
        ApplyTheme();
        ApplyLanguage();
        await LoadCountriesAsync();
        _initializing = false;
    }

    private async Task LoadCountriesAsync()
    {
        SetBusy(true, T("LoadingCountries"));
        try
        {
            var loaded = await _footy.GetCountriesAsync();
            _countries.Clear();
            foreach (var country in loaded)
            {
                country.DisplayName = _settings.Language == "tr" ? country.NameTr : country.NameEn;
                _countries.Add(country);
            }
            SortCountries();

            var wanted = _countries.FirstOrDefault(x => x.Slug.Equals(_settings.LastCountrySlug, StringComparison.OrdinalIgnoreCase))
                         ?? _countries.FirstOrDefault(x => x.Slug == "turkey")
                         ?? _countries.FirstOrDefault();
            CountryCombo.SelectedItem = wanted;
            StatusText.Text = string.Format(T("CountriesReady"), _countries.Count);
        }
        catch
        {
            StatusText.Text = T("CountriesError");
            ShowInlineResult(T("FetchError"), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SortCountries()
    {
        var selectedSlug = (CountryCombo.SelectedItem as CountryItem)?.Slug;
        var sorted = _countries.OrderBy(x => x.Slug == "__international__" ? 0 : 1)
            .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        _countries.Clear();
        foreach (var item in sorted) _countries.Add(item);
        if (!string.IsNullOrWhiteSpace(selectedSlug))
            CountryCombo.SelectedItem = _countries.FirstOrDefault(x => x.Slug == selectedSlug);
    }

    private async void CountryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CountryCombo.SelectedItem is not CountryItem country) return;
        _settings.LastCountrySlug = country.Slug;
        ResetResult();
        UpdateSelectionSummary();
        await LoadCompetitionsAsync(country);
    }

    private async Task LoadCompetitionsAsync(CountryItem country)
    {
        LeagueCombo.IsEnabled = false;
        _competitions.Clear();
        StatusText.Text = T("LoadingLeagues");
        CurrentItemText.Text = country.DisplayName;
        SetBusy(true, T("LoadingLeagues"), keepCountryEnabled: true);

        try
        {
            var loaded = await _footy.GetCompetitionsForCountryAsync(country.Slug);
            foreach (var competition in loaded) _competitions.Add(competition);

            if (_competitions.Count > 0)
            {
                var remembered = _competitions.FirstOrDefault(x => x.Slug.Equals(_settings.LastCompetitionSlug, StringComparison.OrdinalIgnoreCase));
                LeagueCombo.SelectedItem = remembered ?? _competitions[0];
                LeagueCombo.IsEnabled = true;
                StatusText.Text = string.Format(T("LeaguesReady"), _competitions.Count);
            }
            else
            {
                StatusText.Text = T("NoLeagues");
            }
        }
        catch
        {
            StatusText.Text = T("LeaguesError");
            ShowInlineResult(T("FetchError"), true);
        }
        finally
        {
            SetBusy(false);
            CountryCombo.IsEnabled = true;
            LeagueCombo.IsEnabled = _competitions.Count > 0;
            CurrentItemText.Text = string.Empty;
        }
    }

    private void LeagueCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LeagueCombo.SelectedItem is CompetitionItem league) _settings.LastCompetitionSlug = league.Slug;
        UpdateSelectionSummary();
        ResetResult();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (CountryCombo.SelectedItem is not CountryItem country || LeagueCombo.SelectedItem is not CompetitionItem league)
        {
            ShowInlineResult(T("SelectionRequired"), true);
            return;
        }

        var rootInput = OutputFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(rootInput))
        {
            ShowInlineResult(T("FolderRequired"), true);
            return;
        }

        if (!TryNormalizeOutputRoot(rootInput, out var root))
        {
            ShowInlineResult(T("FolderInvalid"), true);
            return;
        }

        var countryName = _settings.Language == "tr" ? country.NameTr : country.NameEn;
        var leagueName = FootyLogosService.GetDisplayCompetitionName(league.Name, league.Slug);
        var folderName = country.Slug == "__international__"
            ? FootyLogosService.GetSafeName($"{T("International")} {leagueName}")
            : FootyLogosService.GetSafeName($"{countryName} {leagueName}");

        var leagueFolder = Path.GetFullPath(Path.Combine(root, folderName));
        var rootPrefix = Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar;
        if (!leagueFolder.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            ShowInlineResult(T("FolderInvalid"), true);
            return;
        }

        try
        {
            Directory.CreateDirectory(leagueFolder);
        }
        catch
        {
            ShowInlineResult(T("FolderInvalid"), true);
            return;
        }

        OutputFolderTextBox.Text = root;
        _lastDownloadFolder = leagueFolder;
        _settings.OutputFolder = root;
        SaveSettings();

        ResetResult();
        LogTextBox.Clear();
        DownloadProgress.Value = 0;
        ProgressCountText.Text = "0 / 0";
        SetBusy(true, T("TeamsLoading"));

        try
        {
            var teams = await _footy.GetTeamEntriesAsync(league.Slug);
            if (teams.Count == 0)
            {
                ShowInlineResult(T("NoTeams"), true);
                return;
            }

            var downloaded = 0;
            var existing = 0;
            var missing = 0;
            var errors = 0;
            var missingNames = new List<string>();
            DownloadProgress.Maximum = teams.Count;

            for (var i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var safeName = FootyLogosService.GetSafeName(team.Name, team.Slug);
                var outputPath = Path.Combine(leagueFolder, safeName + ".svg");
                StatusText.Text = string.Format(T("DownloadingItem"), team.Name, i + 1, teams.Count);
                CurrentItemText.Text = outputPath;
                ProgressCountText.Text = $"{i + 1} / {teams.Count}";

                var result = await _footy.DownloadSvgAsync(team.Slug, outputPath);
                switch (result)
                {
                    case DownloadResult.Downloaded: downloaded++; AppendLog($"✓  {team.Name}"); break;
                    case DownloadResult.Exists: existing++; AppendLog($"—  {team.Name}"); break;
                    case DownloadResult.Missing: missing++; missingNames.Add(team.Name); AppendLog($"○  {team.Name}  [SVG unavailable]"); break;
                    case DownloadResult.Error: errors++; AppendLog($"✕  {team.Name}"); break;
                }
                DownloadProgress.Value = i + 1;
            }

            var missingPath = Path.Combine(leagueFolder, "_Missing-SVG.txt");
            if (missingNames.Count > 0)
                await File.WriteAllLinesAsync(missingPath, missingNames);
            else if (File.Exists(missingPath))
                File.Delete(missingPath);

            StatusText.Text = string.Format(T("Complete"), downloaded, existing, missing, errors);
            CurrentItemText.Text = leagueFolder;
            ShowInlineResult(StatusText.Text, errors > 0, leagueFolder);
        }
        catch
        {
            ShowInlineResult(T("DownloadError"), true);
        }
        finally
        {
            SetBusy(false);
            CountryCombo.IsEnabled = true;
            LeagueCombo.IsEnabled = _competitions.Count > 0;
        }
    }

    private void SetBusy(bool busy, string? status = null, bool keepCountryEnabled = false)
    {
        _busy = busy;
        if (!string.IsNullOrWhiteSpace(status)) StatusText.Text = status;
        DownloadButton.Content = busy ? T("Downloading") : T("Download");
        DownloadButton.IsEnabled = !busy;
        BrowseButton.IsEnabled = !busy;
        OutputFolderTextBox.IsEnabled = !busy;
        ThemeCombo.IsEnabled = !busy;
        LanguageCombo.IsEnabled = !busy;
        CountryCombo.IsEnabled = !busy || keepCountryEnabled;
        LeagueCombo.IsEnabled = !busy && _competitions.Count > 0;
    }

    private void ApplyLanguage()
    {
        WindowTitleText.Text = "Football Logo Downloader";
        HeroTitleText.Text = "Football Logo Downloader";
        SubtitleText.Text = T("Subtitle");
        CompetitionHeaderText.Text = T("Competition");
        CountryLabelText.Text = T("Country");
        LeagueLabelText.Text = T("League");
        SaveHeaderText.Text = T("Save");
        FolderLabelText.Text = T("Folder");
        BrowseButton.Content = T("Browse");
        DownloadButton.Content = _busy ? T("Downloading") : T("Download");
        OpenFolderButton.Content = T("Open");
        ResultOpenButton.Content = T("Open");
        StatusHeaderText.Text = T("Status");
        DetailsHeaderText.Text = T("Details");
        SourceLabelRun.Text = T("Source");
        RightsRun.Text = T("Rights");

        RenameThemeComboItems();
        var selectedSlug = (CountryCombo.SelectedItem as CountryItem)?.Slug;
        foreach (var country in _countries)
            country.DisplayName = _settings.Language == "tr" ? country.NameTr : country.NameEn;
        SortCountries();
        if (!string.IsNullOrWhiteSpace(selectedSlug))
            CountryCombo.SelectedItem = _countries.FirstOrDefault(x => x.Slug == selectedSlug);
        CountryCombo.Items.Refresh();
        UpdateSelectionSummary();
    }

    private void RenameThemeComboItems()
    {
        foreach (var item in ThemeCombo.Items.OfType<ComboBoxItem>())
        {
            item.Content = item.Tag?.ToString() switch
            {
                "System" => T("System"), "Dark" => T("Dark"), "Light" => T("Light"), _ => item.Content
            };
        }
    }

    private void ApplyTheme()
    {
        var useDark = _settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ||
                      (_settings.Theme.Equals("System", StringComparison.OrdinalIgnoreCase) && IsSystemDark());
        var resources = Application.Current.Resources;
        if (useDark)
        {
            resources["WindowBrush"] = Brush("#0B0F14"); resources["TitleBarBrush"] = Brush("#0B0F14");
            resources["CardBrush"] = Brush("#111821"); resources["InputBrush"] = Brush("#0E151E"); resources["PopupBrush"] = Brush("#111821");
            resources["TextBrush"] = Brush("#F6F8FB"); resources["MutedBrush"] = Brush("#93A4B8"); resources["BorderBrush"] = Brush("#243142");
            resources["HoverBrush"] = Brush("#1A2635"); resources["SelectedBrush"] = Brush("#20365A"); resources["TrackBrush"] = Brush("#182231"); resources["LogBrush"] = Brush("#0B1119");
            resources["SuccessBgBrush"] = Brush("#10251E"); resources["SuccessBorderBrush"] = Brush("#1E4A39");
            resources["SuccessIconBgBrush"] = Brush("#17362B"); resources["SuccessActionBgBrush"] = Brush("#153127");
            resources["SuccessActionHoverBrush"] = Brush("#1B4032"); resources["SuccessActionBorderBrush"] = Brush("#2B654E");
            resources["SuccessBrush"] = Brush("#6FE2AD");
        }
        else
        {
            resources["WindowBrush"] = Brush("#F5F7FA"); resources["TitleBarBrush"] = Brush("#FFFFFF");
            resources["CardBrush"] = Brush("#FFFFFF"); resources["InputBrush"] = Brush("#F8FAFC"); resources["PopupBrush"] = Brush("#FFFFFF");
            resources["TextBrush"] = Brush("#172033"); resources["MutedBrush"] = Brush("#68758A"); resources["BorderBrush"] = Brush("#DCE3EC");
            resources["HoverBrush"] = Brush("#EEF3FA"); resources["SelectedBrush"] = Brush("#DCEAFF"); resources["TrackBrush"] = Brush("#E8EDF4"); resources["LogBrush"] = Brush("#F4F7FA");
            resources["SuccessBgBrush"] = Brush("#ECF9F2"); resources["SuccessBorderBrush"] = Brush("#B9E6CF");
            resources["SuccessIconBgBrush"] = Brush("#D8F2E4"); resources["SuccessActionBgBrush"] = Brush("#FFFFFF");
            resources["SuccessActionHoverBrush"] = Brush("#DFF5E9"); resources["SuccessActionBorderBrush"] = Brush("#A9DCC2");
            resources["SuccessBrush"] = Brush("#18794E");
        }
        resources["AccentBrush"] = Brush("#4F8CFF");
        resources["AccentSoftBrush"] = Brush(useDark ? "#8BB5FF" : "#326FD1");
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { return true; }
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedItem is not ComboBoxItem item || item.Tag is null) return;
        _settings.Language = item.Tag.ToString() == "tr" ? "tr" : "en";
        if (!_initializing) { ApplyLanguage(); SaveSettings(); }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is not ComboBoxItem item || item.Tag is null) return;
        _settings.Theme = item.Tag.ToString() ?? "System";
        if (!_initializing) { ApplyTheme(); SaveSettings(); }
    }

    private void UpdateSelectionSummary()
    {
        var country = CountryCombo.SelectedItem as CountryItem;
        var league = LeagueCombo.SelectedItem as CompetitionItem;
        SelectionSummaryText.Text = country is null ? string.Empty : league is null ? country.DisplayName : $"{country.DisplayName}  •  {league.Name}  •  SVG";
    }

    private void ResetResult()
    {
        ResultBanner.Visibility = Visibility.Collapsed;
        _lastDownloadFolder = string.Empty;
    }

    private void ShowInlineResult(string message, bool isError, string? folder = null)
    {
        ResultText.Text = message;

        if (isError)
        {
            var useDark = _settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) ||
                          (_settings.Theme.Equals("System", StringComparison.OrdinalIgnoreCase) && IsSystemDark());

            var errorBrush = Brush(useDark ? "#FF8B93" : "#B4232E");
            ResultText.Foreground = errorBrush;
            ResultBanner.Background = Brush(useDark ? "#2B1518" : "#FFF1F2");
            ResultBanner.BorderBrush = Brush(useDark ? "#5A2930" : "#F4C7CC");
            ResultIconBadge.Background = Brush(useDark ? "#3A1B20" : "#FFE0E3");
            ResultIconPath.Stroke = errorBrush;
        }
        else
        {
            ResultText.Foreground = (Brush)Application.Current.Resources["SuccessBrush"];
            ResultBanner.Background = (Brush)Application.Current.Resources["SuccessBgBrush"];
            ResultBanner.BorderBrush = (Brush)Application.Current.Resources["SuccessBorderBrush"];
            ResultIconBadge.Background = (Brush)Application.Current.Resources["SuccessIconBgBrush"];
            ResultIconPath.Stroke = (Brush)Application.Current.Resources["SuccessBrush"];
        }

        ResultOpenButton.Visibility = string.IsNullOrWhiteSpace(folder) ? Visibility.Collapsed : Visibility.Visible;
        _lastDownloadFolder = folder ?? string.Empty;
        ResultBanner.Visibility = Visibility.Visible;
    }

    private void AppendLog(string line)
    {
        LogTextBox.AppendText(line + Environment.NewLine);
        LogTextBox.ScrollToEnd();
    }

    private void SaveSettings()
    {
        _settings.OutputFolder = OutputFolderTextBox.Text.Trim();
        _settingsService.Save(_settings);
    }

    private static void SelectComboItemByTag(ComboBox combo, string tag)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
            if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase)) { combo.SelectedItem = item; return; }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }


    private static bool TryNormalizeOutputRoot(string input, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            if (!Path.IsPathFullyQualified(input)) return false;
            var full = Path.GetFullPath(input);

            // Keep downloads on a normal local/removable drive path. Device namespaces and
            // UNC/SMB paths are intentionally rejected to avoid special-device access and
            // unintended network authentication from a tampered settings file.
            if (full.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(@"\\.\", StringComparison.OrdinalIgnoreCase) ||
                full.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
                return false;

            var pathRoot = Path.GetPathRoot(full);
            if (string.IsNullOrWhiteSpace(pathRoot) || pathRoot.Length < 3 || pathRoot[1] != ':')
                return false;

            normalized = Path.TrimEndingDirectorySeparator(full);
            return !string.IsNullOrWhiteSpace(normalized);
        }
        catch
        {
            return false;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = T("Folder") };
        if (Directory.Exists(OutputFolderTextBox.Text)) dialog.InitialDirectory = OutputFolderTextBox.Text;
        if (dialog.ShowDialog(this) == true)
        {
            OutputFolderTextBox.Text = dialog.FolderName;
            _settings.OutputFolder = dialog.FolderName;
            SaveSettings();
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) => OpenFolder(OutputFolderTextBox.Text);
    private void ResultOpenButton_Click(object sender, RoutedEventArgs e) => OpenFolder(_lastDownloadFolder);
    private static void OpenFolder(string path)
    {
        if (!TryNormalizeOutputRoot(path, out var normalized)) return;
        try
        {
            Directory.CreateDirectory(normalized);
            Process.Start(new ProcessStartInfo { FileName = normalized, UseShellExecute = true });
        }
        catch
        {
            // Opening the destination is a convenience action and should never crash the app.
        }
    }

    private void OutputFolderTextBox_LostFocus(object sender, RoutedEventArgs e) => SaveSettings();

    private void RightsLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        if (TrustedUriPolicy.IsTrustedFootyLogosUri(e.Uri))
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
        }
        e.Handled = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleMaximize();
        else DragMove();
    }
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _footy.Dispose();
        base.OnClosed(e);
    }
}
