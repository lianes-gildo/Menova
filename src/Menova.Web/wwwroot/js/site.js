// Menova - JavaScript Modular do Painel (Sem Frameworks Pesados, 100% Offline)
document.addEventListener('DOMContentLoaded', function () {
    initSidebar();
    initClock();
    initToasts();
    initConfirmModals();
});

// 1. Controlo da Barra Lateral (Sidebar) com Persistência
function initSidebar() {
    const sidebar = document.getElementById('appSidebar');
    const toggleBtn = document.getElementById('sidebarToggleBtn');
    if (!sidebar || !toggleBtn) return;

    const savedState = localStorage.getItem('menova_sidebar_collapsed');
    if (savedState === 'true') {
        sidebar.classList.add('collapsed');
    }

    toggleBtn.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        const isCollapsed = sidebar.classList.contains('collapsed');
        localStorage.setItem('menova_sidebar_collapsed', isCollapsed);
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
            clockEl.textContent = new Intl.DateTimeFormat('pt-PT', options).format(now) + ' (CAT)';
        } catch {
            clockEl.textContent = now.toTimeString().split(' ')[0] + ' (CAT)';
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
