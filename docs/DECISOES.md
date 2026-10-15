# Registo de Decisões de Arquitetura (ADR) - MENOVA

Este documento regista formalmente todas as decisões técnicas, pressupostos e escolhas arquiteturais do sistema **Menova**.

---

## ADR 001: Localização, Fuso Horário e Moeda (Padrão Moçambique)
- **Estado**: Aprovado
- **Contexto**: O sistema destina-se a operação prioritária em Moçambique, com suporte total a regras e formatos regionais.
- **Decisão**:
  - **Fuso Horário**: `Africa/Maputo` (UTC+2) forçado em toda a camada aplicacional, formatação de logs, relatórios e ecrãs.
  - **Base de Dados**: PostgreSQL armazena todas as marcas temporais estritamente em UTC (`timestamptz`).
  - **Moeda**: Metical (**MZN**), com 2 casas decimais e formatação de milhar com espaço ou ponto conforme padrão (`1 250,00 MZN`).
  - **Idioma & Cultura**: `pt-PT` como idioma de interface (Português de Portugal) e cultura regional `pt-MZ` para datas (`dd/MM/yyyy`), horas (24h) e números.
  - **Identificação Fiscal**: NUIT (Número Único de Identificação Tributária) de 9 dígitos.
  - **Imposto**: Taxa de IVA por omissão em 16% (configurável).

---

## ADR 002: Runtime e Versão da Plataforma .NET
- **Estado**: Aprovado
- **Contexto**: O documento mestre previa .NET 10 LTS; o ambiente atual estável em Ubuntu 24.04 LTS tem o SDK .NET 8.0 LTS instalado.
- **Decisão**: Adota-se **.NET 8.0 LTS** com C# 12 moderno. A solução é arquitetada de forma pura e modular, sem dependências proprietárias temporárias, garantindo transição transparente para .NET 10 LTS assim que este for lançado pela Microsoft.

---

## ADR 003: Separação Estrita de Ambientes (Desenvolvimento e Produção)
- **Estado**: Aprovado
- **Decisão**:
  - **Desenvolvimento (`Development`)**:
    - `appsettings.Development.json`
    - `deploy/docker-compose.yml`: Expõe a porta 5432 do PostgreSQL para inspeção por ferramentas como DBeaver/pgAdmin.
    - Seeding automático de dados essenciais e credenciais de desenvolvimento conhecidas (`admin` / `MenovaAdmin@2026`).
    - Exibição de detalhes de diagnóstico e logs em consola colorida.
  - **Produção (`Production`)**:
    - `appsettings.Production.json` com variáveis de ambiente injetadas via `.env` (permissões 600).
    - `deploy/docker-compose.prod.yml`: PostgreSQL não expõe nenhuma porta no host (rede bridge interna `menova_net`).
    - Contentor da aplicação executado como utilizador não-root (`menova`), sistema de ficheiros só de leitura (`read_only: true`), `tmpfs` para `/tmp`.
    - Reverse proxy Caddy com HTTPS automático (CA interna local ou Let's Encrypt em VPS) e HTTP/2/3.
    - Obrigatoriedade de alteração imediata de palavra-passe no primeiro acesso do Administrador.

---

## ADR 004: Auditoria Imutável (WORM) com Cadeia Criptográfica
- **Estado**: Aprovado
- **Decisão**:
  - Tabela `auditoria` operada em regime append-only.
  - Cada registo calcula `hash = SHA-256(hash_anterior || data_utc || utilizador_id || acao || entidade || entidade_id || payload_canonico)`.
  - Trigger em PostgreSQL (`BEFORE UPDATE OR DELETE OR TRUNCATE`) que bloqueia e lança exceção em qualquer tentativa de mutação manual ou por injeção.
  - A escrita da auditoria é atómica com a transação de negócio.

---

## ADR 005: 100% Offline e Recursos Locais
- **Estado**: Aprovado
- **Decisão**: Nenhuma dependência externa de CDN (Google Fonts, Cloudflare, unpkg, etc.). Todos os ativos (Bootstrap 5.3, Bootstrap Icons, JS, fontes) são embutidos e servidos localmente via `wwwroot`.

---

## ADR 006: Escopo v1 - Ausência de Gestão de Stock
- **Estado**: Aprovado
- **Decisão**: Não existem tabelas de inventário ou movimentos de stock na v1. É implementado apenas o mecanismo de "Esgotado Manual (86)" por artigo (`esgotado_manual = true`), ativado e desativado com um toque na cozinha, bar ou gestão.
