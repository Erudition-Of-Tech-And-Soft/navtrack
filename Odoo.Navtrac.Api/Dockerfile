FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy Directory.Build.props first (defines TargetFramework)
COPY Directory.Build.props ./

# Copy backend (for dependencies) and Odoo.Navtrac.Api
COPY backend/ ./backend/
COPY Odoo.Navtrac.Api/ ./Odoo.Navtrac.Api/

# Publish the Odoo API project
RUN dotnet publish "Odoo.Navtrac.Api/Odoo.Navtrac.Api.csproj" -c Release -o /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Odoo.Navtrac.Api.dll"]
