import { cookies } from "next/headers";
import { NextRequest } from "next/server";

const cookieName = "ekiphan_admin_access_token";
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
const types = new Set(["product", "category", "contentpage", "pressrelease"]);
type Context = { params: Promise<{ segments?: string[] }> };

function problem(status: number, detail: string) {
  return Response.json({ status, detail }, { status, headers: { "Cache-Control": "private, no-store" } });
}
function allowed(parts: string[], method: "GET" | "PUT") {
  if (method === "GET" && (parts.length === 0 || (parts.length === 1 && ["summary", "duplicates", "slug-availability"].includes(parts[0] ?? "")))) return true;
  return parts.length === 2 && types.has(parts[0] ?? "") && guid.test(parts[1] ?? "");
}
async function proxy(request: NextRequest, context: Context, method: "GET" | "PUT") {
  const { segments = [] } = await context.params;
  if (!allowed(segments, method)) return problem(404, "SEO API yolu desteklenmiyor.");
  const token = (await cookies()).get(cookieName)?.value;
  if (!token) return problem(401, "Admin oturumu bulunamadı.");
  if (method === "PUT") {
    const origin = request.headers.get("origin");
    if (origin && new URL(origin).host !== request.nextUrl.host) return problem(403, "Cross-site istek reddedildi.");
    if (!request.headers.get("content-type")?.startsWith("application/json")) return problem(415, "İstek JSON olmalıdır.");
  }
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return problem(503, "Backend API adresi eksik.");
  const target = new URL(`/api/admin/seo/${segments.join("/")}`, base);
  target.search = request.nextUrl.search;
  try {
    const upstream = await fetch(target, { method, headers: { Accept: "application/json", Authorization: `Bearer ${token}`, ...(method === "PUT" ? { "Content-Type": "application/json" } : {}) }, body: method === "PUT" ? await request.arrayBuffer() : undefined, cache: "no-store", signal: request.signal });
    return new Response(upstream.body, { status: upstream.status, headers: { "Cache-Control": "private, no-store", "Content-Type": upstream.headers.get("content-type") ?? "application/problem+json" } });
  } catch { return problem(502, "SEO servisine ulaşılamıyor."); }
}
export function GET(request: NextRequest, context: Context) { return proxy(request, context, "GET"); }
export function PUT(request: NextRequest, context: Context) { return proxy(request, context, "PUT"); }
