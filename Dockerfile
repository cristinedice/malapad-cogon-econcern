# ==============================================================================
# Multi-stage Dockerfile for Malapad Cogon e-CONCERN (.NET 8.0)
# Optimized for Render.com, Railway, Fly.io, or standard Linux containers
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["MalapadCogonEConcern.csproj", "./"]
RUN dotnet restore "MalapadCogonEConcern.csproj"

# Copy source code and build
COPY . .
RUN dotnet publish "MalapadCogonEConcern.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Port 8080 is the standard port used by Render, Google Cloud Run, and ASP.NET 8
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published binaries
COPY --from=build /app/publish .

# Create directories for persistent SQLite database and upload storage
RUN mkdir -p /app/data /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "MalapadCogonEConcern.dll"]
