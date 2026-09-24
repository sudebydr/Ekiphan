import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const accessTokenCookie = "ekiphan_admin_access_token";
const maximumRequestBytes = 25 * 1024 * 1024 + 64 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

interface RouteContext {
  params: Promise<{ segments?: string[] }>;
}

function problem(status: number, title: string, detail: string): Response {
  return Response.json({ status, title, detail }, { status });
}

function isAllowedPath(segments: string[], method: "GET" | "POST"): boolean {
  if (segments.length === 0) {
    return method === "GET" || method === "POST";
  }

  if (!guidPattern.test(segments[0] ?? "")) {
    return false;
  }

  if (segments.length === 1) {
    return method === "GET";
  }

  if (segments.length !== 2) {
    return false;
  }

  return method === "GET"
    ? segments[1] === "issues" || segments[1] === "issues.csv"
    : segments[1] === "publish";
}

async function proxy(
  request: NextRequest,
  context: RouteContext,
  method: "GET" | "POST"
): Promise<Response> {
  const { segments = [] } = await context.params;
  if (!isAllowedPath(segments, method)) {
    return problem(404, "Bulunamadı", "İstenen import API yolu desteklenmiyor.");
  }

  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value;
  if (!accessToken) {
    return problem(
      401,
      "Oturum gerekli",
      "Import yönetimi için güvenli admin oturumu açılmalıdır."
    );
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(
      503,
      "API yapılandırılmamış",
      "Sunucu tarafı EKIPHAN_API_BASE_URL ayarı eksik."
    );
  }

  if (method === "POST") {
    const requestHeaders = await headers();
    const origin = request.headers.get("origin");
    const host = requestHeaders.get("host");
    if (origin && host && new URL(origin).host !== host) {
      return problem(403, "İstek reddedildi", "Cross-site mutation isteği reddedildi.");
    }

    const contentLength = Number(request.headers.get("content-length") ?? "0");
    if (contentLength > maximumRequestBytes) {
      return problem(413, "Dosya çok büyük", "İstek gövdesi 25 MB sınırını aşıyor.");
    }
  }

  let target: URL;
  try {
    target = new URL(
      `/api/admin/imports${segments.length > 0 ? `/${segments.join("/")}` : ""}`,
      apiBaseUrl
    );
  } catch {
    return problem(503, "API yapılandırması geçersiz", "Backend API URL değeri geçersiz.");
  }

  target.search = request.nextUrl.search;
  const outboundHeaders = new Headers({
    Accept: request.headers.get("accept") ?? "application/json",
    Authorization: `Bearer ${accessToken}`
  });
  const contentType = request.headers.get("content-type");
  if (contentType) {
    outboundHeaders.set("Content-Type", contentType);
  }

  try {
    const body = method === "POST" ? await request.arrayBuffer() : undefined;
    if (body && body.byteLength > maximumRequestBytes) {
      return problem(413, "Dosya çok büyük", "İstek gövdesi 25 MB sınırını aşıyor.");
    }

    const upstream = await fetch(target, {
      method,
      headers: outboundHeaders,
      body,
      cache: "no-store",
      signal: request.signal
    });
    const responseHeaders = new Headers();
    for (const name of ["content-type", "content-disposition"]) {
      const value = upstream.headers.get(name);
      if (value) {
        responseHeaders.set(name, value);
      }
    }

    responseHeaders.set("Cache-Control", "no-store");
    return new Response(upstream.body, {
      status: upstream.status,
      headers: responseHeaders
    });
  } catch {
    return problem(
      502,
      "API bağlantı hatası",
      "Import servisine şu anda ulaşılamıyor."
    );
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}
