# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
COPY global.json ./
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build and publish the application
RUN dotnet publish TravelAndAccommodationBookingPlatform.csproj -c Release -o out

# Use the official ASP.NET Core 9.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install necessary packages for debugging (optional, remove in production)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published app from the build stage
COPY --from=build-env /app/out .

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose the port the app runs on
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "TravelAndAccommodationBookingPlatform.dll"]
