import { AlertTriangle, Loader2, Trash2, X } from "lucide-react";
import { Modal } from "./Modal";

type ConfirmDialogProps = {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isBusy?: boolean;
  variant?: "danger" | "default";
  onCancel: () => void;
  onConfirm: () => void;
};

export function ConfirmDialog({
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  isBusy = false,
  variant = "default",
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  const isDanger = variant === "danger";

  return (
    <Modal
      title={title}
      onClosed={onCancel}
      maxWidthClassName="max-w-md"
    >
      {(close) => (
        <section className="rounded-lg border border-slate-200 bg-white shadow-xl">
          <div className="flex items-start gap-3 p-5">
            <div
              className={`grid h-10 w-10 shrink-0 place-items-center rounded-full ${
                isDanger
                  ? "bg-red-100 text-red-700"
                  : "bg-blue-100 text-blue-700"
              }`}
            >
              {isDanger ? (
                <Trash2 size={18} aria-hidden="true" />
              ) : (
                <AlertTriangle size={18} aria-hidden="true" />
              )}
            </div>

            <div className="min-w-0 flex-1">
              <h2 className="text-base font-semibold text-slate-950">
                {title}
              </h2>
              <p className="mt-1 text-sm leading-6 text-slate-600">{message}</p>
            </div>
          </div>

          <div className="flex justify-end gap-2 border-t border-slate-200 px-5 py-4">
            <button
              type="button"
              onClick={close}
              disabled={isBusy}
              className="inline-flex min-h-9 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <X size={16} aria-hidden="true" />
              {cancelLabel}
            </button>

            <button
              type="button"
              onClick={onConfirm}
              disabled={isBusy}
              className={`inline-flex min-h-9 items-center gap-2 rounded-lg px-3 text-sm font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-300 ${
                isDanger
                  ? "bg-red-700 hover:bg-red-800"
                  : "bg-blue-700 hover:bg-blue-800"
              }`}
            >
              {isBusy ? (
                <Loader2 className="animate-spin" size={16} aria-hidden="true" />
              ) : (
                <Trash2 size={16} aria-hidden="true" />
              )}
              {confirmLabel}
            </button>
          </div>
        </section>
      )}
    </Modal>
  );
}
