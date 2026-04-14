const API_URL = window.location.origin + "/api";

// ========================================================
// SIDEBAR: Função global para abrir/fechar submenus
// ========================================================
window.toggleSubmenu = function(e, id) {
    e.preventDefault();
    const el = document.getElementById(id);
    if (!el) return;
    const isOpen = el.style.display !== 'none' && el.style.display !== '';
    el.style.display = isOpen ? 'none' : 'block';
};

// ========================================================
// SIDEBAR: Auto-detecta a página atual e aplica .active
// ========================================================
function highlightActiveMenu() {
    const currentPage = window.location.pathname.split('/').pop() || 'index.html';

    // Mapa de página → id do submenu pai a abrir
    const submenuMap = {
        'analytics_clevel.html': 'submenu-clv',
        'vendas.html':           'submenu-vendas',
        'financeiro_dre.html':   'submenu-fin',
        'rh_recrutamento.html':  'submenu-rh',
        'dp_quadro.html':        'submenu-dp',
        'dp.html':               'submenu-dp',
        'vale_alimentacao.html': 'submenu-dp',
        'rotinas.html':          'submenu-ti',
        'usuarios.html':         'submenu-ti',
        'grupos.html':           'submenu-ti',
    };

    // Remove todos os .active do menu
    document.querySelectorAll('.sidebar-menu a').forEach(a => a.classList.remove('active'));

    // Aplica .active no link que bate com a página atual
    document.querySelectorAll('.sidebar-menu a[href]').forEach(a => {
        const href = a.getAttribute('href').split('/').pop();
        if (href === currentPage) {
            a.classList.add('active');
        }
    });

    // Abre o submenu correto
    const parentSubmenuId = submenuMap[currentPage];
    if (parentSubmenuId) {
        const submenu = document.getElementById(parentSubmenuId);
        if (submenu) submenu.style.display = 'block';
    }
}

// ========================================================
// DEMO MODE: Local Storage Database initialization
// ========================================================
function initLocalDB() {
    let users = JSON.parse(localStorage.getItem("demoUsers"));
    if (!users) {
        users = [
            { id: 1, username: "admin", email: "admin@controlpro.com", password: "123", groupId: 1, isActive: true },
            { id: 2, username: "eduardo.demetrio", email: "eduardo.demetrio@gruposetup.com", password: "123", groupId: 1, isActive: true },
            { id: 3, username: "comercial.user", email: "vendas@empresa.com", password: "123", groupId: 2, isActive: true },
            { id: 4, username: "rh.admin", email: "rh@empresa.com", password: "123", groupId: 1, isActive: true }
        ];
    }
    
    // Auto-injeção do contato caso já tenha sido populado antes
    if (!users.find(u => u.email === "contato@grupocontrolpro.com")) {
        users.push({ id: users.length + 1, username: "contato", email: "contato@grupocontrolpro.com", password: "123", groupId: 1, isActive: true });
    }
    
    localStorage.setItem("demoUsers", JSON.stringify(users));

    if (!groups) {
        groups = [
            { id: 1, name: "Administrador", screens: ['home.html', 'dp.html', 'vale_alimentacao.html', 'rh_recrutamento.html', 'analytics_clevel.html', 'vendas.html', 'financeiro_dre.html', 'dp_quadro.html', 'rotinas.html', 'conexoes.html', 'usuarios.html', 'grupos.html'] },
            { id: 2, name: "Padrão", screens: ['home.html', 'vendas.html', 'financeiro_dre.html'] }
        ];
        localStorage.setItem("demoGroups", JSON.stringify(groups));
    } else {
        // Migration: force add missing screens to groups if they don't have it
        let modified = false;
        groups.forEach(g => {
            // Everyone gets Vendas for the demo
            if (!g.screens.includes('vendas.html')) {
                g.screens.push('vendas.html');
                modified = true;
            }
            // Admins get everything
            if (g.id === 1) {
                ['financeiro_dre.html', 'rh_recrutamento.html', 'dp_quadro.html', 'analytics_clevel.html'].forEach(s => {
                    if (!g.screens.includes(s)) { g.screens.push(s); modified = true; }
                });
            }
        });

        if (modified) {
            localStorage.setItem("demoGroups", JSON.stringify(groups));
        }

        // Migration: ensure current active session (userScreens) is updated with new dashboard screens
        const currentScreensStr = localStorage.getItem("userScreens");
        if (currentScreensStr) {
            let currentScreens = JSON.parse(currentScreensStr);
            let sessionModified = false;
            
            // Screens that everyone should have in this demo
            const mandatoryScreens = ['vendas.html', 'financeiro_dre.html'];
            mandatoryScreens.forEach(s => {
                if (!currentScreens.includes(s)) {
                    currentScreens.push(s);
                    sessionModified = true;
                }
            });

            // Admins get everything else
            if (localStorage.getItem("userGroupId") === "1") {
                ['rh_recrutamento.html', 'dp_quadro.html', 'analytics_clevel.html'].forEach(s => {
                    if (!currentScreens.includes(s)) {
                        currentScreens.push(s);
                        sessionModified = true;
                    }
                });
            }

            if (sessionModified) {
                localStorage.setItem("userScreens", JSON.stringify(currentScreens));
            }
        }
    }
}
initLocalDB();

function showAlert(message, isError = false) {
    const alertBox = document.getElementById("alertBox");
    if (!alertBox) return;
    
    alertBox.textContent = message;
    alertBox.className = "alert " + (isError ? "error" : "success");
    // Remove after 4s
    setTimeout(() => {
        alertBox.className = "alert";
        alertBox.textContent = "";
    }, 4000);
}

async function handleLogin(e) {
    e.preventDefault();
    const username = e.target.username.value.toLowerCase().trim();
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Processando...";
    
    setTimeout(() => {
        const users = JSON.parse(localStorage.getItem("demoUsers"));
        const groups = JSON.parse(localStorage.getItem("demoGroups"));
        
        const user = users.find(u => (u.username.toLowerCase() === username || u.email.toLowerCase() === username) && u.password === password);
        
        if (user) {
            if (!user.isActive) {
                showAlert("Seu usuário está desativado. Contate o TI.", true);
                e.target.querySelector('button').textContent = "Entrar";
                return;
            }
            const group = groups.find(g => g.id === user.groupId) || { screens: [] };
            showAlert("Login realizado com sucesso!", false);
            localStorage.setItem("loggedUsername", user.username);
            localStorage.setItem("userGroupId", user.groupId);
            localStorage.setItem("userScreens", JSON.stringify(group.screens));
            setTimeout(() => window.location.href = "home.html", 1000);
        } else {
            showAlert("Credenciais inválidas.", true);
            e.target.querySelector('button').textContent = "Entrar";
        }
    }, 600);
}

async function handleRegister(e) {
    e.preventDefault();
    const username = e.target.username.value.trim();
    const email = e.target.email.value.trim();
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Criando...";
    setTimeout(() => {
        let users = JSON.parse(localStorage.getItem("demoUsers"));
        if (users.find(u => u.username.toLowerCase() === username.toLowerCase() || u.email.toLowerCase() === email.toLowerCase())) {
            showAlert("Usuário ou e-mail já existe.", true);
            e.target.querySelector('button').textContent = "Criar Conta";
            return;
        }
        
        const newUser = {
            id: users.length > 0 ? Math.max(...users.map(u => u.id)) + 1 : 1,
            username, email, password, groupId: 2, isActive: true
        };
        users.push(newUser);
        localStorage.setItem("demoUsers", JSON.stringify(users));
        
        showAlert("Conta criada com sucesso!", false);
        setTimeout(() => window.location.href = "index.html", 1500);
    }, 600);
}

async function handleReset(e) {
    e.preventDefault();
    const username = e.target.username.value.trim();
    const email = e.target.email.value.trim();
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Redefinindo...";
    setTimeout(() => {
        let users = JSON.parse(localStorage.getItem("demoUsers"));
        let user = users.find(u => u.username.toLowerCase() === username.toLowerCase() && u.email.toLowerCase() === email.toLowerCase());
        
        if (user) {
            user.password = password;
            localStorage.setItem("demoUsers", JSON.stringify(users));
            showAlert("Senha redefinida com sucesso!", false);
            setTimeout(() => window.location.href = "index.html", 1500);
        } else {
            showAlert("Usuário ou E-mail não encontrado.", true);
            e.target.querySelector('button').textContent = "Redefinir Senha";
        }
    }, 600);
}

async function checkAccess() {
    const screens = JSON.parse(localStorage.getItem("userScreens") || "[]");
    const groupId = parseInt(localStorage.getItem("userGroupId") || "0");
    const currentPath = window.location.pathname.split("/").pop();
    
    // Se não estiver logado, redireciona (exceto páginas públicas)
    const publicPages = ['index.html', 'register.html', 'reset.html', ''];
    if (!localStorage.getItem("loggedUsername") && !publicPages.includes(currentPath)) {
        window.location.href = "index.html";
        return;
    }

    const sidebar = document.querySelector(".sidebar-menu");
    if (!sidebar) return;

    // Se for Admin (grupo 1) ou não tiver lista de screens (sessão antiga), mostra tudo
    if (groupId === 1 || screens.length === 0) return;

    // Filtra o menu lateral baseado nas permissões do grupo
    const links = sidebar.querySelectorAll("a");
    links.forEach(link => {
        const href = link.getAttribute("href");
        if (href && href !== "#" && href !== "home.html") {
            if (!screens.includes(href)) {
                const li = link.closest("li");
                if (li) li.style.display = "none";
            }
        }
    });

    // Esconde submenus que ficaram completamente vazios
    const submenus = sidebar.querySelectorAll(".has-submenu");
    submenus.forEach(sm => {
        const sub = sm.querySelector(".submenu");
        if (sub) {
            const visibleItems = Array.from(sub.children).filter(child => child.style.display !== "none");
            if (visibleItems.length === 0) sm.style.display = "none";
        }
    });
}

document.addEventListener("DOMContentLoaded", () => {
    // Injeta o favicon globalmente:
    if (!document.querySelector("link[rel*='icon']")) {
        const link = document.createElement('link');
        link.type = 'image/png';
        link.rel = 'shortcut icon';
        link.href = 'assets/logo.png';
        document.head.appendChild(link);
    }

    // Evita loop no index
    const currentPath = window.location.pathname.split("/").pop();
    if (!['index.html', 'register.html', 'reset.html', ''].includes(currentPath)) {
        checkAccess();
        highlightActiveMenu();
        
        // Injeta o Assistente Virtual (Pro AI) globalmente
        if (!document.querySelector("script[src='ai_chat.js']")) {
            const aiScript = document.createElement("script");
            aiScript.src = "ai_chat.js";
            document.body.appendChild(aiScript);
        }
    }
});
