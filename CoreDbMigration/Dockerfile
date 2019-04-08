FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app


FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["CoreDbMigration/CoreDbMigration.csproj", "CoreDbMigration/"]
COPY ["Migration/Migration.csproj", "Migration/"]
RUN dotnet restore "CoreDbMigration/CoreDbMigration.csproj"
COPY . .
WORKDIR "/src/CoreDbMigration"
RUN dotnet build "CoreDbMigration.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CoreDbMigration.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CoreDbMigration.dll"]
