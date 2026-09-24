export type AdminAttributeTranslation = {
  languageCode: "tr" | "en";
  name: string;
};

export type AdminAttributeOption = {
  id: string;
  code: string;
  sortOrder: number;
  isActive: boolean;
  translations: AdminAttributeTranslation[];
};

export type AdminAttribute = {
  id: string;
  code: string;
  dataType: "Text" | "Number" | "Boolean" | "Option" | "MultiOption";
  unitDimension: string | null;
  isActive: boolean;
  translations: AdminAttributeTranslation[];
  options: AdminAttributeOption[];
};

export type AdminCategoryAttribute = {
  categoryId: string;
  attributeId: string;
  isRequired: boolean;
  isFilterable: boolean;
  isVisibleOnProduct: boolean;
  isVisibleOnComparison: boolean;
  sortOrder: number;
};

export type AdminAttributeCatalog = {
  attributes: AdminAttribute[];
  assignments: AdminCategoryAttribute[];
  units: AdminUnit[];
};

export type AdminUnit = {
  id: string;
  code: string;
  symbol: string;
  dimension: string;
  isActive: boolean;
};
