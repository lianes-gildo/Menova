// Menova - JavaScript Modular do Painel (Sem Frameworks Pesados, 100% Offline)
document.addEventListener('DOMContentLoaded', function () {
    initSidebar();
    initClock();
    initToasts();
    initConfirmModals();
    initAutoTablePagination();
    initInactivityWatchdog();
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

    // Gestão de Submenus Acordeão
    const submenu = document.getElementById('submenuSistema');
    if (submenu) {
        submenu.addEventListener('show.bs.collapse', function () {
            const dropdownItem = submenu.closest('.sidebar-item-dropdown');
            if (dropdownItem) {
                dropdownItem.classList.add('open');
                const chevron = dropdownItem.querySelector('.sidebar-chevron');
                if (chevron) chevron.classList.add('rotate');
            }
            // Se o sidebar estiver colapsado no desktop, expandir automaticamente
            if (!isMobile() && sidebar.classList.contains('collapsed')) {
                applyState(false);
                localStorage.setItem('menova_sidebar_collapsed', false);
            }
        });

        submenu.addEventListener('hide.bs.collapse', function () {
            const dropdownItem = submenu.closest('.sidebar-item-dropdown');
            if (dropdownItem) {
                dropdownItem.classList.remove('open');
                const chevron = dropdownItem.querySelector('.sidebar-chevron');
                if (chevron) chevron.classList.remove('rotate');
            }
        });
    }

    // Fechar ao navegar num item no ecrã móvel (exceto botões de acordeão)
    const menuLinks = sidebar.querySelectorAll('.sidebar-item a, .sidebar-subitem a');
    menuLinks.forEach(link => {
        link.addEventListener('click', function () {
            if (this.getAttribute('data-bs-toggle') === 'collapse') {
                return; // Não fechar o sidebar móvel ao alternar submenu
            }
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

// 3. Inicialização de Toasts do Servidor (TempData) com Auto-Dismiss e Remoção de Órfãos
function initToasts() {
    document.querySelectorAll('.toast').forEach(function (toastEl) {
        const body = toastEl.querySelector('.toast-body');
        const text = body ? body.textContent.trim() : '';
        
        // Se for um elemento de modelo vazio ou sem conteúdo, remover do DOM imediatamente
        if (!text) {
            toastEl.remove();
            return;
        }

        // Inicializar apenas se tiver texto válido e classe show do servidor
        if (toastEl.classList.contains('show')) {
            const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 4500 });
            toast.show();
            toastEl.addEventListener('hidden.bs.toast', function () {
                toastEl.remove();
            });
        }
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

// 5. Motor Centralizado de Notificações Toast (Inteligente, Responsivo e com Auto-Dismiss)
window.mostrarToast = function (mensagem, tipo = 'info', duracaoMs = 4000) {
    if (!mensagem || typeof mensagem !== 'string' || !mensagem.trim()) {
        return; // Nunca exibir popups vazios ou sem texto
    }

    let container = document.getElementById('menovaToastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'menovaToastContainer';
        container.className = 'menova-toast-container';
        document.body.appendChild(container);
    }

    const tipoNormalizado = (tipo || 'info').toLowerCase();
    let bgClass = 'bg-dark text-white';
    let iconClass = 'bi-info-circle-fill';
    let closeBtnClass = 'btn-close-white';

    if (tipoNormalizado === 'sucesso' || tipoNormalizado === 'success') {
        bgClass = 'bg-success text-white';
        iconClass = 'bi-check-circle-fill';
    } else if (tipoNormalizado === 'erro' || tipoNormalizado === 'danger' || tipoNormalizado === 'error') {
        bgClass = 'bg-danger text-white';
        iconClass = 'bi-x-circle-fill';
    } else if (tipoNormalizado === 'aviso' || tipoNormalizado === 'warning' || tipoNormalizado === 'warn') {
        bgClass = 'bg-warning text-dark';
        iconClass = 'bi-exclamation-triangle-fill';
        closeBtnClass = '';
    }

    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center ${bgClass} border-0 shadow-lg mb-2`;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');
    toastEl.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="toast-body d-flex align-items-center py-2 px-3">
                <i class="bi ${iconClass} me-2 fs-5 flex-shrink-0"></i>
                <span class="text-break">${mensagem.trim()}</span>
            </div>
            <button type="button" class="btn-close ${closeBtnClass} me-2 m-auto" data-bs-dismiss="toast" aria-label="Fechar"></button>
        </div>
    `;

    container.appendChild(toastEl);
    const bsToast = new bootstrap.Toast(toastEl, { autohide: true, delay: duracaoMs });
    bsToast.show();
    toastEl.addEventListener('hidden.bs.toast', function () {
        toastEl.remove();
    });
};

// Aliases globais para compatibilidade total entre páginas
window.mostrarAviso = window.mostrarToast;
window.mostrarAvisoApp = window.mostrarToast;

// =========================================================================
// 6. Paginação Universal e Reativa de Tabelas (Menova Senior Architecture)
// =========================================================================
function initAutoTablePagination() {
    document.querySelectorAll('table[data-paginate]').forEach(table => {
        initTablePagination(table);
    });
}

function initTablePagination(tableEl) {
    if (!tableEl) return;
    
    // Se já foi inicializado, apenas refrescar
    if (tableEl.dataset.paginationInitialized === 'true') {
        if (tableEl._refreshPagination) tableEl._refreshPagination();
        return;
    }
    tableEl.dataset.paginationInitialized = 'true';

    const defaultPageSize = parseInt(tableEl.dataset.paginate || tableEl.dataset.pageSize || '10', 10);
    let currentPage = 1;
    let pageSize = defaultPageSize;
    let filterQuery = '';

    const tbody = tableEl.querySelector('tbody');
    if (!tbody) return;

    // Criar Contentor do Rodapé de Paginação
    const cardParent = tableEl.closest('.card') || tableEl.parentElement;
    let footer = cardParent.querySelector('.table-pagination-footer');
    if (!footer) {
        footer = document.createElement('div');
        footer.className = 'table-pagination-footer card-footer bg-white border-top d-flex flex-wrap justify-content-between align-items-center py-2 px-3 gap-2';
        cardParent.appendChild(footer);
    }

    // Suporte a filtro por campo de pesquisa externo se especificado
    const filterInputSelector = tableEl.dataset.filterInput;
    if (filterInputSelector) {
        const filterInput = document.querySelector(filterInputSelector);
        if (filterInput) {
            filterInput.addEventListener('input', (e) => {
                filterQuery = (e.target.value || '').toLowerCase().trim();
                currentPage = 1;
                render();
            });
        }
    }

    function getRows() {
        return Array.from(tbody.querySelectorAll('tr')).filter(tr => !tr.querySelector('td[colspan]'));
    }

    function getFilteredRows(allRows) {
        if (!filterQuery) return allRows;
        return allRows.filter(tr => {
            return (tr.textContent || '').toLowerCase().includes(filterQuery);
        });
    }

    function render() {
        const allRows = getRows();
        const filtered = getFilteredRows(allRows);
        const total = filtered.length;

        // Se a tabela não tem registos, ocultar footer
        if (total === 0) {
            allRows.forEach(tr => tr.style.display = '');
            footer.style.display = 'none';
            return;
        }

        footer.style.display = 'flex';
        const totalPages = Math.max(1, Math.ceil(total / pageSize));

        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        const startIdx = (currentPage - 1) * pageSize;
        const endIdx = startIdx + pageSize;

        // Ocultar todas as linhas válidas
        allRows.forEach(tr => tr.style.display = 'none');

        // Exibir apenas o lote da página ativa
        filtered.slice(startIdx, endIdx).forEach(tr => tr.style.display = '');

        const showingFrom = startIdx + 1;
        const showingTo = Math.min(endIdx, total);

        let paginationHtml = '';
        if (totalPages > 1) {
            paginationHtml = `
                <ul class="pagination pagination-sm mb-0">
                    <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                        <button type="button" class="page-link btn-prev" aria-label="Anterior">&lsaquo; Anterior</button>
                    </li>
            `;

            let startP = Math.max(1, currentPage - 2);
            let endP = Math.min(totalPages, currentPage + 2);
            if (endP - startP < 4) {
                if (startP === 1) endP = Math.min(totalPages, startP + 4);
                else if (endP === totalPages) startP = Math.max(1, endP - 4);
            }

            if (startP > 1) {
                paginationHtml += `<li class="page-item"><button type="button" class="page-link btn-page" data-page="1">1</button></li>`;
                if (startP > 2) paginationHtml += `<li class="page-item disabled"><span class="page-link py-1">&hellip;</span></li>`;
            }

            for (let p = startP; p <= endP; p++) {
                paginationHtml += `
                    <li class="page-item ${p === currentPage ? 'active' : ''}">
                        <button type="button" class="page-link btn-page" data-page="${p}">${p}</button>
                    </li>
                `;
            }

            if (endP < totalPages) {
                if (endP < totalPages - 1) paginationHtml += `<li class="page-item disabled"><span class="page-link py-1">&hellip;</span></li>`;
                paginationHtml += `<li class="page-item"><button type="button" class="page-link btn-page" data-page="${totalPages}">${totalPages}</button></li>`;
            }

            paginationHtml += `
                    <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                        <button type="button" class="page-link btn-next" aria-label="Seguinte">Seguinte &rsaquo;</button>
                    </li>
                </ul>
            `;
        }

        footer.innerHTML = `
            <div class="d-flex align-items-center gap-2 small text-muted">
                <span>A mostrar <strong class="text-dark">${showingFrom}</strong> a <strong class="text-dark">${showingTo}</strong> de <strong class="text-dark">${total}</strong> registos</span>
                <select class="form-select form-select-sm py-0 ps-2 pe-4 select-pagesize" style="width: auto; height: 28px; font-size: 0.8rem; cursor: pointer;">
                    <option value="10" ${pageSize === 10 ? 'selected' : ''}>10 / pág.</option>
                    <option value="25" ${pageSize === 25 ? 'selected' : ''}>25 / pág.</option>
                    <option value="50" ${pageSize === 50 ? 'selected' : ''}>50 / pág.</option>
                </select>
            </div>
            <div>${paginationHtml}</div>
        `;

        const selectEl = footer.querySelector('.select-pagesize');
        if (selectEl) {
            selectEl.addEventListener('change', (e) => {
                pageSize = parseInt(e.target.value, 10);
                currentPage = 1;
                render();
            });
        }

        const btnPrev = footer.querySelector('.btn-prev');
        if (btnPrev && currentPage > 1) {
            btnPrev.addEventListener('click', () => {
                currentPage--;
                render();
            });
        }

        const btnNext = footer.querySelector('.btn-next');
        if (btnNext && currentPage < totalPages) {
            btnNext.addEventListener('click', () => {
                currentPage++;
                render();
            });
        }

        footer.querySelectorAll('.btn-page').forEach(btn => {
            btn.addEventListener('click', () => {
                currentPage = parseInt(btn.dataset.page, 10);
                render();
            });
        });
    }

    tableEl._refreshPagination = render;
    render();
}

window.initTablePagination = initTablePagination;
window.refreshTablePagination = function (tableId) {
    const el = typeof tableId === 'string' ? document.getElementById(tableId) : tableId;
    if (el && el._refreshPagination) el._refreshPagination();
};

// =========================================================================
// 7. Controlo de Inatividade e Sessão Única Concorrente (Senior Security)
// =========================================================================
function initInactivityWatchdog() {
    // Não executar no ecrã de login
    if (document.body.classList.contains('login-page') || window.location.pathname.startsWith('/auth/login')) {
        return;
    }

    const INACTIVITY_TIMEOUT_SEC = 15 * 60; // 15 minutos (tempo de expiração automática)
    const WARNING_TIMEOUT_SEC = 13 * 60;    // 13 minutos (aviso prévio de 2 minutos)
    const HEARTBEAT_INTERVAL_SEC = 4 * 60; // Heartbeat a cada 4 minutos de atividade real

    let idleSeconds = 0;
    let secondsSinceLastHeartbeat = 0;
    let warningModalInstance = null;

    function resetActivity() {
        idleSeconds = 0;
        if (warningModalInstance) {
            warningModalInstance.hide();
            warningModalInstance = null;
        }
    }

    // Monitorizar toques, cliques e teclas do utilizador
    const events = ['mousedown', 'mousemove', 'keypress', 'keydown', 'scroll', 'touchstart', 'click'];
    events.forEach(evt => {
        window.addEventListener(evt, resetActivity, { passive: true });
    });

    function getOrCreateWarningModal() {
        let modalEl = document.getElementById('inactivityWarningModal');
        if (!modalEl) {
            modalEl = document.createElement('div');
            modalEl.id = 'inactivityWarningModal';
            modalEl.className = 'modal fade';
            modalEl.setAttribute('tabindex', '-1');
            modalEl.setAttribute('aria-hidden', 'true');
            modalEl.innerHTML = `
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content shadow border-0 rounded-4">
                        <div class="modal-header border-0 pb-0" style="background-color: #FAF7F8;">
                            <h5 class="modal-title fw-bold text-danger d-flex align-items-center">
                                <i class="bi bi-clock-history me-2"></i>Aviso de Inatividade
                            </h5>
                        </div>
                        <div class="modal-body py-3">
                            <p class="mb-1 text-dark fw-medium">
                                A sua sessão irá expirar dentro de <strong><span id="inactivityCountdown">120</span> segundos</strong> por inatividade para sua segurança.
                            </p>
                            <p class="small text-muted mb-0">Toque no ecrã ou clique no botão abaixo para continuar a trabalhar.</p>
                        </div>
                        <div class="modal-footer border-0 pt-0">
                            <button type="button" class="btn text-white w-100 py-2 fw-semibold" id="btnContinuarSessao" style="background-color: #6E1A33; border-radius: 8px;">
                                <i class="bi bi-arrow-repeat me-1"></i>Continuar Sessão
                            </button>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(modalEl);

            const btnContinuar = modalEl.querySelector('#btnContinuarSessao');
            if (btnContinuar) {
                btnContinuar.addEventListener('click', () => {
                    resetActivity();
                    enviarHeartbeat();
                });
            }
        }
        return modalEl;
    }

    function enviarHeartbeat() {
        secondsSinceLastHeartbeat = 0;
        fetch('/auth/heartbeat', {
            method: 'GET',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(res => {
            if (res.status === 401 || res.redirected) {
                // Sessão foi invalidada noutro dispositivo
                window.location.href = '/auth/login?motivo=sessao_duplicada';
            }
        }).catch(() => {});
    }

    // Cronómetro de inatividade a cada 1 segundo
    setInterval(() => {
        idleSeconds++;
        secondsSinceLastHeartbeat++;

        // Se o utilizador esteve ativo e passaram 4 minutos, renovar sessão com heartbeat
        if (idleSeconds < WARNING_TIMEOUT_SEC && secondsSinceLastHeartbeat >= HEARTBEAT_INTERVAL_SEC) {
            enviarHeartbeat();
        }

        // Verificar se outra sessão tomou conta (cookie de motivo gerado pelo servidor)
        if (document.cookie.includes('menova_logout_motivo=sessao_duplicada')) {
            window.location.href = '/auth/login?motivo=sessao_duplicada';
            return;
        }

        // Aviso prévio de inatividade (13 minutos atingidos)
        if (idleSeconds >= WARNING_TIMEOUT_SEC && idleSeconds < INACTIVITY_TIMEOUT_SEC) {
            const restantes = INACTIVITY_TIMEOUT_SEC - idleSeconds;
            const modalEl = getOrCreateWarningModal();
            const countEl = modalEl.querySelector('#inactivityCountdown');
            if (countEl) countEl.textContent = restantes;

            if (!warningModalInstance && typeof bootstrap !== 'undefined') {
                warningModalInstance = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false });
                warningModalInstance.show();
            }
        }

        // Tempo limite de inatividade atingido (15 minutos)
        if (idleSeconds >= INACTIVITY_TIMEOUT_SEC) {
            window.location.href = '/auth/login?motivo=inactividade';
        }
    }, 1000);
}
