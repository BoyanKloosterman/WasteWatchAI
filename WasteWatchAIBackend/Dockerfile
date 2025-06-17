# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image voor runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
USER root
ARG BUILD_CONFIGURATION=Release
WORKDIR /app

# Copy only the main project file first
COPY ["WasteWatchAIBackend.csproj", "./"]

# Restore only the main project dependencies (this will skip test project references that don't exist)
RUN dotnet restore "WasteWatchAIBackend.csproj" --ignore-failed-sources

# Copy the rest of the source code (excluding test projects via .dockerignore)
COPY . .

# Build only the main project, skip tests
RUN dotnet build "WasteWatchAIBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
USER root
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WasteWatchAIBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WasteWatchAIBackend.dll"]