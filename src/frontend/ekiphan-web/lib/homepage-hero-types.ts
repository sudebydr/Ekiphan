export type HomepageHeroTranslation = {
  languageCode: "tr" | "en";
  title: string;
  subtitle: string | null;
  primaryCtaLabel: string;
  primaryCtaUrl: string;
  secondaryCtaLabel: string | null;
  secondaryCtaUrl: string | null;
};

export type AdminHomepageHero = {
  id: string;
  desktopMediaId: string | null;
  mobileMediaId: string | null;
  sortOrder: number;
  isPublished: boolean;
  startsAt: string | null;
  endsAt: string | null;
  translations: HomepageHeroTranslation[];
};

export type PublicHomepageHero = {
  id: string;
  title: string;
  subtitle: string | null;
  primaryCtaLabel: string;
  primaryCtaUrl: string;
  secondaryCtaLabel: string | null;
  secondaryCtaUrl: string | null;
  desktopMediaUrl: string | null;
  mobileMediaUrl: string | null;
};
