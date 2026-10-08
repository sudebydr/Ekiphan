import { cookies } from "next/headers";
import { NextRequest } from "next/server";
import { Agent } from "undici";

const accessTokenCookie = "ekiphan_admin_access_token";
const maximumRequestBytes = 1536 * 1024 * 1024;
const executeResponseTimeoutMilliseconds = 30 * 60 * 1000;
const executeDispatcher = new Agent({
  headersTimeout: executeResponseTimeoutMilliseconds,
  bodyTimeout: executeResponseTimeoutMilliseconds
});

class RequestBodyTooLargeError extends Error {}

interface RouteContext {
  params: Promise<{ segments?: string[] }>;
}

function problem(status: number, title: string, detail: string): Response {
  return Response.json(
    { status, title, detail },
    { status, headers: { "Cache-Control": "no-store" } }
  );
}

function limitRequestBody(
  body: ReadableStream<Uint8Array>
): ReadableStream<Uint8Array> {
  const reader = body.getReader();
  let bytesRead = 0;

  return new ReadableStream<Uint8Array>({
    async pull(controller) {
      const { done, value } = await reader.read();

      if (done) {
        controller.close();
        return;
      }

      bytesRead += value.byteLength;
      if (bytesRead > maximumRequestBytes) {
        const error = new RequestBodyTooLargeError();
        await reader.cancel(error);
        controller.error(error);
        return;
      }

      controller.enqueue(value);
    },
    cancel(reason) {
      return reader.cancel(reason);
    }
  });
}

async function proxy(
  request: NextRequest,
  context: RouteContext
): Promise<Response> {
  const { segments = [] } = await context.params;

  if (segments.length !== 1 || !["preview", "execute", "single", "backfill-covers"].includes(segments[0])) {
    return problem(404, "Bulunamadı", "İstenen katalog PDF import yolu desteklenmiyor.");
  }

  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value;

  if (!accessToken) {
    return problem(401, "Oturum gerekli", "Katalog PDF importu için admin oturumu gereklidir.");
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(503, "API yapılandırılmamış", "Backend API adresi eksik.");
  }

  const contentLength = request.headers.get("content-length");
  if (
    contentLength !== null &&
    (!Number.isSafeInteger(Number(contentLength)) ||
      Number(contentLength) > maximumRequestBytes)
  ) {
    return problem(413, "Dosya çok büyük", "ZIP dosyası boyut sınırını aşıyor.");
  }

  const headers = new Headers({
    Accept: request.headers.get("accept") ?? "application/json",
    Authorization: `Bearer ${accessToken}`
  });
  const contentType = request.headers.get("content-type");
  if (contentType) headers.set("Content-Type", contentType);

  try {
    const body = request.body ? limitRequestBody(request.body) : undefined;
    const isExecuteRequest = segments[0] === "execute";
    const upstream = await fetch(
      new URL(`/api/admin/catalog-pdf-import/${segments[0]}`, apiBaseUrl),
      {
        method: "POST",
        headers,
        body,
        cache: "no-store",
        signal: request.signal,
        duplex: "half",
        ...(isExecuteRequest ? { dispatcher: executeDispatcher } : {})
      } as RequestInit & { duplex: "half"; dispatcher?: Agent }
    );

    const responseHeaders = new Headers({ "Cache-Control": "no-store" });
    const upstreamContentType = upstream.headers.get("content-type");
    if (upstreamContentType) responseHeaders.set("Content-Type", upstreamContentType);

    return new Response(upstream.body, { status: upstream.status, headers: responseHeaders });
  } catch (error) {
    if (error instanceof RequestBodyTooLargeError) {
      return problem(413, "Dosya çok büyük", "ZIP dosyası boyut sınırını aşıyor.");
    }
    const detail = error instanceof Error && error.message
      ? `Katalog PDF import servisine ulaşılamıyor: ${error.message}`
      : "Katalog PDF import servisine ulaşılamıyor.";
    return problem(502, "API bağlantı hatası", detail);
  }
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context);
}
