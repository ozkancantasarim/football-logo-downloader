# Football Logo Downloader
# Dagitim icin tek-lig secimli surum.
# Kaynak: FootyLogos.com
# Bu arac FootyLogos ile baglantili veya FootyLogos tarafindan onaylanmis degildir.
# Kulup/lig markalari ilgili hak sahiplerine aittir.

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$ErrorActionPreference = "Continue"

$BaseUrl = "https://www.footylogos.com"
$RequestDelayMs = 1000
$UserAgent = "FootballLogoDownloader/1.0.2 (Windows PowerShell; personal-design-tool)"

try {
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
    $OutputEncoding = [Console]::OutputEncoding
} catch {}

# ---------------------------
# ULKE ADLARI
# ---------------------------

$CountryTRBySlug = @{
    "albania" = "Arnavutluk"
    "algeria" = "Cezayir"
    "andorra" = "Andorra"
    "argentina" = "Arjantin"
    "armenia" = "Ermenistan"
    "australia" = "Avustralya"
    "austria" = "Avusturya"
    "azerbaijan" = "Azerbaycan"
    "belarus" = "Belarus"
    "belgium" = "Belçika"
    "bolivia" = "Bolivya"
    "bosnia-and-herzegovina" = "Bosna Hersek"
    "brazil" = "Brezilya"
    "bulgaria" = "Bulgaristan"
    "canada" = "Kanada"
    "chile" = "Şili"
    "china" = "Çin"
    "colombia" = "Kolombiya"
    "costa-rica" = "Kosta Rika"
    "croatia" = "Hırvatistan"
    "cyprus" = "Kıbrıs"
    "czech-republic" = "Çekya"
    "czechia" = "Çekya"
    "denmark" = "Danimarka"
    "ecuador" = "Ekvador"
    "egypt" = "Mısır"
    "england" = "İngiltere"
    "estonia" = "Estonya"
    "faroe-islands" = "Faroe Adaları"
    "finland" = "Finlandiya"
    "france" = "Fransa"
    "georgia" = "Gürcistan"
    "germany" = "Almanya"
    "greece" = "Yunanistan"
    "hungary" = "Macaristan"
    "iceland" = "İzlanda"
    "india" = "Hindistan"
    "indonesia" = "Endonezya"
    "ireland" = "İrlanda"
    "israel" = "İsrail"
    "italy" = "İtalya"
    "japan" = "Japonya"
    "kazakhstan" = "Kazakistan"
    "kosovo" = "Kosova"
    "latvia" = "Letonya"
    "lithuania" = "Litvanya"
    "luxembourg" = "Lüksemburg"
    "malaysia" = "Malezya"
    "mexico" = "Meksika"
    "moldova" = "Moldova"
    "montenegro" = "Karadağ"
    "morocco" = "Fas"
    "netherlands" = "Hollanda"
    "new-zealand" = "Yeni Zelanda"
    "north-macedonia" = "Kuzey Makedonya"
    "northern-ireland" = "Kuzey İrlanda"
    "norway" = "Norveç"
    "paraguay" = "Paraguay"
    "peru" = "Peru"
    "poland" = "Polonya"
    "portugal" = "Portekiz"
    "qatar" = "Katar"
    "romania" = "Romanya"
    "russia" = "Rusya"
    "saudi-arabia" = "Suudi Arabistan"
    "scotland" = "İskoçya"
    "serbia" = "Sırbistan"
    "singapore" = "Singapur"
    "slovakia" = "Slovakya"
    "slovenia" = "Slovenya"
    "south-africa" = "Güney Afrika"
    "south-korea" = "Güney Kore"
    "spain" = "İspanya"
    "sweden" = "İsveç"
    "switzerland" = "İsviçre"
    "thailand" = "Tayland"
    "turkey" = "Türkiye"
    "turkiye" = "Türkiye"
    "ukraine" = "Ukrayna"
    "united-arab-emirates" = "Birleşik Arap Emirlikleri"
    "united-states" = "ABD"
    "usa" = "ABD"
    "uruguay" = "Uruguay"
    "venezuela" = "Venezuela"
    "vietnam" = "Vietnam"
    "wales" = "Galler"
    "hong-kong" = "Hong Kong"
    "iran" = "İran"
    "iraq" = "Irak"
    "nigeria" = "Nijerya"
    "puerto-rico" = "Porto Riko"
    "tanzania" = "Tanzanya"
    "tunisia" = "Tunus"
    "uzbekistan" = "Özbekistan"
}

$MajorInternational = @(
    [pscustomobject]@{ Name = "UEFA Champions League"; Slug = "uefa-champions-league" },
    [pscustomobject]@{ Name = "UEFA Europa League"; Slug = "uefa-europa-league" },
    [pscustomobject]@{ Name = "UEFA Conference League"; Slug = "uefa-conference-league" },
    [pscustomobject]@{ Name = "Copa Libertadores"; Slug = "copa-libertadores" },
    [pscustomobject]@{ Name = "FIFA World Cup 2026"; Slug = "fifa-world-cup-2026" }
)

# ---------------------------
# METIN / DOSYA YARDIMCILARI
# ---------------------------

function Normalize-TextValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    $Value = [System.Net.WebUtility]::HtmlDecode($Value)

    $Value = [regex]::Replace(
        $Value,
        '\\u([0-9a-fA-F]{4})',
        {
            param($m)
            return [char][Convert]::ToInt32($m.Groups[1].Value, 16)
        }
    )

    $Value = $Value -replace '\\/', '/'
    $Value = $Value -replace '\\"', '"'

    return $Value.Normalize([System.Text.NormalizationForm]::FormC)
}

function Slug-ToName {
    param([string]$Slug)

    $Parts = @()

    foreach ($Part in ($Slug -split '-')) {
        if ($Part.Length -gt 0) {
            $Parts += ($Part.Substring(0,1).ToUpper() + $Part.Substring(1))
        }
    }

    return (Normalize-TextValue ($Parts -join ' '))
}

function Get-SafeName {
    param(
        [string]$Name,
        [string]$Fallback = "Unknown"
    )

    if ([string]::IsNullOrWhiteSpace($Name)) {
        $Name = $Fallback
    }

    $Name = Normalize-TextValue $Name

    # Windows'ta yasak olan karakterleri temizle.
    # Unicode karakterler korunur: ö ç ü ş ğ ı İ â ê é ñ ø å ...
    $Name = [regex]::Replace($Name, '[<>:"/\\|?*\x00-\x1F]', '')
    $Name = [regex]::Replace($Name, '\s+', ' ')
    $Name = $Name.Trim().TrimEnd('.')

    if ($Name -match '^(?i)(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$') {
        $Name = "_$Name"
    }

    if ($Name.Length -gt 120) {
        $Name = $Name.Substring(0,120).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $Fallback
    }

    return $Name
}

function Convert-HtmlToText {
    param([string]$Html)

    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    $Text = $Html
    $Text = [regex]::Replace($Text, '(?is)<script\b.*?</script>', ' ')
    $Text = [regex]::Replace($Text, '(?is)<style\b.*?</style>', ' ')
    $Text = [regex]::Replace($Text, '(?i)<br\s*/?>', "`n")
    $Text = [regex]::Replace($Text, '(?i)</(?:div|p|li|dt|dd|h[1-6]|section|article)>', "`n")
    $Text = [regex]::Replace($Text, '(?is)<[^>]+>', ' ')
    $Text = [System.Net.WebUtility]::HtmlDecode($Text)
    $Text = $Text -replace [char]0xA0, ' '
    $Text = [regex]::Replace($Text, '[ \t]+', ' ')
    $Text = [regex]::Replace($Text, '(\r?\n\s*){2,}', "`n")

    return (Normalize-TextValue $Text.Trim())
}

function Get-CountryDisplayName {
    param([string]$Slug)

    if ($CountryTRBySlug.ContainsKey($Slug)) {
        return $CountryTRBySlug[$Slug]
    }

    return (Slug-ToName $Slug)
}

function Get-DisplayCompetitionName {
    param(
        [string]$Name,
        [string]$Slug
    )

    $Name = Normalize-TextValue $Name

    switch -Regex ($Slug) {
        '^laliga$'                  { return "LA LIGA" }
        '^la-liga$'                 { return "LA LIGA" }
        '^laliga-2$'                { return "LA LIGA 2" }
        '^la-liga-2$'               { return "LA LIGA 2" }
        '^segunda-division$'        { return "LA LIGA 2" }
        '^2-bundesliga$'            { return "Bundesliga 2" }
        '^bundesliga-2$'            { return "Bundesliga 2" }
    }

    if ($Name -match '^(?i)2\.\s*Bundesliga$') {
        return "Bundesliga 2"
    }

    if ($Name -match '^(?i)LaLiga$|^(?i)La Liga$') {
        return "LA LIGA"
    }

    return $Name
}

# ---------------------------
# WEB
# ---------------------------

function Get-WebText {
    param(
        [string]$Url,
        [int]$Retries = 4
    )

    for ($Try = 1; $Try -le $Retries; $Try++) {
        try {
            $Response = Invoke-WebRequest `
                -Uri $Url `
                -UseBasicParsing `
                -UserAgent $UserAgent `
                -Headers @{ "Accept-Language" = "en-US,en;q=0.9" } `
                -TimeoutSec 45

            if ($Response.StatusCode -ge 200 -and $Response.StatusCode -lt 300) {
                $Content = $null

                try {
                    $Stream = $Response.RawContentStream

                    if ($Stream) {
                        if ($Stream.CanSeek) {
                            $Stream.Position = 0
                        }

                        $Memory = New-Object System.IO.MemoryStream

                        try {
                            $Stream.CopyTo($Memory)
                            $Bytes = $Memory.ToArray()

                            if ($Bytes.Length -gt 0) {
                                $Utf8 = New-Object System.Text.UTF8Encoding($false, $false)
                                $Content = $Utf8.GetString($Bytes)
                            }
                        }
                        finally {
                            $Memory.Dispose()
                        }
                    }
                }
                catch {}

                if ([string]::IsNullOrWhiteSpace($Content)) {
                    $Content = [string]$Response.Content
                }

                Start-Sleep -Milliseconds $RequestDelayMs
                return $Content
            }
        }
        catch {
            if ($Try -eq $Retries) {
                throw
            }

            Start-Sleep -Seconds (2 * $Try)
        }
    }

    return $null
}

function Get-SitemapUrls {
    $Urls = @()
    $Queue = @("$BaseUrl/sitemap.xml")
    $Visited = @{}

    while ($Queue.Count -gt 0) {
        $Current = $Queue[0]

        if ($Queue.Count -eq 1) {
            $Queue = @()
        }
        else {
            $Queue = @($Queue[1..($Queue.Count - 1)])
        }

        if ($Visited.ContainsKey($Current)) {
            continue
        }

        $Visited[$Current] = $true

        try {
            $XmlText = Get-WebText $Current
        }
        catch {
            continue
        }

        if (-not $XmlText) {
            continue
        }

        $LocMatches = [regex]::Matches($XmlText, '(?is)<loc>\s*(.*?)\s*</loc>')

        foreach ($M in $LocMatches) {
            $Loc = [System.Net.WebUtility]::HtmlDecode($M.Groups[1].Value.Trim())

            if ($Loc -match '(?i)\.xml($|\?)') {
                if ($Queue -notcontains $Loc) {
                    $Queue += $Loc
                }
            }
            else {
                $Urls += $Loc
            }
        }

        if ($Visited.Count -gt 30) {
            break
        }
    }

    return @($Urls | Sort-Object -Unique)
}

function Get-Countries {
    # Yalnizca FootyLogos'un İngilizce /countries indeksindeki kanonik ülke
    # kartlarini kullaniriz. Sitemap kullanmiyoruz; sitemap çok dilli alias
    # sayfalarini da (Alemania, Alemanha, Argelia, Barein vb.) içerebiliyor.
    #
    # Ayrıca kart üzerindeki "X Competitions" sayısını okuyup 0 olan ülkeleri
    # listeye hiç eklemiyoruz. Böylece lig/turnuva koleksiyonu olmayan ülkeler
    # ülke seçicisinde görünmez.

    $Html = Get-WebText "$BaseUrl/countries"

    if (-not $Html) {
        return @()
    }

    $Seen = @{}
    $Items = @()

    # Ülke kartları normal HTML anchor olarak geliyor:
    # /country/germany, /country/spain, /country/saudi-arabia vb.
    $CardPattern = '(?is)<a\b[^>]*href\s*=\s*["'']/country/([a-z0-9][a-z0-9-]*)["''][^>]*>(.*?)</a>'
    $Cards = [regex]::Matches($Html, $CardPattern)

    foreach ($Card in $Cards) {
        $Slug = $Card.Groups[1].Value.Trim()

        if (-not $Slug -or $Seen.ContainsKey($Slug)) {
            continue
        }

        $CardText = Convert-HtmlToText $Card.Groups[2].Value

        # Sadece üzerinde gerçek competition sayacı bulunan ülke kartlarını al.
        # Footer/popüler linkler bu kalıba uymaz ve otomatik elenir.
        $CompetitionMatch = [regex]::Match(
            $CardText,
            '(?i)(\d+)\s*Competitions?\b'
        )

        if (-not $CompetitionMatch.Success) {
            continue
        }

        $CompetitionCount = 0

        try {
            $CompetitionCount = [int]$CompetitionMatch.Groups[1].Value
        }
        catch {
            continue
        }

        # Lig/turnuva koleksiyonu olmayan ülkeleri gösterme.
        if ($CompetitionCount -le 0) {
            continue
        }

        # İngilizce gerçek ülke adını kart metninden çıkar.
        # Örnek:
        # Europe Germany 159 Logos 9 Competitions 4 Divisions
        # Asia Saudi Arabia 24 Logos 1 Competitions 1 Divisions
        $NameEN = $null

        $NameMatch = [regex]::Match(
            $CardText,
            '(?i)^(?:Europe|Africa|Asia|South\s+America|North\s*&\s*Central\s+America|Oceania|International)\s+(.+?)\s+\d+\s*Logos?\s+\d+\s*Competitions?\b'
        )

        if ($NameMatch.Success) {
            $NameEN = Normalize-TextValue $NameMatch.Groups[1].Value.Trim()
        }

        if (-not $NameEN) {
            $NameEN = Slug-ToName $Slug
        }

        $Seen[$Slug] = $true

        $Items += [pscustomobject]@{
            Slug             = $Slug
            NameTR           = (Get-CountryDisplayName $Slug)
            NameEN           = $NameEN
            CompetitionCount = $CompetitionCount
        }
    }

    # Uluslararasi turnuvalar ülke değildir ama ayrı bir seçim olarak kalır.
    $Result = @(
        [pscustomobject]@{
            Slug             = "__international__"
            NameTR           = "Uluslararası"
            NameEN           = "International"
            CompetitionCount = $MajorInternational.Count
        }
    )

    $Result += $Items
    return $Result
}

function Clean-CompetitionCardName {
    param(
        [string]$Name,
        [string]$Slug
    )

    $Name = Normalize-TextValue $Name

    # Kart metinlerinde bulunan "Division 1", "(Turkey)", "18 logos" gibi
    # listeleme metadatasini klasor/combobox adindan cikar.
    $Name = $Name -replace '(?i)^\s*Division\s+\d+\s+', ''
    $Name = $Name -replace '(?i)\s+\d+\s+logos?\s*$', ''
    $Name = $Name -replace '(?i)\s*\([^)]+\)\s*$', ''
    $Name = $Name.Trim()

    if (-not $Name) {
        $Name = Slug-ToName $Slug
    }

    return (Get-DisplayCompetitionName $Name $Slug)
}

function Get-CompetitionsForCountry {
    param([string]$CountrySlug)

    if ($CountrySlug -eq "__international__") {
        return $MajorInternational
    }

    $Html = Get-WebText "$BaseUrl/country/$CountrySlug"

    if (-not $Html) {
        return @()
    }

    $Seen = @{}
    $Items = @()

    # ÖNEMLİ:
    # Sadece ülke sayfasındaki "Domestic football / <Country> competitions"
    # bölümünü okuyoruz. Sayfanın altındaki "Popular competitions" alanı
    # Premier League, Serie A, LaLiga vb. global linkleri içerdiği için
    # tüm sayfayı taramak yanlış sonuç veriyordu.
    $CompetitionSection = $null

    $H2Matches = [regex]::Matches(
        $Html,
        '(?is)<h2\b[^>]*>(.*?)</h2>'
    )

    foreach ($H2 in $H2Matches) {
        $HeadingText = Convert-HtmlToText $H2.Groups[1].Value

        if ($HeadingText -match '(?i)\bcompetitions\s*$') {
            $StartIndex = $H2.Index + $H2.Length
            $Tail = $Html.Substring($StartIndex)

            $NextH2 = [regex]::Match($Tail, '(?is)<h2\b')

            if ($NextH2.Success) {
                $CompetitionSection = $Tail.Substring(0, $NextH2.Index)
            }
            else {
                $CompetitionSection = $Tail
            }

            break
        }
    }

    if (-not $CompetitionSection) {
        # Site yapisi degisirse kontrollu fallback:
        # "Domestic football" metninden sonraki ilk H2 bolumunu al.
        $Domestic = [regex]::Match(
            $Html,
            '(?is)Domestic\s+football.*?<h2\b[^>]*>.*?competitions.*?</h2>'
        )

        if ($Domestic.Success) {
            $Tail = $Html.Substring($Domestic.Index + $Domestic.Length)
            $NextH2 = [regex]::Match($Tail, '(?is)<h2\b')

            if ($NextH2.Success) {
                $CompetitionSection = $Tail.Substring(0, $NextH2.Index)
            }
            else {
                $CompetitionSection = $Tail
            }
        }
    }

    if (-not $CompetitionSection) {
        return @()
    }

    $Pattern = '(?is)<a\b[^>]*href\s*=\s*["'']/competition/([a-z0-9][a-z0-9-]*)["''][^>]*>(.*?)</a>'
    $Matches = [regex]::Matches($CompetitionSection, $Pattern)

    foreach ($M in $Matches) {
        $Slug = $M.Groups[1].Value.Trim()

        if (-not $Slug -or $Seen.ContainsKey($Slug)) {
            continue
        }

        $RawName = Convert-HtmlToText $M.Groups[2].Value
        $Name = Clean-CompetitionCardName $RawName $Slug

        $Seen[$Slug] = $true

        $Items += [pscustomobject]@{
            Slug = $Slug
            Name = $Name
        }
    }

    # Escaped Next.js link fallback, ama yine sadece domestic section icinde.
    if ($Items.Count -eq 0) {
        $Pattern2 = '(?i)(?:/|\\/)competition(?:/|\\/)([a-z0-9][a-z0-9-]*)'
        $Matches2 = [regex]::Matches($CompetitionSection, $Pattern2)

        foreach ($M in $Matches2) {
            $Slug = $M.Groups[1].Value.Trim()

            if (-not $Slug -or $Seen.ContainsKey($Slug)) {
                continue
            }

            $Seen[$Slug] = $true

            $Items += [pscustomobject]@{
                Slug = $Slug
                Name = (Clean-CompetitionCardName (Slug-ToName $Slug) $Slug)
            }
        }
    }

    return @($Items | Sort-Object Name)
}

function Get-TeamSection {
    param([string]$Html)

    $Patterns = @(
        '(?is)<h2\b[^>]*>.*?team\s+logos.*?</h2>',
        '(?is)<h2\b[^>]*>.*?club\s+logos.*?</h2>',
        '(?is)<h2\b[^>]*>.*?clubs\s+by\s+league.*?</h2>',
        '(?is)<h2\b[^>]*>.*?teams\s+by\s+group.*?</h2>',
        '(?is)<h2\b[^>]*>.*?national\s+team\s+logos.*?</h2>'
    )

    $Start = -1

    foreach ($Pattern in $Patterns) {
        $M = [regex]::Match($Html, $Pattern)

        if ($M.Success) {
            $Start = $M.Index
            break
        }
    }

    if ($Start -lt 0) {
        $M = [regex]::Match(
            $Html,
            '(?is)<input\b[^>]*(?:placeholder|aria-label)\s*=\s*["''][^"'']*(?:team|club)[^"'']*["''][^>]*>'
        )

        if ($M.Success) {
            $Start = $M.Index
        }
    }

    if ($Start -lt 0) {
        return $Html
    }

    $Tail = $Html.Substring($Start)

    $EndPatterns = @(
        '(?is)<h2\b[^>]*>.*?Competition\s+(?:overview|details).*?</h2>',
        '(?is)<h2\b[^>]*>.*?About\b.*?</h2>',
        '(?is)<h2\b[^>]*>.*?FAQ\b.*?</h2>'
    )

    $End = $Tail.Length

    foreach ($Pattern in $EndPatterns) {
        $M = [regex]::Match($Tail, $Pattern)

        if ($M.Success -and $M.Index -gt 50 -and $M.Index -lt $End) {
            $End = $M.Index
        }
    }

    return $Tail.Substring(0, $End)
}

function Get-TeamEntries {
    param([string]$Html)

    $Section = Get-TeamSection $Html
    $Seen = @{}
    $Items = @()

    $AnchorPattern = '(?is)<a\b[^>]*href\s*=\s*["'']/logos/([^"'']+?)(?:\?[^"'']*)?["''][^>]*>(.*?)</a>'
    $Matches = [regex]::Matches($Section, $AnchorPattern)

    foreach ($Match in $Matches) {
        $Slug = $Match.Groups[1].Value.Trim()

        if (-not $Slug -or $Seen.ContainsKey($Slug)) {
            continue
        }

        $Inner = $Match.Groups[2].Value
        $Name = $null

        $Alt = [regex]::Match($Inner, '(?is)alt\s*=\s*["'']([^"'']+?)(?:\s+logo)?["'']')

        if ($Alt.Success) {
            $Name = Normalize-TextValue $Alt.Groups[1].Value.Trim()
        }

        if (-not $Name) {
            $Candidate = Convert-HtmlToText $Inner
            $Candidate = $Candidate -replace '(?i)\bPNG\b', ''
            $Candidate = $Candidate -replace '(?i)\bSVG\b', ''
            $Candidate = $Candidate -replace '(?i)\b\d+\s+variants?\b', ''
            $Candidate = $Candidate.Trim()

            if ($Candidate -and $Candidate.Length -le 120) {
                $Name = $Candidate
            }
        }

        if (-not $Name) {
            $Name = Slug-ToName $Slug
        }

        $Seen[$Slug] = $true

        $Items += [pscustomobject]@{
            Slug = $Slug
            Name = $Name
        }
    }

    # JSON/Next fallback.
    if ($Items.Count -eq 0) {
        $SlugSet = @{}
        $Matches2 = [regex]::Matches($Section, '(?i)(?:/|\\/)logos(?:/|\\/)([a-z0-9][a-z0-9-]*)')

        foreach ($M in $Matches2) {
            $Slug = $M.Groups[1].Value.Trim()

            if ($Slug) {
                $SlugSet[$Slug] = $true
            }
        }

        foreach ($Slug in @($SlugSet.Keys | Sort-Object)) {
            $Items += [pscustomobject]@{
                Slug = $Slug
                Name = (Slug-ToName $Slug)
            }
        }
    }

    return $Items
}

function Test-ValidSvg {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    try {
        $Stream = [System.IO.File]::OpenRead($Path)

        try {
            $ReadLength = [Math]::Min([int64]8192, $Stream.Length)

            if ($ReadLength -le 0) {
                return $false
            }

            $Buffer = New-Object byte[] ([int]$ReadLength)
            [void]$Stream.Read($Buffer, 0, [int]$ReadLength)

            $Head = [System.Text.Encoding]::UTF8.GetString($Buffer)

            return ($Head -match '(?i)<svg\b')
        }
        finally {
            $Stream.Dispose()
        }
    }
    catch {
        return $false
    }
}

function Download-TeamSvg {
    param(
        [string]$Slug,
        [string]$OutputPath
    )

    if (Test-ValidSvg $OutputPath) {
        return "exists"
    }

    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue
    }

    $Url = "$BaseUrl/downloads/logo/$Slug-logo-footylogos.svg"
    $Temp = "$OutputPath.part"

    for ($Try = 1; $Try -le 3; $Try++) {
        try {
            Invoke-WebRequest `
                -Uri $Url `
                -UseBasicParsing `
                -UserAgent $UserAgent `
                -Headers @{ "Accept" = "image/svg+xml,image/*;q=0.9,*/*;q=0.8" } `
                -OutFile $Temp `
                -TimeoutSec 45

            if (Test-ValidSvg $Temp) {
                Move-Item -LiteralPath $Temp -Destination $OutputPath -Force
                Start-Sleep -Milliseconds $RequestDelayMs
                return "ok"
            }

            Remove-Item -LiteralPath $Temp -Force -ErrorAction SilentlyContinue
        }
        catch {
            Remove-Item -LiteralPath $Temp -Force -ErrorAction SilentlyContinue

            $Status = ""

            try {
                $Status = [int]$_.Exception.Response.StatusCode.value__
            }
            catch {}

            if ($Status -eq 404) {
                return "missing"
            }

            if ($Try -lt 3) {
                Start-Sleep -Seconds (3 * $Try)
            }
        }
    }

    return "error"
}

# ---------------------------
# GUI + LOCALIZATION
# ---------------------------

$Strings = @{
    tr = @{
        AppTitle           = "Football Logo Downloader"
        Subtitle           = "Bir ülke ve lig seç; yalnızca seçtiğin ligin mevcut SVG logolarını indir."
        Language           = "Dil"
        Country            = "Ülke"
        League             = "Lig / turnuva"
        OutputFolder       = "Kayıt klasörü"
        Browse             = "Seç..."
        Download           = "SVG Logoları İndir"
        OpenFolder         = "Klasörü Aç"
        RightsTitle        = "Kaynak ve haklar"
        RightsText         = "Kaynak: FootyLogos.com. Bu araç FootyLogos ile bağlantılı değildir. İndirilen kulüp ve turnuva markaları ilgili hak sahiplerine aittir; indirme işlemi kullanım lisansı vermez."
        RightsLink         = "FootyLogos logo kullanım hakları"
        LoadingInitial     = "İlk veriler yükleniyor. Lütfen bekleyin..."
        LoadingInitialSub  = "İnternet bağlantınıza göre bu işlem birkaç saniye sürebilir."
        LoadingLeagues     = "Ligler yükleniyor..."
        LoadingTeams       = "Takım listesi alınıyor..."
        CountriesReady     = "{0} ülke/seçenek bulundu."
        LeaguesReady       = "{0} lig/turnuva bulundu."
        NoLeagues          = "Bu ülke için lig bulunamadı."
        CountriesError     = "Ülke listesi alınamadı."
        LeaguesError       = "Lig listesi alınamadı."
        Downloading        = "İndiriliyor: {0}  ({1}/{2})"
        Finished           = "Tamamlandı: {0} indirildi, {1} zaten vardı, {2} SVG yok, {3} hata."
        CompleteTitle      = "Football Logo Downloader"
        CompleteMessage    = "İşlem tamamlandı.`n`nİndirilen: {0}`nZaten bulunan: {1}`nSVG bulunmayan: {2}`nHata: {3}`n`n{4}"
        SelectFolderWarn   = "Önce bir kayıt klasörü seçin."
        Failure            = "İşlem başarısız."
        FolderLine         = "Klasör: {0}"
        TeamCountLine      = "Takım sayısı: {0}"
        Ready              = "Hazır."
        International      = "Uluslararası"
        DefaultRoot        = "Football-Logo-Downloads"
        LogOK              = "OK"
        LogExists          = "VAR"
        LogMissing         = "YOK"
        LogError           = "HATA"
    }

    en = @{
        AppTitle           = "Football Logo Downloader"
        Subtitle           = "Choose a country and league; download only the available SVG logos for that league."
        Language           = "Language"
        Country            = "Country"
        League             = "League / competition"
        OutputFolder       = "Download folder"
        Browse             = "Browse..."
        Download           = "Download SVG Logos"
        OpenFolder         = "Open Folder"
        RightsTitle        = "Source & rights"
        RightsText         = "Source: FootyLogos.com. This tool is not affiliated with FootyLogos. Club and competition marks belong to their respective rights holders; downloading does not grant a license to use them."
        RightsLink         = "FootyLogos logo usage & rights"
        LoadingInitial     = "Loading initial data. Please wait..."
        LoadingInitialSub  = "This may take a few seconds depending on your internet connection."
        LoadingLeagues     = "Loading leagues..."
        LoadingTeams       = "Loading team list..."
        CountriesReady     = "{0} countries/options found."
        LeaguesReady       = "{0} leagues/competitions found."
        NoLeagues          = "No leagues were found for this country."
        CountriesError     = "Could not load the country list."
        LeaguesError       = "Could not load the league list."
        Downloading        = "Downloading: {0}  ({1}/{2})"
        Finished           = "Finished: {0} downloaded, {1} already existed, {2} no SVG, {3} errors."
        CompleteTitle      = "Football Logo Downloader"
        CompleteMessage    = "Download complete.`n`nDownloaded: {0}`nAlready existed: {1}`nNo SVG available: {2}`nErrors: {3}`n`n{4}"
        SelectFolderWarn   = "Please select a download folder first."
        Failure            = "Operation failed."
        FolderLine         = "Folder: {0}"
        TeamCountLine      = "Teams: {0}"
        Ready              = "Ready."
        International      = "International"
        DefaultRoot        = "Football-Logo-Downloads"
        LogOK              = "OK"
        LogExists          = "EXISTS"
        LogMissing         = "NO SVG"
        LogError           = "ERROR"
    }
}

$script:CurrentLanguage = "en"

try {
    if ([System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -eq "tr") {
        $script:CurrentLanguage = "tr"
    }
}
catch {}

function T {
    param([string]$Key)

    if ($Strings[$script:CurrentLanguage].ContainsKey($Key)) {
        return $Strings[$script:CurrentLanguage][$Key]
    }

    return $Key
}

function Format-T {
    param(
        [string]$Key,

        [Parameter(ValueFromRemainingArguments = $true)]
        [object[]]$Values
    )

    # Windows PowerShell 5.1'de String.Format + object[] overload secimi
    # bazı sistemlerde arguman dizisini yanlış yorumlayabiliyor.
    # Basit yer tutuculari ({0}, {1}, ...) doğrudan değiştirerek
    # bu uyumsuzluğu tamamen kaldırıyoruz.
    $Text = [string](T $Key)

    if ($null -eq $Values) {
        return $Text
    }

    for ($Index = 0; $Index -lt $Values.Count; $Index++) {
        $Replacement = ""

        if ($null -ne $Values[$Index]) {
            $Replacement = [string]$Values[$Index]
        }

        $Text = $Text.Replace(("{" + $Index + "}"), $Replacement)
    }

    return $Text
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "Football Logo Downloader"
$form.Size = New-Object System.Drawing.Size(760, 655)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$title = New-Object System.Windows.Forms.Label
$title.Font = New-Object System.Drawing.Font("Segoe UI Semibold", 18)
$title.Location = New-Object System.Drawing.Point(24, 18)
$title.AutoSize = $true
$form.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Location = New-Object System.Drawing.Point(27, 58)
$subtitle.Size = New-Object System.Drawing.Size(680, 24)
$form.Controls.Add($subtitle)

$languageLabel = New-Object System.Windows.Forms.Label
$languageLabel.Location = New-Object System.Drawing.Point(560, 22)
$languageLabel.AutoSize = $true
$form.Controls.Add($languageLabel)

$languageCombo = New-Object System.Windows.Forms.ComboBox
$languageCombo.Location = New-Object System.Drawing.Point(625, 18)
$languageCombo.Size = New-Object System.Drawing.Size(90, 28)
$languageCombo.DropDownStyle = "DropDownList"
[void]$languageCombo.Items.Add("Türkçe")
[void]$languageCombo.Items.Add("English")
$form.Controls.Add($languageCombo)

$countryLabel = New-Object System.Windows.Forms.Label
$countryLabel.Location = New-Object System.Drawing.Point(28, 100)
$countryLabel.AutoSize = $true
$form.Controls.Add($countryLabel)

$countryCombo = New-Object System.Windows.Forms.ComboBox
$countryCombo.Location = New-Object System.Drawing.Point(28, 123)
$countryCombo.Size = New-Object System.Drawing.Size(330, 30)
$countryCombo.DropDownStyle = "DropDownList"
$countryCombo.DisplayMember = "DisplayName"
$form.Controls.Add($countryCombo)

$leagueLabel = New-Object System.Windows.Forms.Label
$leagueLabel.Location = New-Object System.Drawing.Point(385, 100)
$leagueLabel.AutoSize = $true
$form.Controls.Add($leagueLabel)

$leagueCombo = New-Object System.Windows.Forms.ComboBox
$leagueCombo.Location = New-Object System.Drawing.Point(385, 123)
$leagueCombo.Size = New-Object System.Drawing.Size(330, 30)
$leagueCombo.DropDownStyle = "DropDownList"
$leagueCombo.DisplayMember = "Name"
$leagueCombo.Enabled = $false
$form.Controls.Add($leagueCombo)

$outputLabel = New-Object System.Windows.Forms.Label
$outputLabel.Location = New-Object System.Drawing.Point(28, 173)
$outputLabel.AutoSize = $true
$form.Controls.Add($outputLabel)

$outputText = New-Object System.Windows.Forms.TextBox
$outputText.Location = New-Object System.Drawing.Point(28, 196)
$outputText.Size = New-Object System.Drawing.Size(585, 28)
$outputText.Text = Join-Path ([Environment]::GetFolderPath("Desktop")) "Football-Logo-Downloads"
$form.Controls.Add($outputText)

$browseButton = New-Object System.Windows.Forms.Button
$browseButton.Location = New-Object System.Drawing.Point(625, 194)
$browseButton.Size = New-Object System.Drawing.Size(90, 30)
$form.Controls.Add($browseButton)

$downloadButton = New-Object System.Windows.Forms.Button
$downloadButton.Location = New-Object System.Drawing.Point(28, 246)
$downloadButton.Size = New-Object System.Drawing.Size(190, 38)
$downloadButton.Enabled = $false
$form.Controls.Add($downloadButton)

$openButton = New-Object System.Windows.Forms.Button
$openButton.Location = New-Object System.Drawing.Point(230, 246)
$openButton.Size = New-Object System.Drawing.Size(125, 38)
$form.Controls.Add($openButton)

$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Location = New-Object System.Drawing.Point(385, 250)
$progress.Size = New-Object System.Drawing.Size(330, 26)
$progress.Minimum = 0
$progress.Maximum = 100
$form.Controls.Add($progress)

$statusLabel = New-Object System.Windows.Forms.Label
$statusLabel.Location = New-Object System.Drawing.Point(28, 300)
$statusLabel.Size = New-Object System.Drawing.Size(687, 25)
$form.Controls.Add($statusLabel)

$logBox = New-Object System.Windows.Forms.TextBox
$logBox.Location = New-Object System.Drawing.Point(28, 330)
$logBox.Size = New-Object System.Drawing.Size(687, 145)
$logBox.Multiline = $true
$logBox.ScrollBars = "Vertical"
$logBox.ReadOnly = $true
$form.Controls.Add($logBox)

$rightsBox = New-Object System.Windows.Forms.GroupBox
$rightsBox.Location = New-Object System.Drawing.Point(28, 490)
$rightsBox.Size = New-Object System.Drawing.Size(687, 88)
$form.Controls.Add($rightsBox)

$rightsText = New-Object System.Windows.Forms.Label
$rightsText.Location = New-Object System.Drawing.Point(12, 20)
$rightsText.Size = New-Object System.Drawing.Size(660, 42)
$rightsBox.Controls.Add($rightsText)

$rightsLink = New-Object System.Windows.Forms.LinkLabel
$rightsLink.Location = New-Object System.Drawing.Point(12, 61)
$rightsLink.AutoSize = $true
$rightsBox.Controls.Add($rightsLink)

# İlk açılışta kullanıcının "program dondu" sanmaması için görünür yükleme katmanı.
$loadingPanel = New-Object System.Windows.Forms.Panel
$loadingPanel.Location = New-Object System.Drawing.Point(0, 0)
$loadingPanel.Size = New-Object System.Drawing.Size(744, 616)
$loadingPanel.BackColor = [System.Drawing.Color]::White
$loadingPanel.Visible = $false
$form.Controls.Add($loadingPanel)
$loadingPanel.BringToFront()

$loadingTitle = New-Object System.Windows.Forms.Label
$loadingTitle.Font = New-Object System.Drawing.Font("Segoe UI Semibold", 15)
$loadingTitle.TextAlign = "MiddleCenter"
$loadingTitle.Location = New-Object System.Drawing.Point(80, 205)
$loadingTitle.Size = New-Object System.Drawing.Size(585, 38)
$loadingPanel.Controls.Add($loadingTitle)

$loadingSub = New-Object System.Windows.Forms.Label
$loadingSub.TextAlign = "MiddleCenter"
$loadingSub.Location = New-Object System.Drawing.Point(80, 247)
$loadingSub.Size = New-Object System.Drawing.Size(585, 28)
$loadingPanel.Controls.Add($loadingSub)

$loadingProgress = New-Object System.Windows.Forms.ProgressBar
$loadingProgress.Location = New-Object System.Drawing.Point(180, 290)
$loadingProgress.Size = New-Object System.Drawing.Size(385, 22)
$loadingProgress.Style = "Marquee"
$loadingProgress.MarqueeAnimationSpeed = 25
$loadingPanel.Controls.Add($loadingProgress)

$script:AllCountries = @()
$script:SuppressCountryEvent = $false
$script:InitialLoadComplete = $false

function Add-LogLine {
    param([string]$Text)

    $logBox.AppendText($Text + [Environment]::NewLine)
    $logBox.SelectionStart = $logBox.TextLength
    $logBox.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

function Show-Loading {
    param(
        [string]$TitleText,
        [string]$SubText = ""
    )

    $loadingTitle.Text = $TitleText
    $loadingSub.Text = $SubText
    $loadingPanel.Visible = $true
    $loadingPanel.BringToFront()
    $loadingPanel.Refresh()
    [System.Windows.Forms.Application]::DoEvents()
}

function Hide-Loading {
    $loadingPanel.Visible = $false
    [System.Windows.Forms.Application]::DoEvents()
}

function Get-CountryDisplayForLanguage {
    param($Country)

    if ($script:CurrentLanguage -eq "tr") {
        return $Country.NameTR
    }

    return $Country.NameEN
}

function Refill-Countries {
    param([string]$PreserveSlug = "")

    $script:SuppressCountryEvent = $true

    try {
        $countryCombo.Items.Clear()

        $Prepared = @()

        foreach ($Country in $script:AllCountries) {
            $Prepared += [pscustomobject]@{
                Slug        = $Country.Slug
                NameTR      = $Country.NameTR
                NameEN      = $Country.NameEN
                DisplayName = (Get-CountryDisplayForLanguage $Country)
            }
        }

        $International = @($Prepared | Where-Object { $_.Slug -eq "__international__" })
        $Domestic = @($Prepared | Where-Object { $_.Slug -ne "__international__" } | Sort-Object DisplayName)

        foreach ($Item in ($International + $Domestic)) {
            [void]$countryCombo.Items.Add($Item)
        }

        $TargetIndex = -1

        if ($PreserveSlug) {
            for ($i = 0; $i -lt $countryCombo.Items.Count; $i++) {
                if ($countryCombo.Items[$i].Slug -eq $PreserveSlug) {
                    $TargetIndex = $i
                    break
                }
            }
        }

        if ($TargetIndex -lt 0) {
            # İlk açılışta Türkiye varsa onu seç.
            for ($i = 0; $i -lt $countryCombo.Items.Count; $i++) {
                if ($countryCombo.Items[$i].Slug -in @("turkey", "turkiye")) {
                    $TargetIndex = $i
                    break
                }
            }
        }

        if ($TargetIndex -lt 0 -and $countryCombo.Items.Count -gt 0) {
            $TargetIndex = 0
        }

        if ($TargetIndex -ge 0) {
            $countryCombo.SelectedIndex = $TargetIndex
        }
    }
    finally {
        $script:SuppressCountryEvent = $false
    }
}

function Apply-Language {
    param([string]$Language)

    $SelectedCountrySlug = ""
    $SelectedLeagueSlug = ""

    if ($countryCombo.SelectedIndex -ge 0) {
        $SelectedCountrySlug = $countryCombo.SelectedItem.Slug
    }

    if ($leagueCombo.SelectedIndex -ge 0) {
        $SelectedLeagueSlug = $leagueCombo.SelectedItem.Slug
    }

    $script:CurrentLanguage = $Language

    $form.Text = T "AppTitle"
    $title.Text = T "AppTitle"
    $subtitle.Text = T "Subtitle"
    $languageLabel.Text = T "Language"
    $countryLabel.Text = T "Country"
    $leagueLabel.Text = T "League"
    $outputLabel.Text = T "OutputFolder"
    $browseButton.Text = T "Browse"
    $downloadButton.Text = T "Download"
    $openButton.Text = T "OpenFolder"
    $rightsBox.Text = T "RightsTitle"
    $rightsText.Text = T "RightsText"
    $rightsLink.Text = T "RightsLink"

    if (-not $script:InitialLoadComplete) {
        $statusLabel.Text = T "LoadingInitial"
        $loadingTitle.Text = T "LoadingInitial"
        $loadingSub.Text = T "LoadingInitialSub"
    }
    elseif ($statusLabel.Text -eq "") {
        $statusLabel.Text = T "Ready"
    }

    if ($script:AllCountries.Count -gt 0) {
        Refill-Countries $SelectedCountrySlug
    }
}

function Load-LeaguesForSelectedCountry {
    if ($countryCombo.SelectedIndex -lt 0) {
        return
    }

    $SelectedCountry = $countryCombo.SelectedItem

    $leagueCombo.Items.Clear()
    $leagueCombo.Enabled = $false
    $downloadButton.Enabled = $false
    $progress.Value = 0
    $logBox.Clear()

    Show-Loading (T "LoadingLeagues") $SelectedCountry.DisplayName

    try {
        $Leagues = @(Get-CompetitionsForCountry $SelectedCountry.Slug)

        foreach ($League in $Leagues) {
            [void]$leagueCombo.Items.Add($League)
        }

        if ($leagueCombo.Items.Count -gt 0) {
            $leagueCombo.SelectedIndex = 0
            $leagueCombo.Enabled = $true
            $downloadButton.Enabled = $true
            $statusLabel.Text = Format-T "LeaguesReady" $leagueCombo.Items.Count
        }
        else {
            $statusLabel.Text = T "NoLeagues"
        }
    }
    catch {
        $statusLabel.Text = T "LeaguesError"
        Add-LogLine ((T "LogError") + ": " + $_.Exception.Message)
    }
    finally {
        Hide-Loading
    }
}

$browseButton.Add_Click({
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = T "OutputFolder"

    if (Test-Path -LiteralPath $outputText.Text) {
        $dialog.SelectedPath = $outputText.Text
    }

    if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $outputText.Text = $dialog.SelectedPath
    }
})

$openButton.Add_Click({
    $Path = $outputText.Text

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Force -Path $Path | Out-Null
    }

    Start-Process explorer.exe $Path
})

$rightsLink.Add_LinkClicked({
    Start-Process "https://www.footylogos.com/logo-usage-right"
})

$languageCombo.Add_SelectedIndexChanged({
    if ($languageCombo.SelectedIndex -eq 0) {
        Apply-Language "tr"
    }
    elseif ($languageCombo.SelectedIndex -eq 1) {
        Apply-Language "en"
    }
})

$countryCombo.Add_SelectedIndexChanged({
    if ($script:SuppressCountryEvent -or -not $script:InitialLoadComplete) {
        return
    }

    Load-LeaguesForSelectedCountry
})

$leagueCombo.Add_SelectedIndexChanged({
    $downloadButton.Enabled = ($leagueCombo.SelectedIndex -ge 0)
})

$downloadButton.Add_Click({
    if ($countryCombo.SelectedIndex -lt 0 -or $leagueCombo.SelectedIndex -lt 0) {
        return
    }

    $Country = $countryCombo.SelectedItem
    $League = $leagueCombo.SelectedItem

    $CountryName = $Country.DisplayName
    $LeagueName = Get-DisplayCompetitionName $League.Name $League.Slug

    if ($Country.Slug -eq "__international__") {
        $FolderName = Get-SafeName ((T "International") + " " + $LeagueName)
    }
    else {
        $FolderName = Get-SafeName ("{0} {1}" -f $CountryName, $LeagueName)
    }

    $RootPath = $outputText.Text

    if ([string]::IsNullOrWhiteSpace($RootPath)) {
        [System.Windows.Forms.MessageBox]::Show(
            (T "SelectFolderWarn"),
            (T "AppTitle"),
            "OK",
            "Warning"
        ) | Out-Null

        return
    }

    $LeagueFolder = Join-Path $RootPath $FolderName
    New-Item -ItemType Directory -Force -Path $LeagueFolder | Out-Null

    $MissingFile = Join-Path $LeagueFolder "_Eksik-SVG.txt"

    $logBox.Clear()
    $progress.Value = 0

    Show-Loading (T "LoadingTeams") $LeagueName

    try {
        $CompetitionHtml = Get-WebText "$BaseUrl/competition/$($League.Slug)"
        $Teams = @(Get-TeamEntries $CompetitionHtml)

        Hide-Loading

        if ($Teams.Count -eq 0) {
            throw (T "NoLeagues")
        }

        Add-LogLine (Format-T "FolderLine" $FolderName)
        Add-LogLine (Format-T "TeamCountLine" $Teams.Count)
        Add-LogLine ""

        $progress.Minimum = 0
        $progress.Maximum = $Teams.Count
        $progress.Value = 0

        $Success = 0
        $Existing = 0
        $Missing = 0
        $Errors = 0

        $countryCombo.Enabled = $false
        $leagueCombo.Enabled = $false
        $languageCombo.Enabled = $false
        $browseButton.Enabled = $false
        $downloadButton.Enabled = $false

        for ($i = 0; $i -lt $Teams.Count; $i++) {
            $Team = $Teams[$i]
            $SafeName = Get-SafeName $Team.Name $Team.Slug
            $OutputPath = Join-Path $LeagueFolder ($SafeName + ".svg")

            $statusLabel.Text = Format-T "Downloading" $SafeName ($i + 1) $Teams.Count
            [System.Windows.Forms.Application]::DoEvents()

            $Result = Download-TeamSvg $Team.Slug $OutputPath

            switch ($Result) {
                "ok" {
                    $Success++
                    Add-LogLine ((T "LogOK") + "   - " + $SafeName)
                }

                "exists" {
                    $Existing++
                    Add-LogLine ((T "LogExists") + " - " + $SafeName)
                }

                "missing" {
                    $Missing++
                    Add-LogLine ((T "LogMissing") + " - " + $SafeName)

                    Add-Content `
                        -LiteralPath $MissingFile `
                        -Value ("{0} | {1}" -f $SafeName, "$BaseUrl/logos/$($Team.Slug)") `
                        -Encoding UTF8
                }

                default {
                    $Errors++
                    Add-LogLine ((T "LogError") + " - " + $SafeName)
                }
            }

            $progress.Value = [Math]::Min($progress.Maximum, $i + 1)
            [System.Windows.Forms.Application]::DoEvents()
        }

        $statusLabel.Text = Format-T "Finished" $Success $Existing $Missing $Errors

        [System.Windows.Forms.MessageBox]::Show(
            (Format-T "CompleteMessage" $Success $Existing $Missing $Errors $LeagueFolder),
            (T "CompleteTitle"),
            "OK",
            "Information"
        ) | Out-Null
    }
    catch {
        Hide-Loading
        $statusLabel.Text = T "Failure"
        Add-LogLine ((T "LogError") + ": " + $_.Exception.Message)

        [System.Windows.Forms.MessageBox]::Show(
            $_.Exception.Message,
            (T "AppTitle"),
            "OK",
            "Error"
        ) | Out-Null
    }
    finally {
        $countryCombo.Enabled = $true
        $leagueCombo.Enabled = ($leagueCombo.Items.Count -gt 0)
        $languageCombo.Enabled = $true
        $browseButton.Enabled = $true
        $downloadButton.Enabled = ($leagueCombo.SelectedIndex -ge 0)
    }
})

$form.Add_Shown({
    $form.Activate()

    # Önce pencereyi tamamen çizdir, sonra ağ isteğini başlat.
    # Böylece kullanıcı ilk açılışta boş/donmuş pencere görmez.
    Show-Loading (T "LoadingInitial") (T "LoadingInitialSub")
    $statusLabel.Text = T "LoadingInitial"

    $script:startupTimer = New-Object System.Windows.Forms.Timer
    $script:startupTimer.Interval = 250

    $script:startupTimer.Add_Tick({
        $script:startupTimer.Stop()

        try {
            $script:AllCountries = @(Get-Countries)
            $script:InitialLoadComplete = $true

            Refill-Countries

            if ($countryCombo.Items.Count -gt 0) {
                $statusLabel.Text = Format-T "CountriesReady" $countryCombo.Items.Count
            }
            else {
                $statusLabel.Text = T "CountriesError"
            }
        }
        catch {
            $script:InitialLoadComplete = $true
            $statusLabel.Text = T "CountriesError"
            Add-LogLine ((T "LogError") + ": " + $_.Exception.Message)
        }
        finally {
            Hide-Loading

            # Seçili ülkenin liglerini ancak ülke listesi tamamen hazır olduktan sonra yükle.
            if ($countryCombo.SelectedIndex -ge 0) {
                Load-LeaguesForSelectedCountry
            }
        }
    })

    $script:startupTimer.Start()
})

# Varsayilan dil: Windows arayuz dili Turkce ise Turkce, diger durumlarda English.
if ($script:CurrentLanguage -eq "tr") {
    $languageCombo.SelectedIndex = 0
}
else {
    $languageCombo.SelectedIndex = 1
}

Apply-Language $script:CurrentLanguage

[void]$form.ShowDialog()
