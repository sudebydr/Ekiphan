import { NextRequest, NextResponse } from "next/server";

const cookieName = "ekiphan_admin_access_token";

export async function POST(request: NextRequest): Promise<Response> {
  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) return Response.json({ detail: "Giriş servisine ulaşılamıyor." }, { status: 503 });
  try {
    const payload = await request.json() as { twoFactorToken?: string; code?: string; method?: string };
    if (!payload.twoFactorToken || !payload.code || !payload.method) {
      return Response.json({ detail: "Doğrulama kodu gerekli." }, { status: 400 });
    }
    const upstream = await fetch(new URL("/api/admin/auth/verify-two-factor", apiBaseUrl), {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body: JSON.stringify(payload),
      cache: "no-store",
      signal: request.signal
    });
    const result = await upstream.json() as { accessToken?: string; expiresAt?: string; displayName?: string; permissions?: string[] };
    if (!upstream.ok || !result.accessToken || !result.expiresAt) {
      return Response.json({ detail: "Doğrulama kodu geçersiz veya süresi dolmuş." }, { status: upstream.status || 401 });
    }
    const response = NextResponse.json({ displayName: result.displayName, permissions: result.permissions }, { headers: { "Cache-Control": "private, no-store", Pragma: "no-cache" } });
    response.cookies.set(cookieName, result.accessToken, { httpOnly: true, secure: process.env.EKIPHAN_ALLOW_HTTP_AUTH !== "true" && process.env.NODE_ENV === "production", sameSite: "lax", path: "/", expires: new Date(result.expiresAt) });
    return response;
  } catch {
    return Response.json({ detail: "Giriş servisine ulaşılamıyor." }, { status: 502 });
  }
}
