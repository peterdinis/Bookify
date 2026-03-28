# Bookify — Backend API

A RESTful API for the Bookify audiobook platform, built with **ASP.NET Core** targeting **.NET 10**. The backend handles authentication via **Microsoft Entra ID**, audiobook file storage in **Azure Blob Storage**, playback progress tracking, OData-powered search/filtering, and admin management.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core (NET 10) |
| Language | C# (nullable enabled) |
| ORM | Entity Framework Core 10 |
| Database | SQLite (dev) / SQL Server (prod) |
| Auth | Microsoft Identity Web (Entra ID / JWT Bearer) |
| File Storage | Azure Blob Storage |
| Query | OData 9 (select, filter, orderBy, expand, count) |
| API Docs | Swagger / OpenAPI |
| Audio Metadata | TagLibSharp |
| Background Work | `IHostedService` (`AudioMetadataWorker`) |

---

## Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download)
- **Azure Storage Emulator** (Azurite) for local blob storage — or a real Azure Storage account
- **Microsoft Entra ID** app registration (optional for development — a mock dev user is seeded automatically)

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repo-url>
cd bookify/backend
```

### 2. Configure `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bookify.db"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "YOUR_TENANT_DOMAIN",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "RedirectUri": "http://localhost:5041/api/auth/callback"
  },
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "AudioContainer": "audiobooks",
    "CoverContainer": "covers"
  }
}
```

> **Local development**: `UseDevelopmentStorage=true` points to Azurite. Start Azurite before running the API.

### 3. Run the API

```bash
cd Bookify.API
dotnet run
```

The API starts on `http://localhost:5041`. Swagger UI is available at:

```
http://localhost:5041/swagger
```

The database is created automatically on first run via `EnsureCreated()`. A default dev user (`00000000-0000-0000-0000-000000000001`) is seeded in Development mode so playback endpoints work without a real Entra ID token.

---

## Project Structure

```
backend/
├── Bookify.API/
│   ├── Controllers/           # API controllers
│   │   ├── AudiobooksController.cs   # Audiobook CRUD + upload + stream
│   │   ├── AuthController.cs         # Microsoft Entra ID OAuth flow
│   │   ├── PlaybackController.cs     # Playback progress tracking
│   │   └── AdminController.cs        # Admin management endpoints
│   ├── DTOs/                  # Data Transfer Objects (request / response)
│   ├── Data/                  # EF Core DbContext
│   ├── Middleware/             # GlobalExceptionHandler
│   ├── Models/                # Domain entities
│   │   ├── Audiobook.cs
│   │   ├── Chapter.cs
│   │   ├── PlaybackProgress.cs
│   │   └── User.cs
│   ├── Services/              # Business logic & infrastructure services
│   │   ├── BlobStorageService.cs     # Azure Blob upload / download
│   │   └── AudioMetadataWorker.cs    # Background metadata extraction
│   ├── Program.cs             # App composition root
│   ├── appsettings.json
│   └── appsettings.Development.json
└── backend.sln
```

---

## API Endpoints

### Auth — `/api/auth`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/auth/microsoft` | Redirect to Microsoft Entra ID login |
| `GET` | `/api/auth/callback` | OAuth callback — sets session cookie |
| `GET` | `/api/auth/me` | Returns the current authenticated user |

### Audiobooks — `/api/audiobooks`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/audiobooks` | List audiobooks (OData: `$filter`, `$orderby`, `$top`, `$skip`, `$expand`) |
| `GET` | `/api/audiobooks/{id}` | Get audiobook details |
| `POST` | `/api/audiobooks` | Create audiobook record |
| `PUT` | `/api/audiobooks/{id}` | Update audiobook metadata |
| `DELETE` | `/api/audiobooks/{id}` | Delete audiobook |
| `POST` | `/api/audiobooks/{id}/upload-audio` | Upload audio file to Blob Storage |
| `POST` | `/api/audiobooks/{id}/upload-cover` | Upload cover image to Blob Storage |
| `GET` | `/api/audiobooks/{id}/stream` | Stream audio file |

### Playback — `/api/playback`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/playback/{audiobookId}` | Get user's playback progress |
| `POST` | `/api/playback/{audiobookId}` | Save / update playback progress |

### Admin — `/api/admin`

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/admin/users` | List all users |
| `PATCH` | `/api/admin/users/{id}/deactivate` | Deactivate a user account |

---

## OData Query Examples

```
# Get first 10 audiobooks sorted by title
GET /api/audiobooks?$orderby=Title&$top=10

# Filter by author and expand chapters
GET /api/audiobooks?$filter=Author eq 'Tolkien'&$expand=Chapters

# Count total records
GET /api/audiobooks?$count=true
```

---

## Authentication Flow

1. Frontend redirects user to `GET /api/auth/microsoft`.
2. Backend builds the Entra ID authorization URL and redirects.
3. Entra ID redirects back to `/api/auth/callback` with an auth code.
4. Backend exchanges the code for tokens, creates/updates the user record, and sets an **HTTP-only session cookie**.
5. All subsequent requests include the cookie; protected endpoints validate via JWT Bearer middleware.

In **Development**, authentication validation is relaxed and a seeded mock user is available so playback/audiobook endpoints can be tested without an Entra ID registration.

---

## Configuration Reference

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQLite (dev) or SQL Server connection string |
| `AzureAd:TenantId` | Entra ID tenant ID |
| `AzureAd:ClientId` | Entra ID app client ID |
| `AzureAd:Domain` | Tenant domain (e.g. `contoso.onmicrosoft.com`) |
| `AzureAd:RedirectUri` | OAuth callback URL |
| `Storage:ConnectionString` | Azure Blob Storage connection string (or `UseDevelopmentStorage=true`) |
| `Storage:AudioContainer` | Blob container name for audio files |
| `Storage:CoverContainer` | Blob container name for cover images |

---

## Running with a Real Database (SQL Server)

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Bookify;Trusted_Connection=True;"
  }
}
```

Then apply migrations:

```bash
dotnet ef database update
```

---

## Building for Production

```bash
dotnet publish -c Release -o ./publish
```

Run the published output:

```bash
cd publish
dotnet Bookify.API.dll
```
