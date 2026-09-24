import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const maximumBytes = 50 * 1024 * 1024 + 64 * 1024;

function problem(status: number, detail: string) {
  return Response.json(
    { status, title: "Medya yükleme reddedildi", detail },
    { status, headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" } });
}

export async function POST(request: NextRequest) {
  const token = (await cookies()).get("ekiphan_admin_access_token")?.value;
  if (!token) return problem(401, "Güvenli admin oturumu gerekli.");
  const origin = request.headers.get("origin");
  const host = (await headers()).get("host");
  if (origin && host) {
    try {
      if (new URL(origin).host !== host) {
        return problem(403, "Cross-site medya yüklemesi reddedildi.");
      }
    } catch {
      return problem(403, "Origin değeri geçersiz.");
    }
  }
  if (!request.headers.get("content-type")?.startsWith("multipart/form-data")) {
    return problem(415, "multipart/form-data içerik gerekli.");
  }
  const length = Number(request.headers.get("content-length") ?? "0");
  if (!Number.isFinite(length) || length > maximumBytes) {
    return problem(413, "Medya isteği boyut sınırını aşıyor.");
  }
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return problem(503, "Backend API adresi yapılandırılmamış.");
  try {
    const body = await request.arrayBuffer();
    if (body.byteLength > maximumBytes) {
      return problem(413, "Medya isteği boyut sınırını aşıyor.");
    }
    const upstream = await fetch(new URL("/api/admin/media", base), {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": request.headers.get("content-type")!
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
    return problem(502, "Medya yükleme servisine ulaşılamıyor.");
  }
}
