// Menova - JavaScript Modular do Painel (Sem Frameworks Pesados, 100% Offline)
document.addEventListener('DOMContentLoaded', function () {
    initSidebar();
    initClock();
    initToasts();
    initConfirmModals();
});

// 1. Controlo da Barra Lateral (Sidebar) com Persistência e Suporte Móvel
function initSidebar() {
    const sidebar = document.getElementById('appSidebar');
    const toggleBtn = document.getElementById('sidebarToggleBtn');
    const backdrop = document.getElementById('sidebarBackdrop');
    if (!sidebar || !toggleBtn) return;

    function isMobile() {
        return window.innerWidth < 992;
    }

    function applyState(collapsed) {
        const icon = toggleBtn.querySelector('i');
        if (collapsed) {
            sidebar.classList.add('collapsed');
            toggleBtn.classList.add('active');
            if (icon) icon.className = 'bi bi-text-indent-left fs-5';
            toggleBtn.setAttribute('title', 'Expandir menu lateral');
        } else {
            sidebar.classList.remove('collapsed');
            toggleBtn.classList.remove('active');
            if (icon) icon.className = 'bi bi-list fs-5';
            toggleBtn.setAttribute('title', 'Recolher menu lateral');
        }
    }

    // Inicialização apenas se desktop
    if (!isMobile()) {
        const savedState = localStorage.getItem('menova_sidebar_collapsed') === 'true';
        applyState(savedState);
    }

    toggleBtn.addEventListener('click', function () {
        if (isMobile()) {
            const isOpen = sidebar.classList.contains('mobile-open');
            if (isOpen) {
                sidebar.classList.remove('mobile-open');
                toggleBtn.classList.remove('active');
                if (backdrop) backdrop.classList.remove('show');
            } else {
                sidebar.classList.add('mobile-open');
                toggleBtn.classList.add('active');
                if (backdrop) backdrop.classList.add('show');
            }
        } else {
            const isCollapsed = !sidebar.classList.contains('collapsed');
            applyState(isCollapsed);
            localStorage.setItem('menova_sidebar_collapsed', isCollapsed);
        }
    });

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            sidebar.classList.remove('mobile-open');
            toggleBtn.classList.remove('active');
            backdrop.classList.remove('show');
        });
    }

    // Fechar ao navegar num item no ecrã móvel
    const menuLinks = sidebar.querySelectorAll('.sidebar-item a');
    menuLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (isMobile()) {
                sidebar.classList.remove('mobile-open');
                toggleBtn.classList.remove('active');
                if (backdrop) backdrop.classList.remove('show');
            }
        });
    });
}

// 2. Relógio do Servidor / Horário de Moçambique (Africa/Maputo)
function initClock() {
    const clockEl = document.getElementById('serverClockDisplay');
    if (!clockEl) return;

    function update() {
        const now = new Date();
        // Forçar fuso de Moçambique (UTC+2)
        const options = {
            timeZone: 'Africa/Maputo',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        };
        try {
            clockEl.textContent = new Intl.DateTimeFormat('pt-PT', options).format(now);
        } catch {
            clockEl.textContent = now.toTimeString().split(' ')[0];
        }
    }

    update();
    setInterval(update, 1000);
}

// 3. Inicialização de Toasts
function initToasts() {
    const toastElList = [].slice.call(document.querySelectorAll('.toast'));
    toastElList.map(function (toastEl) {
        const toast = new bootstrap.Toast(toastEl, { delay: 5000 });
        toast.show();
    });
}

// 4. Modais Internos de Confirmação (Sem alert/confirm nativo do navegador)
function initConfirmModals() {
    document.querySelectorAll('[data-confirm-modal]').forEach(function (trigger) {
        trigger.addEventListener('click', function (e) {
            e.preventDefault();
            const message = trigger.getAttribute('data-confirm-message') || 'Tem a certeza que deseja prosseguir?';
            const title = trigger.getAttribute('data-confirm-title') || 'Confirmação';
            const actionText = trigger.getAttribute('data-confirm-action') || 'Confirmar';
            const formId = trigger.getAttribute('data-confirm-form');

            const modalTitle = document.getElementById('globalConfirmModalTitle');
            const modalBody = document.getElementById('globalConfirmModalBody');
            const modalBtn = document.getElementById('globalConfirmModalSubmit');

            if (modalTitle) modalTitle.textContent = title;
            if (modalBody) modalBody.textContent = message;
            if (modalBtn) {
                modalBtn.textContent = actionText;
                modalBtn.onclick = function () {
                    if (formId) {
                        const form = document.getElementById(formId);
                        if (form) form.submit();
                    } else if (trigger.tagName === 'A') {
                        window.location.href = trigger.href;
                    }
                };
            }

            const modalInstance = new bootstrap.Modal(document.getElementById('globalConfirmModal'));
            modalInstance.show();
        });
    });
}

// 5. Função Global de Toast para Notificações do Sistema (Substitui alert nativo)
window.mostrarToast = function (mensagem, tipo = 'info') {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '1090';
        document.body.appendChild(container);
    }

    const toastEl = document.createElement('div');
    const bgClass = tipo === 'sucesso' ? 'bg-success text-white' : (tipo === 'erro' ? 'bg-danger text-white' : (tipo === 'aviso' ? 'bg-warning text-dark' : 'bg-dark text-white'));
    const iconClass = tipo === 'sucesso' ? 'bi-check-circle-fill' : (tipo === 'erro' ? 'bi-x-circle-fill' : (tipo === 'aviso' ? 'bi-exclamation-triangle-fill' : 'bi-info-circle-fill'));

    toastEl.className = `toast align-items-center ${bgClass} border-0 shadow mb-2`;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body d-flex align-items-center">
                <i class="bi ${iconClass} me-2 fs-6"></i>
                <div>${mensagem}</div>
            </div>
            <button type="button" class="btn-close ${tipo === 'aviso' ? '' : 'btn-close-white'} me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>
    `;

    container.appendChild(toastEl);
    const bsToast = new bootstrap.Toast(toastEl, { delay: 4500 });
    bsToast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
};
