# Plano de Implementação: Gestão de Acessos e Grupos

Este plano detalha a implementação de um sistema de Controle de Acesso Baseado em Grupos (RBAC) e visibilidade de dados por Centro de Custo.

## User Review Required

> [!IMPORTANT]
> - Precisamos definir quais são os nomes exatos das telas para as permissões (ex: "Home", "Precificacao", "DP", "RH", "Rotinas TI", "Usuarios").
> - Por padrão, novos usuários pertencerão ao grupo "Padrao".

## Mudanças Propostas

### 1. Banco de Dados (SQL Server Local)
- **Groups**: `Id, Name`
- **GroupScreens**: `GroupId, ScreenPath`
- **Users**: Adicionar `GroupId (INT, NULLABLE)`
- **UserCostCenters**: `UserId, CostCenterCode`

### 2. Backend (Program.cs)
- **Inicialização**: Criar tabelas e inserir grupos iniciais ("Administrador", "Padrao").
- **Auth**: Retornar o `GroupId` e as permissões no login (ou fornecer endpoint para isso).
- **Filtros**:
    - Nos endpoints que buscam dados da Senior (ex: `/api/birthdays`), verificar se o usuário tem centros de custo restritos vinculados. Se não tiver nada na tabela `UserCostCenters`, vê tudo. Se tiver, filtra o SQL da Senior com `IN (...)`.

### 3. Frontend
- **App.js**: Guardar o grupo/permissões no `localStorage`.
- **Usuarios.html**: 
    - Adicionar coluna de Grupo na tabela.
    - Implementar troca de grupo (dropdown) na linha do usuário.
    - Adicionar modal/botão para gerenciar CCs permitidos por usuário.
- **Grupos.html [NOVA]**:
    - Tela de CRUD de grupos.
    - Lista de telas disponíveis (Home, Jurídico, DP, RH, TI, etc.) com checkboxes para definir o acesso do grupo.
- **Menu Centralizado**: Ajustar o menu lateral para esconder itens baseados no grupo do usuário (ler do `localStorage`).

## Plano de Verificação
- Criar usuário teste e verificar se cai no grupo "Padrao".
- Alterar grupo do usuário para ver se o menu reage.
- Vincular um CC específico a um usuário e verificar se a lista de aniversariantes diminui.
