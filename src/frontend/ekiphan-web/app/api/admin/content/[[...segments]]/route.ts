import { cookies } from "next/headers";
import { NextRequest } from "next/server";

const cookieName = "ekiphan_admin_access_token";
const maximumRequestBytes = 128 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

type Context = { params: Promise<{ segments?: string[] }> };

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

function allowed(segments: string[], method: "GET" | "POST" | "PUT") {
  if (segments.length === 1 && segments[0] === "pages") {
    return method === "GET" || method === "POST";
  }
  if (segments.length === 1 && segments[0] === "menu-items") {
    return method === "GET" || method === "POST";
  }
  if (segments.length === 1 && segments[0] === "homepage-heroes") {
    return method === "GET" || method === "POST";
  }
  if (segments.length === 1 && segments[0] === "gallery") {
    return method === "GET" || method === "POST";
  }
  if (segments.length === 1 && segments[0] === "press-releases") {
    return method === "GET" || method === "POST";
  }
  if (
    segments.length === 2 &&
    segments[0] === "menu-items" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }
  if (
    segments.length === 2 &&
    segments[0] === "homepage-heroes" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }
  if (
    segments.length === 2 &&
    segments[0] === "gallery" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }
  if (
    segments.length === 2 &&
    segments[0] === "press-releases" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }
  return (
    segments.length === 2 &&
    segments[0] === "pages" &&
    guidPattern.test(segments[1] ?? "") &&
    method === "PUT"
  );
}

async function proxy(
  request: NextRequest,
  context: Context,
  method: "GET" | "POST" | "PUT"
) {
  const { segments = [] } = await context.params;
  if (!allowed(segments, method)) {
    return problem(404, "Bulunamadı", "İçerik API yolu desteklenmiyor.");
  }

  const token = (await cookies()).get(cookieName)?.value;
  if (!token) {
    return problem(401, "Oturum gerekli", "Admin oturumu bulunamadı.");
  }

  if (method !== "GET") {
    const origin = request.headers.get("origin");
    if (origin) {
      try {
        if (new URL(origin).host !== request.nextUrl.host) {
          return problem(403, "İstek reddedildi", "Cross-site istek reddedildi.");
        }
      } catch {
        return problem(403, "İstek reddedildi", "Origin değeri geçersiz.");
      }
    }
    if (!request.headers
      .get("content-type")
      ?.toLowerCase()
      .startsWith("application/json")) {
      return problem(415, "Desteklenmeyen içerik", "İstek JSON olmalıdır.");
    }
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(503, "API yapılandırılmamış", "Backend API adresi eksik.");
  }

  const body = method === "GET" ? undefined : await request.arrayBuffer();
  if (body && body.byteLength > maximumRequestBytes) {
    return problem(413, "İstek çok büyük", "İçerik isteği sınırı aşıldı.");
  }

  try {
    const upstream = await fetch(
      new URL(`/api/admin/content/${segments.join("/")}`, apiBaseUrl),
      {
        method,
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
          ...(body ? { "Content-Type": "application/json" } : {})
        },
        body,
        cache: "no-store",
        signal: request.signal
      }
    );
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
      "İçerik servisine ulaşılamıyor."
    );
  }
}

export function GET(request: NextRequest, context: Context) {
  return proxy(request, context, "GET");
}
export function POST(request: NextRequest, context: Context) {
  return proxy(request, context, "POST");
}
export function PUT(request: NextRequest, context: Context) {
  return proxy(request, context, "PUT");
}
