import { apiRequest } from "./client";

export type DraftLineResponse = {
    quantity: number;
    itemCode: string;
    description: string;
    unitPrice: number;
    amount: number;
};

export type DraftListResponse = {
    id: string;
    uploadFileI: string;
    vendorName: string;
    poDate: string | null;
    referenceNumber: string;
    dateExpected: string | null;
    paymentTerms: string;
    totalAmount: number | null,
    lineCount: number;
    createdAt: string;
    warnings: string[];
};

export type DraftDetailResponse = DraftListResponse & {
    shipTo: string;
    shipVia: string;
    lines: DraftLineResponse[];
};

export type DraftLineUpdateRequest = DraftLineResponse;

export type DraftUpdateRequest = {
    vendorName: string;
    poDate: string | null;
    referenceNumber: string;
    dateExpected: string | null;
    shipTo: string;
    shipVia: string;
    paymentTerms: string;
    totalAmount: number | null;
    lines: DraftLineUpdateRequest[];
};

export async function getDrafts(): Promise<DraftListResponse[]> {
    return apiRequest<DraftListResponse[]>("/api/drafts");
}

export async function getDraft(draftId: string): Promise<DraftDetailResponse> {
return apiRequest<DraftDetailResponse>(`/api/drafts/${draftId}`);
}

export async function saveDraft(
    draftId: string,
    request: DraftUpdateRequest,): Promise<DraftDetailResponse> {
        return apiRequest<DraftDetailResponse>(`/api/drafts/${draftId}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        }
    );
}