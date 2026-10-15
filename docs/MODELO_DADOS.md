# Modelo de Dados (PostgreSQL 16) - MENOVA

O esquema da base de dados do Menova foi desenhado segundo os mais rigorosos padrões de integridade referencial, concorrência otimista, auditoria imutável (WORM) e alta performance.

---

## 1. Convenções de Desenho

- **Nomenclatura**: Nomes de tabelas e colunas em `snake_case`, em português sem acentos (ex.: `sessoes_caixa`).
- **Chaves Primárias**: `uuid` (UUIDv7 ordenável) para entidades operacionais e mestres; `bigint identity` para tabelas de elevado volume e registos de auditoria.
- **Carimbos de Data/Hora**: Gravados estritamente em `timestamptz` (UTC).
- **Valores Monetários**: `numeric(18,4)` em preços e pagamentos; `numeric(18,3)` em quantidades. É expressamente proibido o uso de tipos de ponto flutuante (`float`/`double`).
- **Concorrência Otimista**: Coluna `versao` (`int`) com controlo de token de concorrência.
- **Eliminação Suave (Soft Delete)**: Colunas `esta_eliminado` (`boolean`), `eliminado_em` e `eliminado_por` em tabelas mestres. Vendas, faturas, movimentos de caixa, registos Z e auditoria nunca são eliminados.

---

## 2. Mapa de Tabelas por Módulo

### 2.1 Identidade e Controlo de Acesso
- `utilizadores`: Credenciais, nome completo, PIN hash (PBKDF2), estado, bloqueio e auditoria.
- `perfis`: Papéis de sistema (`ADMIN`, `GERENTE`, `CAIXA`, `GARCOM`, `COZINHA`).
- `permissoes`: Catálogo granular de mais de 30 permissões de sistema.
- `utilizadores_perfis`: Associação N:M entre utilizadores e perfis.
- `perfis_permissoes`: Associação N:M entre perfis e permissões.
- `dispositivos`: Registo e aprovação prévia de tablets/telemóveis autorizados na rede local.
- `sessoes_utilizador`: Sessões HTTP/Cookies ativas com controlo de revogação.
- `tentativas_login`: Histórico de segurança com proteção contra ataques de força bruta.

### 2.2 Configuração e Restaurante
- `restaurante`: Dados fiscais (Nome, NUIT de 9 dígitos, Morada, Fuso Horário `Africa/Maputo`, Moeda `MZN`, Taxa IVA 16%).
- `configuracoes`: Chave-valor para parâmetros operacionais.
- `sequencias`: Controlo de numeração contínua anual de comandas e documentos.

### 2.3 Sala e Mesas
- `zonas`: Divisões físicas do restaurante (Sala Principal, Terraço, Esplanada, Balcão).
- `mesas`: Mesas com identificador visual, capacidade de lugares, coordenadas na planta e estado (`Livre`, `Ocupada`, `AguardandoConta`, `Reservada`).
- `contas_mesas`: Relação entre a conta em curso e a mesa física.

### 2.4 Menu e Artigos
- `categorias`: Categorias de artigos (Entradas, Peixes & Mariscos, Carnes, Bebidas, etc.) com ordem de exibição.
- `artigos`: Artigos com preço, taxa de IVA (16%), alocação a estação de preparação e regra 86 (`esgotado_manual`).
- `artigos_imagens`: Fotografias locais dos artigos para o PWA do garçom e modo apresentação.
- `alergenios`: Alergénios alimentares legais (Glúten, Crustáceos, Amendoim, etc.).
- `artigos_alergenios`: Associação N:M entre artigos e alergénios.
- `grupos_opcoes`: Grupos de opções (Ponto da Carne, Picante, Acompanhamentos).
- `opcoes`: Itens de escolha (Mal passado, Picante Médio, Batata frita, etc.) com eventuais custos adicionais.
- `artigos_grupos_opcoes`: Associação N:M entre artigos e grupos de opções.
- `menu_versao`: Carimbo monotónico incrementado para invalidação de cache em PWAs.

### 2.5 Pedidos, Contas e KDS
- `estacoes_preparacao`: Pontos de confeção (Cozinha Principal, Grelhador, Bar).
- `contas`: Documento agregador de consumo de uma mesa ou balcão. Estado (`Aberta`, `Fechada`, `Anulada`), totais financeiros (Bruto, Desconto, IVA, Líquido).
- `pedidos`: Comanda numerada com observações, associada a uma conta.
- `itens_pedido`: Linhas da comanda com quantidade, preço unitário, IVA 16%, estado de preparação (`Pendente`, `EmPreparacao`, `Pronto`, `Entregue`, `Anulado`), motivo e PIN em anulações.
- `itens_pedido_opcoes`: Opções selecionadas pelo cliente em cada artigo.
- `chaves_idempotencia`: Tabela com cabeçalhos `Idempotency-Key` e respostas em JSON com expiração de 24h.

### 2.6 Caixa, Pagamentos e Talão Z
- `caixas`: Pontos de venda físicos configurados (Caixa 1, Caixa 2, etc.).
- `sessoes_caixa`: Turnos de caixa com operador, saldo inicial, total de vendas, suprimentos, sangrias, saldo teórico em gaveta e saldo contado.
- `movimentos_caixa`: Entradas/saídas físicas (Suprimentos e Sangrias com PIN de supervisor).
- `pagamentos`: Liquidações financeiras (Numerário com troco, POS Multicaixa, M-Pesa manual, e-Mola manual, etc.).
- `documentos_z`: Fechos diários/turnos definitivos assinados com encadeamento SHA-256 (`HashAnterior -> Hash`).

### 2.7 Impressão Térmica ESC/POS
- `impressoras`: Impressoras de rede Ethernet/Wi-Fi (IP, porta 9100, largura 42/32 colunas, corte de papel, gaveta).
- `trabalhos_impressao`: Fila assíncrona de trabalhos com dados binários ESC/POS, tentativas e estado de envio.

### 2.8 Relatórios e Tabelas Fato
- `fato_vendas_diarias`: Tabela analítica agregada com `(data, artigo_id, categoria_id, quantidade_vendida, total_bruto, total_desconto, total_iva, total_liquido)`.

### 2.9 Auditoria Imutável (WORM)
- `auditoria`: Registo sequencial criptografado.
- Protegida por trigger `fn_bloquear_mutacao_auditoria` que rejeita qualquer tentativa de `UPDATE`, `DELETE` ou `TRUNCATE`.
