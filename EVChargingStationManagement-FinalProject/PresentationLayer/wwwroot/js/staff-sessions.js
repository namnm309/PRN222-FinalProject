(function () {
    const utils = window.dashboardUtils;
    if (!utils) {
        console.error('dashboardUtils not found');
        return;
    }

    const state = {
        stations: [],
        stationId: null,
        status: null,
        currentPage: 1,
        pageSize: 50,
        totalPages: 1,
        connection: null,
        refreshInterval: null
    };

    // DOM Elements
    const stationFilter = document.getElementById('SessionStationFilter');
    const statusFilter = document.getElementById('SessionStatusFilter');
    const activeTableBody = document.getElementById('activeSessionsTableBody');
    const historyTableBody = document.getElementById('historySessionsTableBody');
    const historyPagination = document.getElementById('historyPagination');
    const historyPaginationInfo = document.getElementById('historyPaginationInfo');
    const activeTab = document.getElementById('active-tab');
    const historyTab = document.getElementById('history-tab');
    
    // Payment modal elements
    const paymentModal = document.getElementById('paymentModal') ? new bootstrap.Modal(document.getElementById('paymentModal')) : null;
    const paymentForm = document.getElementById('paymentForm');
    const paymentSessionId = document.getElementById('paymentSessionId');
    const paymentAmount = document.getElementById('paymentAmount');
    const paymentMethod = document.getElementById('paymentMethod');
    const paymentNotes = document.getElementById('paymentNotes');
    const sessionCost = document.getElementById('sessionCost');
    const savePaymentBtn = document.getElementById('savePaymentBtn');

    const buildQueryString = (params) => {
        const query = new URLSearchParams();
        Object.entries(params).forEach(([key, value]) => {
            if (value !== undefined && value !== null && value !== '') {
                query.append(key, value);
            }
        });
        const qs = query.toString();
        return qs ? `?${qs}` : '';
    };

    const loadStations = async () => {
        try {
            const data = await utils.fetchJson('/api/ChargingStation');
            state.stations = data || [];
            renderStations();
        } catch (err) {
            console.error('Error loading stations:', err);
            if (stationFilter) {
                stationFilter.innerHTML = '<option value="">Lỗi tải trạm</option>';
            }
        }
    };

    const renderStations = () => {
        if (!stationFilter) return;
        stationFilter.innerHTML = '<option value="">Tất cả trạm</option>';
        state.stations.forEach(station => {
            const opt = document.createElement('option');
            opt.value = station.id;
            opt.textContent = station.name;
            stationFilter.appendChild(opt);
        });
    };

    const loadActiveSessions = async () => {
        try {
            const params = {};
            if (state.stationId) params.stationId = state.stationId;
            
            const data = await utils.fetchJson(`/api/ChargingSession/active${buildQueryString(params)}`);
            renderActiveSessions(data || []);
        } catch (err) {
            console.error('Error loading active sessions:', err);
            if (activeTableBody) {
                activeTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
            }
        }
    };

    const renderActiveSessions = (sessions) => {
        if (!activeTableBody) return;
        
        if (!sessions || sessions.length === 0) {
            activeTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted">Không có phiên sạc đang hoạt động</td></tr>';
            return;
        }

        activeTableBody.innerHTML = sessions.map(session => {
            const duration = session.sessionEndTime 
                ? Math.floor((new Date(session.sessionEndTime) - new Date(session.sessionStartTime)) / 60000)
                : Math.floor((new Date() - new Date(session.sessionStartTime)) / 60000);
            const durationText = duration >= 60 
                ? `${Math.floor(duration / 60)}h ${duration % 60}m`
                : `${duration}m`;

            const canStart = session.status === 0; // Scheduled
            const canStop = session.status === 1; // InProgress
            const canPay = session.status === 2; // Completed

            return `
                <tr>
                    <td>${utils.formatDateTime(session.sessionStartTime)}</td>
                    <td><strong>${session.chargingStationName || '--'}</strong><br/><small>Điểm: ${session.chargingSpotNumber || '--'}</small></td>
                    <td>${session.userName || session.user?.fullName || session.user?.email || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td>${utils.formatNumber(session.energyDeliveredKwh ?? session.energyRequestedKwh ?? 0, 2)}</td>
                    <td>${durationText}</td>
                    <td>${utils.renderStatusBadge ? utils.renderStatusBadge(session.status) : getStatusBadge(session.status)}</td>
                    <td>
                        <div class="btn-group" role="group">
                            ${canStart ? `<button class="btn btn-sm btn-success" data-action="start-session" data-session-id="${session.id}" title="Khởi động phiên">
                                <i class="bi bi-play-fill"></i> Khởi động
                            </button>` : ''}
                            ${canStop ? `<button class="btn btn-sm btn-warning" data-action="stop-session" data-session-id="${session.id}" title="Dừng phiên">
                                <i class="bi bi-stop-fill"></i> Dừng
                            </button>` : ''}
                            ${canPay ? `<button class="btn btn-sm btn-primary" data-action="pay-session" data-session-id="${session.id}" data-session-cost="${session.cost || 0}" title="Thanh toán">
                                <i class="bi bi-cash-coin"></i> Thanh toán
                            </button>` : ''}
                        </div>
                    </td>
                </tr>
            `;
        }).join('');

        // Attach event listeners
        activeTableBody.querySelectorAll('[data-action="start-session"]').forEach(btn => {
            btn.addEventListener('click', function() {
                const sessionId = this.getAttribute('data-session-id');
                startSession(sessionId);
            });
        });

        activeTableBody.querySelectorAll('[data-action="stop-session"]').forEach(btn => {
            btn.addEventListener('click', function() {
                const sessionId = this.getAttribute('data-session-id');
                stopSession(sessionId);
            });
        });

        activeTableBody.querySelectorAll('[data-action="pay-session"]').forEach(btn => {
            btn.addEventListener('click', function() {
                const sessionId = this.getAttribute('data-session-id');
                const cost = parseFloat(this.getAttribute('data-session-cost')) || 0;
                openPaymentModal(sessionId, cost);
            });
        });
    };

    const getStatusBadge = (status) => {
        const statusMap = {
            0: '<span class="badge bg-secondary">Scheduled</span>',
            1: '<span class="badge bg-primary">InProgress</span>',
            2: '<span class="badge bg-success">Completed</span>',
            3: '<span class="badge bg-danger">Cancelled</span>',
            4: '<span class="badge bg-warning">Failed</span>'
        };
        return statusMap[status] || '<span class="badge bg-secondary">Unknown</span>';
    };

    const startSession = async (sessionId) => {
        if (!confirm('Bạn có chắc chắn muốn khởi động phiên sạc này?')) {
            return;
        }

        try {
            const response = await fetch(`/api/ChargingSession/${sessionId}/status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({
                    status: 1, // InProgress
                    notes: 'Khởi động bởi nhân viên trạm sạc'
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Không thể khởi động phiên sạc');
            }

            alert('Đã khởi động phiên sạc thành công!');
            loadActiveSessions();
        } catch (error) {
            console.error('Error starting session:', error);
            alert('Lỗi khi khởi động phiên sạc: ' + error.message);
        }
    };

    const stopSession = async (sessionId) => {
        if (!confirm('Bạn có chắc chắn muốn dừng phiên sạc này?')) {
            return;
        }

        try {
            const response = await fetch(`/api/ChargingSession/${sessionId}/status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({
                    status: 2, // Completed
                    notes: 'Dừng bởi nhân viên trạm sạc'
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Không thể dừng phiên sạc');
            }

            alert('Đã dừng phiên sạc thành công!');
            loadActiveSessions();
        } catch (error) {
            console.error('Error stopping session:', error);
            alert('Lỗi khi dừng phiên sạc: ' + error.message);
        }
    };

    const openPaymentModal = async (sessionId, cost) => {
        if (!paymentModal || !paymentForm) return;

        paymentSessionId.value = sessionId;
        paymentAmount.value = cost || 0;
        if (sessionCost) {
            sessionCost.textContent = utils.formatNumber ? utils.formatNumber(cost, 0) : cost.toLocaleString('vi-VN');
        }
        paymentMethod.value = '5'; // Default to Cash
        paymentNotes.value = '';

        paymentModal.show();
    };

    const processPayment = async () => {
        if (!paymentForm || !paymentForm.checkValidity()) {
            paymentForm.reportValidity();
            return;
        }

        const sessionId = paymentSessionId.value;
        const amount = parseFloat(paymentAmount.value);
        const method = parseInt(paymentMethod.value);
        const notes = paymentNotes.value;

        if (!sessionId) {
            alert('Không tìm thấy phiên sạc');
            return;
        }

        if (savePaymentBtn) {
            savePaymentBtn.disabled = true;
            savePaymentBtn.textContent = 'Đang xử lý...';
        }

        try {
            // First, get the session to get user ID
            const sessionResponse = await fetch(`/api/ChargingSession/${sessionId}`, {
                credentials: 'include'
            });

            if (!sessionResponse.ok) {
                throw new Error('Không tìm thấy phiên sạc');
            }

            const session = await sessionResponse.json();

            // Create payment
            const paymentResponse = await fetch('/api/Payment', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({
                    chargingSessionId: sessionId,
                    amount: amount,
                    currency: 'VND',
                    method: method,
                    description: notes || `Thanh toán tại chỗ cho phiên sạc ${sessionId}`
                })
            });

            if (!paymentResponse.ok) {
                const error = await paymentResponse.json();
                throw new Error(error.message || 'Không thể tạo thanh toán');
            }

            const payment = await paymentResponse.json();

            // Update payment status to Captured (for on-site payment)
            const updateResponse = await fetch(`/api/Payment/${payment.id}/status`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({
                    status: 2, // Captured
                    providerTransactionId: `ONSITE-${Date.now()}`
                })
            });

            if (!updateResponse.ok) {
                throw new Error('Không thể cập nhật trạng thái thanh toán');
            }

            alert('Thanh toán thành công!');
            if (paymentModal) {
                paymentModal.hide();
            }
            loadActiveSessions();
            if (historyTab.classList.contains('active')) {
                loadHistorySessions();
            }
        } catch (error) {
            console.error('Error processing payment:', error);
            alert('Lỗi khi xử lý thanh toán: ' + error.message);
        } finally {
            if (savePaymentBtn) {
                savePaymentBtn.disabled = false;
                savePaymentBtn.textContent = 'Xác nhận thanh toán';
            }
        }
    };

    const loadHistorySessions = async () => {
        try {
            const params = {
                page: state.currentPage,
                pageSize: state.pageSize
            };
            if (state.stationId) params.stationId = state.stationId;
            if (state.status !== null && state.status !== '') params.status = parseInt(state.status);

            const response = await utils.fetchJson(`/api/dashboard/sessions/all${buildQueryString(params)}`);
            renderHistorySessions(response.data || [], response.totalCount || 0, response.totalPages || 1);
        } catch (err) {
            console.error('Error loading history sessions:', err);
            if (historyTableBody) {
                historyTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
            }
        }
    };

    const renderHistorySessions = (sessions, totalCount, totalPages) => {
        if (!historyTableBody) return;
        
        state.totalPages = totalPages;

        if (!sessions || sessions.length === 0) {
            historyTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted">Không có dữ liệu</td></tr>';
            if (historyPaginationInfo) {
                historyPaginationInfo.textContent = 'Hiển thị 0 - 0 của 0';
            }
            if (historyPagination) {
                historyPagination.innerHTML = '';
            }
            return;
        }

        const start = (state.currentPage - 1) * state.pageSize + 1;
        const end = Math.min(start + sessions.length - 1, totalCount);
        if (historyPaginationInfo) {
            historyPaginationInfo.textContent = `Hiển thị ${start} - ${end} của ${totalCount}`;
        }

        historyTableBody.innerHTML = sessions.map(session => {
            // Format thời gian bắt đầu
            let startTime = '--';
            if (session.sessionStartTime) {
                try {
                    startTime = utils.formatDateTime ? utils.formatDateTime(session.sessionStartTime) : new Date(session.sessionStartTime).toLocaleString('vi-VN');
                } catch (e) {
                    startTime = new Date(session.sessionStartTime).toLocaleString('vi-VN');
                }
            }
            
            // Format thời gian kết thúc
            let endTime = null;
            if (session.sessionEndTime) {
                try {
                    endTime = utils.formatDateTime ? utils.formatDateTime(session.sessionEndTime) : new Date(session.sessionEndTime).toLocaleString('vi-VN');
                } catch (e) {
                    endTime = new Date(session.sessionEndTime).toLocaleString('vi-VN');
                }
            }
            
            // Tính thời gian
            const duration = session.durationMinutes 
                ? (session.durationMinutes >= 60 
                    ? `${Math.floor(session.durationMinutes / 60)}h ${session.durationMinutes % 60}m`
                    : `${session.durationMinutes}m`)
                : '--';

            return `
                <tr>
                    <td>
                        <div>${startTime}</div>
                        ${endTime ? `<small class="text-muted">Kết thúc: ${endTime}</small>` : ''}
                    </td>
                    <td><strong>${session.stationName || session.chargingStationName || '--'}</strong><br/><small>Cổng: ${session.spotNumber || session.chargingSpotNumber || '--'}</small></td>
                    <td>${session.userName || session.user?.fullName || session.user?.email || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td>${utils.formatNumber ? utils.formatNumber(session.energyDeliveredKwh ?? session.energyRequestedKwh ?? 0, 2) : (session.energyDeliveredKwh ?? session.energyRequestedKwh ?? 0).toFixed(2)}</td>
                    <td>${utils.formatCurrency ? utils.formatCurrency(session.cost) : (session.cost || 0).toLocaleString('vi-VN')} VND</td>
                    <td>${duration}</td>
                    <td>${utils.renderStatusBadge ? utils.renderStatusBadge(session.status) : getStatusBadge(session.status)}</td>
                </tr>
            `;
        }).join('');

        renderPagination(totalPages);
    };

    const renderPagination = (totalPages) => {
        if (!historyPagination) return;
        
        if (totalPages <= 1) {
            historyPagination.innerHTML = '';
            return;
        }

        let html = '';
        const maxVisible = 5;
        let startPage = Math.max(1, state.currentPage - Math.floor(maxVisible / 2));
        let endPage = Math.min(totalPages, startPage + maxVisible - 1);

        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        if (startPage > 1) {
            html += `<li class="page-item"><a class="page-link" href="#" data-page="1">Đầu</a></li>`;
            html += `<li class="page-item"><a class="page-link" href="#" data-page="${state.currentPage - 1}">Trước</a></li>`;
        }

        for (let i = startPage; i <= endPage; i++) {
            html += `<li class="page-item ${i === state.currentPage ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${i}">${i}</a>
            </li>`;
        }

        if (endPage < totalPages) {
            html += `<li class="page-item"><a class="page-link" href="#" data-page="${state.currentPage + 1}">Sau</a></li>`;
            html += `<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">Cuối</a></li>`;
        }

        historyPagination.innerHTML = html;

        historyPagination.querySelectorAll('a[data-page]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(link.dataset.page);
                if (page >= 1 && page <= totalPages && page !== state.currentPage) {
                    state.currentPage = page;
                    loadHistorySessions();
                }
            });
        });
    };

    const initSignalR = async () => {
        if (!window.signalR) return;

        state.connection = new signalR.HubConnectionBuilder()
            .withAutomaticReconnect()
            .withUrl('/hubs/station')
            .build();

        state.connection.on('SessionUpdated', () => {
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        });

        try {
            await state.connection.start();
            if (state.stationId) {
                await state.connection.invoke('SubscribeStation', state.stationId);
            }
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    };

    const setupAutoRefresh = () => {
        if (state.refreshInterval) {
            clearInterval(state.refreshInterval);
        }

        state.refreshInterval = setInterval(() => {
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        }, 10000); // Refresh mỗi 10 giây
    };

    const bindEvents = () => {
        if (stationFilter) {
            stationFilter.addEventListener('change', (e) => {
                state.stationId = e.target.value || null;
                if (state.connection && state.stationId) {
                    state.connection.invoke('SubscribeStation', state.stationId).catch(console.error);
                }
                if (activeTab && activeTab.classList.contains('active')) {
                    loadActiveSessions();
                } else {
                    state.currentPage = 1;
                    loadHistorySessions();
                }
            });
        }

        document.querySelector('[data-action="apply-filters"]')?.addEventListener('click', () => {
            if (statusFilter) state.status = statusFilter.value;
            state.currentPage = 1;
            loadHistorySessions();
        });

        document.querySelector('[data-action="reset-filters"]')?.addEventListener('click', () => {
            if (stationFilter) stationFilter.value = '';
            if (statusFilter) statusFilter.value = '';
            state.stationId = null;
            state.status = null;
            state.currentPage = 1;
            loadHistorySessions();
        });

        document.querySelector('[data-action="refresh-active"]')?.addEventListener('click', loadActiveSessions);
        document.querySelector('[data-action="refresh-history"]')?.addEventListener('click', () => {
            state.currentPage = 1;
            loadHistorySessions();
        });

        if (activeTab) {
            activeTab.addEventListener('shown.bs.tab', () => {
                loadActiveSessions();
                setupAutoRefresh();
            });
        }

        if (historyTab) {
            historyTab.addEventListener('shown.bs.tab', () => {
                if (state.refreshInterval) {
                    clearInterval(state.refreshInterval);
                }
                loadHistorySessions();
            });
        }

        if (savePaymentBtn) {
            savePaymentBtn.addEventListener('click', processPayment);
        }
    };

    const init = async () => {
        await loadStations();
        bindEvents();
        await initSignalR();
        loadActiveSessions();
        setupAutoRefresh();
    };

    init().catch(err => console.error('Initialization error:', err));
})();

