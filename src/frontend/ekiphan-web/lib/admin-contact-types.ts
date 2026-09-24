export type ContactStatus = 1 | 2 | 3 | 4 | 5;

export type ContactAssignee = { id: string; displayName: string };

export type AdminContactReason = {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  isComplaintReason: boolean;
};

export type AdminComplaintCategory = {
  id: string;
  contactReasonId: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
};

export type AdminContactTaxonomy = {
  reasons: AdminContactReason[];
  complaintCategories: AdminComplaintCategory[];
};

export type AdminContactSummary = {
  id: string;
  createdAt: string;
  fullName: string;
  maskedEmail: string;
  maskedPhone: string | null;
  subject: string;
  messagePreview: string;
  status: ContactStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  updatedAt: string;
  version: string;
};

export type AdminContactPage = {
  items: AdminContactSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminComplaintSummary = {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  companyName: string | null;
  categoryName: string;
  subject: string;
  createdAt: string;
  status: ContactStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
};

export type AdminComplaintPage = {
  items: AdminComplaintSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminContactDetail = {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  companyName: string | null;
  subject: string;
  message: string;
  languageCode: string;
  consentAt: string;
  consentVersion: string;
  status: ContactStatus;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  createdAt: string;
  updatedAt: string;
  version: string;
  reasonName: string | null;
  complaintCategoryName: string | null;
  statusHistory: Array<{
    id: string;
    fromStatus: ContactStatus | null;
    toStatus: ContactStatus;
    changedByUserId: string | null;
    changedByDisplayName: string | null;
    changedAt: string;
  }>;
  internalNotes: Array<{
    id: string;
    text: string;
    authorUserId: string;
    authorDisplayName: string;
    recordedAt: string;
  }>;
};

export const contactStatusLabels: Record<ContactStatus, string> = {
  1: "Yeni",
  2: "Okundu",
  3: "Yanıt bekliyor",
  4: "Tamamlandı",
  5: "Arşivlendi"
};

export const contactStatusOptions = [
  ["New", 1], ["Read", 2], ["AwaitingResponse", 3],
  ["Completed", 4], ["Archived", 5]
] as const;

export const contactTransitions: Record<ContactStatus, ContactStatus[]> = {
  1: [2, 5],
  2: [3, 4, 5],
  3: [2, 4, 5],
  4: [5],
  5: []
};
