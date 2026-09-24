export type AdminCatalogProduct = {
  id: string;
  sku: string;
  name: string;
  isPublished: boolean;
  brandId: string | null;
  brandName: string | null;
  primaryCategoryId: string | null;
  primaryCategoryName: string | null;
  missingGalleryImage: boolean;
  missingEnglishContent: boolean;
  updatedAt: string;
};

export type AdminCatalogProductPage = {
  items: AdminCatalogProduct[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type ProductRelationType =
  | "Similar"
  | "Complementary"
  | "Accessory"
  | "Alternative";

export type AdminProductRelation = {
  id: string;
  sourceProductId: string;
  targetProductId: string;
  relatedProductId: string;
  relatedSKU: string;
  relatedName: string;
  relationType: number;
  isBidirectional: boolean;
  isIncoming: boolean;
  sortOrder: number;
};

export type ProblemDetails = {
  title?: string;
  detail?: string;
};

export const relationTypeOptions: Array<{
  value: ProductRelationType;
  numericValue: number;
  label: string;
}> = [
  { value: "Similar", numericValue: 1, label: "Benzer ürün" },
  { value: "Complementary", numericValue: 2, label: "Tamamlayıcı ürün" },
  { value: "Accessory", numericValue: 3, label: "Aksesuar" },
  { value: "Alternative", numericValue: 4, label: "Alternatif ürün" }
];
