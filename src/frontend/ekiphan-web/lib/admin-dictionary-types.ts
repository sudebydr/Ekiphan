export type AdminTagTranslation = {
  languageCode: "tr" | "en";
  name: string;
  slug: string;
};

export type AdminTag = {
  id: string;
  code: string;
  isActive: boolean;
  translations: AdminTagTranslation[];
};

export type AdminUnitDefinition = {
  id: string;
  code: string;
  symbol: string;
  dimension: string;
  conversionFactorToBase: number;
  isBaseUnit: boolean;
  isActive: boolean;
};

export type AdminProductTagAssignment = {
  productId: string;
  tagIds: string[];
};

export type AdminDictionaryCatalog = {
  tags: AdminTag[];
  units: AdminUnitDefinition[];
  productTags: AdminProductTagAssignment[];
};
