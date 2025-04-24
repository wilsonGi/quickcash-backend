# Use .NET 8 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution and project files (update the path to match your real structure)
COPY *.sln ./
COPY backend/QuickCashJob/QuickCashJobAPI/*.csproj ./QuickCashJobAPI/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the code (preserve correct structure)
COPY . .

# Build and publish the project (also update path)
RUN dotnet publish backend/QuickCashJob/QuickCashJobAPI/QuickCashJobAPI.csproj -c Release -o out

# Final stage: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Start the application
ENTRYPOINT ["dotnet", "QuickCashJobAPI.dll"]
