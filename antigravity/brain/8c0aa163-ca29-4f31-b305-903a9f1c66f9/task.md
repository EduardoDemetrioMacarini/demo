# Gestão de Grupos e Permissões

- [ ] Modelagem de Dados (SQL Server)
    - [ ] Criar tabela `Groups` (Administrador, Padrao, etc.)
    - [ ] Criar tabela `GroupPermissions` (GroupId, ScreenName)
    - [ ] Alterar tabela `Users` para incluir `GroupId`
    - [ ] Criar tabela `UserCostCenters` (UserId, CostCenterCode)
- [ ] Implementação Backend (Program.cs)
    - [ ] Endpoints para CRUD de Grupos e Permissões
    - [ ] Endpoint para associar Usuário a Grupo
    - [ ] Endpoint para associar Usuário a Centros de Custo
    - [ ] Lógica de filtragem por CC nos endpoints de busca (Aniversariantes, Dossiê, Vagas)
- [ ] Interface Frontend
    - [ ] Atualizar `usuarios.html` para exibir e trocar grupos (dropdown)
    - [ ] Criar nova tela `grupos.html` para Gestão de Grupos e Telas Liberadas
    - [ ] Implementar menu dinâmico (esconder telas não permitidas)
- [ ] Validação e Testes
    - [ ] Testar criação de novo usuário (Grupo Padrao)
    - [ ] Testar restrição de acesso a telas
    - [ ] Testar restrição de visibilidade de Centros de Custo
