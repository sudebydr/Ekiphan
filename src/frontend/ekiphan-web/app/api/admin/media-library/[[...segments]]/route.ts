import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const tokenCookie = "ekiphan_admin_access_token";
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const word = /^[a-zA-Z]+$/;
type Method = "GET" | "POST" | "PUT" | "DELETE";
type Context = { params: Promise<{ segments?: string[] }> };

function problem(status: number, detail: string) {
  return Response.json(
    { status, title: "Medya isteği reddedildi", detail },
    { status, headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" } });
}

function allowed(parts: string[], method: Method) {
  if (parts.length === 0) return method === "GET";
  if (parts.length === 1 && parts[0] === "external-video") return method === "POST";
  if (parts.length === 2 && parts[0] === "assets" && guid.test(parts[1] ?? "")) return method === "PUT" || method === "DELETE";
  if (parts.length === 3 && parts[0] === "assets" && guid.test(parts[1] ?? "") && parts[2] === "status") return method === "PUT";
  if (parts.length === 1 && parts[0] === "assignments") return method === "PUT";
  return parts.length === 5 && parts[0] === "assignments" &&
    word.test(parts[1] ?? "") && guid.test(parts[2] ?? "") &&
    guid.test(parts[3] ?? "") && word.test(parts[4] ?? "") &&
    method === "DELETE";
}

async function proxy(request: NextRequest, context: Context, method: Method) {
  const { segments = [] } = await context.params;
  if (!allowed(segments, method)) return problem(404, "Desteklenmeyen medya yolu.");
  const token = (await cookies()).get(tokenCookie)?.value;
  if (!token) return problem(401, "Güvenli admin oturumu gerekli.");
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return problem(503, "Backend API adresi yapılandırılmamış.");
  if (method !== "GET") {
    const origin = request.headers.get("origin");
    const host = (await headers()).get("host");
    if (origin && host) {
      try {
        if (new URL(origin).host !== host) {
          return problem(403, "Cross-site medya değişikliği reddedildi.");
        }
      } catch {
        return problem(403, "Origin değeri geçersiz.");
      }
    }
  }
  if ((method === "POST" || method === "PUT") &&
      !request.headers.get("content-type")?.startsWith("application/json")) {
    return problem(415, "JSON içerik gerekli.");
  }
  const target = new URL(
    `/api/admin/media-library${segments.length ? `/${segments.join("/")}` : ""}`,
    base);
  target.search = request.nextUrl.search;
  try {
    const body = method === "POST" || method === "PUT"
      ? await request.arrayBuffer()
      : undefined;
    if (body && body.byteLength > 32 * 1024) {
      return problem(413, "Medya JSON isteği 32 KB sınırını aşıyor.");
    }
    const upstream = await fetch(target, {
      method,
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
        ...(method === "POST" || method === "PUT"
          ? { "Content-Type": "application/json" }
          : {})
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
    return problem(502, "Medya servisine ulaşılamıyor.");
  }
}

export const GET = (request: NextRequest, context: Context) => proxy(request, context, "GET");
export const POST = (request: NextRequest, context: Context) => proxy(request, context, "POST");
export const PUT = (request: NextRequest, context: Context) => proxy(request, context, "PUT");
export const DELETE = (request: NextRequest, context: Context) => proxy(request, context, "DELETE");
