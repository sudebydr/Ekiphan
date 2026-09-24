import { cookies } from "next/headers";
import { NextRequest } from "next/server";

const cookieName = "ekiphan_admin_access_token";

function problem(status: number, detail: string) {
  return Response.json(
    { status, detail },
    { status, headers: { "Cache-Control": "private, no-store" } }
  );
}

async function proxy(request: NextRequest, method: "GET" | "PUT") {
  const token = (await cookies()).get(cookieName)?.value;
  if (!token) return problem(401, "Admin oturumu bulunamadı.");

  if (method === "PUT") {
    const origin = request.headers.get("origin");
    if (origin && new URL(origin).host !== request.nextUrl.host)
      return problem(403, "Cross-site istek reddedildi.");
    if (!request.headers.get("content-type")?.startsWith("application/json"))
      return problem(415, "İstek JSON olmalıdır.");
  }

  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return problem(503, "Backend API adresi eksik.");

  const target = new URL(`/api/admin/settings`, base);
  target.search = request.nextUrl.search;

  try {
    let parsedBodyString: string | undefined = undefined;

    if (method === "PUT" && request.body) {
      let size = 0;
      const chunks: Uint8Array[] = [];
      const reader = request.body.getReader();
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        if (value) {
          size += value.length;
          if (size > 10 * 1024) return problem(413, "İstek boyutu çok büyük (Maks 10KB).");
          chunks.push(value);
        }
      }

      const totalBuffer = new Uint8Array(size);
      let offset = 0;
      for (const chunk of chunks) {
        totalBuffer.set(chunk, offset);
        offset += chunk.length;
      }

      parsedBodyString = new TextDecoder().decode(totalBuffer);
      try {
        JSON.parse(parsedBodyString);
      } catch {
        return problem(400, "Geçersiz JSON formatı.");
      }
    }

    const upstream = await fetch(target, {
      method,
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
        ...(method === "PUT" ? { "Content-Type": "application/json" } : {}),
      },
      body: parsedBodyString,
      cache: "no-store",
      signal: request.signal,
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type":
          upstream.headers.get("content-type") ?? "application/problem+json",
      },
    });
  } catch {
    return problem(502, "Settings servisine ulaşılamıyor.");
  }
}

export function GET(request: NextRequest) {
  return proxy(request, "GET");
}

export function PUT(request: NextRequest) {
  return proxy(request, "PUT");
}
