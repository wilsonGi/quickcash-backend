# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the csproj file and restore
COPY ["QuickCashJobAPI/QuickCashJobAPI.csproj", "QuickCashJobAPI/"]
RUN dotnet restore "QuickCashJobAPI/QuickCashJobAPI.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/QuickCashJobAPI"
RUN dotnet publish "QuickCashJobAPI.csproj" -c Release -o /app/publish

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "QuickCashJobAPI.dll"]
