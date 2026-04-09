# Implementation Plan - Fix and Verify DP Dossier

This plan addresses the port mismatch between the frontend and backend and verifies the newly implemented "Dossiê do Colaborador" (Employee Dossier) feature.

## Proposed Changes

### Frontend Fixes

#### [MODIFY] [app.js](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/app.js)
- Update `API_URL` from `http://localhost:5289/api` to `http://localhost:5000/api` to match the backend configuration in `launchSettings.json`.

#### [MODIFY] [home.html](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/home.html)
- Ensure consistent use of `http://localhost:5000` for API calls. (Already exists but should be verified).

#### [MODIFY] [dp.html](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/dp.html)
- Ensure consistent use of `http://localhost:5000` for API calls. (Already exists but should be verified).

## Verification Plan

### Automated Tests
- No automated tests currently exist for this project. I will perform manual verification using the browser.

### Manual Verification
1. **Start Backend**: Run `dotnet run` in the `backend` directory.
2. **Verify Port**: Confirm backend is listening on `http://localhost:5000`.
3. **Test Login**: Open `index.html`, attempt to login. This verifies `app.js` can now reach the backend.
4. **Test DP Dossier**:
   - Navigate to `dp.html`.
   - Search for a known employee (e.g., "Edson" or a specific registration number).
   - Verify search results appear.
   - Click on a result to open the dossier drawer.
   - Verify all sections (Personal, Contractual, Financial, Absences, etc.) load data from the Senior database.
5. **Check Backend Logs**: Monitor the terminal for any SQL errors during Senior DB queries.

> [!IMPORTANT]
> The Senior database connection depends on the `seniorConnString` hardcoded in `Program.cs`. If the connection fails, it might be due to network restrictions or invalid credentials in that string.
