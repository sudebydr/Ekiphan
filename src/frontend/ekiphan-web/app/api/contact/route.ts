import { headers } from "next/headers";
import { NextRequest } from "next/server";

const maximumBytes = 16 * 1024;

function problem(status: number, detail: string) {
  return Response.json({ status, detail }, {
    status, headers: { "Cache-Control": "no-store" }
  });
}

export async function POST(request: NextRequest) {
  const host = (await headers()).get("host");
  const origin = request.headers.get("origin");
  if (origin && host) {
    try {
      if (new URL(origin).host !== host) return problem(403, "İstek reddedildi.");
    } catch {
      return problem(403, "Origin değeri geçersiz.");
    }
  }
  if (!request.headers.get("content-type")?.toLowerCase()
    .startsWith("application/json")) {
    return problem(415, "İstek JSON olmalıdır.");
  }
  const body = await request.arrayBuffer();
  if (body.byteLength > maximumBytes) return problem(413, "İstek çok büyük.");
  const base = process.env.EKIPHAN_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!base) return problem(503, "İletişim servisi hazır değil.");
  try {
    const upstream = await fetch(new URL("/api/contact", base), {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body, cache: "no-store", signal: request.signal
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "no-store",
        "Content-Type": upstream.headers.get("content-type") ??
          "application/problem+json"
      }
    });
  } catch {
    return problem(502, "İletişim servisine ulaşılamıyor.");
  }
}
