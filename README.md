# RaceDay.API_Part2
# 🏃 RaceDay — Event Management System

### Part 2: RESTful API Development · Part 3: MVC Front End

---

## 📋 Project Overview

| | |
|---|---|
| **Student Name** | Botshelo Letebele |
| **Student Number** | ST10478568 |
| **Module** | PROG6212 – Part 2 |
| **Repository** | [github.com/botletebele66/RaceDay.API_Part2](https://github.com/botletebele66/RaceDay.API_Part2) |

[![Build](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml/badge.svg)](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml)

---

## 🚀 About Part 2

This repository contains the backend **RESTful API** that powers the RaceDay platform. Following the endpoint plan and database schema designed in Part 1, this API handles all data operations, enforces role-based access, and serves as the backbone for the MVC frontend to be built in Part 3.

### Tech Stack

- **Framework:** ASP.NET Core 10 Web API (C#)
- **Database:** SQL Server
- **ORM:** Entity Framework Core
- **Authentication:** Session-based Authentication with BCrypt password hashing
- **API Documentation:** Swagger (Swashbuckle)
- **Testing:** xUnit (companion test project in the local solution)
- **CI:** GitHub Actions

---

## 📁 Project Structure

```text
RaceDay.API_Part2/
├── .github/
│   └── workflows/
│       └── ci.yml                 # Build workflow
├── Attributes/                    # Custom authorization attributes
├── Controllers/                   # API controllers
├── DTOs/                          # Data Transfer Objects
├── Data/                          # EF Core DbContext and database initialization
├── Helper/                        # Password hashing and utility classes
├── Models/                        # Database entities
├── Properties/                    # Local launch settings
├── .dockerignore
├── .gitattributes
├── .gitignore
├── Dockerfile
├── Program.cs
├── README.md
└── RaceDay.API_Part2.csproj
```

---

## 👥 User Roles

As per the Part 1 specification, the system supports two distinct roles:

### 🏢 Organiser

- Can register and log in to the system.
- Can create, update, and delete their own events.
- Can define event categories (distances, entry fees, age restrictions).
- Can view all enrolments for their events.
- Can capture finish times and placement ranks for participants.

### 🏃 Participant

- Can register and log in to the system.
- Can browse all upcoming events and view details.
- Can enrol in an event by selecting a specific category.
- Can view their own enrolment history and race results.
- Can withdraw from an event.

---

## ⚙️ Setup and Run Instructions

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (or a standard SQL Server instance)
- Visual Studio 2026 or another .NET 10-compatible IDE

### 1. Clone the Repository

```bash
git clone https://github.com/botletebele66/RaceDay.API_Part2.git
cd RaceDay.API_Part2
```

### 2. Configure the Database Connection

Configure `ConnectionStrings:DefaultConnection` in User Secrets or `appsettings.Development.json` for your SQL Server instance. Do not commit credentials to the repository.

### 3. Run the API

From the repository root, run:

```bash
dotnet restore RaceDay.API_Part2.csproj
dotnet run --project RaceDay.API_Part2.csproj
```

The API will start, and you can access the Swagger UI at the URL printed by the application (typically `https://localhost:<port>/swagger`). The application initializes the database at startup.

### 4. Run the Tests

```bash
dotnet test tests/RaceDay.APItests_Part2/RaceDay.APItests_Part2.csproj
dotnet test tests/RaceDay.Tests_Part3/RaceDay.Tests_Part3.csproj
```

The API suite runs the real API in-process against an EF Core in-memory database. The MVC suite covers view helpers, routing and access control.

---

## 🖥️ Part 3: MVC Front End (`RaceDay.MVC`)

The MVC application is the user interface for RaceDay. It never talks to the database or to Azure directly. Every request goes through the Part 2 REST API, and every file upload is sent to the API, which stores it in Azure Blob Storage (or in local `wwwroot/uploads` when no storage connection is configured).

### How it works

- **Login and registration** are posted from MVC to `api/auth/login` and `api/auth/register`. The API session cookie is kept in the MVC's server-side session and forwarded on each API call, so the browser never holds an API credential.
- **Session state** is kept for 30 minutes of inactivity, matching the API.
- **Access control** is enforced in MVC by `[RequireRole]`. Anonymous users are redirected to the login page with a return URL. A participant opening an organiser page gets a 403 page.
- **Logout** clears the session and redirects to the home page.
- **Input validation** runs in MVC first. API errors are shown as friendly messages, never as raw exception output.
- **Times** are stored in UTC by the API and shown in South African Standard Time (UTC+2).

### Pages

| Area | Who | Page | What it does |
|---|---|---|---|
| Home | Everyone | `/` | Event listing with type, search and upcoming/all filters |
| Event details | Everyone | `/Home/Details/{id}` | Banner, facts, categories with fees; enrol button for participants |
| Login / Register | Everyone | `/Auth/Login`, `/Auth/Register` | Register as a participant or an organiser (organisers need the invite code) |
| Dashboard | Both | `/Dashboard` | Organiser: events, enrolment totals, upcoming dates. Participant: upcoming entries and completed races |
| My events (organiser) | Organiser | `/Events` | Create, edit, delete events; add and remove categories; banner upload |
| Manage event | Organiser | `/Events/Manage/{id}` | Categories, recent enrolments, links to participants and results |
| Participants | Organiser | `/Enrolments/Event/{id}` | Enrolments per event, grouped by category, with status |
| Enter results | Organiser | `/Results/Event/{id}` | Finish time (h/m/s) and position per participant |
| My events (participant) | Participant | `/Enrolments` | Upcoming and past entries with status; withdraw from upcoming events |
| My results | Participant | `/Results` | Finish time, placing and "47th out of 312 finishers" |
| Profile | Both | `/Profile` | Personal details, password change, profile picture upload |

### Status colour guide

The same colours are used everywhere a status appears.

| Stored status | Shown as | Colour |
|---|---|---|
| `Registered` | Confirmed | Cyan |
| `Started` | In progress | Amber |
| `Completed` | Completed | Green |
| `Withdrawn` | Withdrawn | Flame red |

### Requirements mapping

| Requirement | Where it is met |
|---|---|
| MVC does not access the database or Azure | Only `ApiClient` makes outbound calls, to `Api:BaseUrl` |
| Register and login through the MVC via the API | `AuthController`, `ApiClient` |
| Session state across pages | MVC session (`CurrentUser`), API cookie forwarded by `ApiClient` |
| Unauthenticated users blocked from protected pages | `RequireRoleAttribute` on every controller |
| Logout redirects | `AuthController.Logout` |
| Organiser dashboard | `DashboardController`, `Views/Dashboard/Index.cshtml` |
| Events CRUD with name, description, date, location, distance, type | `EventsController`, `_EventForm.cshtml` |
| Banner upload through the API | `EventsController` → `POST api/events/{id}/image` |
| Category management | `EventsController.AddCategory` / `DeleteCategory`, category builder |
| Enrolments per event and upcoming dates | `EnrolmentsController.Event`, dashboard timeline |
| Results entry (finish time and position) | `ResultsController.Event` / `Record` |
| Participant home with filters | `HomeController.Index` |
| Event detail and enrol | `HomeController.Details`, `EnrolmentsController.Enrol` |
| My events with status | `EnrolmentsController.Index` |
| My results with placing out of finishers | `ResultsController.Index`, API finisher count |
| Profile with picture upload | `ProfileController`, `POST api/users/{id}/picture` |
| Invalid input handled cleanly | Data annotations, validation summary, `ApiResult` friendly messages |
| Consistent sporting UI | `wwwroot/css/site.css`, Bootstrap 5 with the RaceDay palette |
| Dockerised | `RaceDay.MVC/Dockerfile` (API: root `Dockerfile`) |
| CI builds the MVC | `.github/workflows/ci.yml` |

### Run with Docker

Build and run each image. The MVC container reaches the API on the host through `host.docker.internal`, so the API must be published on port 5080.

```bash
# 1. API (needs a reachable SQL Server; this example points at SQL Server on the host)
docker build -t raceday-api .
docker run --rm -p 5080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=RaceDay;User Id=sa;Password=<your-password>;TrustServerCertificate=True" \
  -e Seed__Demo=true \
  -e ASPNETCORE_ENVIRONMENT=Development \
  raceday-api

# 2. MVC front end
docker build -t raceday-mvc RaceDay.MVC
docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway \
  -e Api__BaseUrl=http://host.docker.internal:5080/ \
  raceday-mvc
```

Open **http://localhost:8080**.

Or run the whole stack, including SQL Server, with Compose:

```bash
docker compose up --build
```

Then open **http://localhost:8090**. SQL Server runs in its own container, so no local SQL Server is needed.

### Run without Docker

```bash
# Terminal 1: API on the port the MVC expects (Api:BaseUrl in RaceDay.MVC/appsettings.json)
dotnet run --project RaceDay.API_Part2.csproj --urls http://localhost:5299

# Terminal 2: MVC
dotnet run --project RaceDay.MVC/RaceDay.MVC.csproj
```

### Configuration

| Setting | Used by | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | API | SQL Server connection |
| `Api:BaseUrl` / `Api__BaseUrl` | MVC | Address of the API, with trailing slash |
| `App:HttpsRedirect` / `App__HttpsRedirect` | Both | Turn HTTPS redirection on or off (off in the containers) |
| `Azure:Storage:ConnectionString` | API | Blob storage. Leave empty to use local `wwwroot/uploads` |
| `Azure:Storage:Container` | API | Container name, default `event-images` |
| `App:PublicBaseUrl` | API | Public address used in image URLs, optional |
| `Registration:OrganiserInviteCode` | API | Code required to register as an organiser |
| `Seed:Demo` | API | Seeds demo accounts and events into an empty database |

### Demo accounts

Seeded when `Seed:Demo` is `true` and the database is empty. All share the password `Password123!`.

| Role | Email |
|---|---|
| Organiser | `org1@raceday.co.za` |
| Participant | `participant1@example.com` |

Organiser invite code in Development: `RACEDAY-ORG-2026`.

### Video walkthrough

YouTube link: _to be added_

---

## 🔗 API Endpoints Summary

All endpoints are fully documented and testable via the Swagger UI. Below is a summary of the implemented endpoints:

| Resource | Method | Endpoint | Role Required | Description |
|---|---|---|---|---|
| **Auth** | `POST` | `/api/auth/register` | Public | Register a new user |
| **Auth** | `POST` | `/api/auth/login` | Public | Authenticate and start a session |
| **Auth** | `POST` | `/api/auth/logout` | Any | Clear the current session |
| **Auth** | `GET` | `/api/auth/me` | Any | Get current session user info |
| **Events** | `GET` | `/api/events` | Public | List all events |
| **Events** | `GET` | `/api/events/{id}` | Public | Get event details |
| **Events** | `POST` | `/api/events` | Organiser | Create a new event |
| **Events** | `PUT` | `/api/events/{id}` | Organiser | Update an event |
| **Events** | `DELETE` | `/api/events/{id}` | Organiser | Delete an event |
| **Categories** | `GET` | `/api/events/{eventId}/categories` | Public | List categories for an event |
| **Categories** | `POST` | `/api/events/{eventId}/categories` | Organiser | Add a category to an event |
| **Categories** | `PUT` | `/api/categories/{id}` | Organiser | Update a category |
| **Categories** | `DELETE` | `/api/categories/{id}` | Organiser | Delete a category |
| **Enrolments** | `GET` | `/api/enrolments` | Participant | View own enrolments |
| **Enrolments** | `POST` | `/api/enrolments` | Participant | Enrol in an event category |
| **Enrolments** | `DELETE` | `/api/enrolments/{id}` | Participant | Withdraw from an event |
| **Enrolments** | `GET` | `/api/events/{eventId}/enrolments` | Organiser | View all enrolments for an event |
| **Results** | `POST` | `/api/results` | Organiser | Capture result for an enrolment |

---

## 🔐 Authentication & Security

- **Session Management:** The API uses server-side session management (`AddSession`) to maintain the user's authenticated state. The `UserID` and `UserRole` are securely stored in the session.
- **Password Hashing:** Plain-text passwords are never stored. Passwords are hashed using **BCrypt** before being saved to the database.
- **Role-Based Access Control:** A custom `[AuthorizeRole]` attribute is applied to endpoints to enforce role permissions (for example, only Organisers can create events).
