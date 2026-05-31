import { useEffect, useState } from "react";
import { Alert } from "./Alert";

type ToastAlertProps = {
  title?: string;
  message: string;
  variant?: "error" | "warning" | "success" | "info";
  autoDismissMs?: number;
  onDismiss: () => void;
};

const closeDelayMs = 160;

export function ToastAlert({
  title,
  message,
  variant = "info",
  autoDismissMs,
  onDismiss,
}: ToastAlertProps) {
  const [isClosing, setIsClosing] = useState(false);

  function dismiss() {
    if (isClosing) {
      return;
    }

    setIsClosing(true);
    window.setTimeout(onDismiss, closeDelayMs);
  }

  useEffect(() => {
    if (!autoDismissMs) {
      return;
    }

    const timeoutId = window.setTimeout(dismiss, autoDismissMs);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [autoDismissMs, message]);

  return (
    <div
      className={`fixed bottom-5 left-1/2 z-[60] w-[min(360px,calc(100%-2rem))] md:left-[calc(50%+var(--toast-sidebar-offset))] ${
        isClosing ? "animate-toast-out" : "animate-toast-in"
      }`}
    >
      <Alert
        title={title}
        message={message}
        variant={variant}
        onDismiss={dismiss}
      />
    </div>
  );
}
