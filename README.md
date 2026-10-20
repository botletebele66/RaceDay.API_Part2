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
| **Repository** | [github.com/botletebele66/PROG6212-POE](https://github.com/botletebele66/PROG6212-POE.git) |

---

## 🚀 About Part 2

This repository contains the backend **RESTful API** that powers the RaceDay platform. Following the endpoint plan and database schema designed in Part 1, this API handles all data operations, enforces role-based access, and serves as the backbone for the MVC frontend to be built in Part 3.

### Tech Stack

- **Framework:** ASP.NET Core 8.0 Web API (C#)
- **Database:** SQL Server
- **ORM:** Entity Framework Core (Code-First Approach)
- **Authentication:** Session-based Authentication with BCrypt password hashing
- **API Documentation:** Swagger (Swashbuckle)
- **Testing:** xUnit & Moq
- **CI/CD:** GitHub Actions

---

## 📁 Project Structure

```text
PROG6212-POE/
│
├── README.md                         # This file - project documentation
│
├── .github/                          # GitHub configuration
│   └── workflows/
│       └── ci.yml                    # CI/CD workflow for build and test
│
├── docs/                             # Planning documentation from Part 1
│   ├── ERD_RaceEventSystem.png       # Entity Relationship Diagram
│   ├── API_Endpoint_Plan.pdf         # Complete API endpoint specifications
│   └── database-schema.sql           # SQL Server database script
│
├── src/                              # Source code for Part 2
│   └── RaceDay.API/
│       ├── Attributes/               # Custom authorization attributes
│       ├── Controllers/              # API Controllers (Auth, Events, etc.)
│       ├── Data/                     # EF Core DbContext
│       ├── DTOs/                     # Data Transfer Objects
│       ├── Helpers/                  # Password hashing and utility classes
│       ├── Models/                   # Database Entities
│       ├── appsettings.json          # Configuration and Connection Strings
│       └── Program.cs                # Application entry point & DI setup
│
├── tests/                            # Unit tests for Part 2
│   └── RaceDay.Tests/
│       ├── Controllers/              # Tests for API endpoints
│       └── Helpers/                  # Mocking helpers
│
└── RaceDay.sln                       # Solution file
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

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (or a standard SQL Server instance)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### 1. Clone the Repository

```bash
git clone https://github.com/botletebele66/PROG6212-POE.git
cd PROG6212-POE
```

### 2. Configure the Database Connection

Open `src/RaceDay.API/appsettings.json` and verify the connection string. It is configured to use LocalDB by default:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

If you are using a different SQL Server instance, update the `Server` value accordingly.

### 3. Apply Database Migrations

Navigate to the API project directory and run the Entity Framework migrations to create the database schema. Ensure you have the `dotnet-ef` tool installed (`dotnet tool install --global dotnet-ef`).

```bash
cd src/RaceDay.API
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run
```

The API will start, and you can access the Swagger UI at `https://localhost:5001/swagger` (or the port specified in your console output).

### 5. Run Unit Tests

To execute the test suite, navigate to the root directory and run:

```bash
dotnet test
```

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
- **Role-Based Access Control:** A custom `[AuthorizeRole]` attribute is applied to endpoints to enforce strict role permissions (e.g., only Organisers can create events).

---

## 🧪 Unit Testing

The project includes a comprehensive suite of unit tests in the `tests/RaceDay.Tests` project. The tests cover:

- **Authentication:** Successful registration, duplicate email rejection, valid/invalid logins.
- **Role-Based Access:** Verifying that Participants cannot access Organiser endpoints, and Organisers cannot access Participant endpoints.
- **Business Logic:** Verifying that event creation and enrolment logic behaves correctly.

Tests are written using **xUnit** and **Moq** (for mocking the database and HTTP context).

---

## 🔄 CI/CD Pipeline

A GitHub Actions workflow (`.github/workflows/ci.yml`) is configured to run on every push to the `main` branch. The pipeline performs the following steps:

1. Checks out the code.
2. Sets up the .NET SDK.
3. Restores NuGet packages.
4. Builds the solution.
5. Runs all unit tests.
6.
7. <img width="988" height="120" alt="image" src="https://github.com/user-attachments/assets/52797553-5670-43b0-bf33-8852b5d2bb86" />


### Build Status

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)

<img width="991" height="260" alt="CI/CD Screenshot" src="https://github.com/user-attachments/assets/3ef6f730-a7ad-4d0b-b646-8970d0894171" />

---

## 🎥 Video Presentation

Watch the Part 2 API Demonstration here: https://youtu.be/MUZnaBPyjAk

The video covers:

- 📊 Running the API and navigating Swagger.
- 🔐 Demonstrating registration, login, and session management.
- 🛡️ Showing role-based access in action (e.g., a Participant trying to create an event).
- 🗄️ Explaining the code structure (Controllers, DbContext, DTOs).
- 🧪 Running the unit tests and explaining the CI/CD pipeline.

---

## 🚀 What's Next — Part 3

In Part 3, an MVC frontend will be built to consume this API. It will provide a user-friendly interface for Organisers to manage events and for Participants to browse and enrol in races, completing the full-stack RaceDay platform.

---

*Maintained by Botshelo Letebele (ST10478568) — PROG6212*
