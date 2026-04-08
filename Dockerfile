# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0 AS build

WORKDIR /src

# Copy everything
COPY . .

# Restore (important: point to csproj)
RUN dotnet restore src/PROCTOR.API/PROCTOR.API.csproj

# Publish
RUN dotnet publish src/PROCTOR.API/PROCTOR.API.csproj -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0

WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Start app
ENTRYPOINT ["dotnet", "PROCTOR.API.dll"]
