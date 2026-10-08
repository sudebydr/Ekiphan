import type { NextConfig } from "next";

const contentSecurityPolicy = [
  "default-src 'self'",
  "base-uri 'self'",
  "object-src 'none'",
  "frame-ancestors 'none'",
  "form-action 'self'",
  "img-src 'self' data: blob: https:",
  "media-src 'self' https:",
  "font-src 'self' data:",
  "style-src 'self' 'unsafe-inline'",
  "script-src 'self' 'unsafe-inline'",
  "connect-src 'self'",
  "worker-src 'self' blob:"
].join("; ");

const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  {
    key: "Referrer-Policy",
    value: "strict-origin-when-cross-origin"
  },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=(), payment=()"
  },
  { key: "Cross-Origin-Opener-Policy", value: "same-origin" },
  { key: "Cross-Origin-Resource-Policy", value: "same-origin" },
  { key: "X-DNS-Prefetch-Control", value: "off" },
  ...(process.env.NODE_ENV === "production"
    ? [
        {
          key: "Content-Security-Policy",
          value: contentSecurityPolicy
        },
        {
          key: "Strict-Transport-Security",
          value: "max-age=31536000; includeSubDomains"
        }
      ]
    : [])
];

const catalogPdfHeaders = [
  ...securityHeaders.filter(
    (header) =>
      header.key !== "X-Frame-Options" &&
      header.key !== "Content-Security-Policy"
  ),
  { key: "X-Frame-Options", value: "SAMEORIGIN" },
  ...(process.env.NODE_ENV === "production"
    ? [
        {
          key: "Content-Security-Policy",
          value: contentSecurityPolicy.replace("frame-ancestors 'none'", "frame-ancestors 'self'")
        }
      ]
    : [])
];
const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    const api = process.env.EKIPHAN_API_BASE_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL;
    if (!api || !["localhost", "127.0.0.1", "[::1]"].includes(new URL(api).hostname)) return [];
    return [{ source: "/media/:path*", destination: `${api.replace(/\/+$/, "")}/media/:path*` }];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders
      },
      {
        source: "/catalogs/:path*",
        headers: catalogPdfHeaders
      },      {
        source: "/images/:path*",
        headers: [
          {
            key: "Cache-Control",
            value: "public, max-age=86400, stale-while-revalidate=604800"
          }
        ]
      }
    ];
  }
};

export default nextConfig;
