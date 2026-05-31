import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Info,
  X,
} from "lucide-react";

type AlertVariant = "error" | "warning" | "success" | "info";

type AlertProps = {
  title?: string;
  message: string;
  variant?: AlertVariant;
  onDismiss?: () => void;
};

const alertStyles: Record<AlertVariant, string> = {
  error: "border-red-200 bg-red-50 text-red-800",
  warning: "border-amber-200 bg-amber-50 text-amber-900",
  success: "border-emerald-200 bg-emerald-50 text-emerald-800",
  info: "border-blue-200 bg-blue-50 text-blue-800",
};

const iconStyles: Record<AlertVariant, string> = {
  error: "text-red-600",
  warning: "text-amber-600",
  success: "text-emerald-600",
  info: "text-blue-600",
};

export function Alert({
  title,
  message,
  variant = "info",
  onDismiss,
}: AlertProps) {
  const Icon =
    variant === "error"
      ? AlertCircle
      : variant === "warning"
        ? AlertTriangle
        : variant === "success"
          ? CheckCircle2
          : Info;

  return (
    <div
      className={`animate-alert-in flex items-start gap-3 rounded-lg border px-3.5 py-3 text-sm shadow-sm ${alertStyles[variant]}`}
      role={variant === "error" ? "alert" : "status"}
    >
      <Icon
        size={18}
        className={`mt-0.5 shrink-0 ${iconStyles[variant]}`}
        aria-hidden="true"
      />

      <div className="min-w-0 flex-1 overflow-hidden">
        {title && <p className="font-semibold">{title}</p>}
        <p className={`${title ? "mt-0.5" : ""} break-words leading-5`}>
          {message}
        </p>
      </div>

      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          className="inline-flex min-h-7 w-7 shrink-0 items-center justify-center rounded-md hover:bg-black/5"
          aria-label="Dismiss alert"
        >
          <X size={15} aria-hidden="true" />
        </button>
      )}
    </div>
  );
}
