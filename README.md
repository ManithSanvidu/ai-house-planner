# HousePlanner

HousePlanner is an AI-assisted home design and cost-planning application. It has a React web client for signing in and viewing role-based workspaces, plus an ASP.NET Core API that verifies Supabase authentication tokens.

## Tech stack

- **Frontend:** React, TypeScript, Vite, Redux Toolkit, Tailwind CSS, Supabase Authentication
- **Backend:** ASP.NET Core 10, Supabase JWT Authentication, Swagger/OpenAPI

## Project structure

```text
HousePlanner-Web/     React frontend
HousePlanner.API/     ASP.NET Core API
```

## Prerequisites

Install the following before you begin:

- [Node.js](https://nodejs.org/) 20 or later (npm is included)
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- A Supabase project with Email/Password authentication enabled

## Quick start

Run the API and frontend in separate terminal windows.

### 1. Configure Supabase

1. Create a project in [Supabase](https://supabase.com/).
2. Navigate to **Project Settings** -> **API** and copy your **Project URL** and **anon public key**.
3. Enable Email/Password authentication in the Supabase Auth settings.

Never commit your `.env` file with real keys to version control.

### 2. Configure and start the API

```bash
cd HousePlanner.API
dotnet restore
dotnet dev-certs https --trust
dotnet run
```

The API runs at `https://localhost:7193` in the default HTTPS profile. When running in Development, Swagger is available at [https://localhost:7193/swagger](https://localhost:7193/swagger).

### 3. Configure and start the frontend

In a new terminal, from the repository root:

```bash
cd HousePlanner-Web
cp .env.example .env
npm install
npm run dev
```

Update `HousePlanner-Web/.env` with your Supabase values. Use the local API address below:

```ini
VITE_SUPABASE_URL=your_supabase_url
VITE_SUPABASE_ANON_KEY=your_supabase_anon_key
VITE_API_BASE_URL=https://localhost:7193/api/v1
```

Open the address shown by Vite, normally [http://localhost:5173](http://localhost:5173).

## Authentication flow

1. The user signs in through Supabase in the web client.
2. The client sends the Supabase access token to the API via standard Bearer headers.
3. The API validates the JWT signature against the Supabase JWT secret and extracts the `sub` claim as the user ID.

At present, the API assigns the **Architect** role to emails containing `architect`; every other authenticated user receives the **Contractor** role. This is temporary role-mapping logic until persistent user roles are added.

## Useful commands

| Component | Command | Purpose |
| --- | --- | --- |
| Frontend | `npm run dev` | Start the Vite development server |
| Frontend | `npm run build` | Type-check and create a production build |
| Frontend | `npm run lint` | Run linting |
| API | `dotnet run` | Start the API |
| API | `dotnet build` | Build the API |

## Before pushing to GitHub

The root `.gitignore` excludes dependencies, build outputs, local environment files. Check what will be committed before your first push:

```bash
git add .
git status
git commit -m "feat: initial HousePlanner application setup"
```

Do not add `HousePlanner-Web/.env` or `HousePlanner.API/.env` manually. Commit `HousePlanner-Web/.env.example` so other contributors know which values they need.
