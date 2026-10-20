# RaceDay API

ASP.NET Core REST API for managing race events, participants, enrolments, and results.

[![Build](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml/badge.svg)](https://github.com/botletebele66/RaceDay.API_Part2/actions/workflows/ci.yml)

## Features

- Event and category management
- Participant registration, authentication, and enrolments
- Race results management
- Role-based authorization and session-based authentication
- Swagger UI for exploring the API

## Technology

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core with SQL Server
- xUnit integration tests (in the companion test project in the local solution)
- GitHub Actions build workflow

## Project layout

```text
.github/
└── workflows/
    └── ci.yml
Attributes/       Custom authorization attributes
Controllers/      API endpoints
DTOs/             Request and response types
Data/             EF Core context and database initialization
Helper/           Shared helpers
Models/           Domain and database models
Properties/       Local launch settings

.dockerignore
.gitattributes
.gitignore
Dockerfile
Program.cs
README.md
RaceDay.API_Part2.csproj
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB or another SQL Server instance)

## Configure and run

1. Set the `ConnectionStrings:DefaultConnection` value for your SQL Server. For local development, use User Secrets or `appsettings.Development.json`; do not commit credentials.
2. From the API project directory, restore and run:

   ```bash
   dotnet restore
   dotnet run
   ```

3. Open the Swagger URL printed by the application (typically `https://localhost:<port>/swagger`).

The application initializes the database on startup. Demo data is seeded when the `Seed:Demo` configuration value is enabled.

## Tests

The test project is included in the local Visual Studio solution as a companion project. The GitHub repository should also contain that project before GitHub Actions can run those tests. Once it is available in the repository, run `dotnet test <path-to-test-project.csproj>`.

## Docker

The Dockerfile is configured for Windows containers. Build and run it using a Windows-container-capable Docker environment.
