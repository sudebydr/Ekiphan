export type PressReleaseTranslation = {
  languageCode: "tr" | "en";
  title: string;
  summary: string;
  body?: string | null;
  slug: string;
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  noIndex: boolean;
  noFollow: boolean;
  openGraphTitle: string | null;
  openGraphDescription: string | null;
  openGraphImageMediaId: string | null;
};

export type AdminPressRelease = {
  id: string;
  coverMediaId?: string | null;
  attachmentMediaId?: string | null;
  publishedAt: string;
  isPublished: boolean;
  translations: PressReleaseTranslation[];
};
