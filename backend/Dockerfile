# Stage 1: Build & Publish with .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files for optimized Docker layer caching
COPY ["backend/src/Ats.Domain/Ats.Domain.csproj", "backend/src/Ats.Domain/"]
COPY ["backend/src/Ats.Application/Ats.Application.csproj", "backend/src/Ats.Application/"]
COPY ["backend/src/Ats.Infrastructure/Ats.Infrastructure.csproj", "backend/src/Ats.Infrastructure/"]
COPY ["backend/src/Ats.Api/Ats.Api.csproj", "backend/src/Ats.Api/"]

# Restore NuGet dependencies
RUN dotnet restore "backend/src/Ats.Api/Ats.Api.csproj"

# Copy full source tree and publish
COPY backend/src/ backend/src/
WORKDIR "/src/backend/src/Ats.Api"
RUN dotnet publish "Ats.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create persistent storage directory for PDFs
RUN mkdir -p /app/Storage

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ats.Api.dll"]

