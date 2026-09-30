import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const cookieName = "ekiphan_admin_access_token";
const maximumRequestBytes = 16 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

type RouteContext = { params: Promise<{ segments?: string[] }> };

function problem(status: number, title: string, detail: string): Response {
  return Response.json(
    { status, title, detail },
    { status, headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" } }
  );
}

function allowed(
  segments: string[],
  method: "GET" | "POST" | "PUT" | "DELETE"
): boolean {
  if (segments.length === 0) return method === "GET";
  if (segments.length === 1 && segments[0] === "taxonomy") {
    return method === "GET";
  }
  if (
    segments.length === 1 &&
    ["reasons", "complaint-categories"].includes(segments[0] ?? "")
  ) {
    return method === "POST";
  }
  if (
    segments.length === 2 &&
    ["reasons", "complaint-categories"].includes(segments[0] ?? "") &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT" || method === "DELETE";
  }
  if (segments.length === 1 && ["assignees", "complaints"].includes(segments[0] ?? "")) {
    return method === "GET";
  }
  if (!guidPattern.test(segments[0] ?? "")) return false;
  if (segments.length === 1) return method === "GET";
  return segments.length === 2 && method === "POST" &&
    ["status", "assignment", "notes"].includes(segments[1] ?? "");
}

async function proxy(
  request: NextRequest,
  context: RouteContext,
  method: "GET" | "POST" | "PUT" | "DELETE"
): Promise<Response> {
  const { segments = [] } = await context.params;
  if (!allowed(segments, method)) {
    return problem(404, "Bulunamadı", "İstenen iletişim yönetimi yolu desteklenmiyor.");
  }
  const token = (await cookies()).get(cookieName)?.value;
  if (!token) {
    return problem(401, "Oturum gerekli", "İletişim talepleri için admin oturumu gereklidir.");
  }
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) {
    return problem(503, "API yapılandırılmamış", "Backend API adresi eksik.");
  }
  if (method !== "GET") {
    const origin = request.headers.get("origin");
    const host = (await headers()).get("host");
    if (origin && host) {
      try {
        if (new URL(origin).host !== host) {
          return problem(403, "İstek reddedildi", "Cross-site değişiklik isteği reddedildi.");
        }
      } catch {
        return problem(403, "İstek reddedildi", "Origin değeri geçersiz.");
      }
    }
    if (method !== "DELETE" && !request.headers.get("content-type")?.toLowerCase().startsWith("application/json")) {
      return problem(415, "Desteklenmeyen içerik", "İstek JSON olmalıdır.");
    }
  }
  const target = new URL(
    `/api/admin/contact-requests${segments.length ? `/${segments.join("/")}` : ""}`,
    base
  );
  target.search = request.nextUrl.search;
  const outbound = new Headers({
    Accept: "application/json",
    Authorization: `Bearer ${token}`
  });
  let body: ArrayBuffer | undefined;
  if (method === "POST" || method === "PUT") {
    body = await request.arrayBuffer();
    if (body.byteLength > maximumRequestBytes) {
      return problem(413, "İstek çok büyük", "İstek 16 KB sınırını aşıyor.");
    }
    outbound.set("Content-Type", "application/json");
  }
  try {
    const upstream = await fetch(target, {
      method,
      headers: outbound,
      body,
      cache: "no-store",
      signal: request.signal
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type": upstream.headers.get("content-type") ?? "application/problem+json",
        Pragma: "no-cache"
      }
    });
  } catch {
    return problem(502, "API bağlantı hatası", "İletişim yönetimi servisine ulaşılamıyor.");
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}

export function PUT(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "PUT");
}

export function DELETE(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "DELETE");
}
