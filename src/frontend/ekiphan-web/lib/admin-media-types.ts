export type AdminMediaTranslation = {
  languageCode: "tr" | "en";
  title: string;
  altText: string | null;
  description: string | null;
};

export type AdminMediaAsset = {
  id: string;
  assetType: "Image" | "Pdf" | "Document" | "ExternalVideo";
  status: "Active" | "Archived";
  originalFileName: string | null;
  mimeType: string | null;
  fileSizeBytes: number | null;
  url: string | null;
  translations: AdminMediaTranslation[];
  createdAt: string;
};

export type AdminMediaAssignment = {
  targetType: "product" | "brand" | "category" | "variant" |
    "homepage-hero" | "gallery" | "press-release";
  targetId: string;
  mediaAssetId: string;
  role: string;
  isDefault: boolean;
  sortOrder: number;
};

export type AdminMediaLibrary = {
  assets: AdminMediaAsset[];
  assignments: AdminMediaAssignment[];
  products: AdminMediaTarget[];
  brands: AdminMediaTarget[];
  categories: AdminMediaTarget[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminMediaTarget = {
  id: string;
  name: string;
  code: string | null;
};
