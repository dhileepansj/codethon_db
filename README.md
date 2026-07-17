# NovacCodeLab — SQL Hackathon Platform

A full-stack web application for conducting supervised SQL hackathons with real-time monitoring, AI-based plagiarism detection, and comprehensive anti-cheat mechanisms.

---

## Overview

NovacCodeLab provides a controlled environment where participants write and execute SQL queries against personal databases while administrators monitor activity, detect AI-generated code, and manage the hackathon lifecycle end-to-end.

---

## Architecture

```
┌─────────────────────┐     ┌─────────────────────────────┐     ┌──────────────────┐
│   React Frontend    │────▶│   .NET 8 Web API (C#)       │────▶│   PostgreSQL     │
│   (Vite + TS)       │     │   Clean Architecture        │     │   (App Metadata) │
└─────────────────────┘     └──────────────┬──────────────┘     └──────────────────┘
                                           │
                                           ▼
                            ┌──────────────────────────────┐
                            │   SQL Server (Participant DBs)│
                            └──────────────────────────────┘
```

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Frontend | React 18, TypeScript, Vite, TailwindCSS, Monaco Editor | Participant workspace & Admin console |
| API | ASP.NET Core 8, Entity Framework Core 9 | Business logic, authentication, query execution |
| App Database | PostgreSQL | Users, sessions, files, logs, settings |
| Participant DBs | SQL Server | Individual databases per participant |
| AI Detection | Ollama (local LLM) | Code plagiarism/AI detection |

---

## Project Structure

```
DCView/
├── HackathonAPI/                    # Backend (.NET 8 Solution)
│   ├── DCView.Hackathon.API/       # Controllers, Middleware, Program.cs
│   ├── DCView.Hackathon.Application/ # Services, DTOs, Interfaces
│   ├── DCView.Hackathon.Domain/    # Entities, Repository Interfaces
│   ├── DCView.Hackathon.Infrastructure/ # EF Core, Repositories, Migrations
│   └── DCView.Hackathon.Shared/    # Helpers, Filters, Response Models
├── HackathonApp/                    # Frontend (React + Vite)
│   ├── src/
│   │   ├── components/             # UI Components
│   │   │   ├── admin/              # Admin panel components
│   │   │   ├── common/             # Shared components
│   │   │   ├── files/              # File manager
│   │   │   ├── history/            # Execution history
│   │   │   └── schema/             # Schema explorer
│   │   ├── pages/                  # Route pages
│   │   ├── services/               # API clients & utilities
│   │   ├── redux/                  # State management
│   │   └── contexts/               # React contexts
│   └── public/
└── SQL_Scripts/                     # Database setup scripts
```

---

## Features

### Participant Features

| Feature | Description |
|---------|-------------|
| **SQL Editor** | Monaco-based editor with syntax highlighting, multi-tab support |
| **Query Execution** | F5 / Ctrl+E to execute, selection-based execution (like SSMS) |
| **Schema Explorer** | Browse tables, views, procedures, functions, triggers |
| **File Manager** | Save/organize SQL scripts in folders |
| **Execution History** | View past queries with copy/open-in-editor actions |
| **Question Paper** | Side panel with hackathon tasks (shown by default) |
| **Starter Scripts** | Pre-provided scripts in a dedicated folder |
| **Keyboard Shortcuts** | F5/Ctrl+E (execute), Ctrl+S (save), Ctrl+R (toggle results) |
| **Multi-cursor Editing** | Alt+Shift+Drag for column selection (like SSMS) |
| **Auto-save Reminder** | Notification after 5 minutes of unsaved changes |
| **Unsaved Changes Warning** | Browser prompt on tab close/refresh/logout |
| **Guided Tour** | Interactive walkthrough on first login |
| **DOs & DON'Ts** | Guidelines modal with hackathon rules |
| **Submission** | Final work submission with file upload |

### Admin Features

| Feature | Description |
|---------|-------------|
| **Participant Management** | Create, activate/deactivate, bulk import users |
| **Session Control** | Timed sessions, extend, expire, activate all/stop all |
| **Hackathon Setup** | Configure title, date, duration, upload question paper |
| **Scaffold Scripts** | Upload SQL scripts that auto-execute on DB creation |
| **AI Detection** | Configurable AI plagiarism detection (Block/Allow & Mark/Disabled) |
| **Tab Switch Logs** | Monitor participant focus/tab switching |
| **DevTools Detection** | Detect and block developer tools usage |
| **Security Settings** | Password complexity, history, lockout, expiry policies |
| **Password Change Logs** | Audit trail for all password changes |
| **Server Configuration** | Configure SQL Server connection for participant databases |
| **Export** | Export individual or all participant submissions |

### Security & Anti-Cheat

| Mechanism | Description |
|-----------|-------------|
| **Clipboard Guard** | Blocks external paste; allows internal copy-paste & question paper content |
| **DevTools Detection** | Uses `devtools-detector` library to detect open DevTools |
| **AI Detection** | Ollama-based LLM analysis with model fallback (minimax → gpt-oss) |
| **Tab Switch Monitoring** | Logs tab_hidden, tab_visible, window_blur, window_focus events |
| **SQL Guardrails** | Blocks dangerous SQL (DROP DATABASE, USE, xp_cmdshell, etc.) |
| **Right-click Blocking** | Context menu disabled |
| **Keyboard Shortcut Blocking** | F12, Ctrl+Shift+I/J/C, Ctrl+U intercepted |
| **Drag & Drop Blocking** | External drag-and-drop into editor prevented |
| **Session Validation** | JWT auth + session status middleware on every request |

---

## Setup & Installation

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- PostgreSQL 15+ (app database)
- SQL Server 2019+ (participant databases)
- Ollama (optional, for AI detection)

### Backend Setup

```bash
cd HackathonAPI

# Restore packages
dotnet restore

# Update connection string in appsettings.json
# PostgreSQL: Host=<ip>;Port=5432;Database=NovacCodeLab;Username=<user>;Password=<pwd>;

# Run migrations
dotnet ef database update --project DCView.Hackathon.Infrastructure --startup-project DCView.Hackathon.API

# Run the API
dotnet run --project DCView.Hackathon.API
```

### Frontend Setup

```bash
cd HackathonApp

# Install dependencies
npm install

# Configure environment
# .env.development: VITE_API_BASE_URL=http://localhost:5000

# Start dev server
npm run dev

# Build for production
npm run build
```

### Default Admin Login

| Field | Value |
|-------|-------|
| User ID | `superadmin` |
| Password | `Admin@123` |

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<ip>;Port=5432;Database=NovacCodeLab;Username=<user>;Password=<pwd>;"
  },
  "Jwt": {
    "Key": "<32+ char secret>",
    "Issuer": "DCView.Hackathon",
    "Audience": "DCView.Hackathon.Client",
    "ExpiryHours": 8
  },
  "AiDetection": {
    "OllamaUrl": "http://localhost:11434",
    "Models": ["minimax-m3:cloud", "gpt-oss:20b-cloud", "gpt-oss:120b-cloud"],
    "Enabled": true,
    "ApiKey": ""
  }
}
```

### Frontend .env

```env
VITE_API_BASE_URL=https://your-domain.com
VITE_APP_BASEPATH=/novaccodelab
```

---

## API Endpoints

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/change-password` | Change own password |
| POST | `/api/auth/forgot-password` | Request password reset |

### Hackathon (Participant)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/hackathon/status` | Session status |
| POST | `/api/hackathon/create-database` | Create personal DB |
| POST | `/api/hackathon/execute` | Execute SQL query |
| GET | `/api/hackathon/question-paper` | Get question paper |

### Files
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/files/root` | Root folder contents |
| POST | `/api/files` | Create file |
| PUT | `/api/files/:id` | Update file |
| DELETE | `/api/files/:id` | Delete file |
| POST | `/api/files/folder` | Create folder |

### Schema
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/schema/overview` | DB overview |
| GET | `/api/schema/tables` | List tables |
| GET | `/api/schema/tables/:name` | Table details |

### Admin
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/dashboard` | Dashboard stats |
| GET | `/api/admin/users` | All participants |
| POST | `/api/admin/users` | Create participant |
| POST | `/api/admin/activate/:userId` | Activate session |
| POST | `/api/admin/deactivate/:userId` | Deactivate session |
| GET | `/api/admin/security-settings` | Security policy |
| PUT | `/api/admin/security-settings` | Update policy |
| GET | `/api/admin/scaffold-scripts` | List scaffold scripts |
| POST | `/api/admin/scaffold-scripts` | Add scaffold script |
| GET | `/api/admin/ai-detection/settings` | AI detection config |
| PUT | `/api/admin/ai-detection/settings` | Update AI config |

---

## Database Schema (Key Tables)

| Table | Purpose |
|-------|---------|
| `Hackathon_Users` | User accounts (admin + participants) |
| `Hackathon_Sessions` | Session state, DB creation status |
| `Hackathon_Config` | SQL Server connection for participant DBs |
| `Hackathon_UserFiles` | Saved SQL scripts |
| `Hackathon_UserFolders` | File organization |
| `Hackathon_ExecutionHistory` | Query execution log |
| `Hackathon_TabSwitchLogs` | Tab switch & DevTools events |
| `Hackathon_ScaffoldScripts` | Admin-uploaded starter scripts |
| `Hackathon_AiDetectionLogs` | AI detection results |
| `Hackathon_AiBlockedSaves` | Saves blocked by AI detection |
| `Hackathon_AiDetectionSettings` | AI detection configuration |
| `Hackathon_SecuritySettings` | Password & security policies |
| `Hackathon_PasswordChangeLogs` | Password change audit trail |
| `Hackathon_QuestionPaper` | Hackathon question paper (HTML) |
| `Hackathon_SubmissionFiles` | Participant submission uploads |

---

## Deployment

### Production Build

```bash
# Backend
cd HackathonAPI
dotnet publish -c Release -o ./publish

# Frontend
cd HackathonApp
npm run build
# Output in dist/ — serve via Nginx/IIS with base path /novaccodelab
```

### IIS / Reverse Proxy

The API runs at path base `/hackathonapi`. Configure your reverse proxy:

```
/novaccodelab/        → Frontend static files
/hackathonapi/        → .NET API
```

---

## Hackathon Workflow

1. **Admin Setup**
   - Configure SQL Server connection (Server Config)
   - Upload question paper (Hackathon Setup)
   - Add scaffold scripts (Scaffold Scripts)
   - Create/import participants (Bulk Import)

2. **Start Hackathon**
   - Activate all sessions (with optional time limit)

3. **Participants**
   - Login → Change password → Read guidelines → Create database
   - Write SQL, execute queries, save scripts
   - Submit final work when done

4. **Admin Monitoring**
   - Real-time participant overview
   - Tab switch monitoring
   - DevTools detection logs
   - AI detection results
   - Export submissions for evaluation

---

## Tech Stack

- **Frontend:** React 18, TypeScript, Vite, TailwindCSS, Monaco Editor, Redux Toolkit, React Router, Sonner (toasts), Lucide Icons
- **Backend:** ASP.NET Core 8, Entity Framework Core 9 (Npgsql), BCrypt, JWT, System.Text.Json
- **Databases:** PostgreSQL (metadata), SQL Server (participant databases)
- **AI:** Ollama with model fallback support
- **DevTools Detection:** devtools-detector library

---

## License

Proprietary — Novac Technology Solutions. All rights reserved.
