FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy all backend projects
COPY backend/ ./backend/

# Tests disabled for production build - run tests in CI/CD pipeline instead
# RUN dotnet test "backend/Navtrack.Listener.Tests/Navtrack.Listener.Tests.csproj"

# Publish the Listener project
RUN dotnet publish "backend/Navtrack.Listener/Navtrack.Listener.csproj" -c Release -o /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Navtrack.Listener.dll"]
