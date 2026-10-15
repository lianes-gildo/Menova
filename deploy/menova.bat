@echo off
chcp 65001 > nul
title Menova - Sistema de Gestao de Restaurante (Mocambique)

echo ==================================================================
echo     MENOVA - SISTEMA DE GESTAO DE RESTAURANTE (v1.0)
echo     Padrao Mocambique ^| MZN (MT) ^| Africa/Maputo (CAT UTC+2)
echo ==================================================================
echo.

:: 1. Verificar Docker
echo [1/5] A verificar o Docker Desktop...
docker --version > nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Docker Desktop nao foi detetado.
    echo Por favor instale ou inicie o Docker Desktop antes de continuar.
    pause
    exit /b 1
)

:: 2. Criar diretorios
echo [2/5] A preparar pastas de trabalho e copias de seguranca...
if not exist "backups" mkdir backups

:: 3. Criar .env se nao existir
echo [3/5] A verificar variaveis de ambiente...
if not exist ".env" (
    echo A gerar ficheiro .env...
    (
        echo POSTGRES_DB=menova_db
        echo POSTGRES_USER=menova_app
        echo POSTGRES_PASSWORD=Menova_Db_Pass_2026_Secure
        echo SECURITY__PINPEPPER=Menova_Pin_Pepper_Maputo_2026
        echo REGIONAL__MOEDA=MZN
        echo REGIONAL__FUSOHORARIO=Africa/Maputo
        echo ASPNETCORE_ENVIRONMENT=Production
    ) > .env
)

:: 4. Iniciar contentores Docker Compose
echo [4/5] A iniciar contentores (PostgreSQL, Aplicacao Web e Caddy)...
docker compose -f deploy/docker-compose.prod.yml up -d --build
if %errorlevel% neq 0 (
    echo AVISO: Falha ao compilar imagem de producao, a tentar docker-compose local...
    docker compose up -d
)

:: 5. Conclusao e Acesso
echo.
echo ==================================================================
echo     SISTEMA MENOVA PRONTO E OPERACIONAL!
echo ==================================================================
echo.
echo Painel de Gestao:       http://localhost (ou https://localhost)
echo PWA dos Garcons:        http://localhost/garcom
echo Ecra Cozinha/Bar (KDS): http://localhost/kds
echo Instalar Certificado:   http://localhost/instalar-certificado
echo.
echo Credenciais Iniciais: admin / Admin123!
echo PIN de Supervisor:    1234
echo.
echo A abrir navegador...
start http://localhost

echo Para encerrar o sistema, execute: docker compose -f deploy/docker-compose.prod.yml down
pause
