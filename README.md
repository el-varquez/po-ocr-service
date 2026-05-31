# PO OCR Service

PO OCR Service is a web application for reducing manual purchase order encoding. Users upload purchase order image files, queue extraction, review the extracted draft, fix any incorrect or missing fields, and keep an auditable history of uploads and drafts.

The project is currently focused on purchase orders. The extraction flow is designed so that other OCR engines, parsers, ERP posting integrations, and client-specific rules can be added later without rewriting the core workflow.

## Current Workflow

1. Upload one or more PO image files.
2. Select uploaded files and queue them for extraction.
3. The worker processes queued files one at a time.
4. OCR reads text from the uploaded image using Tesseract.
5. The parser maps OCR text into PO header fields and line items.
6. The system creates an editable PO draft.
7. The user opens the draft, checks warnings, edits fields, and saves changes.
8. Soft-deleted uploads and drafts remain available in the history module.

## Extracted PO Fields

Header fields:

- Vendor name
- PO date
- Reference number
- Date expected
- Ship to
- Ship via
- Payment terms
- Total amount

Line item fields:

- Quantity
- Item code
- Description
- Unit price
- Amount

## Main Features

- Upload PO image files.
- Queue extraction manually after upload.
- Process extraction in a background worker.
- Create one PO draft per successfully extracted upload.
- Show warnings when required extracted fields are missing.
- Review and edit draft header fields and line items.
- Soft delete uploads and drafts.
- View upload and draft history.
- Store uploaded files locally.
- Store service data in MySQL.

## Tech Stack

Backend:

- C#
- .NET 9
- ASP.NET Core Minimal APIs
- Entity Framework Core
- MySQL
- Tesseract OCR
- xUnit tests

Frontend:

- React
- TypeScript
- Vite
- Tailwind CSS
- Lucide React icons

## Architecture

The backend follows Clean Architecture.

### Domain

Located in `backend/src/PoOcr.Domain`.

The domain layer contains the core business entities and rules:

- `UploadFile`
- `ExtractionJob`
- `PoDraft`
- `PoDraftLine`
- `AuditEvent`

This layer does not depend on the database, web API, OCR engine, or frontend.

### Application

Located in `backend/src/PoOcr.Application`.

The application layer contains use cases and interfaces:

- Queue extraction jobs.
- Process the next extraction job.
- Define abstractions for storage, OCR, parsing, repositories, and audit writing.

The application layer coordinates workflow but does not know the concrete database or OCR implementation.

### Infrastructure

Located in `backend/src/PoOcr.Infrastructure`.

The infrastructure layer contains concrete adapters:

- EF Core `OcrDbContext`
- MySQL repositories
- local file storage
- Tesseract OCR adapter
- rule-based PO parser
- audit writer
- EF Core migrations

### API

Located in `backend/src/PoOcr.Api`.

The API layer exposes HTTP endpoints for the frontend:

- `/api/uploads`
- `/api/extraction/queue`
- `/api/drafts`
- `/api/history/uploads`
- `/api/history/drafts`

Endpoints are grouped by module in `backend/src/PoOcr.Api/Api`.

### Worker

Located in `backend/src/PoOcr.Worker`.

The worker continuously checks for queued extraction jobs. When it finds one, it extracts text, parses the PO, creates a draft, and updates the upload and job status.

## Design Patterns Used

- Clean Architecture: separates domain rules, use cases, infrastructure, and API.
- Use Case pattern: application actions are represented by focused classes such as `QueueExtractionUseCase` and `ProcessNextExtractionJobUseCase`.
- Repository pattern: database access is hidden behind application interfaces.
- Adapter pattern: OCR, file storage, parsing, and persistence are replaceable implementations.
- Background Worker pattern: extraction runs outside the request/response cycle.
- DTO/Contract pattern: API responses and requests are separated from domain entities.


## Project Structure

```text
po-ocr-service/
  backend/
    src/
      PoOcr.Api/             HTTP API
      PoOcr.Application/     use cases and abstractions
      PoOcr.Domain/          business entities and rules
      PoOcr.Infrastructure/  persistence, OCR, parser, storage
      PoOcr.Worker/          background extraction worker
    tests/
      PoOcr.Api.Tests/
      PoOcr.Application.Tests/
      PoOcr.Domain.Tests/
      PoOcr.Infrastructure.Tests/
      PoOcr.Worker.Tests/
  frontend/
    src/
      api/                   frontend API clients
      components/            reusable UI components
      features/              uploads, drafts, history modules
  .env.example
  PoOcrService.sln
```

## Prerequisites

- .NET 9 SDK
- Node.js and npm
- MySQL Server 8
- Tesseract OCR installed locally

Default Tesseract path used by the worker:

```text
C:\Program Files\Tesseract-OCR\tesseract.exe
```

## Environment Setup

Copy `.env.example` to `.env` in the project root and set the MySQL values:

```env
PO_OCR_DB_SERVER=localhost
PO_OCR_DB_NAME=po_ocr_service
PO_OCR_DB_USER=your_mysql_user
PO_OCR_DB_PASSWORD=your_mysql_password
```

The backend resolves the database connection from `.env`. You can also use `PO_OCR_CONNECTION_STRING` if you prefer a full MySQL connection string.

For the frontend, copy `frontend/.env.example` to `frontend/.env` if needed:

```env
VITE_API_BASE_URL=http://localhost:5123
```

## Database Setup

Create the MySQL schema:

```sql
CREATE DATABASE po_ocr_service;
```

Apply EF Core migrations:

```bash
dotnet ef database update --project backend/src/PoOcr.Infrastructure --startup-project backend/src/PoOcr.Api
```

## Running Locally

Run the API:

```bash
dotnet run --project backend/src/PoOcr.Api
```

Run the worker in a separate terminal:

```bash
dotnet run --project backend/src/PoOcr.Worker
```

Run the frontend in a separate terminal:

```bash
cd frontend
npm install
npm run dev
```

Default local URLs:

- API: `http://localhost:5123`
- Frontend: `http://localhost:5173`

## Testing

Run all backend tests:

```bash
dotnet test PoOcrService.sln
```

Build the frontend:

```bash
cd frontend
npm run build
```

## Current Limitations

- OCR currently supports image files only: PNG and JPEG.
- PDF extraction is not implemented yet. PDF files currently fail with a message that PDF-to-image conversion is required.
- The parser is rule-based and currently tuned for the current PO layout.
- Posting to a client ERP is not implemented yet.
- Login and role-based access are not implemented yet.
- User identity is currently hardcoded as `test-user` or `system`.
- Multi-page PO extraction is planned for a later version.
- The current deployment model is one on-premise installation per client.

## Planned Enhancements

- PDF-to-image conversion before OCR.
- Client-specific parser templates.
- Real authentication and role-based access.
- ERP lookup sync and posting endpoints.
- Batch posting queue.
- Better parser confidence and field-level warnings.
- Multi-page PO support.

