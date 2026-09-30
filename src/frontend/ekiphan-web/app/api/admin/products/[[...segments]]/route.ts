import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const accessTokenCookie = "ekiphan_admin_access_token";
const maximumRequestBytes = 256 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

type Method = "GET" | "POST";
type RouteContext = { params: Promise<{ segments?: string[] }> };

function problem(status: number, title: string, detail: string): Response {
  return Response.json({ status, title, detail }, {
    status,
    headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" }
  });
}

function isAllowedPath(segments: string[], method: Method): boolean {
  if (method === "POST") {
    return (segments.length === 1 && segments[0] === "bulk") ||
      (segments.length === 2 && segments[0] === "bulk" && segments[1] === "preview");
  }

  return (segments.length === 2 && segments[0] === "bulk" && segments[1] === "selection") ||
    (segments.length === 2 && segments[0] === "bulk-operations" && guidPattern.test(segments[1] ?? "")) ||
    (segments.length === 3 && segments[0] === "bulk-operations" && guidPattern.test(segments[1] ?? "") && segments[2] === "errors");
}

async function verifySameOrigin(request: NextRequest): Promise<Response | null> {
  const origin = request.headers.get("origin");
  const host = (await headers()).get("host");
  if (!origin || !host) return null;

  try {
    return new URL(origin).host === host
      ? null
      : problem(403, "İstek reddedildi", "Cross-site ürün değişikliği reddedildi.");
  } catch {
    return problem(403, "İstek reddedildi", "Origin değeri geçersiz.");
  }
}

async function proxy(request: NextRequest, context: RouteContext, method: Method): Promise<Response> {
  const { segments = [] } = await context.params;
  if (!isAllowedPath(segments, method)) {
    return problem(404, "Bulunamadı", "İstenen toplu ürün işlemi desteklenmiyor.");
  }

  const accessToken = (await cookies()).get(accessTokenCookie)?.value;
  if (!accessToken) {
    return problem(401, "Oturum gerekli", "Toplu ürün işlemi için admin oturumu açılmalıdır.");
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(503, "API yapılandırılmamış", "Sunucu tarafı backend API adresi eksik.");
  }

  let body: ArrayBuffer | undefined;
  if (method === "POST") {
    const originProblem = await verifySameOrigin(request);
    if (originProblem) return originProblem;
    if (!request.headers.get("content-type")?.toLowerCase().startsWith("application/json")) {
      return problem(415, "Desteklenmeyen içerik", "Toplu ürün isteği application/json olmalıdır.");
    }

    const contentLength = Number(request.headers.get("content-length") ?? "0");
    if (!Number.isFinite(contentLength) || contentLength > maximumRequestBytes) {
      return problem(413, "İstek çok büyük", "Toplu ürün isteği 256 KB sınırını aşıyor.");
    }

    body = await request.arrayBuffer();
    if (body.byteLength > maximumRequestBytes) {
      return problem(413, "İstek çok büyük", "Toplu ürün isteği 256 KB sınırını aşıyor.");
    }
  }

  let target: URL;
  try {
    target = new URL(`/api/admin/products/${segments.join("/")}`, apiBaseUrl);
    target.search = request.nextUrl.search;
  } catch {
    return problem(503, "API yapılandırması geçersiz", "Backend API adresi geçersiz.");
  }

  try {
    const upstream = await fetch(target, {
      method,
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${accessToken}`,
        ...(method === "POST" ? { "Content-Type": "application/json" } : {})
      },
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
    return problem(502, "API bağlantı hatası", "Toplu ürün servisine şu anda ulaşılamıyor.");
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}
