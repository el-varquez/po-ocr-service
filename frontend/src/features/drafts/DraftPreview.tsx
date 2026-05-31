import { AlertTriangle, FileText } from "lucide-react";
import type { DraftListResponse } from "../../api/drafts";

type DraftPreviewProps = {
  draft: DraftListResponse;
  onClose: () => void;
};

export function DraftPreview({ draft, onClose }: DraftPreviewProps) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white">
      <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">
            Draft Preview
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Basic draft details. Editing comes in the next stage.
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          className="min-h-9 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
        >
          Close
        </button>
      </div>

      <div className="grid gap-5 p-5 md:grid-cols-2">
        <PreviewField label="Vendor" value={draft.vendorName} />
        <PreviewField label="Reference Number" value={draft.referenceNumber} />
        <PreviewField label="PO Date" value={draft.poDate ?? "-"} />
        <PreviewField label="Date Expected" value={draft.dateExpected ?? "-"} />
        <PreviewField label="Payment Terms" value={draft.paymentTerms} />
        <PreviewField
          label="Total Amount"
          value={
            draft.totalAmount === null ? "-" : formatMoney(draft.totalAmount)
          }
        />
        <PreviewField label="Line Count" value={draft.lineCount.toString()} />
        <PreviewField label="Created" value={formatDateTime(draft.createdAt)} />
      </div>

      {draft.warnings.length > 0 && (
        <div className="border-t border-slate-200 px-5 py-4">
          <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-amber-700">
            <AlertTriangle size={17} aria-hidden="true" />
            Warnings
          </div>
          <ul className="grid gap-1 text-sm text-slate-600">
            {draft.warnings.map((warning) => (
              <li key={warning}>- {warning}</li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}

function PreviewField({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
      <div className="flex items-center gap-2 text-xs font-semibold uppercase text-slate-500">
        <FileText size={14} aria-hidden="true" />
        {label}
      </div>
      <div className="mt-1 min-h-6 text-sm font-semibold text-slate-900">
        {value || "-"}
      </div>
    </div>
  );
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
