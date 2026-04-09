# Tela de Conexões - TI

Criar uma nova página `conexoes.html` acessível pelo submenu **TI → Conexões**. A tela concentra todas as integrações existentes do sistema, cada uma exibida como um card com dados técnicos e um indicador de status (Online / Offline) testado em tempo real via API do backend.

## Proposed Changes

### Frontend

#### [NEW] [conexoes.html](file:///C:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/conexoes.html)
- Nova página com o padrão visual das outras (sidebar idêntica com submenu TI expandido, topbar, glass-cards).
- Grid de cards, um por integração, contendo:
  - **Senior SQL**, **Senior REST API**, **SQL Local** e **Nexti API**.
  - **Badge de status** (🟢 Online / 🔴 Offline), testado via chamada ao novo endpoint `/api/connections/status`
  - **Botão "Verificar"** para testar manualmente a conexão
- Suporte a expansão futura: o layout em grid se adapta automaticamente a mais cards.

#### [NEW] [conexoes.html](file:///C:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/conexoes.html) (Já criado, será modificado)
- Adicionar o card da **Nexti API** no array `connectionsData`.

#### [MODIFY] Todos os demais HTMLs (home, rotinas, usuarios, grupos, dp, rh, precificacao)
- Adicionar `<li><a href="conexoes.html">Conexões</a></li>` no submenu TI.
- Adicionar `conexoes.html` às permissões do grupo Administrador no backend.

---

### Backend

#### [MODIFY] [Program.cs](file:///C:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/backend/Program.cs)
- Novo endpoint `GET /api/connections/status` que testa em paralelo cada integração existente e devolve um array com `{ name, online, latencyMs, error? }`.
  - **Senior SQL**: tenta abrir conexão ao SQL Server Senior + executar `SELECT 1`.
  - **Senior REST API**: tenta autenticar com `GetSeniorToken()` e verifica se retornou token válido.
  - **SQL Local (LoginSystem)**: testa conexão ao `connStr` + `SELECT 1`.
  - **Nexti API**: tenta realizar uma requisição de autenticação OAuth2 para `https://api.nexti.com/security/oauth/token`.
- Cada teste tem timeout de **5 s** para não travar a UI.

- Adicionar `conexoes.html` nas permissões do grupo Administrador no `GroupScreens`.

---

## Verification Plan

### Browser (manual)
1. Acesse `http://localhost:5000`.
2. Faça login com o usuário `admin`.
3. Expanda o menu **TI** na sidebar e clique em **Conexões**.
4. Verifique que os 3 cards aparecem: **Senior SQL**, **Senior REST API** e **SQL Local**.
5. Cada card deve ter um badge colorido (verde = Online, vermelho = Offline).
6. Clique em **Verificar** em cada card e confirme que o badge atualiza.
