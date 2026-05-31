import { useEffect, useState } from "react";
import { AlertTriangle, Loader2, Plus, Save, Trash2, X } from "lucide-react";
import {
  getDraft,
  saveDraft,
  type DraftDetailResponse,
  type DraftUpdateRequest,
} from "../../api/drafts";
import { ToastAlert } from "../../components/ToastAlert";

type DraftPreviewProps = {
  draftId: string;
  onClose: () => void;
  onSaved: (draft: DraftDetailResponse) => void;
};

type DraftFormState = DraftUpdateRequest;

const emptyLine = {
  quantity: 0,
  itemCode: "",
  description: "",
  unitPrice: 0,
  amount: 0,
};

export function DraftPreview({ draftId, onClose, onSaved }: DraftPreviewProps) {
  const [draft, setDraft] = useState<DraftDetailResponse | null>(null);
  const [form, setForm] = useState<DraftFormState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadDraft() {
    setIsLoading(true);
    setError(null);

    try {
      const result = await getDraft(draftId);
      setDraft(result);
      setForm(toFormState(result));
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to load draft.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSave() {
    if (!form) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const saved = await saveDraft(draftId, form);
      setDraft(saved);
      setForm(toFormState(saved));
      onSaved(saved);
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Unable to save draft.");
    } finally {
      setIsSaving(false);
    }
  }

  function updateField<K extends keyof DraftFormState>(
    key: K,
    value: DraftFormState[K],
  ) {
    setForm((current) => (current ? { ...current, [key]: value } : current));
  }

  function updateLine(
    index: number,
    key: keyof DraftFormState["lines"][number],
    value: string,
  ) {
    setForm((current) => {
      if (!current) {
        return current;
      }

      const lines = current.lines.map((line, lineIndex) => {
        if (lineIndex !== index) {
          return line;
        }

        return {
          ...line,
          [key]: isNumericLineField(key) ? parseNumber(value) : value,
        };
      });

      return { ...current, lines };
    });
  }

  function addLine() {
    setForm((current) =>
      current ? { ...current, lines: [...current.lines, emptyLine] } : current,
    );
  }

  function removeLine(index: number) {
    setForm((current) =>
      current
        ? {
            ...current,
            lines: current.lines.filter((_, lineIndex) => lineIndex !== index),
          }
        : current,
    );
  }

  useEffect(() => {
    void loadDraft();
  }, [draftId]);

  if (isLoading) {
    return (
      <section className="rounded-lg border border-slate-200 bg-white p-8">
        <div className="flex items-center justify-center gap-2 text-sm text-slate-500">
          <Loader2 className="animate-spin" size={18} aria-hidden="true" />
          Loading draft
        </div>
      </section>
    );
  }

  if (!form || !draft) {
    return (
      <section className="rounded-lg border border-red-200 bg-red-50 p-5 text-sm text-red-700">
        {error ?? "Draft was not found."}
      </section>
    );
  }

  return (
    <section className="rounded-lg border border-slate-200 bg-white">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-lg font-semibold text-slate-900">
            Draft Review
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Verify extracted fields before saving changes.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            className="inline-flex min-h-9 items-center gap-2 rounded-lg bg-blue-700 px-3 text-sm font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {isSaving ? (
              <Loader2 className="animate-spin" size={16} aria-hidden="true" />
            ) : (
              <Save size={16} aria-hidden="true" />
            )}
            Save
          </button>

          <button
            type="button"
            onClick={onClose}
            className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <X size={16} aria-hidden="true" />
            Close
          </button>
        </div>
      </div>

      {error && (
        <ToastAlert
          title="Unable to save draft"
          message={error}
          variant="error"
          onDismiss={() => setError(null)}
        />
      )}

      {draft.warnings.length > 0 && (
        <div className="animate-alert-in mx-5 mt-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 shadow-sm">
          <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-amber-800">
            <AlertTriangle size={17} aria-hidden="true" />
            Extraction warnings
          </div>
          <ul className="grid gap-1 text-sm text-amber-800">
            {draft.warnings.map((warning) => (
              <li key={warning}>- {warning}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="grid gap-4 p-5 md:grid-cols-2 xl:grid-cols-4">
        <TextField
          label="Vendor"
          value={form.vendorName}
          onChange={(value) => updateField("vendorName", value)}
        />
        <DateField
          label="PO Date"
          value={form.poDate}
          onChange={(value) => updateField("poDate", value)}
        />
        <TextField
          label="Reference Number"
          value={form.referenceNumber}
          onChange={(value) => updateField("referenceNumber", value)}
        />
        <DateField
          label="Date Expected"
          value={form.dateExpected}
          onChange={(value) => updateField("dateExpected", value)}
        />
        <TextField
          label="Ship To"
          value={form.shipTo}
          onChange={(value) => updateField("shipTo", value)}
        />
        <TextField
          label="Ship Via"
          value={form.shipVia}
          onChange={(value) => updateField("shipVia", value)}
        />
        <TextField
          label="Payment Terms"
          value={form.paymentTerms}
          onChange={(value) => updateField("paymentTerms", value)}
        />
        <NumberField
          label="Total Amount"
          value={form.totalAmount}
          onChange={(value) => updateField("totalAmount", value)}
        />
      </div>

      <div className="border-t border-slate-200 px-5 py-4">
        <div className="mb-3 flex items-center justify-between gap-3">
          <div>
            <h3 className="text-base font-semibold text-slate-900">
              Line Items
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Check item code, description, quantity, unit price, and amount.
            </p>
          </div>

          <button
            type="button"
            onClick={addLine}
            className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
          >
            <Plus size={16} aria-hidden="true" />
            Add Line
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full min-w-[960px] text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-3 py-3 font-semibold">Quantity</th>
                <th className="px-3 py-3 font-semibold">Item Code</th>
                <th className="px-3 py-3 font-semibold">Description</th>
                <th className="px-3 py-3 font-semibold">Unit Price</th>
                <th className="px-3 py-3 font-semibold">Amount</th>
                <th className="w-16 px-3 py-3 font-semibold">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {form.lines.map((line, index) => (
                <tr key={`${line.itemCode}-${index}`}>
                  <td className="px-3 py-3">
                    <TableInput
                      type="number"
                      value={line.quantity.toString()}
                      onChange={(value) => updateLine(index, "quantity", value)}
                    />
                  </td>
                  <td className="px-3 py-3">
                    <TableInput
                      value={line.itemCode}
                      onChange={(value) => updateLine(index, "itemCode", value)}
                    />
                  </td>
                  <td className="px-3 py-3">
                    <TableInput
                      value={line.description}
                      onChange={(value) =>
                        updateLine(index, "description", value)
                      }
                    />
                  </td>
                  <td className="px-3 py-3">
                    <TableInput
                      value={line.unitPrice.toString()}
                      format="money"
                      onChange={(value) => updateLine(index, "unitPrice", value)}
                    />
                  </td>
                  <td className="px-3 py-3">
                    <TableInput
                      value={line.amount.toString()}
                      format="money"
                      onChange={(value) => updateLine(index, "amount", value)}
                    />
                  </td>
                  <td className="px-3 py-3">
                    <button
                      type="button"
                      onClick={() => removeLine(index)}
                      className="inline-flex min-h-9 w-9 items-center justify-center rounded-lg border border-slate-300 text-slate-600 hover:bg-red-50 hover:text-red-700"
                      aria-label="Remove line"
                    >
                      <Trash2 size={16} aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {form.lines.length === 0 && (
            <div className="border border-t-0 border-slate-200 px-4 py-6 text-center text-sm text-slate-500">
              No line items yet.
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function toFormState(draft: DraftDetailResponse): DraftFormState {
  return {
    vendorName: draft.vendorName,
    poDate: draft.poDate,
    referenceNumber: draft.referenceNumber,
    dateExpected: draft.dateExpected,
    shipTo: draft.shipTo,
    shipVia: draft.shipVia,
    paymentTerms: draft.paymentTerms,
    totalAmount: draft.totalAmount,
    lines: draft.lines,
  };
}

function TextField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="grid gap-1.5">
      <span className="text-xs font-semibold uppercase text-slate-500">
        {label}
      </span>
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="min-h-10 rounded-lg border border-slate-300 px-3 text-sm outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function DateField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string | null;
  onChange: (value: string | null) => void;
}) {
  return (
    <label className="grid gap-1.5">
      <span className="text-xs font-semibold uppercase text-slate-500">
        {label}
      </span>
      <input
        type="date"
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value || null)}
        className="min-h-10 rounded-lg border border-slate-300 px-3 text-sm outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number | null;
  onChange: (value: number | null) => void;
}) {
  return (
    <label className="grid gap-1.5">
      <span className="text-xs font-semibold uppercase text-slate-500">
        {label}
      </span>
      <input
        type="text"
        inputMode="decimal"
        value={value === null ? "" : formatNumberInput(value.toString())}
        onChange={(event) =>
          onChange(event.target.value === "" ? null : parseNumber(event.target.value))
        }
        className="min-h-10 rounded-lg border border-slate-300 px-3 text-sm outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
      />
    </label>
  );
}

function TableInput({
  type = "text",
  format,
  value,
  onChange,
}: {
  type?: "text" | "number";
  format?: "money";
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <input
      type={format === "money" ? "text" : type}
      inputMode={format === "money" || type === "number" ? "decimal" : undefined}
      value={format === "money" ? formatNumberInput(value) : value}
      onChange={(event) => onChange(event.target.value)}
      className="min-h-9 w-full rounded-lg border border-slate-300 px-2.5 text-sm outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
    />
  );
}

function isNumericLineField(key: keyof DraftFormState["lines"][number]) {
  return key === "quantity" || key === "unitPrice" || key === "amount";
}

function parseNumber(value: string) {
  const normalizedValue = value.replace(/,/g, "").trim();

  if (normalizedValue === "") {
    return 0;
  }

  const parsed = Number(normalizedValue);
  return Number.isFinite(parsed) ? parsed : 0;
}

function formatNumberInput(value: string) {
  const normalizedValue = value.replace(/,/g, "").trim();

  if (normalizedValue === "") {
    return "";
  }

  const parsed = Number(normalizedValue);

  if (!Number.isFinite(parsed)) {
    return value;
  }

  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 6,
  }).format(parsed);
}
