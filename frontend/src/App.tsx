import { Archive, FileText, UploadCloud } from "lucide-react";
import { useState } from "react";
import { DraftsPage } from "./features/drafts/DraftsPage";
import { HistoryPage } from "./features/history/HistoryPage";
import { UploadPage } from "./features/uploads/UploadPage";

type AppView = "uploads" | "drafts" | "history";

export function App() {
  const [activeView, setActiveView] = useState<AppView>("uploads");

  return (
    <main className="grid min-h-screen grid-cols-1 bg-slate-100 text-slate-900 md:grid-cols-[240px_minmax(0,1fr)]">
      <aside className="flex flex-col gap-7 border-b border-slate-200 bg-white px-4 py-5 md:border-b-0 md:border-r">
        <div className="flex items-center gap-2.5 text-lg font-bold text-blue-700">
          <FileText aria-hidden="true" size={22} />
          <span>PO OCR</span>
        </div>
        <nav className="grid gap-1.5" aria-label="Main navigation">
          <button
            type="button"
            onClick={() => setActiveView("uploads")}
            className={`flex min-h-11 items-center gap-2.5 rounded-lg px-3 text-left text-sm font-semibold ${
              activeView === "uploads"
                ? "bg-blue-50 text-blue-700"
                : "text-slate-600 hover:bg-blue-50 hover:text-blue-700"
            }`}
          >
            <UploadCloud aria-hidden="true" size={18} />
            Uploads
          </button>
          <button
            type="button"
            onClick={() => setActiveView("drafts")}
            className={`flex min-h-11 items-center gap-2.5 rounded-lg px-3 text-left text-sm font-semibold ${
              activeView === "drafts"
                ? "bg-blue-50 text-blue-700"
                : "text-slate-600 hover:bg-blue-50 hover:text-blue-700"
            }`}
          >
            <FileText aria-hidden="true" size={18} />
            Drafts
          </button>
          <button
            type="button"
            onClick={() => setActiveView("history")}
            className={`flex min-h-11 items-center gap-2.5 rounded-lg px-3 text-left text-sm font-semibold ${
              activeView === "history"
                ? "bg-blue-50 text-blue-700"
                : "text-slate-600 hover:bg-blue-50 hover:text-blue-700"
            }`}
          >
            <Archive aria-hidden="true" size={18} />
            History
          </button>
        </nav>
      </aside>

      <section className="p-5 md:p-7">
        <header className="mb-6 flex items-center justify-between gap-5">
          <div>
            <h1 className="text-2xl font-bold leading-tight text-slate-900">
              Purchase Order Extraction
            </h1>
            <p className="mt-2 text-sm text-slate-500">
              Upload purchase orders, extract draft data, and review before posting.
            </p>
          </div>
        </header>

        {activeView === "uploads" && <UploadPage />}
        {activeView === "drafts" && <DraftsPage />}
        {activeView === "history" && <HistoryPage />}
      </section>
    </main>
  );
}
