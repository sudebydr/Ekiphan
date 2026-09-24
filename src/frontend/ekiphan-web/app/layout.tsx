import type { Metadata, Viewport } from "next";
import type { ReactNode } from "react";
import { PublicFooter } from "../components/public-footer";
import {
  getSiteOrigin,
  isIndexingEnabled
} from "../lib/site-url";
import { defaultSocialImage } from "../lib/social-metadata";
import { getPublicSettings } from "../lib/public-settings";
import "./styles.css";

export const metadata: Metadata = {
  metadataBase: getSiteOrigin() ?? undefined,
  title: {
    default: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    template: "%s | Ekiphan"
  },
  description:
    "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
  applicationName: "Ekiphan",
  openGraph: {
    type: "website",
    locale: "tr_TR",
    siteName: "Ekiphan",
    title: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    description:
      "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
    images: defaultSocialImage
      ? [{
        url: defaultSocialImage,
        width: 1536,
        height: 1024,
        alt: "Profesyonel Ekiphan mutfak çözümleri"
      }]
      : undefined
  },
  twitter: {
    card: "summary_large_image",
    title: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    description:
      "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
    images: defaultSocialImage ? [defaultSocialImage] : undefined
  },
  manifest: "/manifest.webmanifest",
  icons: {
    icon: "/icon.svg"
  },
  robots: {
    index: isIndexingEnabled(),
    follow: isIndexingEnabled(),
    nocache: !isIndexingEnabled()
  }
};

export const viewport: Viewport = {
  colorScheme: "light",
  themeColor: "#30465F",
  width: "device-width",
  initialScale: 1
};

export default async function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  const origin = getSiteOrigin();
  const settings = await getPublicSettings();
  const structuredData = {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "Organization",
        "@id": origin ? new URL("/#organization", origin).toString() : undefined,
        name: "Ekiphan",
        url: origin?.toString(),
        logo: origin ? new URL("/icon.svg", origin).toString() : undefined
      },
      {
        "@type": "WebSite",
        "@id": origin ? new URL("/#website", origin).toString() : undefined,
        name: "Ekiphan",
        url: origin?.toString(),
        publisher: origin
          ? { "@id": new URL("/#organization", origin).toString() }
          : undefined,
        inLanguage: "tr-TR",
        potentialAction: {
          "@type": "SearchAction",
          target: origin
            ? {
                "@type": "EntryPoint",
                urlTemplate: new URL(
                  "/katalog?q={search_term_string}",
                  origin
                ).toString()
              }
            : "/katalog?q={search_term_string}",
          "query-input": "required name=search_term_string"
        }
      }
    ]
  };

  return (
    <html lang="tr">
      <body>
        <a className="skip-link" href="#main-content">
          Ana içeriğe geç
        </a>
        <div id="main-content">{children}</div>
        <PublicFooter settings={settings} />
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify(structuredData).replace(/</g, "\\u003c")
          }}
        />
      </body>
    </html>
  );
}
