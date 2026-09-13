namespace FootballLogoDownloader.Models;

public sealed class AppSettings
{
    public string Language { get; set; } = string.Empty;
    public string Theme { get; set; } = "System";
    public string OutputFolder { get; set; } = string.Empty;
    public string LastCountrySlug { get; set; } = "turkey";
    public string LastCompetitionSlug { get; set; } = string.Empty;
}
