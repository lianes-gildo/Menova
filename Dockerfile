# ==============================================================================
# MENOVA - DOCKERFILE MULTI-STAGE (.NET 8.0 / C# 12 / NON-ROOT / HARDENED)
# ==============================================================================

# Estágio 1: Build & Publicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar ficheiros de projeto para restore em cache
COPY Menova.sln ./
COPY src/Menova.Domain/Menova.Domain.csproj src/Menova.Domain/
COPY src/Menova.Application/Menova.Application.csproj src/Menova.Application/
COPY src/Menova.Infrastructure/Menova.Infrastructure.csproj src/Menova.Infrastructure/
COPY src/Menova.Web/Menova.Web.csproj src/Menova.Web/
COPY tests/Menova.UnitTests/Menova.UnitTests.csproj tests/Menova.UnitTests/
COPY tests/Menova.IntegrationTests/Menova.IntegrationTests.csproj tests/Menova.IntegrationTests/

RUN dotnet restore Menova.sln

# Copiar o restante código-fonte e publicar em Release
COPY . .
WORKDIR /app/src/Menova.Web
RUN dotnet publish Menova.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Estágio 2: Runtime Seguro com PostgreSQL Client 16 oficial
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Instalar repositório oficial PostgreSQL para obter postgresql-client-16 exato
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates gnupg lsb-release && \
    install -d /etc/apt/keyrings && \
    curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor -o /etc/apt/keyrings/postgresql.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/postgresql.gpg] http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends postgresql-client-16 && \
    apt-get purge -y --auto-remove gnupg lsb-release && \
    rm -rf /var/lib/apt/lists/*

# Criar utilizador não-root específico 'menova'
RUN groupadd -g 10001 menova && \
    useradd -u 10001 -g menova -m -s /bin/bash menova && \
    mkdir -p /app/logs /app/media /app/backups && \
    chown -R menova:menova /app

COPY --from=build --chown=menova:menova /app/publish .

USER menova

ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=Africa/Maputo
ENV LC_ALL=pt_PT.UTF-8
ENV LANG=pt_PT.UTF-8

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Menova.Web.dll"]
