export interface SiteSettingsDto {
  phone: string;
  fax: string;
  contactEmail: string;
  showroomAddress: string;
  factoryAddress: string;
  warehouseAddress: string;
  instagramUrl: string | null;
  linkedInUrl: string | null;
  youTubeUrl: string | null;
  companyTitle: string;
  companySlogan: string;
  showroomTourUrl: string | null;
  rowVersion: string;
}

export async function getPublicSettings(): Promise<SiteSettingsDto | null> {
  const base = process.env.EKIPHAN_API_BASE_URL;
  if (!base) return null;

  try {
    const response = await fetch(`${base}/api/public/settings`, {
      cache: "no-store",
      signal: AbortSignal.timeout(8_000)
    });
    if (!response.ok) return null;
    return (await response.json()) as SiteSettingsDto;
  } catch {
    return null;
  }
}
