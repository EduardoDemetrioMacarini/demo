# Cálculo Definitivo por Escala Individual (3967 Colaboradores)

Identificamos que a parametrização de tipos de horários do Senior (`TipHor`) não é padronizada para todas as escalas (algumas usam Tip 5 para trabalho, outras usam Tip 1). Além disso, o calendário gerado (`R006CES`) está vazio.

Para garantir que o cálculo respeite a escala de **todos** os colaboradores, utilizaremos uma lógica baseada no **Nome do Horário** e na **Regra da Escala (R006HOR)**.

## User Review Required

> [!IMPORTANT]
> - **Detecção por Nome:** Um dia será considerado **Trabalho** se o nome do horário **NÃO** contiver as palavras: `FOLGA`, `DSR`, `COMPENSADO`, `COMPENS`, `FERIADO` ou `DESCANSO`. 
> - **Assumption:** Como 99% das escalas são do tipo **'P'** (Prorrogáveis/Semanais), mapearemos a Sequência 1 do ciclo sempre para a **Segunda-Feira**.
> - **Escopo:** Esta regra será processada para todos os 3.967 colaboradores simultaneamente.

## Proposed Changes

### [Component Name] Backend (C#)

#### [MODIFY] [Program.cs](file:///C:/Users/SETUP/.gemini/antigravity/scratch/login_system/backend/Program.cs)
- Atualizar o helper `GetScaleWorkDays` para:
    1. Incluir o campo `deshor` (Nome do Horário) na consulta.
    2. Filtrar dias de trabalho baseando-se na ausência de palavras-chave de folga (DSR/Folga).
    3. Aplicar a projeção semanal fixa (Seg-Dom) para todas as escalas ativas.
- Disparar o Sincronismo Global para atualizar todo o banco local.

## Verification Plan

### Automated Tests
1. **Adilson de Agostinho Azevedo:** Deve manter os **20 dias**.
2. **Escala 565 / 790 (Anteriormente Base ZERO):** Devem agora exibir valores baseados em seus dias de trabalho (ex: 22 ou 24 dias).
3. **Contagem Global:** Validar se o número de "Base ZERO" caiu drasticamente (perto de zero).

### Manual Verification
- Visualizar na interface que os colaboradores que trabalham em regimes diferenciados (ex: 6x1) possuem Bases maiores (ex: 24 ou 25 dias).
