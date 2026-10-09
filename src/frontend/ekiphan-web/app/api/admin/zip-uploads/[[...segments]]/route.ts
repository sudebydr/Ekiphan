import { cookies } from "next/headers";
import { NextRequest } from "next/server";
import { Agent } from "undici";

export const runtime = "nodejs";
const dispatcher = new Agent({ headersTimeout: 15 * 60 * 1000, bodyTimeout: 15 * 60 * 1000 });
const limit = 4 * 1024 * 1024;

async function proxy(request: NextRequest, context: { params: Promise<{ segments?: string[] }> }) {
  const { segments = [] } = await context.params;
  const validPath = segments.length === 0 ||
    (segments.length <= 2 && /^[0-9a-f-]{36}$/i.test(segments[0]) &&
      (segments.length === 1 || segments[1] === "process"));
  if (!validPath) return new Response(null, { status: 404 });
  const token = (await cookies()).get("ekiphan_admin_access_token")?.value;
  if (!token) return new Response(null, { status: 401 });
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return Response.json({ detail: "Backend API adresi eksik." }, { status: 503 });
  const headers = new Headers({ Authorization: `Bearer ${token}` });
  for (const name of ["content-type", "content-length", "x-chunk-sha256"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  if (Number(headers.get("content-length")) > limit) return new Response(null, { status: 413 });
  let bytes = 0;
  const body = request.body?.pipeThrough(new TransformStream<Uint8Array, Uint8Array>({
    transform(chunk, controller) {
      bytes += chunk.byteLength;
      if (bytes > limit) throw new Error("Parça boyutu aşıldı.");
      controller.enqueue(chunk);
    }
  }));
  try {
    const url = new URL(`/api/admin/zip-uploads/${segments.join("/")}`, base);
    url.search = request.nextUrl.search;
    const response = await fetch(url, {
      method: request.method, headers, body, duplex: "half", dispatcher,
      cache: "no-store", signal: request.signal
    } as RequestInit & { duplex: "half"; dispatcher: Agent });
    return new Response(response.body, { status: response.status,
      headers: { "Content-Type": response.headers.get("content-type") ?? "application/json", "Cache-Control": "no-store" } });
  } catch {
    return Response.json({ detail: "Aktarım bağlantısı kesildi. Yükleme kaldığı yerden sürdürülebilir." }, { status: 502 });
  }
}

export const POST = proxy;
export const GET = proxy;
export const PUT = proxy;
export const DELETE = proxy;
