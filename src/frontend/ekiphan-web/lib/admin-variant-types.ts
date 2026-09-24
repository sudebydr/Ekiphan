export type AdminVariantTranslation = {
  languageCode: "tr" | "en";
  name: string;
};

export type AdminVariantOption = {
  id: string;
  code: string;
  sortOrder: number;
  isActive: boolean;
  translations: AdminVariantTranslation[];
};

export type AdminVariantGroup = {
  id: string;
  code: string;
  sortOrder: number;
  translations: AdminVariantTranslation[];
  options: AdminVariantOption[];
};

export type AdminProductVariant = {
  id: string;
  sku: string;
  sortOrder: number;
  isActive: boolean;
  selectedOptions: Record<string, string>;
  mediaAssetId: string | null;
};

export type AdminProductVariantCatalog = {
  productId: string;
  productSKU: string;
  groups: AdminVariantGroup[];
  variants: AdminProductVariant[];
};
