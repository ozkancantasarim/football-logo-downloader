namespace FootballLogoDownloader.Security;

public static class TrustedUriPolicy
{
    public static bool IsTrustedFootyLogosUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri) return false;
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.IsNullOrEmpty(uri.UserInfo)) return false;
        if (!uri.IsDefaultPort && uri.Port != 443) return false;

        var host = uri.IdnHost.TrimEnd('.');
        return host.Equals("footylogos.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".footylogos.com", StringComparison.OrdinalIgnoreCase);
    }
}
