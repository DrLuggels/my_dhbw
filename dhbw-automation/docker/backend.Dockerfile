# Backend Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Backend_New/DHBWAutomation.API/DHBWAutomation.API.csproj", "DHBWAutomation.API/"]
COPY ["src/Backend_New/DHBWAutomation.Core/DHBWAutomation.Core.csproj", "DHBWAutomation.Core/"]
COPY ["src/Backend_New/DHBWAutomation.Infrastructure/DHBWAutomation.Infrastructure.csproj", "DHBWAutomation.Infrastructure/"]

RUN dotnet restore "DHBWAutomation.API/DHBWAutomation.API.csproj"

# Copy all source files
COPY src/Backend_New/DHBWAutomation.API/. DHBWAutomation.API/
COPY src/Backend_New/DHBWAutomation.Core/. DHBWAutomation.Core/
COPY src/Backend_New/DHBWAutomation.Infrastructure/. DHBWAutomation.Infrastructure/

# Build the application
WORKDIR "/src/DHBWAutomation.API"
RUN dotnet build "DHBWAutomation.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DHBWAutomation.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DHBWAutomation.API.dll"]
