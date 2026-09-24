import { NextRequest, NextResponse } from "next/server";

const cookieName = "ekiphan_admin_access_token";
const maximumRequestBytes = 8 * 1024;

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

export async function POST(request: NextRequest): Promise<Response> {
  const origin = request.headers.get("origin");
  if (origin) {
    try {
      const host = request.headers.get("host");
      if (!host || new URL(origin).host !== host) {
        return problem(
          403,
          "İstek reddedildi",
          "Cross-site giriş isteği reddedildi."
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
    return problem(415, "Desteklenmeyen içerik", "İstek JSON olmalıdır.");
  }

  const contentLength = Number(request.headers.get("content-length") ?? "0");
  if (!Number.isFinite(contentLength) || contentLength > maximumRequestBytes) {
    return problem(413, "İstek çok büyük", "Giriş isteği sınırı aşıldı.");
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(503, "API yapılandırılmamış", "Backend API adresi eksik.");
  }

  const body = await request.arrayBuffer();
  if (body.byteLength > maximumRequestBytes) {
    return problem(413, "İstek çok büyük", "Giriş isteği sınırı aşıldı.");
  }

  try {
    const upstream = await fetch(
      new URL("/api/admin/auth/login", apiBaseUrl),
      {
        method: "POST",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json"
        },
        body,
        cache: "no-store",
        signal: request.signal
      }
    );
    const payload = (await upstream.json()) as {
      accessToken?: string;
      expiresAt?: string;
      displayName?: string;
      permissions?: string[];
      detail?: string;
      title?: string;
    };
    const twoFactorPayload = payload as typeof payload & {
      requiresTwoFactor?: boolean;
      twoFactorToken?: string;
      allowedMethods?: string[];
      expiresAt?: string;
    };
    if (upstream.ok && twoFactorPayload.requiresTwoFactor && twoFactorPayload.twoFactorToken) {
      return NextResponse.json({
        requiresTwoFactor: true,
        twoFactorToken: twoFactorPayload.twoFactorToken,
        allowedMethods: twoFactorPayload.allowedMethods ?? ["Totp"],
        expiresAt: twoFactorPayload.expiresAt
      }, { headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" } });
    }
    if (!upstream.ok || !payload.accessToken || !payload.expiresAt) {
      return problem(
        upstream.status,
        payload.title ?? "Giriş başarısız",
        payload.detail ?? "E-posta veya parola geçersiz."
      );
    }

    const expires = new Date(payload.expiresAt);
    const response = NextResponse.json(
      {
        displayName: payload.displayName,
        permissions: payload.permissions
      },
      {
        headers: {
          "Cache-Control": "private, no-store",
          Pragma: "no-cache"
        }
      }
    );
    response.cookies.set(cookieName, payload.accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      expires
    });
    return response;
  } catch {
    return problem(
      502,
      "API bağlantı hatası",
      "Kimlik doğrulama servisine ulaşılamıyor."
    );
  }
}
