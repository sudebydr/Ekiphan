import { cookies, headers } from "next/headers";
import { NextRequest } from "next/server";

const accessTokenCookie = "ekiphan_admin_access_token";
const maximumRequestBytes = 32 * 1024;
const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

type Method = "GET" | "POST" | "PUT" | "DELETE";
type RouteContext = {
  params: Promise<{ segments?: string[] }>;
};

function problem(status: number, title: string, detail: string): Response {
  return Response.json(
    { status, title, detail },
    {
      status,
      headers: {
        "Cache-Control": "private, no-store",
        Pragma: "no-cache"
      }
    }
  );
}

function isAllowedPath(segments: string[], method: Method): boolean {
  if (
    segments.length === 1 &&
    segments[0] === "products"
  ) {
    return method === "GET" || method === "POST";
  }

  if (
    segments.length === 2 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "GET" || method === "PUT" || method === "DELETE";
  }

  if (segments.length === 1 && segments[0] === "brands") {
    return method === "GET" || method === "POST";
  }

  if (
    segments.length === 2 &&
    segments[0] === "brands" &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "GET" || method === "PUT" || method === "DELETE";
  }

  if (segments.length === 1 && segments[0] === "structure") {
    return method === "GET";
  }

  if (
    segments.length === 1 &&
    (segments[0] === "sections" || segments[0] === "categories")
  ) {
    return method === "POST";
  }

  if (
    segments.length === 2 &&
    (segments[0] === "sections" || segments[0] === "categories") &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT" ||
      (segments[0] === "categories" && method === "DELETE");
  }

  if (segments.length === 1 && segments[0] === "attributes") {
    return method === "GET" || method === "POST";
  }

  if (segments.length === 1 && segments[0] === "dictionaries") {
    return method === "GET";
  }

  if (
    segments.length === 1 &&
    (segments[0] === "tags" || segments[0] === "units")
  ) {
    return method === "POST";
  }

  if (
    segments.length === 2 &&
    (segments[0] === "tags" || segments[0] === "units") &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }

  if (
    segments.length === 3 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "") &&
    segments[2] === "tags"
  ) {
    return method === "PUT";
  }

  if (
    segments.length === 2 &&
    (segments[0] === "attributes" ||
      segments[0] === "attribute-options") &&
    guidPattern.test(segments[1] ?? "")
  ) {
    return method === "PUT";
  }

  if (
    segments.length === 3 &&
    segments[0] === "attributes" &&
    guidPattern.test(segments[1] ?? "") &&
    segments[2] === "options"
  ) {
    return method === "POST";
  }

  if (segments.length === 1 && segments[0] === "category-attributes") {
    return method === "PUT";
  }

  if (
    segments.length === 3 &&
    segments[0] === "category-attributes" &&
    guidPattern.test(segments[1] ?? "") &&
    guidPattern.test(segments[2] ?? "")
  ) {
    return method === "DELETE";
  }

  if (
    segments.length === 3 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "") &&
    (segments[2] === "variants" || segments[2] === "variant-groups")
  ) {
    return segments[2] === "variants"
      ? method === "GET" || method === "POST"
      : method === "POST";
  }

  if (
    segments.length === 4 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "") &&
    (segments[2] === "variants" ||
      segments[2] === "variant-groups" ||
      segments[2] === "variant-options") &&
    guidPattern.test(segments[3] ?? "")
  ) {
    return method === "PUT";
  }

  if (
    segments.length === 5 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "") &&
    segments[2] === "variant-groups" &&
    guidPattern.test(segments[3] ?? "") &&
    segments[4] === "options"
  ) {
    return method === "POST";
  }

  if (
    segments.length === 3 &&
    segments[0] === "products" &&
    guidPattern.test(segments[1] ?? "") &&
    segments[2] === "relations"
  ) {
    return method === "GET" || method === "POST";
  }

  return (
    segments.length === 2 &&
    segments[0] === "relations" &&
    guidPattern.test(segments[1] ?? "") &&
    method === "DELETE"
  );
}

async function verifySameOrigin(request: NextRequest): Promise<Response | null> {
  const requestHeaders = await headers();
  const origin = request.headers.get("origin");
  const host = requestHeaders.get("host");
  if (!origin || !host) return null;

  try {
    return new URL(origin).host === host
      ? null
      : problem(
          403,
          "İstek reddedildi",
          "Cross-site katalog değişikliği reddedildi."
        );
  } catch {
    return problem(
      403,
      "İstek reddedildi",
      "Origin değeri geçersiz."
    );
  }
}

async function proxy(
  request: NextRequest,
  context: RouteContext,
  method: Method
): Promise<Response> {
  const { segments = [] } = await context.params;
  if (!isAllowedPath(segments, method)) {
    return problem(
      404,
      "Bulunamadı",
      "İstenen katalog yönetimi API yolu desteklenmiyor."
    );
  }

  const accessToken = (await cookies()).get(accessTokenCookie)?.value;
  if (!accessToken) {
    return problem(
      401,
      "Oturum gerekli",
      "Katalog yönetimi için güvenli admin oturumu açılmalıdır."
    );
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(
      503,
      "API yapılandırılmamış",
      "Sunucu tarafı backend API adresi eksik."
    );
  }

  if (method !== "GET") {
    const originProblem = await verifySameOrigin(request);
    if (originProblem) return originProblem;
  }

  if (method === "POST" || method === "PUT") {
    if (!request.headers
      .get("content-type")
      ?.toLowerCase()
      .startsWith("application/json")) {
      return problem(
        415,
        "Desteklenmeyen içerik",
        "Katalog isteği application/json olmalıdır."
      );
    }

    const contentLength = Number(
      request.headers.get("content-length") ?? "0"
    );
    if (
      !Number.isFinite(contentLength) ||
      contentLength > maximumRequestBytes
    ) {
      return problem(
        413,
        "İstek çok büyük",
        "Katalog isteği 32 KB sınırını aşıyor."
      );
    }
  }

  let target: URL;
  try {
    target = new URL(
      `/api/admin/catalog/${segments.join("/")}`,
      apiBaseUrl
    );
  } catch {
    return problem(
      503,
      "API yapılandırması geçersiz",
      "Backend API adresi geçersiz."
    );
  }
  target.search = request.nextUrl.search;

  const outboundHeaders = new Headers({
    Accept: "application/json",
    Authorization: `Bearer ${accessToken}`
  });
  if (method === "POST" || method === "PUT") {
    outboundHeaders.set("Content-Type", "application/json");
  }

  try {
    const body =
      method === "POST" || method === "PUT"
        ? await request.arrayBuffer()
        : undefined;
    if (body && body.byteLength > maximumRequestBytes) {
      return problem(
        413,
        "İstek çok büyük",
        "Katalog isteği 32 KB sınırını aşıyor."
      );
    }

    const upstream = await fetch(target, {
      method,
      headers: outboundHeaders,
      body,
      cache: "no-store",
      signal: request.signal
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type":
          upstream.headers.get("content-type") ??
          "application/problem+json",
        Pragma: "no-cache"
      }
    });
  } catch {
    return problem(
      502,
      "API bağlantı hatası",
      "Katalog yönetimi servisine şu anda ulaşılamıyor."
    );
  }
}

export function GET(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "GET");
}

export function POST(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "POST");
}

export function PUT(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "PUT");
}

export function DELETE(request: NextRequest, context: RouteContext) {
  return proxy(request, context, "DELETE");
}
