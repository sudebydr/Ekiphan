import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "Ekiphan Profesyonel Ekipman",
    short_name: "Ekiphan",
    description:
      "Profesyonel otel, restoran ve endüstriyel mutfak ekipmanları kataloğu.",
    start_url: "/",
    display: "standalone",
    background_color: "#f4f1ed",
    theme_color: "#171a1d",
    lang: "tr",
    icons: [
      {
        src: "/icon.svg",
        sizes: "any",
        type: "image/svg+xml",
        purpose: "any"
      }
    ]
  };
}
