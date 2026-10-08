export type CatalogBrand = {
  id: string;
  name: string;
  slug: string | null;
};

export type CatalogCategory = {
  id: string;
  name: string;
  slug: string;
};

export type CatalogProductSummary = {
  id: string;
  sku: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  brand: CatalogBrand | null;
  primaryCategory: CatalogCategory | null;
  updatedAt: string;
  image: CatalogImage | null;
};

export type CatalogImage = {
  id: string;
  url: string;
  altText: string;
};

export type CatalogDocument = {
  id: string;
  url: string;
  title: string;
};

export type CatalogBrandListItem = {
  id: string;
  name: string;
  slug: string;
  description: string;
  websiteUrl: string | null;
  logo: CatalogImage | null;
  updatedAt: string;
};

export type CatalogBrandDetail = CatalogBrandListItem & {
  catalogs: CatalogDocument[];
  categories: CatalogCategory[];
  products: CatalogProductSummary[];
};

export type CatalogPagedResult = {
  items: CatalogProductSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type CatalogSitemapEntry = {
  slug: string;
  updatedAt: string;
  kind: "product" | "category";
};

export type CatalogNavigation = {
  usageAreas?: Array<{ id: string; code: string; name: string; slug: string }>;
  sections: Array<{
    id: string;
    code: string;
    name: string;
    slug: string;
  }>;
  categories: Array<{
    id: string;
    sectionId: string;
    parentId: string | null;
    name: string;
    slug: string;
    metaTitle: string | null;
    metaDescription: string | null;
    canonicalUrl: string | null;
    noIndex: boolean;
    noFollow: boolean;
    openGraphTitle: string | null;
    openGraphDescription: string | null;
    openGraphImageUrl: string | null;
  }>;
  brands: CatalogBrand[];
  tags: Array<{
    id: string;
    code: string;
    name: string;
    slug: string;
  }>;
};

export type CatalogFacetOption = {
  value: string;
  label: string;
};

export type CatalogFacet = {
  attributeId: string;
  code: string;
  name: string;
  dataType: number;
  unitSymbol: string | null;
  minimum: number | null;
  maximum: number | null;
  options: CatalogFacetOption[];
};

export type CatalogAttribute = {
  id: string;
  attributeId: string;
  name: string;
  dataType: number;
  sequence: number;
  textValue: string | null;
  numericValue: number | null;
  booleanValue: boolean | null;
  optionId: string | null;
  optionName: string | null;
  unitId: string | null;
  unitSymbol: string | null;
};

export type CatalogProductDetail = {
  id: string;
  sku: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  longDescription: string | null;
  brand: CatalogBrand | null;
  categories: CatalogCategory[];
  tags: CatalogNavigation["tags"];
  attributes: CatalogAttribute[];
  similarProducts: CatalogProductSummary[];
  complementaryProducts: CatalogProductSummary[];
  images: CatalogImage[];
  updatedAt: string;
  metaTitle: string | null;
  metaDescription: string | null;
  canonicalUrl: string | null;
  noIndex: boolean;
  noFollow: boolean;
  openGraphTitle: string | null;
  openGraphDescription: string | null;
  openGraphImageUrl: string | null;
  alternates: Array<{ languageCode: "tr" | "en"; slug: string }> | null;
};
