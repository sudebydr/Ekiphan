import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const accessTokenCookie = "ekiphan_admin_access_token";
const maximumRequestBytes = 16 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

type RouteContext = {
  params: Promise<{ segments?: string[] }>;
};

function problem(status: number, title: string, detail: string): Response {
  return Response.json(
    { status, title, detail },
    {
      status,
      headers: {
        "Cache-Control": "private, no-store",
        Pragma: "no-cache"
      }
    }
  );
}

function isAllowedPath(segments: string[], method: "GET" | "POST"): boolean {
  if (segments.length === 0) return method === "GET";
  if (segments.length === 1 && segments[0] === "assignees") {
    return method === "GET";
  }
  if (!guidPattern.test(segments[0] ?? "")) return false;
  if (segments.length === 1) return method === "GET";
  return (
    segments.length === 2 &&
    ["status", "assignment", "notes"].includes(segments[1] ?? "") &&
    method === "POST"
  );
}

async function proxy(
  request: NextRequest,
  context: RouteContext,
  method: "GET" | "POST"
): Promise<Response> {
  const { segments = [] } = await context.params;
  if (!isAllowedPath(segments, method)) {
    return problem(
      404,
      "Bulunamadı",
      "İstenen teklif yönetimi API yolu desteklenmiyor."
    );
  }

  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value;
  if (!accessToken) {
    return problem(
      401,
      "Oturum gerekli",
      "Teklif yönetimi için güvenli admin oturumu açılmalıdır."
    );
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(
      503,
      "API yapılandırılmamış",
      "Sunucu tarafı backend API adresi eksik."
    );
  }

  if (method === "POST") {
    const requestHeaders = await headers();
    const origin = request.headers.get("origin");
    const host = requestHeaders.get("host");
    if (origin && host) {
      try {
        if (new URL(origin).host !== host) {
          return problem(
            403,
            "İstek reddedildi",
            "Cross-site durum değişikliği reddedildi."
          );
        }
      } catch {
        return problem(
          403,
          "İstek reddedildi",
          "Origin değeri geçersiz."
        );
      }
    }

    if (!request.headers
      .get("content-type")
      ?.toLowerCase()
      .startsWith("application/json")) {
      return problem(
        415,
        "Desteklenmeyen içerik",
        "Durum isteği application/json olmalıdır."
      );
    }

    const contentLength = Number(
      request.headers.get("content-length") ?? "0"
    );
    if (
      !Number.isFinite(contentLength) ||
      contentLength > maximumRequestBytes
    ) {
      return problem(
        413,
        "İstek çok büyük",
        "Durum isteği 16 KB sınırını aşıyor."
      );
    }
  }

  let target: URL;
  try {
    target = new URL(
      `/api/admin/quotes${
        segments.length > 0 ? `/${segments.join("/")}` : ""
      }`,
      apiBaseUrl
    );
  } catch {
    return problem(
      503,
      "API yapılandırması geçersiz",
      "Backend API adresi geçersiz."
    );
  }
  target.search = request.nextUrl.search;

  const outboundHeaders = new Headers({
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`
  });
  if (method === "POST") {
    outboundHeaders.set("Content-Type", "application/json");
  }

  try {
    const body = method === "POST" ? await request.arrayBuffer() : undefined;
    if (body && body.byteLength > maximumRequestBytes) {
      return problem(
        413,
        "İstek çok büyük",
        "Durum isteği 16 KB sınırını aşıyor."
      );
    }

    const upstream = await fetch(target, {
      method,
      headers: outboundHeaders,
      body,
      cache: "no-store",
      signal: request.signal
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type":
          upstream.headers.get("content-type") ??
          "application/problem+json",
        Pragma: "no-cache"
      }
    });
  } catch {
    return problem(
      502,
      "API bağlantı hatası",
      "Teklif yönetimi servisine şu anda ulaşılamıyor."
    );
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}
