export type ImportJobStatus =
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8;

export type ImportIssueSeverity = 1 | 2 | 3;

export interface ImportJobSummary {
  id: string;
  originalFileName: string | null;
  sourceType: number;
  status: ImportJobStatus;
  isDryRun: boolean;
  totalRowCount: number;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  publishedRowCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ImportJobDetail extends ImportJobSummary {
  sourceSha256Checksum: string;
  createdByUserId: string | null;
  validationStartedAt: string | null;
  validationCompletedAt: string | null;
  publishingStartedAt: string | null;
  completedAt: string | null;
  failureReason: string | null;
}

export interface ImportIssue {
  id: string;
  sheetName: string;
  rowNumber: number;
  sku: string | null;
  severity: ImportIssueSeverity;
  code: string;
  message: string;
  columnName: string | null;
  rawValue: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ImportUploadResult {
  id: string;
  fileName: string;
  sourceType: number;
  status: ImportJobStatus;
  isDryRun: boolean;
  totalRowCount: number;
  validRowCount: number;
  invalidRowCount: number;
  warningCount: number;
  failureReason: string | null;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

export const statusLabels: Record<ImportJobStatus, string> = {
  1: "Yüklendi",
  2: "Doğrulanıyor",
  3: "Yayına hazır",
  4: "Doğrulama başarısız",
  5: "Yayınlanıyor",
  6: "Tamamlandı",
  7: "Başarısız",
  8: "İptal edildi"
};

export const severityLabels: Record<ImportIssueSeverity, string> = {
  1: "Bilgi",
  2: "Uyarı",
  3: "Hata"
};
