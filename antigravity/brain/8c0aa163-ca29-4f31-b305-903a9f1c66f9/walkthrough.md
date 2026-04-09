# Walkthrough: Sistema Online via Ngrok

O sistema foi configurado com sucesso para acesso externo, permitindo que usuários fora da rede local acessem os painéis de RH, Jurídico e TI.

## Link de Acesso Público

> [!IMPORTANT]
> **URL do Sistema:** [https://hilaria-nonleguminous-chester.ngrok-free.dev](https://hilaria-nonleguminous-chester.ngrok-free.dev/index.html)

## O que foi realizado

1.  **Instalação do Ngrok**: O executável do Ngrok foi baixado, extraído e configurado com o Authtoken fornecido.
2.  **Túnel de Backend**: Iniciamos um túnel HTTP para a porta `5000`.
3.  **Servidor de Arquivos**: Configuramos o Backend para servir o conteúdo da pasta `frontend`, permitindo que o link do Ngrok carregue a interface visual (index.html, etc.) e a API simultaneamente.
4.  **Atualização do Frontend**:
    -   Todas as referências a `localhost:5000` foram substituídas pela URL pública do Ngrok.
    -   Adicionado o header `ngrok-skip-browser-warning` em todas as requisições de API para evitar a tela de aviso do Ngrok.
4.  **Backend Ativo**: O script `Iniciar_Backend.bat` foi executado para garantir que a API esteja processando as requisições.

## Como manter o sistema online

Para que o link continue funcionando, o computador atual deve permanecer ligado com os seguintes processos rodando:
-   **Terminal do Backend**: Rodando o `Program.cs`.
-   **Terminal do Ngrok**: Mantendo o túnel aberto.

Se o computador for reiniciado ou o Ngrok fechado, um novo link poderá ser gerado ao reiniciar o processo.

---
**Status:** ✅ Pronto para uso externo.
