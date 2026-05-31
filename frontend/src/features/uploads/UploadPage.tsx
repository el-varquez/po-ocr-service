import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  ExternalLink,
  FileText,
  Loader2,
  Trash2,
  UploadCloud,
  XCircle,
} from "lucide-react";
import {
  getDrafts,
  type DraftDetailResponse,
  type DraftListResponse,
} from "../../api/drafts";
import { queueExtraction } from "../../api/extraction";
import {
  deleteUpload,
  getUploads,
  uploadFiles,
  type UploadResponse,
  type UploadStatus,
} from "../../api/uploads";
import { ConfirmDialog } from "../../components/ConfirmDialog";
import { Modal } from "../../components/Modal";
import { ToastAlert } from "../../components/ToastAlert";
import { DraftPreview } from "../drafts/DraftPreview";

const statusStyles: Record<UploadStatus, string> = {
  PendingExtraction: "bg-slate-100 text-slate-700",
  QueuedForExtraction: "bg-amber-100 text-amber-800",
  Extracting: "bg-blue-100 text-blue-800",
  NeedsReview: "bg-emerald-100 text-emerald-800",
  Saved: "bg-slate-200 text-slate-700",
  Failed: "bg-red-100 text-red-800",
};

export function UploadPage() {
  const [uploads, setUploads] = useState<UploadResponse[]>([]);
  const [drafts, setDrafts] = useState<DraftListResponse[]>([]);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [selectedUploadIds, setSelectedUploadIds] = useState<string[]>([]);
  const [selectedDraftId, setSelectedDraftId] = useState<string | null>(null);
  const [uploadToDelete, setUploadToDelete] = useState<UploadResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [isQueueingExtraction, setIsQueueingExtraction] = useState(false);
  const [deletingUploadId, setDeletingUploadId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const selectedFileNames = useMemo(
    () => selectedFiles.map((file) => file.name).join(", "),
    [selectedFiles],
  );

  const extractableUploads = uploads.filter(
    (upload) =>
      upload.status === "PendingExtraction" || upload.status === "Failed",
  );

  const selectedExtractableUploadIds = selectedUploadIds.filter((uploadId) =>
    extractableUploads.some((upload) => upload.id === uploadId),
  );

  const isAllVisibleExtractableSelected =
    extractableUploads.length > 0 &&
    extractableUploads.every((upload) => selectedUploadIds.includes(upload.id));

  const hasActiveExtraction = uploads.some(
    (upload) =>
      upload.status === "QueuedForExtraction" || upload.status === "Extracting",
  );

  async function loadDrafts() {
    try {
      const result = await getDrafts();
      setDrafts(result);
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to load drafts.");
    }
  }

  function getDraftForUpload(uploadId: string) {
    return drafts.find((draft) => draft.uploadFileId === uploadId) ?? null;
  }

  async function loadUploads() {
    setError(null);

    try {
      const result = await getUploads();
      setUploads(result);
      await loadDrafts();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to load uploads.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleUpload() {
    if (selectedFiles.length === 0) {
      setError("Select at least one file first.");
      return;
    }

    setIsUploading(true);
    setError(null);

    try {
      await uploadFiles(selectedFiles);
      setSelectedFiles([]);
      await loadUploads();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Upload failed.");
    } finally {
      setIsUploading(false);
    }
  }

  function toggleUploadSelection(uploadId: string) {
    setSelectedUploadIds((current) =>
      current.includes(uploadId)
        ? current.filter((id) => id !== uploadId)
        : [...current, uploadId],
    );
  }

  function toggleSelectAllExtractable() {
    setSelectedUploadIds((current) => {
      if (isAllVisibleExtractableSelected) {
        return current.filter(
          (uploadId) =>
            !extractableUploads.some((upload) => upload.id === uploadId),
        );
      }

      const next = new Set(current);
      extractableUploads.forEach((upload) => next.add(upload.id));
      return Array.from(next);
    });
  }

  async function handleQueueExtraction() {
    if (selectedExtractableUploadIds.length === 0) {
      setError("Select at least one pending or failed upload to extract.");
      return;
    }

    setIsQueueingExtraction(true);
    setError(null);

    try {
      await queueExtraction(selectedExtractableUploadIds);
      setSelectedUploadIds([]);
      await loadUploads();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to queue extraction.");
    } finally {
      setIsQueueingExtraction(false);
    }
  }

  function requestDeleteUpload(upload: UploadResponse) {
    if (
      upload.status === "QueuedForExtraction" ||
      upload.status === "Extracting"
    ) {
      setError("Upload cannot be deleted while extraction is running.");
      return;
    }

    setUploadToDelete(upload);
  }

  async function handleDeleteUpload() {
    if (!uploadToDelete) {
      return;
    }

    setDeletingUploadId(uploadToDelete.id);
    setError(null);

    try {
      await deleteUpload(uploadToDelete.id);
      setSelectedUploadIds((current) =>
        current.filter((id) => id !== uploadToDelete.id),
      );
      setUploadToDelete(null);
      await loadUploads();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to delete upload.");
    } finally {
      setDeletingUploadId(null);
    }
  }

  useEffect(() => {
    void loadUploads();
  }, []);

  useEffect(() => {
    if (!hasActiveExtraction) {
      return;
    }

    const intervalId = window.setInterval(() => {
      void loadUploads();
    }, 3000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [hasActiveExtraction]);

  return (
    <div className="grid gap-6">
      <section className="rounded-lg border border-slate-200 bg-white p-5">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">
              Upload PO Files
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Add image files first. Extraction will be queued in the next
              stage.
            </p>
          </div>

          <button
            type="button"
            onClick={handleUpload}
            disabled={isUploading || selectedFiles.length === 0}
            className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg bg-blue-700 px-4 text-sm font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {isUploading ? (
              <Loader2 className="animate-spin" size={18} aria-hidden="true" />
            ) : (
              <UploadCloud size={18} aria-hidden="true" />
            )}
            Upload
          </button>
        </div>

        <label className="mt-5 flex min-h-36 cursor-pointer flex-col items-center justify-center rounded-lg border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center hover:bg-slate-100">
          <UploadCloud
            size={32}
            className="text-slate-500"
            aria-hidden="true"
          />
          <span className="mt-3 text-sm font-semibold text-slate-800">
            Choose PO image files
          </span>
          <span className="mt-1 max-w-md text-sm text-slate-500">
            {selectedFiles.length > 0
              ? selectedFileNames
              : "PNG, JPG, or other image files supported by OCR"}
          </span>
          <input
            type="file"
            multiple
            className="sr-only"
            accept="image/*"
            onChange={(event) => {
              setSelectedFiles(Array.from(event.target.files ?? []));
            }}
          />
        </label>

        {error && (
          <ToastAlert
            title="Action needed"
            message={error}
            variant="error"
            onDismiss={() => setError(null)}
          />
        )}
      </section>

      <section className="rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">
              Uploaded Files
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {hasActiveExtraction
                ? "Extraction is running. This list refreshes automatically."
                : "Recent uploaded purchase order files."}
            </p>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => void handleQueueExtraction()}
              disabled={
                isQueueingExtraction ||
                selectedExtractableUploadIds.length === 0
              }
              className="inline-flex min-h-9 items-center gap-2 rounded-lg bg-blue-700 px-3 text-sm font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:bg-slate-300"
            >
              {isQueueingExtraction && (
                <Loader2
                  className="animate-spin"
                  size={16}
                  aria-hidden="true"
                />
              )}
              Extract
            </button>

            <button
              type="button"
              onClick={() => void loadUploads()}
              className="min-h-9 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              Refresh
            </button>
          </div>
        </div>

        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center gap-2 text-sm text-slate-500">
            <Loader2 className="animate-spin" size={18} aria-hidden="true" />
            Loading uploads
          </div>
        ) : uploads.length === 0 ? (
          <div className="grid min-h-48 place-items-center px-5 py-8 text-center">
            <div>
              <FileText
                className="mx-auto text-slate-400"
                size={34}
                aria-hidden="true"
              />
              <p className="mt-3 text-sm font-semibold text-slate-700">
                No uploads yet
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Upload a PO image to start.
              </p>
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px] text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                <tr>
                  <th className="w-12 px-5 py-3">
                    <input
                      type="checkbox"
                      checked={isAllVisibleExtractableSelected}
                      disabled={extractableUploads.length === 0}
                      onChange={toggleSelectAllExtractable}
                      aria-label="Select all extractable uploads"
                      className="h-4 w-4 rounded border-slate-300"
                    />
                  </th>
                  <th className="px-5 py-3 font-semibold">File</th>
                  <th className="px-5 py-3 font-semibold">Status</th>
                  <th className="px-5 py-3 font-semibold">Size</th>
                  <th className="px-5 py-3 font-semibold">Uploaded</th>
                  <th className="px-5 py-3 font-semibold">Issue</th>
                  <th className="px-5 py-3 font-semibold">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {uploads.map((upload) => {
                  const draft = getDraftForUpload(upload.id);

                  return (
                  <tr key={upload.id}>
                    <td className="px-5 py-4">
                      <input
                        type="checkbox"
                        checked={selectedUploadIds.includes(upload.id)}
                        disabled={
                          upload.status !== "PendingExtraction" &&
                          upload.status !== "Failed"
                        }
                        onChange={() => toggleUploadSelection(upload.id)}
                        aria-label={`Select ${upload.originalFileName}`}
                        className="h-4 w-4 rounded border-slate-300"
                      />
                    </td>
                    <td className="px-5 py-4 font-medium text-slate-900">
                      {upload.originalFileName}
                    </td>
                    <td className="px-5 py-4">
                      <span
                        className={`inline-flex min-h-7 items-center gap-1 rounded-full px-2.5 text-xs font-semibold ${getUploadStatusStyle(upload, draft)}`}
                      >
                        <UploadStatusIcon upload={upload} draft={draft} />
                        {upload.status}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {formatFileSize(upload.sizeBytes)}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {formatDateTime(upload.uploadedAt)}
                    </td>
                    <td className="px-5 py-4">
                      <UploadIssue
                        upload={upload}
                        draft={draft}
                      />
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-2">
                        {upload.status === "NeedsReview" &&
                        draft ? (
                          <button
                            type="button"
                            onClick={() => {
                              if (draft) {
                                setSelectedDraftId(draft.id);
                              }
                            }}
                            className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-slate-300 px-2.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                          >
                            <ExternalLink size={14} aria-hidden="true" />
                            Open Draft
                          </button>
                        ) : null}

                        <button
                          type="button"
                          onClick={() => requestDeleteUpload(upload)}
                          disabled={
                            deletingUploadId === upload.id ||
                            upload.status === "QueuedForExtraction" ||
                            upload.status === "Extracting"
                          }
                          className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-red-200 px-2.5 text-xs font-semibold text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:border-slate-200 disabled:text-slate-400 disabled:hover:bg-white"
                        >
                          {deletingUploadId === upload.id ? (
                            <Loader2
                              className="animate-spin"
                              size={14}
                              aria-hidden="true"
                            />
                          ) : (
                            <Trash2 size={14} aria-hidden="true" />
                          )}
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {selectedDraftId && (
        <Modal title="Draft review" onClosed={() => setSelectedDraftId(null)}>
          {(close) => (
            <DraftPreview
              draftId={selectedDraftId}
              onClose={close}
              onSaved={(savedDraft: DraftDetailResponse) => {
                setDrafts((current) =>
                  current.map((draft) =>
                    draft.id === savedDraft.id
                      ? {
                          id: savedDraft.id,
                          uploadFileId: savedDraft.uploadFileId,
                          vendorName: savedDraft.vendorName,
                          poDate: savedDraft.poDate,
                          referenceNumber: savedDraft.referenceNumber,
                          dateExpected: savedDraft.dateExpected,
                          paymentTerms: savedDraft.paymentTerms,
                          totalAmount: savedDraft.totalAmount,
                          lineCount: savedDraft.lines.length,
                          createdAt: savedDraft.createdAt,
                          warnings: savedDraft.warnings,
                        }
                      : draft,
                  ),
                );
              }}
            />
          )}
        </Modal>
      )}

      {uploadToDelete && (
        <ConfirmDialog
          title="Delete upload?"
          message={`This will remove ${uploadToDelete.originalFileName} from the upload list. The original record stays in history as soft deleted.`}
          confirmLabel="Delete"
          isBusy={deletingUploadId === uploadToDelete.id}
          variant="danger"
          onCancel={() => setUploadToDelete(null)}
          onConfirm={() => void handleDeleteUpload()}
        />
      )}
    </div>
  );
}

function formatFileSize(sizeBytes: number) {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`;
  }

  if (sizeBytes < 1024 * 1024) {
    return `${(sizeBytes / 1024).toFixed(1)} KB`;
  }

  return `${(sizeBytes / 1024 / 1024).toFixed(1)} MB`;
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function UploadIssue({
  upload,
  draft,
}: {
  upload: UploadResponse;
  draft: DraftListResponse | null;
}) {
  if (upload.failureReason) {
    return (
      <span className="inline-flex min-h-7 max-w-md items-center rounded-full bg-red-100 px-2.5 text-xs font-semibold text-red-800">
        {upload.failureReason}
      </span>
    );
  }

  if (draft && draft.warnings.length > 0) {
    return (
      <span className="inline-flex min-h-7 items-center rounded-full bg-amber-100 px-2.5 text-xs font-semibold text-amber-800">
        Some fields are missing
      </span>
    );
  }

  return <span className="text-sm text-slate-600">-</span>;
}

function getUploadStatusStyle(
  upload: UploadResponse,
  draft: DraftListResponse | null,
) {
  if (upload.status === "NeedsReview" && draft && draft.warnings.length > 0) {
    return "bg-amber-100 text-amber-800";
  }

  return statusStyles[upload.status];
}

function UploadStatusIcon({
  upload,
  draft,
}: {
  upload: UploadResponse;
  draft: DraftListResponse | null;
}) {
  if (upload.status === "Failed") {
    return <XCircle size={14} aria-hidden="true" />;
  }

  if (upload.status !== "NeedsReview") {
    return null;
  }

  if (draft && draft.warnings.length > 0) {
    return <AlertTriangle size={14} aria-hidden="true" />;
  }

  return <CheckCircle2 size={14} aria-hidden="true" />;
}
