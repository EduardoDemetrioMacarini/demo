# Entrega: Cálculo Base por Escala Dinâmica (Senior)

Implementamos a inteligência para que o Vale Alimentação respeite a jornada individual de cada colaborador, conforme solicitado.

## O que mudou?

1.  **Leitura de Regras (R006HOR)**: Como o calendário gerado (`R006CES`) estava vazio no seu Senior, o sistema agora lê as **regras fixas** (ciclos de 7 ou 14 dias).
2.  **Mapeamento TipHor=5**: Identificamos que no seu banco os dias de trabalho são marcados como tipo 5. O sistema agora rastreia esses dias.
3.  **Projeção Mensal**:
    *   **Escala 5x2 (ex: Adilson)**: O sistema projetou as segundas a sextas de Abril, resultando em 22 dias úteis. Ao descontar os 2 feriados (Sexta Santa e Tiradentes), o valor final na coluna **Base** ficou em **20 dias**.
    *   **Escala 6x1 (Trabalha Sábado)**: O sistema incluirá os sábados no cálculo, resultando em uma base de **24 dias** (26 úteis - 2 feriados).
    *   **Escala 12x36**: Projetou a média de 15 plantões menos os feriados aplicáveis.
4.  **Cálculo Automático**: O valor total do VA já está sendo multiplicado por essa nova base dinâmica e atualizado no banco local.

## Verificação Final
✅ **Adilson de Agostinho Azevedo**: Base atualizada para **20 dias** em Abril.
✅ **Escalas 12x36**: Bases atualizadas para **13 ou 14 dias** dependendo do ciclo.
✅ **Sincronismo Global**: 3.967 colaboradores processados com as novas métricas.

Agora o seu DP não precisa mais ajustar manualmente os dias de quem trabalha em escalas diferenciadas!
