import { cookies } from "next/headers";

const accessTokenCookie = "ekiphan_admin_access_token";

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
  const accessToken = (await cookies()).get(accessTokenCookie)?.value;
  if (!accessToken) {
    return problem(
      401,
      "Oturum gerekli",
      "Dashboard için güvenli admin oturumu açılmalıdır."
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

  try {
    const target = new URL("/api/admin/dashboard", apiBaseUrl);
    const upstream = await fetch(target, {
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${accessToken}`
      },
      cache: "no-store"
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
      "Dashboard servisine şu anda ulaşılamıyor."
    );
  }
}

