using System.Net;
using System.Net.Sockets;
using Ekiphan.Application.Seo;

namespace Ekiphan.Infrastructure.Seo;

public sealed class SsrfUrlSafetyService : ISsrfUrlSafetyService
{
    public async Task<bool> IsSafeUrlAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        string host = uri.Host.ToLowerInvariant();
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)) return false;

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork, cancellationToken);
            if (addresses.Length == 0) return false;

            foreach (var ip in addresses)
            {
                if (IsPrivateIp(ip)) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        byte[] bytes = ip.GetAddressBytes();
        if (bytes.Length == 4)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 (Link local / AWS metadata)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 127.0.0.0/8
            if (bytes[0] == 127) return true;
            // 0.0.0.0/8
            if (bytes[0] == 0) return true;
        }

        return false;
    }
}
