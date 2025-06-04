FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["backend/src/Eduva.API/Eduva.API.csproj", "src/Eduva.API/"]
COPY ["backend/src/Eduva.Application/Eduva.Application.csproj", "src/Eduva.Application/"]
COPY ["backend/src/Eduva.Domain/Eduva.Domain.csproj", "src/Eduva.Domain/"]
COPY ["backend/src/Eduva.Infrastructure/Eduva.Infrastructure.csproj", "src/Eduva.Infrastructure/"]
COPY ["backend/src/Eduva.Shared/Eduva.Shared.csproj", "src/Eduva.Shared/"]

RUN dotnet restore "src/Eduva.API/Eduva.API.csproj"

COPY backend/src/ src/

WORKDIR "/src/src/Eduva.API"
RUN dotnet build "Eduva.API.csproj" -c "$BUILD_CONFIGURATION" -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Eduva.API.csproj" -c "$BUILD_CONFIGURATION" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Eduva.API.dll"]