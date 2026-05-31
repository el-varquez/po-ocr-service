import { apiRequest } from "./client";
import type { UploadStatus } from "./uploads";

export type UploadHistoryResponse = {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  status: UploadStatus;
  uploadedBy: string;
  uploadedAt: string;
  failureReason: string | null;
  isDeleted: boolean;
  deletedAt: string | null;
  deletedBy: string | null;
};

export type DraftHistoryResponse = {
  id: string;
  uploadFileId: string;
  vendorName: string;
  poDate: string | null;
  referenceNumber: string;
  dateExpected: string | null;
  paymentTerms: string;
  totalAmount: number | null;
  lineCount: number;
  createdAt: string;
  warnings: string[];
  isDeleted: boolean;
  deletedAt: string | null;
  deletedBy: string | null;
};

export async function getUploadHistory(): Promise<UploadHistoryResponse[]> {
  return apiRequest<UploadHistoryResponse[]>("/api/history/uploads");
}

export async function getDraftHistory(): Promise<DraftHistoryResponse[]> {
  return apiRequest<DraftHistoryResponse[]>("/api/history/drafts");
}
