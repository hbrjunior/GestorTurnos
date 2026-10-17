# Gestor de turnos — Etapa 01: Fundação do backend

Estrutura inicial da prova técnica: .NET 9 + SQL Server + EF Core 9 + xUnit.

## Estado desta entrega

Inclui projetos, entidades, mapeamento EF Core, OpenAPI/Swagger, endpoint de health e testes unitários da regra temporal pura.
**Ainda não inclui** CRUDs, validação transacional de sobreposição, Excel, feriados, Angular, Docker Compose nem migrations geradas.

## Pré-requisitos

- SDK .NET 9 (qualquer feature band compatível com global.json)
- SQL Server acessível localmente; criar banco via migrations na próxima etapa
- Ferramenta dotnet-ef versão 9

## Gerar solução no Windows (PowerShell)

A partir da raiz deste repositório:

```powershell
dotnet new sln -n GestorTurnos --format sln
dotnet sln GestorTurnos.sln add backend/GestorTurnos.Domain/GestorTurnos.Domain.csproj backend/GestorTurnos.Infrastructure/GestorTurnos.Infrastructure.csproj backend/GestorTurnos.Api/GestorTurnos.Api.csproj backend/GestorTurnos.Tests/GestorTurnos.Tests.csproj
dotnet restore GestorTurnos.sln
dotnet build GestorTurnos.sln
dotnet test GestorTurnos.sln
```

O arquivo `.sln` é gerado localmente para evitar depender de edição manual. Commitar o `.sln` após criá-lo.

## Configurar conexão sem salvar senha no Git

```powershell
cd backend/GestorTurnos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=GestorTurnos;User Id=sa;Password=SUA_SENHA_FORTE;Encrypt=True;TrustServerCertificate=True"
dotnet run --urls http://localhost:5080
```

Teste: `http://localhost:5080/health`, Swagger UI em `http://localhost:5080/swagger` e OpenAPI em `http://localhost:5080/openapi/v1.json` (em Development).

## Migrations (quando o SQL Server estiver disponível)

```powershell
dotnet tool install --global dotnet-ef --version 9.0.10
dotnet ef migrations add InitialCreate --project ../GestorTurnos.Infrastructure --startup-project . --output-dir Persistence/Migrations
dotnet ef database update --project ../GestorTurnos.Infrastructure --startup-project .
```

## Decisões

- Monólito simples separado em API, Domain, Infrastructure e Tests.
- CountryCode ISO 3166-1 alpha-2.
- Intervalos de turnos semiabertos `[início, fim)`; turnos adjacentes não conflitam.
- Turnos não atravessam meia-noite nesta versão. Não há conversão de fuso horário; data/hora local do colaborador.
- Exclusão de colaborador com turnos: restrita.
- API externa deverá ser resiliente e não bloquear o CRUD se estiver fora.
- Uso de IA: apoio na elaboração da estrutura e código inicial; revisar, executar e dominar todo o conteúdo antes da entrega.

## Próxima etapa

Adicionar contratos DTO, validações, CRUD de colaboradores, CRUD de turnos e proteção contra concorrência nas verificações de conflito.
