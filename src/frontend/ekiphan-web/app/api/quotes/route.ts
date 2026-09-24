import { headers } from "next/headers";
import { NextRequest } from "next/server";

const maximumRequestBytes = 64 * 1024;

function problem(status: number, title: string, detail: string): Response {
  return Response.json(
    { status, title, detail },
    {
      status,
      headers: { "Cache-Control": "no-store" }
    }
  );
}

export async function POST(request: NextRequest): Promise<Response> {
  const requestHeaders = await headers();
  const origin = request.headers.get("origin");
  const host = requestHeaders.get("host");
  if (origin && host) {
    try {
      if (new URL(origin).host !== host) {
        return problem(
          403,
          "İstek reddedildi",
          "Cross-site teklif isteği reddedildi."
        );
      }
    } catch {
      return problem(403, "İstek reddedildi", "Origin değeri geçersiz.");
    }
  }

  if (!request.headers
    .get("content-type")
    ?.toLowerCase()
    .startsWith("application/json")) {
    return problem(
      415,
      "Desteklenmeyen içerik",
      "Teklif isteği application/json olmalıdır."
    );
  }

  const contentLength = Number(request.headers.get("content-length") ?? "0");
  if (!Number.isFinite(contentLength) || contentLength > maximumRequestBytes) {
    return problem(
      413,
      "İstek çok büyük",
      "Teklif isteği 64 KB sınırını aşıyor."
    );
  }

  const apiBaseUrl =
    process.env.EKIPHAN_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(
      503,
      "Teklif servisi hazır değil",
      "Backend API adresi yapılandırılmamış."
    );
  }

  let target: URL;
  try {
    target = new URL("/api/quotes", apiBaseUrl);
  } catch {
    return problem(
      503,
      "Teklif servisi hazır değil",
      "Backend API adresi geçersiz."
    );
  }

  try {
    const body = await request.arrayBuffer();
    if (body.byteLength > maximumRequestBytes) {
      return problem(
        413,
        "İstek çok büyük",
        "Teklif isteği 64 KB sınırını aşıyor."
      );
    }

    const upstream = await fetch(target, {
      method: "POST",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json"
      },
      body,
      cache: "no-store",
      signal: request.signal
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "no-store",
        "Content-Type":
          upstream.headers.get("content-type") ??
          "application/problem+json"
      }
    });
  } catch {
    return problem(
      502,
      "Teklif servisine ulaşılamıyor",
      "Lütfen daha sonra tekrar deneyin."
    );
  }
}
