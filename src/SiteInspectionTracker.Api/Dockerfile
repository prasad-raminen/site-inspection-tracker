# Build context is the repository root:
#   docker build -f src/SiteInspectionTracker.Api/Dockerfile -t site-inspection-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first using only project files so the layer is cached between code changes.
COPY global.json Directory.Build.props ./
COPY src/SiteInspectionTracker.Domain/SiteInspectionTracker.Domain.csproj src/SiteInspectionTracker.Domain/
COPY src/SiteInspectionTracker.Application/SiteInspectionTracker.Application.csproj src/SiteInspectionTracker.Application/
COPY src/SiteInspectionTracker.Infrastructure/SiteInspectionTracker.Infrastructure.csproj src/SiteInspectionTracker.Infrastructure/
COPY src/SiteInspectionTracker.Api/SiteInspectionTracker.Api.csproj src/SiteInspectionTracker.Api/
RUN dotnet restore src/SiteInspectionTracker.Api/SiteInspectionTracker.Api.csproj

COPY src/ src/
RUN dotnet publish src/SiteInspectionTracker.Api/SiteInspectionTracker.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Non-root user provided by the official .NET 8 images.
USER app
ENTRYPOINT ["dotnet", "SiteInspectionTracker.Api.dll"]
