using FootballLogoDownloader.Models;
using FootballLogoDownloader.Security;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace FootballLogoDownloader.Services;

public sealed class FootyLogosService : IDisposable
{
    private const string BaseUrl = "https://www.footylogos.com";
    private const int MaxHtmlBytes = 8 * 1024 * 1024;
    private const int MaxRedirects = 3;
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private readonly HttpClient _http;

    private static readonly IReadOnlyList<CompetitionItem> MajorInternational =
    [
        new() { Name = "UEFA Champions League", Slug = "uefa-champions-league" },
        new() { Name = "UEFA Europa League", Slug = "uefa-europa-league" },
        new() { Name = "UEFA Conference League", Slug = "uefa-conference-league" },
        new() { Name = "Copa Libertadores", Slug = "copa-libertadores" },
        new() { Name = "FIFA World Cup 2026", Slug = "fifa-world-cup-2026" }
    ];

    private static readonly Dictionary<string, string> CountryTrBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["albania"]="Arnavutluk", ["algeria"]="Cezayir", ["andorra"]="Andorra", ["argentina"]="Arjantin",
        ["armenia"]="Ermenistan", ["australia"]="Avustralya", ["austria"]="Avusturya", ["azerbaijan"]="Azerbaycan",
        ["belarus"]="Belarus", ["belgium"]="Belçika", ["bolivia"]="Bolivya", ["bosnia-and-herzegovina"]="Bosna Hersek",
        ["brazil"]="Brezilya", ["bulgaria"]="Bulgaristan", ["canada"]="Kanada", ["chile"]="Şili",
        ["china"]="Çin", ["colombia"]="Kolombiya", ["costa-rica"]="Kosta Rika", ["croatia"]="Hırvatistan",
        ["cyprus"]="Kıbrıs", ["czech-republic"]="Çekya", ["czechia"]="Çekya", ["denmark"]="Danimarka",
        ["ecuador"]="Ekvador", ["egypt"]="Mısır", ["england"]="İngiltere", ["estonia"]="Estonya",
        ["faroe-islands"]="Faroe Adaları", ["finland"]="Finlandiya", ["france"]="Fransa", ["georgia"]="Gürcistan",
        ["germany"]="Almanya", ["greece"]="Yunanistan", ["hungary"]="Macaristan", ["iceland"]="İzlanda",
        ["india"]="Hindistan", ["indonesia"]="Endonezya", ["ireland"]="İrlanda", ["israel"]="İsrail",
        ["italy"]="İtalya", ["japan"]="Japonya", ["kazakhstan"]="Kazakistan", ["kosovo"]="Kosova",
        ["latvia"]="Letonya", ["lithuania"]="Litvanya", ["luxembourg"]="Lüksemburg", ["malaysia"]="Malezya",
        ["mexico"]="Meksika", ["moldova"]="Moldova", ["montenegro"]="Karadağ", ["morocco"]="Fas",
        ["netherlands"]="Hollanda", ["new-zealand"]="Yeni Zelanda", ["north-macedonia"]="Kuzey Makedonya",
        ["northern-ireland"]="Kuzey İrlanda", ["norway"]="Norveç", ["paraguay"]="Paraguay", ["peru"]="Peru",
        ["poland"]="Polonya", ["portugal"]="Portekiz", ["qatar"]="Katar", ["romania"]="Romanya",
        ["russia"]="Rusya", ["saudi-arabia"]="Suudi Arabistan", ["scotland"]="İskoçya", ["serbia"]="Sırbistan",
        ["singapore"]="Singapur", ["slovakia"]="Slovakya", ["slovenia"]="Slovenya", ["south-africa"]="Güney Afrika",
        ["south-korea"]="Güney Kore", ["spain"]="İspanya", ["sweden"]="İsveç", ["switzerland"]="İsviçre",
        ["thailand"]="Tayland", ["turkey"]="Türkiye", ["turkiye"]="Türkiye", ["ukraine"]="Ukrayna",
        ["united-arab-emirates"]="Birleşik Arap Emirlikleri", ["united-states"]="ABD", ["usa"]="ABD",
        ["uruguay"]="Uruguay", ["venezuela"]="Venezuela", ["vietnam"]="Vietnam", ["wales"]="Galler",
        ["hong-kong"]="Hong Kong", ["iran"]="İran", ["iraq"]="Irak", ["nigeria"]="Nijerya",
        ["puerto-rico"]="Porto Riko", ["tanzania"]="Tanzanya", ["tunisia"]="Tunus", ["uzbekistan"]="Özbekistan"
    };

    public FootyLogosService()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            UseCookies = false,
            CheckCertificateRevocationList = true,
            MaxResponseHeadersLength = 32
        };

        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(45)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("FootballLogoDownloader/2.0.0 (Windows; .NET WPF; personal-design-tool)");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    public async Task<IReadOnlyList<CountryItem>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        var html = await GetTextAsync(new Uri($"{BaseUrl}/countries"), cancellationToken);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<CountryItem>();

        foreach (Match card in RxMatches(html, @"<a\b[^>]*href\s*=\s*[""']/country/([a-z0-9][a-z0-9-]*)[""'][^>]*>(.*?)</a>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var slug = card.Groups[1].Value.Trim();
            if (!IsSafeSlug(slug)) continue;

            var cardText = ConvertHtmlToText(card.Groups[2].Value);
            var competitionMatch = RxMatch(cardText, @"(\d+)\s*Competitions?\b", RegexOptions.IgnoreCase);
            if (!competitionMatch.Success || !int.TryParse(competitionMatch.Groups[1].Value, out var count) || count <= 0)
                continue;

            // FootyLogos lists some countries in a “Popular” links block before the
            // full country cards. Do not mark a slug as seen until a real card with
            // a positive competition count has been validated, otherwise countries
            // such as England, Spain, Germany and Brazil can be skipped.
            if (!seen.Add(slug)) continue;

            var nameMatch = RxMatch(
                cardText,
                @"^(?:Europe|Africa|Asia|South\s+America|North\s*&\s*Central\s+America|Oceania|International)\s+(.+?)\s+\d+\s*Logos?\s+\d+\s*Competitions?\b",
                RegexOptions.IgnoreCase);

            var nameEn = nameMatch.Success ? NormalizeText(nameMatch.Groups[1].Value.Trim()) : SlugToName(slug);
            var nameTr = CountryTrBySlug.TryGetValue(slug, out var translated) ? translated : SlugToName(slug);

            items.Add(new CountryItem
            {
                Slug = slug,
                NameTr = nameTr,
                NameEn = nameEn,
                CompetitionCount = count
            });
        }

        var result = new List<CountryItem>
        {
            new()
            {
                Slug = "__international__",
                NameTr = "Uluslararası",
                NameEn = "International",
                CompetitionCount = MajorInternational.Count
            }
        };
        result.AddRange(items);
        return result;
    }

    public async Task<IReadOnlyList<CompetitionItem>> GetCompetitionsForCountryAsync(
        string countrySlug,
        CancellationToken cancellationToken = default)
    {
        if (countrySlug == "__international__") return MajorInternational;
        if (!IsSafeSlug(countrySlug)) return [];

        var html = await GetTextAsync(new Uri($"{BaseUrl}/country/{Uri.EscapeDataString(countrySlug)}"), cancellationToken);
        var section = GetCompetitionSection(html);
        if (string.IsNullOrWhiteSpace(section)) return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<CompetitionItem>();

        foreach (Match match in RxMatches(section,
                     @"<a\b[^>]*href\s*=\s*[""']/competition/([a-z0-9][a-z0-9-]*)[""'][^>]*>(.*?)</a>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var slug = match.Groups[1].Value.Trim();
            if (!IsSafeSlug(slug) || !seen.Add(slug)) continue;
            var rawName = ConvertHtmlToText(match.Groups[2].Value);
            items.Add(new CompetitionItem { Slug = slug, Name = CleanCompetitionCardName(rawName, slug) });
        }

        if (items.Count == 0)
        {
            foreach (Match match in RxMatches(section, @"(?:/|\\/)competition(?:/|\\/)([a-z0-9][a-z0-9-]*)", RegexOptions.IgnoreCase))
            {
                var slug = match.Groups[1].Value.Trim();
                if (!IsSafeSlug(slug) || !seen.Add(slug)) continue;
                items.Add(new CompetitionItem { Slug = slug, Name = CleanCompetitionCardName(SlugToName(slug), slug) });
            }
        }

        return items.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<TeamItem>> GetTeamEntriesAsync(
        string competitionSlug,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeSlug(competitionSlug)) return [];

        var html = await GetTextAsync(new Uri($"{BaseUrl}/competition/{Uri.EscapeDataString(competitionSlug)}"), cancellationToken);
        var section = GetTeamSection(html);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<TeamItem>();

        foreach (Match match in RxMatches(section,
                     @"<a\b[^>]*href\s*=\s*[""']/logos/([^""']+?)(?:\?[^""']*)?[""'][^>]*>(.*?)</a>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var slug = WebUtility.UrlDecode(match.Groups[1].Value.Trim()) ?? string.Empty;
            if (!IsSafeSlug(slug) || !seen.Add(slug)) continue;

            var inner = match.Groups[2].Value;
            string? name = null;
            var alt = RxMatch(inner, @"alt\s*=\s*[""']([^""']+?)(?:\s+logo)?[""']", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (alt.Success) name = NormalizeText(alt.Groups[1].Value.Trim());

            if (string.IsNullOrWhiteSpace(name))
            {
                var candidate = ConvertHtmlToText(inner);
                candidate = RxReplace(candidate, @"\bPNG\b", "", RegexOptions.IgnoreCase);
                candidate = RxReplace(candidate, @"\bSVG\b", "", RegexOptions.IgnoreCase);
                candidate = RxReplace(candidate, @"\b\d+\s+variants?\b", "", RegexOptions.IgnoreCase).Trim();
                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 120) name = candidate;
            }

            name ??= SlugToName(slug);
            items.Add(new TeamItem { Slug = slug, Name = name });
        }

        if (items.Count == 0)
        {
            foreach (Match match in RxMatches(section, @"(?:/|\\/)logos(?:/|\\/)([a-z0-9][a-z0-9-]*)", RegexOptions.IgnoreCase))
            {
                var slug = match.Groups[1].Value.Trim();
                if (IsSafeSlug(slug) && seen.Add(slug))
                    items.Add(new TeamItem { Slug = slug, Name = SlugToName(slug) });
            }
        }

        return items;
    }

    public async Task<DownloadResult> DownloadSvgAsync(
        string slug,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (!IsSafeSlug(slug)) return DownloadResult.Error;
        if (!Path.GetExtension(outputPath).Equals(".svg", StringComparison.OrdinalIgnoreCase)) return DownloadResult.Error;

        if (SvgSecurityValidator.IsSafeSvgFile(outputPath)) return DownloadResult.Exists;
        if (File.Exists(outputPath)) File.Delete(outputPath);

        var targetDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (string.IsNullOrWhiteSpace(targetDirectory)) return DownloadResult.Error;
        Directory.CreateDirectory(targetDirectory);

        var url = new Uri($"{BaseUrl}/downloads/logo/{Uri.EscapeDataString(slug)}-logo-footylogos.svg");
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await SendTrustedGetAsync(
                    url,
                    "image/svg+xml,image/*;q=0.9,application/xml;q=0.8,text/xml;q=0.8,*/*;q=0.1",
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound) return DownloadResult.Missing;
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength is long contentLength &&
                    contentLength > SvgSecurityValidator.MaxSvgBytes)
                {
                    return DownloadResult.Error;
                }

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not null && mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
                    return DownloadResult.Error;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var bytes = await ReadLimitedBytesAsync(stream, SvgSecurityValidator.MaxSvgBytes, cancellationToken);
                if (!SvgSecurityValidator.IsSafeSvg(bytes, out _)) return DownloadResult.Error;

                var tempPath = outputPath + ".download-" + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
                    File.Move(tempPath, outputPath, true);
                }
                finally
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
                }

                await Task.Delay(RequestDelay, cancellationToken);
                return DownloadResult.Downloaded;
            }
            catch (OperationCanceledException) { throw; }
            catch when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(3 * attempt), cancellationToken);
            }
            catch
            {
                return DownloadResult.Error;
            }
        }

        return DownloadResult.Error;
    }

    public static string GetSafeName(string? name, string fallback = "Unknown")
    {
        name = string.IsNullOrWhiteSpace(name) ? fallback : NormalizeText(name);
        name = RxReplace(name, "[<>:\"/\\\\|?*\\x00-\\x1F]", "");
        name = RxReplace(name, @"\s+", " ").Trim().TrimEnd('.');
        if (RxIsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase)) name = "_" + name;
        if (name.Length > 120) name = name[..120].Trim();
        return string.IsNullOrWhiteSpace(name) ? fallback : name;
    }

    public static string GetDisplayCompetitionName(string name, string slug)
    {
        name = NormalizeText(name);
        return slug.ToLowerInvariant() switch
        {
            "laliga" or "la-liga" => "LA LIGA",
            "laliga-2" or "la-liga-2" or "segunda-division" => "LA LIGA 2",
            "2-bundesliga" or "bundesliga-2" => "Bundesliga 2",
            _ when RxIsMatch(name, @"^2\.\s*Bundesliga$", RegexOptions.IgnoreCase) => "Bundesliga 2",
            _ when RxIsMatch(name, @"^(LaLiga|La Liga)$", RegexOptions.IgnoreCase) => "LA LIGA",
            _ => name
        };
    }

    private async Task<string> GetTextAsync(Uri url, CancellationToken cancellationToken)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            try
            {
                using var response = await SendTrustedGetAsync(
                    url,
                    "text/html,application/xhtml+xml;q=0.9,*/*;q=0.1",
                    cancellationToken);
                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength is long contentLength && contentLength > MaxHtmlBytes)
                    throw new InvalidDataException("HTML response exceeds the size limit.");

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var bytes = await ReadLimitedBytesAsync(stream, MaxHtmlBytes, cancellationToken);
                await Task.Delay(RequestDelay, cancellationToken);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                last = ex;
                if (attempt < 4) await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
        }

        throw last ?? new HttpRequestException("Request failed.");
    }

    private async Task<HttpResponseMessage> SendTrustedGetAsync(Uri initialUri, string accept, CancellationToken cancellationToken)
    {
        var currentUri = initialUri;

        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            if (!TrustedUriPolicy.IsTrustedFootyLogosUri(currentUri))
                throw new HttpRequestException("Blocked an untrusted download address.");

            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            request.Headers.Accept.ParseAdd(accept);

            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!IsRedirect(response.StatusCode)) return response;

            var location = response.Headers.Location;
            response.Dispose();

            if (location is null)
                throw new HttpRequestException("Redirect response did not contain a location.");

            currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
        }

        throw new HttpRequestException("Too many redirects.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.Moved or
        HttpStatusCode.Redirect or
        HttpStatusCode.RedirectMethod or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private static async Task<byte[]> ReadLimitedBytesAsync(Stream stream, int maximumBytes, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream(Math.Min(maximumBytes, 64 * 1024));
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            var total = 0;
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0) break;
                total += read;
                if (total > maximumBytes) throw new InvalidDataException("Response exceeds the size limit.");
                output.Write(buffer, 0, read);
            }
            return output.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string? GetCompetitionSection(string html)
    {
        foreach (Match h2 in RxMatches(html, @"<h2\b[^>]*>(.*?)<\/h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var heading = ConvertHtmlToText(h2.Groups[1].Value);
            if (!RxIsMatch(heading, @"\bcompetitions\s*$", RegexOptions.IgnoreCase)) continue;
            var start = h2.Index + h2.Length;
            var tail = html[start..];
            var next = RxMatch(tail, @"<h2\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return next.Success ? tail[..next.Index] : tail;
        }

        var domestic = RxMatch(html, @"Domestic\s+football.*?<h2\b[^>]*>.*?competitions.*?<\/h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!domestic.Success) return null;
        var fallbackTail = html[(domestic.Index + domestic.Length)..];
        var fallbackNext = RxMatch(fallbackTail, @"<h2\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return fallbackNext.Success ? fallbackTail[..fallbackNext.Index] : fallbackTail;
    }

    private static string GetTeamSection(string html)
    {
        var patterns = new[]
        {
            @"<h2\b[^>]*>.*?team\s+logos.*?<\/h2>",
            @"<h2\b[^>]*>.*?club\s+logos.*?<\/h2>",
            @"<h2\b[^>]*>.*?clubs\s+by\s+league.*?<\/h2>",
            @"<h2\b[^>]*>.*?teams\s+by\s+group.*?<\/h2>",
            @"<h2\b[^>]*>.*?national\s+team\s+logos.*?<\/h2>"
        };

        var start = -1;
        foreach (var pattern in patterns)
        {
            var match = RxMatch(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success) { start = match.Index; break; }
        }

        if (start < 0)
        {
            var input = RxMatch(html, @"<input\b[^>]*(?:placeholder|aria-label)\s*=\s*[""'][^""']*(?:team|club)[^""']*[""'][^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (input.Success) start = input.Index;
        }

        if (start < 0) return html;
        var tail = html[start..];
        var end = tail.Length;
        foreach (var pattern in new[]
                 {
                     @"<h2\b[^>]*>.*?Competition\s+(?:overview|details).*?<\/h2>",
                     @"<h2\b[^>]*>.*?About\b.*?<\/h2>",
                     @"<h2\b[^>]*>.*?FAQ\b.*?<\/h2>"
                 })
        {
            var match = RxMatch(tail, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Index > 50 && match.Index < end) end = match.Index;
        }
        return tail[..end];
    }

    private static string CleanCompetitionCardName(string name, string slug)
    {
        name = NormalizeText(name);
        name = RxReplace(name, @"^\s*Division\s+\d+\s+", "", RegexOptions.IgnoreCase);
        name = RxReplace(name, @"\s+\d+\s+logos?\s*$", "", RegexOptions.IgnoreCase);
        name = RxReplace(name, @"\s*\([^)]+\)\s*$", "", RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = SlugToName(slug);
        return GetDisplayCompetitionName(name, slug);
    }

    private static string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = RxReplace(html, @"<script\b.*?<\/script>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = RxReplace(text, @"<style\b.*?<\/style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        text = RxReplace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = RxReplace(text, @"<\/(?:div|p|li|dt|dd|h[1-6]|section|article)>", "\n", RegexOptions.IgnoreCase);
        text = RxReplace(text, @"<[^>]+>", " ", RegexOptions.Singleline);
        text = WebUtility.HtmlDecode(text).Replace((char)0x00A0, ' ');
        text = RxReplace(text, @"[ \t]+", " ");
        text = RxReplace(text, @"(\r?\n\s*){2,}", "\n");
        return NormalizeText(text.Trim());
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        value = WebUtility.HtmlDecode(value);
        value = RxReplace(value, @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        value = value.Replace("\\/", "/").Replace("\\\"", "\"");
        return value.Normalize(NormalizationForm.FormC);
    }

    private static string SlugToName(string slug) => NormalizeText(string.Join(' ', slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => char.ToUpperInvariant(part[0]) + part[1..])));

    private static bool IsSafeSlug(string slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        slug.Length <= 128 &&
        RxIsMatch(slug, @"^[a-z0-9][a-z0-9-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static Match RxMatch(string input, string pattern, RegexOptions options = RegexOptions.None) =>
        Regex.Match(input, pattern, options | RegexOptions.CultureInvariant, RegexTimeout);

    private static MatchCollection RxMatches(string input, string pattern, RegexOptions options = RegexOptions.None) =>
        Regex.Matches(input, pattern, options | RegexOptions.CultureInvariant, RegexTimeout);

    private static bool RxIsMatch(string input, string pattern, RegexOptions options = RegexOptions.None) =>
        Regex.IsMatch(input, pattern, options | RegexOptions.CultureInvariant, RegexTimeout);

    private static string RxReplace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.None) =>
        Regex.Replace(input, pattern, replacement, options | RegexOptions.CultureInvariant, RegexTimeout);

    private static string RxReplace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options = RegexOptions.None) =>
        Regex.Replace(input, pattern, evaluator, options | RegexOptions.CultureInvariant, RegexTimeout);

    public void Dispose() => _http.Dispose();
}

public enum DownloadResult
{
    Downloaded,
    Exists,
    Missing,
    Error
}
