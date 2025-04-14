# order-generator-api

Este projeto é um cliente FIX (initiator) construído em C# com a biblioteca [QuickFIX/n](https://github.com/connamara/quickfixn). Ele é responsável por gerar e enviar ordens de compra e venda para um servidor FIX (OrderAccumulator) e processar os relatórios de execução de volta.

## Funcionalidades

- Interface HTTP para submissão de ordens via API REST.
- Conversão das ordens REST para mensagens `NewOrderSingle` do protocolo FIX.
- Recebimento de mensagens `ExecutionReport` e `OrderCancelReject`.
- Controle de sessões FIX e gerenciamento assíncrono de respostas.
- Comunicação segura com o `OrderAccumulator`.

## Tecnologias Utilizadas

- .NET 8
- QuickFIX/n
- ASP.NET Core
- Swagger (para documentação da API)

## Requisitos

- [.NET 8.0 SDK ou superior](https://dotnet.microsoft.com/en-us/download)
- [QuickFIX/n](https://www.nuget.org/packages/QuickFIXn.FIX4.4/1.13.0?_src=template)
- OrderAccumulator executando localmente (porta e IP definidos no `initiator.cfg`) -> https://github.com/hamilton-cardoso/order-accumulator.git

## Como Executar

1. Clone o repositório:
```bash
git clone https://github.com/hamilton-cardoso/order-generator-api.git
cd order-generator-api
```

2. Verifique e ajuste o arquivo de configuração FIX `initiator.cfg` se necessário.

3. Execute a aplicação:
```bash
dotnet run --project OrderGenerator
```

4. Acesse a documentação da API via Swagger:
```
https://localhost:7268/swagger
```

## Estrutura

```
OrderGenerator/
├── Controllers/
│   └── OrdersController.cs
├── Application/
│   └── OrderService.cs
├── Infrastructure/
│   ├── Fix/
│   │   ├── FixClientApp.cs
│   │   ├── FixSessionManager.cs
│   │   └── initiator.cfg
├── Models/
│   ├── OrderDto.cs
│   └── FixOrderResultDto.cs
└── Program.cs
```

## Observações

- As mensagens FIX são assincronamente processadas e o resultado é retornado em até 5 segundos (timeout).
- As ordens são rejeitadas caso ultrapassem o limite de exposição financeira definido no OrderAccumulator.
