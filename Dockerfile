# RaceDay API - Linux container, multi-stage build, runs as the non-root "app" user.
# Build from the repository root:   docker build -t raceday-api -f Dockerfile .

# ---- Build stage: restore (cached on the project file), then publish ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RaceDay.API_Part2.csproj ./
RUN dotnet restore RaceDay.API_Part2.csproj

COPY . ./
RUN dotnet publish RaceDay.API_Part2.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage: only the published output and the ASP.NET runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    App__HttpsRedirect=false

COPY --from=build /app/publish .

# Local image fallback (used when no Azure connection string is configured) writes to wwwroot/uploads.
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.API_Part2.dll"]
