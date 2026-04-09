# Login System Implementation Plan

This plan details the implementation of a modern login screen backed by a local SQL Server.

## User Review Required

> [!IMPORTANT]  
> **Aprovação da Calculadora de Precificação (Jurídico)**
> Analisando sua imagem, extraí a seguinte lógica para criarmos a tela interativa de forma "rápida e fácil":
> 1. **Variáveis de Entrada na Tela:** Quantidade de Postos, Quantidade de Pessoas (por posto), Cargo, Salário Base, Adicionais, VA, VT e Insumos (Material/Uniforme). E também o Lucro desejado.
> 2. **Cálculos Automáticos Mágicos:** O sistema vai calcular automaticamente os **Encargos (ex: 53%)** em cima da remuneração e mastigar toda a fórmula até chegar no **C/ Lucro C/ Impostos (Valor Final)**. As variáveis de impostos (ISS/PIS/COFINS) serão calculadas em cascata.
> 3. **Histórico:** Clicando em "Salvar Cotação", a API vai gravar no SQL Server: Data, Hora, Usuário que gerou, Nome do Cliente alvo e o Valor Total.
> 
> Gostaria de confirmar: quer que os Encargos venham cravados em **53%** e os Impostos fiquem invisíveis/chumbados no código, ou quer que o usuário possa editar essas porcentagens na hora se ele precisar?

## Proposed Changes

### Database Setup
A SQL script to create a `Users` table storing `Username`, `PasswordHash`, and `Email`.

#### [NEW] database/schema.sql

### Backend Services
A minimal ASP.NET Core API app that runs locally, connects to the SQL Server via Entity Framework Core or Dapper, and provides the necessary API endpoints.

#### [NEW] backend/Program.cs
#### [NEW] backend/backend.csproj
#### [NEW] backend/appsettings.json

### Frontend Interfaces
Premium, animated, and responsive HTML interfaces.

#### [NEW] frontend/index.html (Login)
#### [NEW] frontend/register.html
#### [NEW] frontend/reset.html
#### [NEW] frontend/styles.css
#### [NEW] frontend/app.js

## Verification Plan

### Automated/Manual Tests
- Ensure `dotnet run` runs the backend successfully.
- Verify that inserting a new user via the UI correctly hashes the password and writes to the SQL Server.
- Verify that attempting to login with valid/invalid credentials responds correctly.
