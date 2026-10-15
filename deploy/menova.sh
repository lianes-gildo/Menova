#!/usr/bin/env bash
# ==============================================================================
# MENOVA - Sistema de Gestão de Restaurante (Moçambique)
# Script de Arranque e Instalação Local / Produção para Linux e macOS
# ==============================================================================

set -e

COR_VINHO='\033[38;2;110;26;51m'
COR_VERDE='\033[0;32m'
COR_AMARELO='\033[1;33m'
COR_VERMELHO='\033[0;31m'
COR_RESET='\033[0m'

echo -e "${COR_VINHO}"
echo "=================================================================="
echo "    MENOVA - SISTEMA DE GESTÃO DE RESTAURANTE (v1.0)"
echo "    Padrão Moçambique | MZN (MT) | África/Maputo (CAT UTC+2)"
echo "=================================================================="
echo -e "${COR_RESET}"

# 1. Verificar Pré-requisitos
echo -e "${COR_AMARELO}[1/6] A verificar pré-requisitos do sistema...${COR_RESET}"

if ! command -v docker &> /dev/null; then
    echo -e "${COR_VERMELHO}ERRO: Docker não está instalado. Por favor instale o Docker antes de continuar.${COR_RESET}"
    exit 1
fi

if ! docker compose version &> /dev/null; then
    echo -e "${COR_VERMELHO}ERRO: Plugin 'docker compose' não encontrado. Atualize o Docker.${COR_RESET}"
    exit 1
fi

echo -e "${COR_VERDE}✓ Docker e Docker Compose detetados.${COR_RESET}"

# 2. Configurar Diretórios e Permissões
echo -e "${COR_AMARELO}[2/6] A preparar diretórios de dados e cópias de segurança...${COR_RESET}"
mkdir -p /var/menova/backups 2>/dev/null || mkdir -p ./backups
mkdir -p ./dados_pg 2>/dev/null || true
echo -e "${COR_VERDE}✓ Diretórios configurados.${COR_RESET}"

# 3. Criar Ficheiro .env se não existir
echo -e "${COR_AMARELO}[3/6] A validar variáveis de ambiente...${COR_RESET}"
if [ ! -f .env ]; then
    echo "A gerar ficheiro de ambiente .env com credenciais seguras..."
    SENHA_BD=$(tr -dc A-Za-z0-9 </dev/urandom | head -c 24 || echo "Menova_Secure_Db_2026")
    PEPPER_PIN=$(tr -dc A-Za-z0-9 </dev/urandom | head -c 32 || echo "Menova_Secure_Pepper_Maputo")
    cat <<EOF > .env
POSTGRES_DB=menova_db
POSTGRES_USER=menova_app
POSTGRES_PASSWORD=${SENHA_BD}
SECURITY__PINPEPPER=${PEPPER_PIN}
REGIONAL__MOEDA=MZN
REGIONAL__FUSOHORARIO=Africa/Maputo
ASPNETCORE_ENVIRONMENT=Production
EOF
    echo -e "${COR_VERDE}✓ Ficheiro .env criado.${COR_RESET}"
else
    echo -e "${COR_VERDE}✓ Ficheiro .env existente carregado.${COR_RESET}"
fi

# 4. Iniciar Contentores via Docker Compose
echo -e "${COR_AMARELO}[4/6] A iniciar contentores (PostgreSQL, Menova Web e Proxy Caddy)...${COR_RESET}"
cd "$(dirname "$0")/.."
docker compose -f deploy/docker-compose.prod.yml up -d --build

# 5. Aguardar Base de Dados Pronta
echo -e "${COR_AMARELO}[5/6] A aguardar arranque e saúde da base de dados...${COR_RESET}"
RETRIES=30
until docker compose -f deploy/docker-compose.prod.yml exec -T db pg_isready -U menova_app -d menova_db &> /dev/null || [ $RETRIES -eq 0 ]; do
    echo "A aguardar PostgreSQL ($RETRIES tentativas restantes)..."
    sleep 2
    RETRIES=$((RETRIES - 1))
done

if [ $RETRIES -eq 0 ]; then
    echo -e "${COR_VERMELHO}Aviso: A base de dados demorou a responder, os contentores continuam em segundo plano.${COR_RESET}"
else
    echo -e "${COR_VERDE}✓ Base de dados PostgreSQL 16 operacional.${COR_RESET}"
fi

# 6. Conclusão e Acesso
echo -e "${COR_AMARELO}[6/6] Menova pronto e operacional!${COR_RESET}"
echo ""
echo -e "${COR_VINHO}=================================================================="
echo "    SISTEMA MENOVA INICIADO COM SUCESSO"
echo "=================================================================="
echo -e "${COR_RESET}"
echo -e "Painel de Gestão:       ${COR_VERDE}http://localhost${COR_RESET} (ou https://localhost)"
echo -e "PWA dos Garçons:        ${COR_VERDE}http://localhost/garcom${COR_RESET}"
echo -e "Ecrã Cozinha/Bar (KDS): ${COR_VERDE}http://localhost/kds${COR_RESET}"
echo -e "Instalar Certificado:   ${COR_VERDE}http://localhost/instalar-certificado${COR_RESET}"
echo ""
echo -e "Credenciais por Omissão: ${COR_AMARELO}admin${COR_RESET} / ${COR_AMARELO}Admin123!${COR_RESET}"
echo -e "PIN de Supervisor:       ${COR_AMARELO}1234${COR_RESET}"
echo ""
echo "Para parar o sistema: docker compose -f deploy/docker-compose.prod.yml down"
echo "Para ver registos:    docker compose -f deploy/docker-compose.prod.yml logs -f app"
echo ""

# Abrir no navegador se ambiente gráfico estiver ativo
if [ -n "$DISPLAY" ] && command -v xdg-open &> /dev/null; then
    xdg-open http://localhost &> /dev/null || true
fi
