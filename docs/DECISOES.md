# Registo de Decisões de Arquitetura (ADR) - MENOVA

Este documento regista formalmente todas as decisões técnicas, pressupostos e escolhas arquiteturais do sistema **Menova**.

---

## ADR 001: Localização, Fuso Horário e Moeda (Padrão Moçambique)
- **Estado**: Aprovado
- **Contexto**: O sistema destina-se a operação prioritária em Moçambique, com suporte total a regras e formatos regionais.
- **Decisão**:
  - **Fuso Horário**: `Africa/Maputo` (UTC+2) forçado em toda a camada aplicacional, formatação de logs, relatórios e ecrãs.
  - **Base de Dados**: PostgreSQL armazena todas as marcas temporais estritamente em UTC (`timestamptz`).
  - **Moeda**: Metical (**MZN**), com 2 casas decimais e símbolo "MT" conforme padrão moçambicano (`1 250,00 MT`).
  - **Idioma & Cultura**: `pt-PT` como idioma de interface (Português de Portugal) e cultura regional `pt-MZ` para datas (`dd/MM/yyyy`), horas (24h) e números.
  - **Identificação Fiscal**: NUIT (Número Único de Identificação Tributária) de 9 dígitos.
  - **Imposto**: Taxa de IVA por omissão em 16% (com preços a retalho ao consumidor final apresentados com IVA incluído).

---

## ADR 002: Runtime e Versão da Plataforma .NET
- **Estado**: Aprovado
- **Contexto**: O documento mestre previa .NET 10 LTS; o ambiente atual estável tem o SDK .NET 8.0 LTS instalado.
- **Decisão**: Adota-se **.NET 8.0 LTS** com C# 12 moderno. A solução é arquitetada de forma limpa e modular, sem dependências proprietárias temporárias, garantindo transição transparente para .NET 10 LTS assim que for promovido a LTS corporativo.

---

## ADR 003: Separação Estrita de Ambientes (Desenvolvimento e Produção)
- **Estado**: Aprovado
- **Decisão**:
  - **Desenvolvimento (`Development`)**:
    - `appsettings.Development.json`
    - `deploy/docker-compose.yml`: Expõe a porta 5432 do PostgreSQL para inspeção por ferramentas como DBeaver/pgAdmin.
    - Seeding automático de dados essenciais e credenciais conhecidas (`admin` / `Admin123!`).
  - **Produção (`Production`)**:
    - `appsettings.Production.json` com variáveis de ambiente injetadas via `.env` (permissões 600).
    - `deploy/docker-compose.prod.yml`: PostgreSQL não expõe nenhuma porta no host (rede bridge interna `menova_net`).
    - Contentor da aplicação executado como utilizador não-root (`menova`), sistema de ficheiros só de leitura (`read_only: true`), `tmpfs` para `/tmp`.
    - Reverse proxy Caddy com HTTPS automático (CA interna local ou Let's Encrypt em VPS) e HTTP/2/3.

---

## ADR 004: Auditoria Imutável (WORM) com Cadeia Criptográfica SHA-256
- **Estado**: Aprovado
- **Decisão**:
  - Tabela `auditoria` operada em regime append-only.
  - Cada registo calcula `hash = SHA-256(hash_anterior || data_utc || utilizador_id || acao || entidade || entidade_id || payload_canonico)`.
  - Trigger em PostgreSQL (`fn_bloquear_mutacao_auditoria`) que bloqueia e lança exceção em qualquer tentativa de mutação manual (`UPDATE`, `DELETE`, `TRUNCATE`).
  - A escrita da auditoria é atómica com a transação de negócio.

---

## ADR 005: 100% Offline e Recursos Locais
- **Estado**: Aprovado
- **Decisão**: Nenhuma dependência externa de CDN (Google Fonts, Cloudflare, unpkg, etc.). Todos os ativos (Bootstrap 5.3, Bootstrap Icons, Chart.js, JS SignalR, fontes) são embutidos e servidos localmente via `wwwroot`.

---

## ADR 006: Escopo v1 - Ausência de Gestão de Stock
- **Estado**: Aprovado
- **Decisão**: Não existem tabelas de inventário ou movimentos de stock na v1. É implementado apenas o mecanismo de "Esgotado Manual (86)" por artigo (`esgotado_manual = true`), ativado e desativado com um toque na cozinha, bar ou gestão.

---

## ADR 007: Idempotência de Pedidos e Pagamentos com Cabeçalho `Idempotency-Key`
- **Estado**: Aprovado
- **Decisão**:
  - Toda a criação de pedidos e registo de pagamentos suporta o cabeçalho HTTP `Idempotency-Key`.
  - Em caso de falha de rede ou reenvio repetido pelo cliente, o servidor recupera a resposta original em cache persistente na tabela `chaves_idempotencia` com expiração de 24 horas, evitando pedidos e cobranças duplicadas.

---

## ADR 008: Fecho Z Diário com Encadeamento SHA-256 e Proteção WORM
- **Estado**: Aprovado
- **Decisão**:
  - O Talão Z representa o fecho diário/turno fiscal definitivo.
  - Possui numeração sequencial contínua sem buracos (Z-1, Z-2, etc.).
  - Cada registo em `documentos_z` assina o hash do documento anterior (`HashAnterior -> Hash`), criando uma cadeia à prova de adulteração.
  - A tabela `documentos_z` é blindada pelo mesmo trigger WORM do PostgreSQL que impede alterações e remoções.

---

## ADR 009: Impressão Térmica ESC/POS em Rede Direta (TCP 9100)
- **Estado**: Aprovado
- **Decisão**:
  - Comunicação direta via TCP/IP na porta 9100 com impressoras térmicas de 80mm (42 colunas) e 58mm (32 colunas).
  - Geração nativa de comandos ESC/POS (`ESC @`, `ESC a`, `GS V`, corte de papel, gaveta).
  - Envio assíncrono e não-bloqueante: a eventual indisponibilidade física da impressora não bloqueia o fluxo de pedidos nem o operador de caixa.

---

## ADR 010: Relatórios Analíticos com Tabelas Fato de Agregação Diária
- **Estado**: Aprovado
- **Decisão**:
  - Tabela `fato_vendas_diarias` armazena o histórico consolidado por data, artigo e categoria.
  - Relatórios executivos e gráficos não realizam leituras pesadas sobre as tabelas operacionais ao vivo.
  - Exportação nativa disponível em CSV (com BOM para Excel moçambicano), Excel (.xlsx via ClosedXML) e PDF profissional (via QuestPDF Community).

---

## ADR 011: Backups Locais com Rotação Automática (7D/4S/12M) e Assinatura SHA-256
- **Estado**: Aprovado
- **Decisão**:
  - Mecanismo de backup que executa `pg_dump` ou fallback nativo SQL comprimido em GZip.
  - Cada ficheiro gera um companheiro `.sha256` verificado a cada leitura para certificar que o arquivo não sofreu corrupção física.
  - Política de rotação automática mantém 7 diários, 4 semanais e 12 mensais.
