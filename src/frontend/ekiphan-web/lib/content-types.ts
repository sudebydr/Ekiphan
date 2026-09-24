export type ContentTranslation = {
  languageCode: "tr" | "en";
  title: string;
  slug: string;
  summary: string | null;
  body: string;
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  noIndex: boolean;
  noFollow: boolean;
  openGraphTitle: string | null;
  openGraphDescription: string | null;
  openGraphImageMediaId: string | null;
};

export type AdminContentPage = {
  id: string;
  code: string;
  status: number;
  publishedAt: string | null;
  translations: ContentTranslation[];
  createdAt: string;
  updatedAt: string;
};

export type PublicContentPage = {
  id: string;
  code: string;
  languageCode: string;
  title: string;
  slug: string;
  summary: string | null;
  body: string;
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  noIndex: boolean;
  noFollow: boolean;
  updatedAt: string;
  openGraphTitle: string | null;
  openGraphDescription: string | null;
  openGraphImageMediaId: string | null;
  openGraphImageUrl: string | null;
  alternates: Array<{ languageCode: "tr" | "en"; slug: string }> | null;
};

export type PublicContentSitemapEntry = {
  slug: string;
  updatedAt: string;
};
