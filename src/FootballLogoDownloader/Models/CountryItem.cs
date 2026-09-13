namespace FootballLogoDownloader.Models;

public sealed class CountryItem
{
    public required string Slug { get; init; }
    public required string NameTr { get; init; }
    public required string NameEn { get; init; }
    public int CompetitionCount { get; init; }
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName) ? NameEn : DisplayName;
}
