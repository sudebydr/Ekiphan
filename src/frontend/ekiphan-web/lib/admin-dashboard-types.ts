export type AdminDashboardProduct = {
  id: string;
  sku: string;
  name: string;
  isPublished: boolean;
  updatedAt: string;
};

export type AdminDashboardQuote = {
  id: string;
  requestNumber: string;
  companyName: string;
  status: string;
  itemCount: number;
  createdAt: string;
};

export type AdminDashboard = {
  generatedAt: string;
  products: {
    total: number;
    published: number;
    unpublished: number;
    missingGalleryImage: number;
    missingTurkishTranslation: number;
    missingEnglishTranslation: number;
  } | null;
  totalCategories: number | null;
  totalBrands: number | null;
  quotes: {
    new: number;
    inProgress: number;
    total: number;
  } | null;
  newContactRequests: number | null;
  totalMediaFiles: number | null;
  contentUpdatesLast30Days: number | null;
  recentQuotes: AdminDashboardQuote[] | null;
  recentProducts: AdminDashboardProduct[] | null;
  productsMissingImages: AdminDashboardProduct[] | null;
  productsMissingEnglishContent: AdminDashboardProduct[] | null;
  latestImport: {
    id: string;
    status: string;
    sourceType: string;
    originalFileName: string | null;
    totalRowCount: number;
    invalidRowCount: number;
    warningCount: number;
    createdAt: string;
  } | null;
  system: {
    apiOperational: boolean;
    databaseReachable: boolean;
    databaseLatencyMilliseconds: number;
  };
  warnings: string[];
};
