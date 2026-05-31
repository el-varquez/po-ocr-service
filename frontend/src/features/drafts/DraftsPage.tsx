import { useEffect, useState } from "react";
import {
  AlertCircle,
  ExternalLink,
  FileText,
  Loader2,
  RefreshCw,
  Send,
} from "lucide-react";
import {
  getDrafts,
  type DraftDetailResponse,
  type DraftListResponse,
} from "../../api/drafts";
import { DraftPreview } from "./DraftPreview";

export function DraftsPage() {
  const [drafts, setDrafts] = useState<DraftListResponse[]>([]);
  const [selectedDraftIds, setSelectedDraftIds] = useState<string[]>([]);
  const [selectedDraftId, setSelectedDraftId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isAllSelected =
    drafts.length > 0 && drafts.every((draft) => selectedDraftIds.includes(draft.id));

  async function loadDrafts() {
    setError(null);

    try {
      const result = await getDrafts();
      setDrafts(result);
      setSelectedDraftIds((current) =>
        current.filter((draftId) => result.some((draft) => draft.id === draftId)),
      );
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to load drafts.");
    } finally {
      setIsLoading(false);
    }
  }

  function toggleDraftSelection(draftId: string) {
    setSelectedDraftIds((current) =>
      current.includes(draftId)
        ? current.filter((id) => id !== draftId)
        : [...current, draftId],
    );
  }

  function toggleSelectAll() {
    setSelectedDraftIds(isAllSelected ? [] : drafts.map((draft) => draft.id));
  }

  function updateSavedDraft(savedDraft: DraftDetailResponse) {
    setDrafts((current) =>
      current.map((draft) =>
        draft.id === savedDraft.id ? toListDraft(savedDraft) : draft,
      ),
    );
  }

  useEffect(() => {
    void loadDrafts();
  }, []);

  return (
    <div className="grid gap-6">
      <section className="rounded-lg border border-slate-200 bg-white">
        <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-semibold text-slate-900">Drafts</h2>
            <p className="mt-1 text-sm text-slate-500">
              Review extracted purchase orders before posting.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={selectedDraftIds.length === 0}
              className="inline-flex min-h-9 items-center gap-2 rounded-lg bg-slate-300 px-3 text-sm font-semibold text-white disabled:cursor-not-allowed"
              title="Posting to ERP will be added in a later stage."
            >
              <Send size={16} aria-hidden="true" />
              Post Selected
            </button>

            <button
              type="button"
              onClick={() => void loadDrafts()}
              className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
            >
              <RefreshCw size={16} aria-hidden="true" />
              Refresh
            </button>
          </div>
        </div>

        {error && (
          <div className="mx-5 mt-4 flex items-start gap-2 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
            <AlertCircle size={18} aria-hidden="true" />
            <span>{error}</span>
          </div>
        )}

        {isLoading ? (
          <div className="flex min-h-48 items-center justify-center gap-2 text-sm text-slate-500">
            <Loader2 className="animate-spin" size={18} aria-hidden="true" />
            Loading drafts
          </div>
        ) : drafts.length === 0 ? (
          <div className="grid min-h-48 place-items-center px-5 py-8 text-center">
            <div>
              <FileText
                className="mx-auto text-slate-400"
                size={34}
                aria-hidden="true"
              />
              <p className="mt-3 text-sm font-semibold text-slate-700">
                No drafts yet
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Extract an uploaded PO to create a draft.
              </p>
            </div>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1040px] text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                <tr>
                  <th className="w-12 px-5 py-3">
                    <input
                      type="checkbox"
                      checked={isAllSelected}
                      onChange={toggleSelectAll}
                      aria-label="Select all drafts"
                      className="h-4 w-4 rounded border-slate-300"
                    />
                  </th>
                  <th className="px-5 py-3 font-semibold">Vendor</th>
                  <th className="px-5 py-3 font-semibold">Reference</th>
                  <th className="px-5 py-3 font-semibold">PO Date</th>
                  <th className="px-5 py-3 font-semibold">Expected</th>
                  <th className="px-5 py-3 font-semibold">Terms</th>
                  <th className="px-5 py-3 font-semibold">Total</th>
                  <th className="px-5 py-3 font-semibold">Lines</th>
                  <th className="px-5 py-3 font-semibold">Warnings</th>
                  <th className="px-5 py-3 font-semibold">Created</th>
                  <th className="px-5 py-3 font-semibold">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {drafts.map((draft) => (
                  <tr key={draft.id}>
                    <td className="px-5 py-4">
                      <input
                        type="checkbox"
                        checked={selectedDraftIds.includes(draft.id)}
                        onChange={() => toggleDraftSelection(draft.id)}
                        aria-label={`Select ${draft.referenceNumber || draft.vendorName}`}
                        className="h-4 w-4 rounded border-slate-300"
                      />
                    </td>
                    <td className="px-5 py-4 font-medium text-slate-900">
                      {draft.vendorName || "-"}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.referenceNumber || "-"}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.poDate ?? "-"}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.dateExpected ?? "-"}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.paymentTerms || "-"}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.totalAmount === null ? "-" : formatMoney(draft.totalAmount)}
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {draft.lineCount}
                    </td>
                    <td className="px-5 py-4">
                      <span
                        className={`inline-flex min-h-7 items-center rounded-full px-2.5 text-xs font-semibold ${
                          draft.warnings.length > 0
                            ? "bg-amber-100 text-amber-800"
                            : "bg-emerald-100 text-emerald-800"
                        }`}
                      >
                        {draft.warnings.length}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-slate-600">
                      {formatDateTime(draft.createdAt)}
                    </td>
                    <td className="px-5 py-4">
                      <button
                        type="button"
                        onClick={() => setSelectedDraftId(draft.id)}
                        className="inline-flex min-h-8 items-center gap-1.5 rounded-lg border border-slate-300 px-2.5 text-xs font-semibold text-slate-700 hover:bg-slate-50"
                      >
                        <ExternalLink size={14} aria-hidden="true" />
                        Open Draft
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {selectedDraftId && (
        <div
          className="fixed inset-0 z-50 flex items-start justify-center bg-slate-950/50 p-4 backdrop-blur-sm"
          role="dialog"
          aria-modal="true"
          aria-label="Draft review"
        >
          <div className="max-h-[calc(100vh-2rem)] w-full max-w-7xl overflow-y-auto rounded-lg">
            <DraftPreview
              draftId={selectedDraftId}
              onClose={() => setSelectedDraftId(null)}
              onSaved={updateSavedDraft}
            />
          </div>
        </div>
      )}
    </div>
  );
}

function toListDraft(draft: DraftDetailResponse): DraftListResponse {
  return {
    id: draft.id,
    uploadFileId: draft.uploadFileId,
    vendorName: draft.vendorName,
    poDate: draft.poDate,
    referenceNumber: draft.referenceNumber,
    dateExpected: draft.dateExpected,
    paymentTerms: draft.paymentTerms,
    totalAmount: draft.totalAmount,
    lineCount: draft.lines.length,
    createdAt: draft.createdAt,
    warnings: draft.warnings,
  };
}

function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "PHP",
  }).format(value);
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
