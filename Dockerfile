# Use .NET 8 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution and project files
COPY *.sln ./
COPY QuickCashJobAPI/*.csproj ./QuickCashJobAPI/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build and publish the project
RUN dotnet publish QuickCashJobAPI/QuickCashJobAPI.csproj -c Release -o out

# Final stage: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Start the application
ENTRYPOINT ["dotnet", "QuickCashJobAPI.dll"]
