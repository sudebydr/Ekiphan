import type { Metadata, Viewport } from "next";
import type { ReactNode } from "react";
import { Inter } from "next/font/google";

import { PublicFooter } from "../components/public-footer";
import { getPublicSettings } from "../lib/public-settings";
import { getSiteOrigin, isIndexingEnabled } from "../lib/site-url";
import { defaultSocialImage } from "../lib/social-metadata";

import "./styles.css";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  metadataBase: getSiteOrigin() ?? undefined,
  title: {
    default: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    template: "%s | Ekiphan",
  },
  description: "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
  applicationName: "Ekiphan",
  openGraph: {
    type: "website",
    locale: "tr_TR",
    siteName: "Ekiphan",
    title: "Ekiphan | Profesyonel Otel ve Mutfak Ekipmanları",
    description: "Otel, restoran ve endüstriyel mutfaklar için profesyonel ekipman kataloğu.",
    url: "/",
    images: defaultSocialImage ? [{ url: defaultSocialImage, width: 1200, height: 630, alt: "Ekiphan" }] : [],
  },
  icons: {
    icon: "/icon.svg",
  },
  robots: {
    index: isIndexingEnabled(),
    follow: isIndexingEnabled(),
    nocache: !isIndexingEnabled(),
  },
};

export const viewport: Viewport = {
  colorScheme: "light",
  themeColor: "#F5F1EB",
  width: "device-width",
  initialScale: 1,
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: ReactNode;
}>) {
  const settings = await getPublicSettings();

  return (
    <html lang="tr">
      <body className={inter.variable}>
        {children}
        <PublicFooter settings={settings} />
      </body>
    </html>
  );
}
