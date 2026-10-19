
# Etapa 1 — Compilação
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore backend/GestorTurnos.Api/GestorTurnos.Api.csproj

RUN dotnet publish backend/GestorTurnos.Api/GestorTurnos.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Etapa 2 — Execução
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER $APP_UID

# Disponibiliza uma ferramenta leve para os healthchecks HTTP
USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID

ENTRYPOINT ["dotnet", "GestorTurnos.Api.dll"]
