import {
  Archive,
  FileText,
  Menu,
  PanelLeftClose,
  PanelLeftOpen,
  UploadCloud,
} from "lucide-react";
import type { CSSProperties } from "react";
import { useState } from "react";
import { DraftsPage } from "./features/drafts/DraftsPage";
import { HistoryPage } from "./features/history/HistoryPage";
import { UploadPage } from "./features/uploads/UploadPage";

type AppView = "uploads" | "drafts" | "history";

const navItems = [
  { view: "uploads" as const, label: "Uploads", Icon: UploadCloud },
  { view: "drafts" as const, label: "Drafts", Icon: FileText },
  { view: "history" as const, label: "History", Icon: Archive },
];

export function App() {
  const [activeView, setActiveView] = useState<AppView>("uploads");
  const [isNavOpen, setIsNavOpen] = useState(true);

  const shellStyle = {
    "--toast-sidebar-offset": isNavOpen ? "120px" : "36px",
  } as CSSProperties;

  function openView(view: AppView) {
    setActiveView(view);

    if (window.innerWidth < 768) {
      setIsNavOpen(false);
    }
  }

  return (
    <main
      style={shellStyle}
      className={`grid min-h-screen grid-cols-1 bg-slate-100 text-slate-900 transition-[grid-template-columns] duration-200 ${
        isNavOpen
          ? "md:grid-cols-[240px_minmax(0,1fr)]"
          : "md:grid-cols-[72px_minmax(0,1fr)]"
      }`}
    >
      {isNavOpen && (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-slate-950/40 md:hidden"
          aria-label="Close navigation"
          onClick={() => setIsNavOpen(false)}
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-60 flex-col gap-7 border-r border-slate-200 bg-white px-4 py-5 shadow-xl transition-transform duration-200 md:sticky md:top-0 md:z-auto md:h-screen md:shadow-none ${
          isNavOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"
        } ${isNavOpen ? "md:w-60" : "md:w-[72px] md:px-3"}`}
      >
        <div
          className={`flex items-center gap-2.5 text-lg font-bold text-blue-700 ${
            isNavOpen ? "justify-between" : "md:justify-center"
          }`}
        >
          <div className="flex min-w-0 items-center gap-2.5">
            <FileText aria-hidden="true" size={22} className="shrink-0" />
            <span className={`truncate ${isNavOpen ? "" : "md:hidden"}`}>
              PO OCR
            </span>
          </div>

          <button
            type="button"
            onClick={() => setIsNavOpen((current) => !current)}
            className="hidden min-h-9 w-9 items-center justify-center rounded-lg text-slate-600 hover:bg-blue-50 hover:text-blue-700 md:inline-flex"
            aria-label={isNavOpen ? "Collapse navigation" : "Expand navigation"}
          >
            {isNavOpen ? (
              <PanelLeftClose size={18} aria-hidden="true" />
            ) : (
              <PanelLeftOpen size={18} aria-hidden="true" />
            )}
          </button>
        </div>

        <nav className="grid gap-1.5" aria-label="Main navigation">
          {navItems.map(({ view, label, Icon }) => (
            <button
              key={view}
              type="button"
              onClick={() => openView(view)}
              title={isNavOpen ? undefined : label}
              className={`flex min-h-11 items-center gap-2.5 rounded-lg px-3 text-left text-sm font-semibold ${
                isNavOpen ? "" : "md:justify-center md:px-0"
              } ${
                activeView === view
                  ? "bg-blue-50 text-blue-700"
                  : "text-slate-600 hover:bg-blue-50 hover:text-blue-700"
              }`}
            >
              <Icon aria-hidden="true" size={18} className="shrink-0" />
              <span className={isNavOpen ? "" : "md:hidden"}>{label}</span>
            </button>
          ))}
        </nav>
      </aside>

      <section className="p-5 md:p-7">
        <header className="mb-6 flex items-center justify-between gap-5">
          <div className="min-w-0">
            <h1 className="text-2xl font-bold leading-tight text-slate-900">
              Purchase Order Extraction
            </h1>
            <p className="mt-2 text-sm text-slate-500">
              Upload purchase orders, extract draft data, and review before posting.
            </p>
          </div>

          <button
            type="button"
            onClick={() => setIsNavOpen(true)}
            className="inline-flex min-h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-slate-300 bg-white text-slate-700 hover:bg-slate-50"
            aria-label="Open navigation"
          >
            <Menu size={20} aria-hidden="true" />
          </button>
        </header>

        {activeView === "uploads" && <UploadPage />}
        {activeView === "drafts" && <DraftsPage />}
        {activeView === "history" && <HistoryPage />}
      </section>
    </main>
  );
}
