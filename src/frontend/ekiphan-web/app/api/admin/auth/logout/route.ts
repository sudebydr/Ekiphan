import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest): Promise<Response> {
  const origin = request.headers.get("origin");
  if (origin) {
    try {
      const host = request.headers.get("host");
      if (!host || new URL(origin).host !== host) {
        return Response.json(
          { status: 403, title: "İstek reddedildi" },
          { status: 403 }
        );
      }
    } catch {
      return Response.json(
        { status: 403, title: "Geçersiz Origin" },
        { status: 403 }
      );
    }
  }

  const accessToken = request.cookies.get(
    "ekiphan_admin_access_token"
  )?.value;
  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  let upstreamStatus = 200;
  if (accessToken && apiBaseUrl) {
    try {
      const upstream = await fetch(
        new URL("/api/admin/auth/logout", apiBaseUrl),
        {
          method: "POST",
          headers: {
            Accept: "application/json",
            Authorization: `Bearer ${accessToken}`
          },
          cache: "no-store",
          signal: request.signal
        }
      );
      upstreamStatus = upstream.status;
    } catch {
      upstreamStatus = 502;
    }
  } else if (accessToken && !apiBaseUrl) {
    upstreamStatus = 503;
  }

  const response = NextResponse.json(
    upstreamStatus >= 500
      ? {
          signedOut: true,
          warning: "Sunucu oturumu doğrulanamadı; yerel oturum kapatıldı."
        }
      : { signedOut: true },
    {
      status: upstreamStatus >= 500 ? upstreamStatus : 200,
      headers: {
        "Cache-Control": "private, no-store",
        Pragma: "no-cache"
      }
    }
  );
  response.cookies.set("ekiphan_admin_access_token", "", {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: 0
  });
  return response;
}
