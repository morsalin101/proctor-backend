# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /src

# Copy everything
COPY . .

# Restore
RUN dotnet restore src/PROCTOR.API/PROCTOR.API.csproj

# Publish
RUN dotnet publish src/PROCTOR.API/PROCTOR.API.csproj -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose port (Coolify usually uses 3000/80)
EXPOSE 8080

# Start app
ENTRYPOINT ["dotnet", "PROCTOR.API.dll"]
