FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy all backend projects
COPY backend/ ./backend/

# Publish the API project
RUN dotnet publish "backend/Navtrack.Api/Navtrack.Api.csproj" -c Release -o /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Navtrack.Api.dll"]
