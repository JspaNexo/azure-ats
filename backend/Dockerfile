# Build context: backend/ (docker build -f Dockerfile . from this folder).
# Stage 1: Build & Publish with .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files for optimized Docker layer caching
COPY ["src/Ats.Domain/Ats.Domain.csproj", "Ats.Domain/"]
COPY ["src/Ats.Application/Ats.Application.csproj", "Ats.Application/"]
COPY ["src/Ats.Infrastructure/Ats.Infrastructure.csproj", "Ats.Infrastructure/"]
COPY ["src/Ats.Api/Ats.Api.csproj", "Ats.Api/"]

# Restore NuGet dependencies
RUN dotnet restore "Ats.Api/Ats.Api.csproj"

# Copy full source tree and publish
COPY src/ ./
WORKDIR "/src/Ats.Api"
RUN dotnet publish "Ats.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create persistent storage directory for PDFs
RUN mkdir -p /app/Storage

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Ats.Api.dll"]

