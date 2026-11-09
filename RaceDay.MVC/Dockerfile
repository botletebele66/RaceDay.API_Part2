# RaceDay MVC front end. Build from the RaceDay.MVC folder:
#   docker build -t raceday-mvc RaceDay.MVC

# ---- Build stage: restore and publish with the full SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the dependency layer is cached until the project file changes.
COPY RaceDay.MVC.csproj ./
RUN dotnet restore RaceDay.MVC.csproj

COPY . ./
RUN dotnet publish RaceDay.MVC.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Runtime stage: ASP.NET runtime only, non-root user ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    App__HttpsRedirect=false \
    Api__BaseUrl=http://host.docker.internal:5080/

COPY --from=build /app/publish .

EXPOSE 8080

# The aspnet image ships with a non-root "app" user (UID 1654).
USER app

ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
