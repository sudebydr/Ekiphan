export type AdminSectionTranslation = {
  languageCode: "tr" | "en";
  name: string;
  slug: string;
};

export type AdminSectionDetail = {
  id: string;
  code: string;
  isPublished: boolean;
  sortOrder: number;
  translations: AdminSectionTranslation[];
};

export type AdminCategoryTranslation = {
  languageCode: "tr" | "en";
  name: string;
  slug: string;
  description: string | null;
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  noIndex: boolean;
  noFollow: boolean;
  openGraphTitle: string | null;
  openGraphDescription: string | null;
  openGraphImageMediaId: string | null;
  openGraphImageUrl?: string | null;
};

export type AdminCategoryDetail = {
  id: string;
  productSectionId: string;
  parentId: string | null;
  isPublished: boolean;
  sortOrder: number;
  productCount: number;
  translations: AdminCategoryTranslation[];
};

export type AdminCatalogStructure = {
  sections: AdminSectionDetail[];
  categories: AdminCategoryDetail[];
};
