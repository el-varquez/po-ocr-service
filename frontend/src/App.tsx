import { FileText, UploadCloud } from "lucide-react";

export function App() {
  return (
    <main className="grid min-h-screen grid-cols-1 bg-slate-100 text-slate-900 md:grid-cols-[240px_minmax(0,1fr)]">
      <aside className="flex flex-col gap-7 border-b border-slate-200 bg-white px-4 py-5 md:border-b-0 md:border-r">
        <div className="flex items-center gap-2.5 text-lg font-bold text-blue-700">
          <FileText aria-hidden="true" size={22} />
          <span>PO OCR</span>
        </div>
        <nav className="grid gap-1.5" aria-label="Main navigation">
          <a
            className="flex min-h-11 items-center gap-2.5 rounded-lg bg-blue-50 px-3 text-sm font-semibold text-blue-700"
            href="/"
          >
            <UploadCloud aria-hidden="true" size={18} />
            Uploads
          </a>
          <a
            className="flex min-h-11 items-center gap-2.5 rounded-lg px-3 text-sm font-semibold text-slate-600 hover:bg-blue-50 hover:text-blue-700"
            href="/"
          >
            <FileText aria-hidden="true" size={18} />
            Drafts
          </a>
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

        <section
          className="grid min-h-[420px] place-items-center rounded-lg border border-dashed border-slate-300 bg-white p-9 text-center"
          aria-label="Upload workspace"
        >
          <UploadCloud aria-hidden="true" size={36} />
          <h2 className="mt-3.5 text-xl font-semibold text-slate-900">
            Frontend shell is ready
          </h2>
          <p className="mt-2 max-w-md text-slate-500">
            The next slice will connect uploads, extraction queueing, and draft review.
          </p>
        </section>
      </section>
    </main>
  );
}
