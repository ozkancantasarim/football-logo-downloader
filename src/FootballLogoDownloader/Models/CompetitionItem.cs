namespace FootballLogoDownloader.Models;

public sealed class CompetitionItem
{
    public required string Slug { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}
