export type AdminBrandTranslation = {
  languageCode: "tr" | "en";
  description: string;
  slug: string;
};

export type AdminBrandSummary = {
  id: string;
  name: string;
  slug: string;
  websiteUrl: string | null;
  isPublished: boolean;
  sortOrder: number;
};

export type AdminBrandPage = {
  items: AdminBrandSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminBrandDetail = {
  id: string;
  name: string;
  websiteUrl: string | null;
  isPublished: boolean;
  sortOrder: number;
  translations: AdminBrandTranslation[];
};
