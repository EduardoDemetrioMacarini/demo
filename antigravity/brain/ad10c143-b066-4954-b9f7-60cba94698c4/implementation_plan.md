# Implementation Plan - Stability Improvements and Navigation Standardization

Improve the application's resilience by implementing global error handling and ensuring a consistent user experience across all modules.

## Proposed Changes

### Backend (Stability & Connections)

#### [MODIFY] [Program.cs](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/backend/Program.cs)
- **Nexti Connection**: Check and fix the Nexti API authentication endpoint (`https://api.nexti.com/security/oauth/token`) using the provided credentials (`client_id: gruposetup`, `client_secret: 611e03d43da3c23a1431027405c993e22b9643c4`). Ensure the response parsing handles errors correctly.
- Implement a **Global Exception Handling Middleware** to catch unhandled exceptions and return a JSON error response instead of crashing or showing a generic HTML error.
- Wrap critical SQL and External API endpoints in `try-catch` blocks to provide graceful fallback or detailed error messages.
- Refactor `GetSeniorToken()` to handle `JsonException` and network timeouts internally.
- Standardize API response formats for errors.

---

### Frontend (Consistency)

#### [MODIFY] [Frontend HTML Files](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/)
Standardize the TI submenu order and styling across the following files:
- `home.html`
- `dp.html`
- `rh.html`
- `precificacao.html`
- `usuarios.html`
- `grupos.html`
- `rotinas.html`
- `conexoes.html`

**Standard TI Submenu Order:**
1. Rotinas
2. Usuários
3. Grupos
4. Conexões

---

## Verification Plan

### Automated Tests
*None available in the current project. Verification will be manual.*

### Manual Verification
1. **Connectivity Check**:
   - Access the **Conexões** page.
   - Click "Verificar Agora" on all cards (Senior SQL, Senior REST, Local SQL, Nexti API).
   - Ensure the UI correctly reflects the state without hanging.
2. **Resilience Testing**:
   - Temporarily modify `appsettings.json` with an invalid `DefaultConnection`.
   - Verify the backend doesn't crash on startup (log error) or returns a clean error on API calls.
   - Simulate a network failure to the Senior API and verify the "Vagas em Aberto" page shows a user-friendly error instead of an empty/broken state.
3. **Navigation Audit**:
   - Visit every page in the system.
   - Verify the sidebar menu is identical in structure and styling on every page.
   - Confirm all links in the sidebar work as expected.
