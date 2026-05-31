import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { Archive, FileText, Loader2, RefreshCw, UploadCloud } from "lucide-react";
import {
  getDraftHistory,
  getUploadHistory,
  type DraftHistoryResponse,
  type UploadHistoryResponse,
} from "../../api/history";
import { ToastAlert } from "../../components/ToastAlert";

type HistoryTab = "uploads" | "drafts";
type RecordFilter = "all" | "active" | "deleted";
type ToastState = {
  title: string;
  message: string;
  variant: "error";
};

export function HistoryPage() {
  const [activeTab, setActiveTab] = useState<HistoryTab>("uploads");
  const [recordFilter, setRecordFilter] = useState<RecordFilter>("all");
  const [uploads, setUploads] = useState<UploadHistoryResponse[]>([]);
  const [drafts, setDrafts] = useState<DraftHistoryResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [toast, setToast] = useState<ToastState | null>(null);

  const filteredUploads = useMemo(
    () => uploads.filter((upload) => matchesFilter(upload.isDeleted, recordFilter)),
    [uploads, recordFilter],
  );

  const filteredDrafts = useMemo(
    () => drafts.filter((draft) => matchesFilter(draft.isDeleted, recordFilter)),
    [drafts, recordFilter],
  );

  async function loadHistory() {
    setIsLoading(true);
    setToast(null);

    try {
      const [uploadResult, draftResult] = await Promise.all([
        getUploadHistory(),
        getDraftHistory(),
      ]);
      setUploads(uploadResult);
      setDrafts(draftResult);
    } catch (ex) {
      setToast({
        title: "Unable to load history",
        message: ex instanceof Error ? ex.message : "Unable to load history.",
        variant: "error",
      });
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadHistory();
  }, []);

  return (
    <div className="grid gap-6">
      <section className="rounded-lg border border-slate-200 bg-white">
        <div className="flex flex-col gap-4 border-b border-slate-200 px-5 py-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">History</h2>
            <p className="mt-1 text-sm text-slate-500">
              Read-only records from uploads and drafts, including deleted items.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <div className="inline-flex rounded-lg border border-slate-300 bg-white p-1">
              <SegmentButton
                active={activeTab === "uploads"}
                onClick={() => setActiveTab("uploads")}
              >
                <UploadCloud size={15} aria-hidden="true" />
                Uploads
              </SegmentButton>
              <SegmentButton
                active={activeTab === "drafts"}
                onClick={() => setActiveTab("drafts")}
              >
                <FileText size={15} aria-hidden="true" />
                Drafts
              </SegmentButton>
            </div>

            <div className="inline-flex rounded-lg border border-slate-300 bg-white p-1">
              <FilterButton
                active={recordFilter === "all"}
                onClick={() => setRecordFilter("all")}
              >
                All
              </FilterButton>
              <FilterButton
                active={recordFilter === "active"}
                onClick={() => setRecordFilter("active")}
              >
                Active
              </FilterButton>
              <FilterButton
                active={recordFilter === "deleted"}
                onClick={() => setRecordFilter("deleted")}
              >
                Deleted
              </FilterButton>
            </div>

            <button
              type="button"
              onClick={() => void loadHistory()}
              className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              <RefreshCw size={16} aria-hidden="true" />
              Refresh
            </button>
          </div>
        </div>

        {toast && (
          <ToastAlert
            title={toast.title}
            message={toast.message}
            variant={toast.variant}
            onDismiss={() => setToast(null)}
          />
        )}

        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center gap-2 text-sm text-slate-500">
            <Loader2 className="animate-spin" size={18} aria-hidden="true" />
            Loading history
          </div>
        ) : activeTab === "uploads" ? (
          <UploadHistoryTable uploads={filteredUploads} />
        ) : (
          <DraftHistoryTable drafts={filteredDrafts} />
        )}
      </section>
    </div>
  );
}

function UploadHistoryTable({ uploads }: { uploads: UploadHistoryResponse[] }) {
  if (uploads.length === 0) {
    return <EmptyHistory label="No upload history found" />;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[1080px] text-left text-sm">
        <thead className="bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th className="px-5 py-3 font-semibold">File</th>
            <th className="px-5 py-3 font-semibold">Status</th>
            <th className="px-5 py-3 font-semibold">Record</th>
            <th className="px-5 py-3 font-semibold">Size</th>
            <th className="px-5 py-3 font-semibold">Uploaded</th>
            <th className="px-5 py-3 font-semibold">Uploaded By</th>
            <th className="px-5 py-3 font-semibold">Deleted</th>
            <th className="px-5 py-3 font-semibold">Issue</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {uploads.map((upload) => (
            <tr key={upload.id}>
              <td className="px-5 py-4 font-medium text-slate-900">
                {upload.originalFileName}
              </td>
              <td className="px-5 py-4 text-slate-600">{upload.status}</td>
              <td className="px-5 py-4">
                <RecordBadge isDeleted={upload.isDeleted} />
              </td>
              <td className="px-5 py-4 text-slate-600">
                {formatFileSize(upload.sizeBytes)}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {formatDateTime(upload.uploadedAt)}
              </td>
              <td className="px-5 py-4 text-slate-600">{upload.uploadedBy}</td>
              <td className="px-5 py-4 text-slate-600">
                {formatDeleted(upload.deletedAt, upload.deletedBy)}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {upload.failureReason ?? "-"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function DraftHistoryTable({ drafts }: { drafts: DraftHistoryResponse[] }) {
  if (drafts.length === 0) {
    return <EmptyHistory label="No draft history found" />;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[1120px] text-left text-sm">
        <thead className="bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th className="px-5 py-3 font-semibold">Vendor</th>
            <th className="px-5 py-3 font-semibold">Reference</th>
            <th className="px-5 py-3 font-semibold">Record</th>
            <th className="px-5 py-3 font-semibold">PO Date</th>
            <th className="px-5 py-3 font-semibold">Expected</th>
            <th className="px-5 py-3 font-semibold">Terms</th>
            <th className="px-5 py-3 font-semibold">Total</th>
            <th className="px-5 py-3 font-semibold">Lines</th>
            <th className="px-5 py-3 font-semibold">Warnings</th>
            <th className="px-5 py-3 font-semibold">Created</th>
            <th className="px-5 py-3 font-semibold">Deleted</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {drafts.map((draft) => (
            <tr key={draft.id}>
              <td className="px-5 py-4 font-medium text-slate-900">
                {draft.vendorName || "-"}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {draft.referenceNumber || "-"}
              </td>
              <td className="px-5 py-4">
                <RecordBadge isDeleted={draft.isDeleted} />
              </td>
              <td className="px-5 py-4 text-slate-600">{draft.poDate ?? "-"}</td>
              <td className="px-5 py-4 text-slate-600">
                {draft.dateExpected ?? "-"}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {draft.paymentTerms || "-"}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {draft.totalAmount === null ? "-" : formatMoney(draft.totalAmount)}
              </td>
              <td className="px-5 py-4 text-slate-600">{draft.lineCount}</td>
              <td className="px-5 py-4 text-slate-600">{draft.warnings.length}</td>
              <td className="px-5 py-4 text-slate-600">
                {formatDateTime(draft.createdAt)}
              </td>
              <td className="px-5 py-4 text-slate-600">
                {formatDeleted(draft.deletedAt, draft.deletedBy)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SegmentButton({
  active,
  children,
  onClick,
}: {
  active: boolean;
  children: ReactNode;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`inline-flex min-h-8 items-center gap-1.5 rounded-md px-3 text-sm font-semibold ${
        active ? "bg-blue-700 text-white" : "text-slate-600 hover:bg-slate-100"
      }`}
    >
      {children}
    </button>
  );
}

function FilterButton({
  active,
  children,
  onClick,
}: {
  active: boolean;
  children: ReactNode;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`min-h-8 rounded-md px-3 text-sm font-semibold ${
        active ? "bg-slate-800 text-white" : "text-slate-600 hover:bg-slate-100"
      }`}
    >
      {children}
    </button>
  );
}

function RecordBadge({ isDeleted }: { isDeleted: boolean }) {
  return (
    <span
      className={`inline-flex min-h-7 items-center rounded-full px-2.5 text-xs font-semibold ${
        isDeleted ? "bg-red-100 text-red-800" : "bg-emerald-100 text-emerald-800"
      }`}
    >
      {isDeleted ? "Deleted" : "Active"}
    </span>
  );
}

function EmptyHistory({ label }: { label: string }) {
  return (
    <div className="grid min-h-48 place-items-center px-5 py-8 text-center">
      <div>
        <Archive className="mx-auto text-slate-400" size={34} aria-hidden="true" />
        <p className="mt-3 text-sm font-semibold text-slate-700">{label}</p>
        <p className="mt-1 text-sm text-slate-500">
          Try another history tab or filter.
        </p>
      </div>
    </div>
  );
}

function matchesFilter(isDeleted: boolean, filter: RecordFilter) {
  if (filter === "active") {
    return !isDeleted;
  }

  if (filter === "deleted") {
    return isDeleted;
  }

  return true;
}

function formatDeleted(deletedAt: string | null, deletedBy: string | null) {
  if (!deletedAt) {
    return "-";
  }

  return `${formatDateTime(deletedAt)} by ${deletedBy ?? "unknown"}`;
}

function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "PHP",
  }).format(value);
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
