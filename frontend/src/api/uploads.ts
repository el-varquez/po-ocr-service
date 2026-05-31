import { apiRequest } from "./client";

export type UploadStatus = 
    | "PendingExtraction"
    | "QueuedForExtraction"
    | "Extracting"
    | "NeedsReview"
    | "Failed";

export type UploadResponse = {
    id: string;
    originalFileName: string;
    contentType: string;
    sizeBytes: number;
    status: UploadStatus;
    uploadedBy: string;
    uploadedAt: string;
    failureReason: string | null;
};

export async function getUploads(): Promise<UploadResponse[]> {
    return apiRequest<UploadResponse[]>("/api/uploads");
}

export async function uploadFiles(files: FileList | File[]): Promise<UploadResponse[]> {
    const formData = new FormData();

    Array.from(files).forEach((file) => {
        formData.append("files", file);
    });

    return apiRequest<UploadResponse[]>("/api/uploads", {
        method: "POST",
        body: formData,
    });
}