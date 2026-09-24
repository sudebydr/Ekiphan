export type QuoteStatus = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

export type AdminAssignee = { id: string; displayName: string };

export type AdminQuoteSummary = {
  id: string;
  requestNumber: string;
  fullName: string;
  companyName: string;
  maskedEmail: string;
  maskedPhone: string;
  country: string;
  productName: string | null;
  sku: string | null;
  status: QuoteStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  createdAt: string;
  updatedAt: string;
  version: string;
};

export type AdminQuoteItem = {
  id: string;
  productId: string | null;
  variantId: string | null;
  productName: string;
  sku: string;
  brandName: string | null;
  quantity: number;
  variantSnapshot: string | null;
  productNote: string | null;
  imageStorageKey: string | null;
};

export type AdminQuoteStatusHistory = {
  id: string;
  fromStatus: QuoteStatus | null;
  toStatus: QuoteStatus;
  changedAt: string;
  changedByUserId: string | null;
  changedByDisplayName: string | null;
  note: string | null;
};

export type AdminQuoteNote = {
  id: string;
  text: string;
  recordedAt: string;
  authorUserId: string;
  authorDisplayName: string;
};

export type AdminQuoteDetail = {
  id: string;
  requestNumber: string;
  fullName: string;
  companyName: string;
  phone: string;
  email: string;
  country: string;
  city: string | null;
  sector: string | null;
  projectName: string | null;
  message: string | null;
  languageCode: string;
  kvkkConsentAt: string;
  kvkkConsentVersion: string;
  commercialCommunicationConsentAt: string | null;
  commercialCommunicationConsentVersion: string | null;
  status: QuoteStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  createdAt: string;
  updatedAt: string;
  version: string;
  items: AdminQuoteItem[];
  statusHistory: AdminQuoteStatusHistory[];
  internalNotes: AdminQuoteNote[];
};

export type AdminQuotePagedResult = {
  items: AdminQuoteSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type ProblemDetails = { title?: string; detail?: string; status?: number };

export const quoteStatusLabels: Record<QuoteStatus, string> = {
  1: "Yeni",
  2: "İnceleniyor",
  3: "İletişime geçildi",
  4: "Teklif hazırlanıyor",
  5: "Teklif gönderildi",
  6: "Sonuçlandı · kazanıldı",
  7: "Sonuçlandı · kaybedildi",
  8: "İptal / arşiv"
};

export const quoteStatusFilterValues = [
  ["New", 1], ["Reviewing", 2], ["Contacted", 3], ["Preparing", 4],
  ["Sent", 5], ["Won", 6], ["Lost", 7], ["Archived", 8]
] as const;

export const quoteStatusTransitions: Record<QuoteStatus, QuoteStatus[]> = {
  1: [2, 8],
  2: [3, 4, 7, 8],
  3: [4, 7, 8],
  4: [5, 7, 8],
  5: [6, 7, 8],
  6: [8],
  7: [8],
  8: []
};
