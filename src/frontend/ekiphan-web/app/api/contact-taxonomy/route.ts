export async function GET() {
  const base = process.env.EKIPHAN_API_BASE_URL ??
    process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!base) {
    return Response.json(
      { status: 503, detail: "İletişim servisi hazır değil." },
      { status: 503, headers: { "Cache-Control": "no-store" } }
    );
  }
  try {
    const upstream = await fetch(new URL("/api/contact-taxonomy", base), {
      headers: { Accept: "application/json" },
      cache: "no-store"
    });
    return new Response(upstream.body, {
      status: upstream.status,
      headers: {
        "Content-Type": upstream.headers.get("content-type") ?? "application/json",
        "Cache-Control": "no-store"
      }
    });
  } catch {
    return Response.json(
      { status: 502, detail: "İletişim servisine ulaşılamıyor." },
      { status: 502, headers: { "Cache-Control": "no-store" } }
    );
  }
}
