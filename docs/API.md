# Especificação da API REST e Tempo Real - MENOVA

O Menova disponibiliza uma API RESTful de alta velocidade concebida para o PWA dos garçons, ecrãs KDS de cozinha, terminais de caixa e integrações locais.

---

## 1. Convenções Globais

- **Formato**: `application/json; charset=utf-8`
- **Autenticação**: Cookies de Sessão HTTP-only ou Token de Dispositivo (`X-Dispositivo-Token`)
- **Fuso Horário**: Todas as datas devolvidas em formato ISO-8601 (`YYYY-MM-DDTHH:mm:ssZ`)
- **Idempotência**: Todos os pedidos POST de criação de pedidos e registo de pagamentos aceitam o cabeçalho:
  `Idempotency-Key: <UUID>`

---

## 2. Endpoints do PWA do Garçom e Sala

### 2.1 Obter Mapa de Mesas e Zonas
`GET /api/v1/sala/mesas`
- **Resposta**:
```json
[
  {
    "id": "018f2d50-1234-7000-8000-000000000001",
    "numero": "01",
    "lugares": 4,
    "estado": "Livre",
    "zonaNome": "Sala Principal",
    "contaAtualId": null,
    "totalContaAtual": 0.00
  }
]
```

### 2.2 Obter Catálogo do Menu
`GET /api/v1/menu/catalogo`
- **Resposta**:
```json
{
  "versao": 14,
  "categorias": [
    {
      "id": "018f2d50-2000-7000-8000-000000000001",
      "nome": "Pratos Típicos Moçambicanos",
      "artigos": [
        {
          "id": "018f2d50-3000-7000-8000-000000000001",
          "nome": "Frango à Zambeziana",
          "descricao": "Grelhado com leite de coco, alho, limão e piripiri",
          "preco": 650.00,
          "taxaIva": 16.00,
          "disponivel": true,
          "esgotadoManual": false,
          "imagemUrl": "/imagens/artigos/frango_zambeziana.jpg"
        }
      ]
    }
  ]
}
```

### 2.3 Criar Comanda / Pedido
`POST /api/v1/pedidos`
- **Cabeçalhos**: `Idempotency-Key: c9d8b7a6-1234-4567-8901-abcdef123456`
- **Corpo**:
```json
{
  "mesaId": "018f2d50-1234-7000-8000-000000000001",
  "observacoes": "Mesa junto à janela",
  "itens": [
    {
      "artigoId": "018f2d50-3000-7000-8000-000000000001",
      "quantidade": 2,
      "notas": "Picante bem vivo",
      "opcoesIds": ["018f2d50-4000-7000-8000-000000000001"]
    }
  ]
}
```

---

## 3. Endpoints do KDS (Cozinha e Bar)

### 3.1 Obter Tickets Ativos
`GET /api/v1/kds/tickets?estacaoId={id}`
- **Resposta**:
```json
[
  {
    "pedidoId": "018f2d50-5000-7000-8000-000000000001",
    "contaId": "018f2d50-6000-7000-8000-000000000001",
    "mesaNumero": "01",
    "zonaNome": "Sala Principal",
    "numeroSequencial": 1,
    "criadoEm": "2026-10-15T12:30:00Z",
    "minutosDecorridos": 4,
    "urgencia": "Normal",
    "itens": [
      {
        "itemId": "018f2d50-7000-7000-8000-000000000001",
        "nomeArtigo": "Frango à Zambeziana",
        "quantidade": 2,
        "notas": "Picante bem vivo",
        "estado": "EmPreparacao"
      }
    ]
  }
]
```

### 3.2 Avançar Estado de Item no KDS
`POST /api/v1/kds/item/{itemId}/avancar`
- Transita o estado: `Pendente` -> `EmPreparacao` -> `Pronto` -> `Entregue`.

---

## 4. Endpoints de Caixa e Pagamentos

### 4.1 Registar Pagamento / Liquidação
`POST /caixa/pagamento`
- **Cabeçalhos**: `Idempotency-Key: a1b2c3d4-5678-90ab-cdef-1234567890ab`
- **Corpo**:
```json
{
  "contaId": "018f2d50-6000-7000-8000-000000000001",
  "sessaoCaixaId": "018f2d50-8000-7000-8000-000000000001",
  "metodo": 1,
  "valor": 1300.00,
  "valorRecebido": 1500.00,
  "gorjeta": 50.00,
  "referencia": null
}
```

---

## 5. Tempo Real (SignalR WebSockets)

- **URL do Hub**: `/hub/menova`
- **Eventos Emitidos pelo Servidor**:
  - `MesaAlterada`: `(Guid mesaId, string estado, string numeroMesa)`
  - `ArtigoEsgotadoAlterado`: `(Guid artigoId, string nome, bool esgotado, long menuVersao)`
  - `NovoPedido`: `(Guid pedidoId, string numeroMesa, Guid? estacaoId)`
  - `EstadoItemPedido`: `(Guid itemId, string novoEstado, Guid? estacaoId)`
  - `AlertaCaixa`: `(string mensagem)`
