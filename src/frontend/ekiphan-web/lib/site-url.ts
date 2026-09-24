const developmentOrigin = new URL("http://localhost:3000");

function configuredOrigin(): URL | null {
  const value = process.env.EKIPHAN_SITE_URL?.trim();
  if (!value) return null;

  try {
    const url = new URL(value);
    const isHttps = url.protocol === "https:";
    const isDevelopmentHttp =
      process.env.NODE_ENV !== "production" &&
      url.protocol === "http:" &&
      (url.hostname === "localhost" || url.hostname === "127.0.0.1");

    if (
      (!isHttps && !isDevelopmentHttp) ||
      url.username ||
      url.password ||
      url.pathname !== "/" ||
      url.search ||
      url.hash
    ) {
      return null;
    }

    return url;
  } catch {
    return null;
  }
}

export function getSiteOrigin(): URL | null {
  return configuredOrigin() ??
    (process.env.NODE_ENV === "production" ? null : developmentOrigin);
}

export function isIndexingEnabled(): boolean {
  return process.env.NODE_ENV === "production" && configuredOrigin() !== null;
}
