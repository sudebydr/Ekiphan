export type AdminProductTranslation = {
  languageCode: "tr" | "en";
  name: string;
  slug: string;
  shortDescription: string | null;
  longDescription: string | null;
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

export type AdminProductAttributeValue = {
  attributeId: string;
  sequence: number;
  textValue: string | null;
  numericValue: number | null;
  booleanValue: boolean | null;
  attributeOptionId: string | null;
  unitId: string | null;
};

export type AdminProductDetail = {
  id: string;
  sku: string;
  brandId: string | null;
  isPublished: boolean;
  translations: AdminProductTranslation[];
  categoryIds: string[];
  primaryCategoryId: string | null;
  attributeValues: AdminProductAttributeValue[];
  tagIds: string[];
  createdAt: string;
  updatedAt: string;
};

export type SaveAdminProduct = {
  sku: string;
  brandId: string | null;
  isPublished: boolean;
  translations: AdminProductTranslation[];
  categoryIds: string[];
  primaryCategoryId: string | null;
  attributeValues: AdminProductAttributeValue[];
  tagIds: string[];
};
