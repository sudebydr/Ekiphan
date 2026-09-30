export type ProductBulkPreview = {
  totalProducts: number;
  eligibleProducts: number;
  skippedProducts: number;
  invalidProducts: number;
  warnings: string[];
  validationErrors: string[];
  affectedProductIds: string[];
  previewToken: string;
  expiresAt: string;
};

export type ProductBulkSelection = {
  productIds: string[];
};

export type ProductBulkOperation = {
  operationId: string;
  operationType: number;
  status: number;
  totalCount: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  progressPercentage: number;
  errorMessage: string | null;
};

export type ProductBulkExecution = {
  bulkOperationId: string;
  status: number;
  isBackgroundJob: boolean;
  totalCount: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  errorMessage: string | null;
};
