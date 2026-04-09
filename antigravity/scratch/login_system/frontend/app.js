const API_URL = window.location.origin + "/api";

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
    const username = e.target.username.value;
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Processando...";
    try {
        const res = await fetch(`${API_URL}/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            },
            body: JSON.stringify({ Username: username, Password: password, Email: "" })
        });
        
        if (res.ok) {
            const data = await res.json();
            showAlert("Login realizado com sucesso!", false);
            localStorage.setItem("loggedUsername", data.username);
            localStorage.setItem("userGroupId", data.groupId);
            localStorage.setItem("userScreens", JSON.stringify(data.screens));
            setTimeout(() => window.location.href = "home.html", 1500);
        } else {
            showAlert("Credenciais inválidas.", true);
        }
    } catch(err) {
        showAlert("Erro de conexão com o servidor.", true);
    } finally {
        e.target.querySelector('button').textContent = "Entrar";
    }
}

async function handleRegister(e) {
    e.preventDefault();
    const username = e.target.username.value;
    const email = e.target.email.value;
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Criando...";
    try {
        const res = await fetch(`${API_URL}/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            },
            body: JSON.stringify({ Username: username, Password: password, Email: email })
        });
        
        if (res.ok) {
            showAlert("Conta criada com sucesso!", false);
            setTimeout(() => window.location.href = "index.html", 2000);
        } else {
            const data = await res.json();
            showAlert(data.message || "Erro ao criar conta.", true);
        }
    } catch(err) {
        showAlert("Erro de conexão com o servidor.", true);
    } finally {
        e.target.querySelector('button').textContent = "Criar Conta";
    }
}

async function handleReset(e) {
    e.preventDefault();
    const username = e.target.username.value;
    const email = e.target.email.value;
    const password = e.target.password.value;
    
    e.target.querySelector('button').textContent = "Redefinindo...";
    try {
        const res = await fetch(`${API_URL}/reset-password`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'ngrok-skip-browser-warning': 'true'
            },
            body: JSON.stringify({ Username: username, Password: password, Email: email })
        });
        
        if (res.ok) {
            showAlert("Senha redefinida com sucesso!", false);
            setTimeout(() => window.location.href = "index.html", 2000);
        } else {
            const data = await res.json();
            showAlert(data.message || "Usuário não encontrado.", true);
        }
    } catch(err) {
        showAlert("Erro de conexão com o servidor.", true);
    } finally {
        e.target.querySelector('button').textContent = "Redefinir Senha";
    }
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
    // Evita loop no index
    const currentPath = window.location.pathname.split("/").pop();
    if (!['index.html', 'register.html', 'reset.html', ''].includes(currentPath)) {
        checkAccess();
    }
});
