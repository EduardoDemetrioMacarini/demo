---

### [NEW] Módulo DP — Dossiê do Colaborador

#### [NEW] [dp.html](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/frontend/dp.html)
Página para consulta detalhada de funcionários.
- **Busca**: Input com busca em tempo real na Senior por nome ou matrícula.
- **Gaveta (Drawer)**: Quando um colaborador é selecionado, abre uma interface lateral ou central com:
    - **Header**: Foto (placeholder), Nome e Status (Cores: Verde=Ativo, Vermelho=Demitido/Afastado).
    - **Cards de Informação**:
        - **Pessoal**: CPF, RG, PIS, CNH, Data Nasc/Idade, Endereço e Contato.
        - **Contratual**: Matrícula, Admissão, Demissão, Salário Atual, Cargo e Centro de Custo.
        - **Financeiro**: Gráfico simples ou tabela com o valor Líquido dos últimos 6 meses.
        - **Históricos**: Tabela de Afastamentos, Promoções e Trocas de Centro de Custo.
        - **Férias**: Dias de saldo e data da última/próxima concessão.

#### [MODIFY] [Program.cs](file:///c:/Users/RPA_COMERCIAL/.gemini/antigravity/scratch/login_system/backend/Program.cs)
- `GET /api/employees/search?q={search}`: Busca rápida na `R034FUN`.
- `GET /api/employees/{numcad}/dossier`: Super consulta que faz join/union das tabelas:
    - `R034FUN`, `R033PES`, `R033END` (Básico)
    - `R044MOV` + `R044CAL` (Financeiro 6 meses)
    - `R038HFA` (Afastamento)
    - `R040FER` (Férias)
    - `R038HCA` (Cargos)
    - `R038HCC` (Centro Custo)

#### [MODIFY] Sidebar em todos os arquivos HTML
Adicionar link para "DP" abaixo de "Jurídico" ou "Financeiro".

---

## Verification Plan

### Busca de Funcionário
1. Digitar parte do nome na busca do DP.
2. Confirmar que retorna resultados da Senior.

### Integridade do Dossiê
1. Selecionar um funcionário ativo.
2. Validar se PIS, CPF, Endereço e Salário conferem.
3. Verificar se o histórico de 6 meses de pagamento aparece.
4. Validar se históricos de Cargo e Afastamento estão presentes.

### Manual Verification
- Testar comportamento com funcionário demitido (deve mostrar Data de Demissão).
- Verificar se a idade é calculada corretamente a partir da `datnas`.

