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

function isAllowedPath(segments: string[], method: "GET" | "POST"): boolean {
  if (segments.length === 1) {
    if (method === "POST") {
      return ["preview", "validate", "execute"].includes(segments[0]);
    }
    return false;
  }

  if (segments.length === 1 && method === "GET") {
    return /^[0-9a-f-]{36}$/i.test(segments[0]);
  }

  if (segments.length === 2) {
    const isGuid = /^[0-9a-f-]{36}$/i.test(segments[0]);

    if (!isGuid) return false;

    if (method === "GET") return segments[1] === "errors";
    if (method === "POST") return segments[1] === "rollback";
  }

  return false;
}

async function proxy(
  request: NextRequest,
  context: RouteContext,
  method: "GET" | "POST"
): Promise<Response> {
  const { segments = [] } = await context.params;

  // Batch detail: GET /api/admin/product-media-import/{batchId}
  const batchDetail =
    method === "GET" &&
    segments.length === 1 &&
    /^[0-9a-f-]{36}$/i.test(segments[0]);

  if (!batchDetail && !isAllowedPath(segments, method)) {
    return problem(404, "Bulunamadı", "İstenen medya import yolu desteklenmiyor.");
  }

  const cookieStore = await cookies();
  const accessToken = cookieStore.get(accessTokenCookie)?.value;

  if (!accessToken) {
    return problem(
      401,
      "Oturum gerekli",
      "Ürün görsel importu için admin oturumu gereklidir."
    );
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;

  if (!apiBaseUrl) {
    return problem(
      503,
      "API yapılandırılmamış",
      "Backend API adresi eksik."
    );
  }

  const target = new URL(
    `/api/admin/product-media-import/${segments.join("/")}`,
    apiBaseUrl
  );
  const isExecuteRequest =
    method === "POST" &&
    segments.length === 1 &&
    segments[0] === "execute";

  const headers = new Headers({
    Accept: request.headers.get("accept") ?? "application/json",
    Authorization: `Bearer ${accessToken}`
  });

  const contentType = request.headers.get("content-type");
  if (contentType) {
    headers.set("Content-Type", contentType);
  }

  const contentLength = request.headers.get("content-length");
  if (
    method === "POST" &&
    contentLength !== null &&
    (!Number.isSafeInteger(Number(contentLength)) ||
      Number(contentLength) > maximumRequestBytes)
  ) {
    return problem(413, "Dosya çok büyük", "ZIP dosyası boyut sınırını aşıyor.");
  }

  try {
    const body =
      method === "POST" && request.body
        ? limitRequestBody(request.body)
        : undefined;

    const upstream = await fetch(target, {
      method,
      headers,
      body,
      cache: "no-store",
      signal: request.signal,
      duplex: "half",
      ...(isExecuteRequest ? { dispatcher: executeDispatcher } : {})
    } as RequestInit & { duplex: "half"; dispatcher?: Agent });

    const responseHeaders = new Headers();
    const upstreamContentType = upstream.headers.get("content-type");

    if (upstreamContentType) {
      responseHeaders.set("Content-Type", upstreamContentType);
    }

    const disposition = upstream.headers.get("content-disposition");
    if (disposition) {
      responseHeaders.set("Content-Disposition", disposition);
    }

    responseHeaders.set("Cache-Control", "no-store");

    return new Response(upstream.body, {
      status: upstream.status,
      headers: responseHeaders
    });
  } catch (error) {
    if (error instanceof RequestBodyTooLargeError) {
      return problem(413, "Dosya çok büyük", "ZIP dosyası boyut sınırını aşıyor.");
    }

    return problem(
      502,
      "API bağlantı hatası",
      "Ürün görsel import servisine ulaşılamıyor."
    );
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}
