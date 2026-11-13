# RaceDay.API_Part2
# 🏃 RaceDay — Event Management System

### Part 2: RESTful API Development

---

## 📋 Project Overview

| | |
|---|---|
| **Student Name** | Botshelo Letebele |
| **Student Number** | ST10478568 |
| **Module** | PROG6212 – Part 2 |
| **Repository** | [github.com/botletebele66/RaceDay.API_Part2](https://github.com/botletebele66/RaceDay.API_Part2) |

[![Build](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml/badge.svg)](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml)

<img width="987" height="192" alt="image" src="https://github.com/user-attachments/assets/504725ee-0dd2-409a-8ee6-84cdefaf1fd4" />

---
## рџЋҐ Video Walkthrough

You can watch a video overview and demonstration of the project here: <img width="1365" height="585" alt="image" src="https://github.com/user-attachments/assets/0e120397-56d5-43b7-a40f-c5528d8354ec" />

**[RaceDay - Event Management System Overview](https://youtu.be/qjv4jQ3NKVc)**

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

### 4. Run Unit Tests

The test project is currently a companion project in the local Visual Studio solution and is not included in this GitHub repository. GitHub Actions builds the API project; tests can be run with `dotnet test <path-to-test-project.csproj>` when that project is available locally.

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
