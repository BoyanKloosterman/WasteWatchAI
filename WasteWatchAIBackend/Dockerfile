# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image voor runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
# Gebruik default user hier (verander alleen als nodig)
# USER $APP_UID   # Laat weg als dit problemen geeft met permissies

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
USER root
ARG BUILD_CONFIGURATION=Release
WORKDIR /app
COPY ["WasteWatchAIBackend.csproj", "./"]
RUN dotnet restore "WasteWatchAIBackend.csproj"
COPY . .
RUN dotnet build "WasteWatchAIBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
USER root
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WasteWatchAIBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WasteWatchAIBackend.dll"]
