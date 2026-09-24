import type { MetadataRoute } from "next";
import {
  getSiteOrigin,
  isIndexingEnabled
} from "../lib/site-url";

export default function robots(): MetadataRoute.Robots {
  const origin = getSiteOrigin();
  if (!origin || !isIndexingEnabled()) {
    return {
      rules: {
        userAgent: "*",
        disallow: "/"
      }
    };
  }

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/admin/", "/api/", "/teklif-listem"]
    },
    sitemap: new URL("/sitemap.xml", origin).href
  };
}
