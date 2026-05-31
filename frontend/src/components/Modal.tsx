import { type ReactNode, useState } from "react";

type ModalProps = {
  title: string;
  children: (close: () => void) => ReactNode;
  onClosed: () => void;
  maxWidthClassName?: string;
};

const closeDelayMs = 180;

export function Modal({
  title,
  children,
  onClosed,
  maxWidthClassName = "max-w-7xl",
}: ModalProps) {
  const [isClosing, setIsClosing] = useState(false);

  function close() {
    if (isClosing) {
      return;
    }

    setIsClosing(true);
    window.setTimeout(onClosed, closeDelayMs);
  }

  return (
    <div
      className={`fixed inset-0 z-50 flex items-start justify-center bg-slate-950/50 p-4 backdrop-blur-sm ${
        isClosing ? "animate-modal-backdrop-out" : "animate-modal-backdrop-in"
      }`}
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        aria-label="Close modal"
        onClick={close}
      />

      <div
        className={`relative max-h-[calc(100vh-2rem)] w-full ${maxWidthClassName} overflow-y-auto rounded-lg ${
          isClosing ? "animate-modal-panel-out" : "animate-modal-panel-in"
        }`}
      >
        {children(close)}
      </div>
    </div>
  );
}
