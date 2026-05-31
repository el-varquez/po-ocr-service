import { apiRequest } from "./client";

export type QueueExtractionRequest = {
    uploadIds: string[];
};

export async function queueExtraction(uploadIds: string[]): Promise<void> {
    await apiRequest<void>("/api/extraction/queue", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            uploadIds,            
        } satisfies QueueExtractionRequest),
    });
}