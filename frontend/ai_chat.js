/* ControlPro AI Assistant - Logic & Mock Engine */

// Inicializa o widget garantindo que o DOM já carregou, mesmo se injetado depois do evento principal
if (document.readyState !== 'loading') {
    injectAIWidget();
} else {
    document.addEventListener("DOMContentLoaded", injectAIWidget);
}

function injectAIWidget() {
    // 1. Injetar a folha de estilos se ainda não houver
    if (!document.querySelector("link[href='ai_chat.css']")) {
        const link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = "ai_chat.css";
        document.head.appendChild(link);
    }

    // 2. Criar a estrutura HTML do Chat e Botão
    const container = document.createElement("div");
    container.id = "ai-assistant-wrapper";
    container.innerHTML = `
        <!-- Floating Button -->
        <div id="ai-assistant-btn" onclick="toggleAIChat()" title="Conversar com o Assistente">
            <i class='bx bx-sparkles bx-burst'></i>
        </div>

        <!-- Chat Window -->
        <div id="ai-chat-window">
            <div id="ai-chat-header">
                <h4><div class="status-dot"></div> Assistente PRO</h4>
                <button id="ai-chat-close" onclick="toggleAIChat()"><i class='bx bx-x'></i></button>
            </div>
            
            <div id="ai-chat-messages">
                <div class="chat-msg bot">
                    Olá! Sou seu <strong>Assistente Virtual</strong>. Como posso ajudar com os dados de hoje? Você pode me perguntar sobre faturamento, maiores despesas ou pedir um resumo geral.
                </div>
            </div>
            
            <div id="ai-chat-input-area">
                <input type="text" id="ai-chat-input" placeholder="Faça uma pergunta sobre a ControlPro..." autocomplete="off">
                <button id="ai-chat-send" onclick="sendAIMessage()"><i class='bx bx-send'></i></button>
            </div>
        </div>
    `;

    document.body.appendChild(container);

    // Listeners para Enter no Inpput
    document.getElementById("ai-chat-input").addEventListener("keypress", function(e) {
        if (e.key === "Enter") {
            sendAIMessage();
        }
    });
}

function toggleAIChat() {
    const chatWindow = document.getElementById("ai-chat-window");
    const btnIcon = document.querySelector("#ai-assistant-btn i");
    
    if (chatWindow.classList.contains("open")) {
        chatWindow.classList.remove("open");
        btnIcon.className = "bx bx-sparkles bx-burst";
    } else {
        chatWindow.classList.add("open");
        btnIcon.className = "bx bx-message-rounded-dots";
        setTimeout(() => document.getElementById("ai-chat-input").focus(), 300);
    }
}

function appendMessage(sender, text) {
    const msgContainer = document.getElementById("ai-chat-messages");
    
    if (sender === 'typing') {
        const typingDiv = document.createElement("div");
        typingDiv.className = "chat-msg bot typing-msg";
        typingDiv.id = "typingIndicator";
        typingDiv.innerHTML = `
            <div class="typing-indicator">
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
                <div class="typing-dot"></div>
            </div>
        `;
        msgContainer.appendChild(typingDiv);
    } else {
        const div = document.createElement("div");
        div.className = "chat-msg " + sender;
        div.innerHTML = text; // Permite HTML como bolding
        msgContainer.appendChild(div);
    }
    
    msgContainer.scrollTop = msgContainer.scrollHeight;
}

function removeTypingIndicator() {
    const ind = document.getElementById("typingIndicator");
    if (ind) ind.remove();
}

function sendAIMessage() {
    const inputField = document.getElementById("ai-chat-input");
    const msg = inputField.value.trim();
    if (!msg) return;

    // Adiciona a mensagem do usuário
    appendMessage("user", msg);
    inputField.value = "";

    // Inicia a resposta da IA simulando tempo de processamento
    appendMessage("typing");
    
    setTimeout(() => {
        removeTypingIndicator();
        processAIResponse(msg.toLowerCase());
    }, 1500 + Math.random() * 1000); // 1.5s a 2.5s de delay para parecer real
}

/* =========================================
   CÉREBRO SIMULADO (MOCK NLP ENGINE)
========================================= */
function processAIResponse(query) {
    let response = "";

    if (query.includes("faturamento") || query.includes("receita") || query.includes("vendas")) {
        response = `Analisando os consolidados globais...<br><br>
        O <strong>Faturamento Acumulado (YTD)</strong> neste momento é de <strong>R$ 4.850.000,00</strong>, o que representa um crescimento de <strong style="color: #4bff7d;">+8.4%</strong> em relação ao mesmo período do ano passado.<br>
        A unidade que mais contribuiu foi a de Tecnologia.`;
    } 
    else if (query.includes("gasto") || query.includes("despesa") || query.includes("pagar") || query.includes("saídas")) {
        response = `As despesas mapeadas requerem atenção ao <strong>Fluxo de Caixa</strong> desta quinzena.<br><br>
        Sua principal obrigação é a <strong>Folha de Pagamento</strong> projetada para o dia 20, no valor de R$ 220 Milhões. Contudo, nosso saldo de disponibilidades atuais é suficiente para quitar os top 5 vencimentos da semana.`;
    } 
    else if (query.includes("resumo") || query.includes("geral") || query.includes("balanço")) {
        response = `Ótima pergunta. Aqui está a visão executiva rápida da empresa hoje:<br>
        <ul>
            <li><strong>Caixa:</strong> R$ 645.000 (Líquido)</li>
            <li><strong>Margem EBITDA:</strong> 28.5% (Alta)</li>
            <li><strong>Inadimplência:</strong> 4.2% (Em queda)</li>
        </ul>
        A saúde do negócio está <strong>Saudável</strong>, com recebimentos cobrindo confortavelmente os compromissos futuros próximos.`;
    } 
    else if (query.includes("rh") || query.includes("vagas") || query.includes("pessoas") || query.includes("colaborador")) {
        response = `No módulo de Recursos Humanos, identifico que o recrutamento está com <strong>12 vagas abertas</strong>. Recomendo focar na redução de admissões atrasadas para não sobrecarregar as alas de Produção e Suporte Técnico.`;
    }
    else if (query.includes("obrigado") || query.includes("valeu") || query.includes("tchau")) {
        response = `De nada! Qualquer outra dúvida gerencial ou de dados, é só me chamar. Estou monitorando o banco 24/7.`;
    }
    else {
        response = `Interessante. Não consegui cruzar a palavra exata com o nosso Data Lake local no momento. Para nossa <strong>demonstração</strong>, experimente perguntar sobre <strong>"resumo"</strong>, <strong>"faturamento"</strong> ou <strong>"despesas"</strong>!`;
    }

    appendMessage("bot", response);
}
