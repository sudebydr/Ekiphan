import { cookies } from "next/headers";
import { NextResponse } from "next/server";

const cookieName = "ekiphan_admin_access_token";

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

export async function GET(): Promise<Response> {
  const accessToken = (await cookies()).get(cookieName)?.value;
  if (!accessToken) {
    return problem(401, "Oturum gerekli", "Geçerli bir admin oturumu bulunamadı.");
  }

  const apiBaseUrl = process.env.EKIPHAN_API_BASE_URL;
  if (!apiBaseUrl) {
    return problem(
      503,
      "API yapılandırılmamış",
      "Sunucu tarafı backend API adresi eksik."
    );
  }

  try {
    const upstream = await fetch(
      new URL("/api/admin/auth/session", apiBaseUrl),
      {
        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${accessToken}`
        },
        cache: "no-store"
      }
    );
    const body = await upstream.text();
    const response = new NextResponse(body || null, {
      status: upstream.status,
      headers: {
        "Cache-Control": "private, no-store",
        "Content-Type":
          upstream.headers.get("content-type") ??
          "application/problem+json",
        Pragma: "no-cache"
      }
    });
    if (upstream.status === 401) {
      response.cookies.set(cookieName, "", {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: 0
      });
    }

    return response;
  } catch {
    return problem(
      502,
      "API bağlantı hatası",
      "Admin oturumu doğrulama servisine ulaşılamıyor."
    );
  }
}
