using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace FootballLogoDownloader.Security;

/// <summary>
/// Strict validation for untrusted SVG files downloaded from the internet.
/// The downloader never executes SVG content; this validator additionally rejects
/// active/external content before a file is written to disk.
/// </summary>
public static class SvgSecurityValidator
{
    public const int MaxSvgBytes = 8 * 1024 * 1024;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    private static readonly HashSet<string> ForbiddenElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "foreignObject", "iframe", "object", "embed", "audio", "video", "canvas"
    };

    public static bool IsSafeSvgFile(string path)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length <= 0 || file.Length > MaxSvgBytes) return false;
            return IsSafeSvg(File.ReadAllBytes(path), out _);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsSafeSvg(byte[] bytes, out string reason)
    {
        reason = string.Empty;
        if (bytes.Length == 0)
        {
            reason = "Empty file.";
            return false;
        }
        if (bytes.Length > MaxSvgBytes)
        {
            reason = "SVG exceeds the size limit.";
            return false;
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = false,
                IgnoreProcessingInstructions = false,
                MaxCharactersInDocument = MaxSvgBytes * 8L,
                MaxCharactersFromEntities = 0
            };

            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(stream, settings);
            var document = XDocument.Load(reader, LoadOptions.None);

            if (document.DocumentType is not null)
            {
                reason = "DOCTYPE is not allowed.";
                return false;
            }

            if (document.DescendantNodes().OfType<XProcessingInstruction>().Any())
            {
                reason = "Processing instructions are not allowed.";
                return false;
            }

            var root = document.Root;
            if (root is null || !root.Name.LocalName.Equals("svg", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Document root is not SVG.";
                return false;
            }

            // Well-formed SVGs normally use the SVG namespace. Empty namespace is accepted
            // for compatibility with simple legacy logos, but all other namespaces are rejected.
            if (root.Name.Namespace != XNamespace.None && root.Name.Namespace != SvgNamespace)
            {
                reason = "Unexpected SVG namespace.";
                return false;
            }

            foreach (var element in root.DescendantsAndSelf())
            {
                if (ForbiddenElements.Contains(element.Name.LocalName))
                {
                    reason = $"Forbidden SVG element: {element.Name.LocalName}.";
                    return false;
                }

                if (element.Name.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase) &&
                    !IsSafeCss(element.Value))
                {
                    reason = "Unsafe CSS content.";
                    return false;
                }

                foreach (var attribute in element.Attributes())
                {
                    if (attribute.IsNamespaceDeclaration) continue;

                    var localName = attribute.Name.LocalName;
                    var value = attribute.Value.Trim();

                    if (localName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                    {
                        reason = $"Event attribute is not allowed: {localName}.";
                        return false;
                    }

                    if (localName.Equals("href", StringComparison.OrdinalIgnoreCase) ||
                        localName.Equals("src", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsSafeReference(value))
                        {
                            reason = "External or active reference is not allowed.";
                            return false;
                        }
                    }

                    if (localName.Equals("style", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("url(", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!IsSafeCss(value))
                        {
                            reason = "Unsafe URL/CSS reference.";
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch (XmlException)
        {
            reason = "Malformed or unsafe XML.";
            return false;
        }
        catch
        {
            reason = "SVG validation failed.";
            return false;
        }
    }

    private static bool IsSafeReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.StartsWith('#')) return true;

        // Embedded raster images are safe enough for a logo container. Nested SVG/data HTML
        // are deliberately rejected because they can carry active content.
        if (Regex.IsMatch(value,
                @"^data:image/(?:png|jpe?g|gif|webp);base64,",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                RegexTimeout))
        {
            return true;
        }

        return false;
    }

    private static bool IsSafeCss(string css)
    {
        if (string.IsNullOrWhiteSpace(css)) return true;

        if (Regex.IsMatch(css,
                @"(?:@import|expression\s*\(|javascript\s*:|vbscript\s*:|-moz-binding\s*:|behavior\s*:)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                RegexTimeout))
        {
            return false;
        }

        foreach (Match match in Regex.Matches(css,
                     @"url\(\s*(['""]?)(.*?)\1\s*\)",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant,
                     RegexTimeout))
        {
            if (!IsSafeReference(match.Groups[2].Value.Trim())) return false;
        }

        return true;
    }
}
