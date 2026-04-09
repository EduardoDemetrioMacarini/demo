# Walkthrough: Módulo de Recursos Humanos (HCM Senior)

Implementamos com sucesso o novo menu **Recursos Humanos** com integração via API REST ao **Senior HCM**.

## Principais Mudanças

1.  **Menu Lateral Atualizado**: Adicionada a nova categoria "Recursos Humanos" em todas as páginas do sistema.
2.  **Dashboard de Vagas**: Nova página `rh.html` que carrega vagas em tempo real da Senior.
3.  **Métricas de Tempo**: Exibição automática de quantos dias a vaga está aberta.
4.  **Insights de Recrutamento**: Dashboard que identifica o "Top Recrutador" e quem possui o "Maior Tempo Médio" para fechar vagas.
5.  **Gaveta de Candidatos**: Possibilidade de visualizar os candidatos inscritos em cada vaga em um painel lateral moderno.

## Como Visualizar

1.  Acesse o sistema normalmente.
2.  Na barra lateral esquerda, clique em **Recursos Humanos** -> **Vagas em Aberto**.
3.  Na página que abrir, você verá os cards de insights no topo e a lista de vagas logo abaixo.
4.  Clique em **Ver Candidatos** em qualquer vaga para abrir a gaveta lateral.

## Integração Técnica

- **Backend**: Novos endpoints em `/api/hr/vacancies` e `/api/hr/recruitment-insights` em `Program.cs`.
- **Autenticação**: Integrada com o serviço de login da Senior Platform usando as credenciais fornecidas.
- **Frontend**: Utiliza CSS moderno (glassmorphism) e ícones Boxicons para um visual premium.

> [!NOTE]
> Os insights são calculados agregando os dados de todas as vagas retornadas pela API da Senior.
