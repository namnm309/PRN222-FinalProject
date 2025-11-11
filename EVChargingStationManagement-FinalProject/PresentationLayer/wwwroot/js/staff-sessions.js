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
        refreshInterval: null,
        durationInterval: null, // Timer để cập nhật thời gian
        energyInterval: null, // Timer để cập nhật năng lượng
        activeSessions: [], // Lưu danh sách session đang hoạt động
        subscribedSessions: new Set(), // Track các session đã subscribe
        sessionSimulations: new Map() // Track simulation data cho mỗi session {startTime, initialEnergy, powerKw}
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
            
            // Subscribe to all stations after loading
            if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
                await subscribeToStations();
            }
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

    const loadProgressForSession = async (sessionId) => {
        try {
            // Chỉ lấy progress từ session entity (giống driver view)
            const progress = await utils.fetchJson(`/api/ChargingSession/${sessionId}/progress`);
            console.log(`Progress loaded for session ${sessionId}:`, progress);
            return progress;
        } catch (err) {
            // Progress có thể chưa có nếu session mới bắt đầu
            console.log(`No progress for session ${sessionId}:`, err.message);
            return null;
        }
    };

    const loadActiveSessions = async () => {
        try {
            const params = {};
            if (state.stationId) params.stationId = state.stationId;
            
            const data = await utils.fetchJson(`/api/ChargingSession/active${buildQueryString(params)}`);
            state.activeSessions = data || [];
            
            // Load progress cho từng session để lấy giá trị mới nhất
            // Sử dụng Promise.allSettled để không bị block nếu một session lỗi
            const progressPromises = state.activeSessions.map(session => 
                loadProgressForSession(session.id).then(progress => ({ session, progress }))
            );
            
            const results = await Promise.allSettled(progressPromises);
            
            // Cập nhật energyDeliveredKwh từ progress nếu có
            results.forEach(result => {
                if (result.status === 'fulfilled') {
                    const { session, progress } = result.value;
                    if (progress) {
                        // Lưu toàn bộ progress vào session
                        session.progress = progress;
                        // Cập nhật energyDeliveredKwh từ progress
                        if (progress.energyDeliveredKwh !== null && progress.energyDeliveredKwh !== undefined) {
                            session.energyDeliveredKwh = progress.energyDeliveredKwh;
                            console.log(`Updated session ${session.id} energyDeliveredKwh to ${session.energyDeliveredKwh}`);
                        }
                    } else {
                        console.log(`No progress data for session ${session.id}`);
                    }
                }
            });
            
            renderActiveSessions(state.activeSessions);
            
            // Subscribe to all active sessions for progress updates
            await subscribeToActiveSessions();
            
            // Khởi tạo simulation cho các session InProgress
            state.activeSessions.forEach(session => {
                if (getStatusCode(session.status) === 1) { // InProgress
                    const powerKw = session.chargingSpotPower || session.progress?.currentPowerKw || 120;
                    const initialEnergy = session.energyDeliveredKwh || session.progress?.energyDeliveredKwh || 0;
                    state.sessionSimulations.set(session.id, {
                        startTime: Date.now(),
                        initialEnergy: initialEnergy,
                        powerKw: powerKw
                    });
                    console.log(`Initialized simulation for session ${session.id}: energy=${initialEnergy}, power=${powerKw}`);
                }
            });
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

            const statusCode = getStatusCode(session.status);
            const canStart = statusCode === 0; // Scheduled
            const canStop = statusCode === 1; // InProgress - Staff có thể dừng session
            const canPay = statusCode === 2; // Completed

            // Lấy năng lượng: ưu tiên progress.energyDeliveredKwh, sau đó session.energyDeliveredKwh
            let energyKwh = 0;
            if (session.progress && session.progress.energyDeliveredKwh != null) {
                energyKwh = session.progress.energyDeliveredKwh;
            } else if (session.energyDeliveredKwh != null) {
                energyKwh = session.energyDeliveredKwh;
            }
            
            // Debug: log giá trị để kiểm tra
            if (session.status === 1) { // InProgress
                console.log(`Session ${session.id} (InProgress):`, {
                    'progress': session.progress,
                    'progress.energyDeliveredKwh': session.progress?.energyDeliveredKwh,
                    'session.energyDeliveredKwh': session.energyDeliveredKwh,
                    'final energyKwh': energyKwh
                });
            }

            return `
                <tr data-session-id="${session.id}">
                    <td>${utils.formatDateTime ? utils.formatDateTime(session.sessionStartTime) : new Date(session.sessionStartTime).toLocaleString('vi-VN')}</td>
                    <td><strong>${session.chargingStationName || '--'}</strong><br/><small>Cổng: ${session.chargingSpotNumber || '--'}</small></td>
                    <td>${session.userName || session.user?.fullName || session.user?.email || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td data-energy="${session.id}">${utils.formatNumber ? utils.formatNumber(energyKwh, 2) : energyKwh.toFixed(2)} kWh</td>
                    <td data-duration="${session.id}">${durationText}</td>
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
        // Normalize when status is a string (because JsonStringEnumConverter)
        const toCode = (s) => {
            if (typeof s === 'number') return s;
            if (typeof s === 'string') {
                const m = { Scheduled: 0, InProgress: 1, Completed: 2, Cancelled: 3, Failed: 4 };
                return m[s] !== undefined ? m[s] : -1;
            }
            return -1;
        };
        const code = toCode(status);
        return statusMap[code] || '<span class="badge bg-secondary">Unknown</span>';
    };

    // Helper: normalize status to numeric code 0..4
    const getStatusCode = (status) => {
        if (typeof status === 'number') return status;
        if (typeof status === 'string') {
            const m = { Scheduled: 0, InProgress: 1, Completed: 2, Cancelled: 3, Failed: 4 };
            return m[status] !== undefined ? m[status] : -1;
        }
        return -1;
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

    const subscribeToStations = async () => {
        if (!state.connection) return;
        
        try {
            if (state.stationId) {
                // Subscribe to specific station
                await state.connection.invoke('SubscribeStation', state.stationId);
            } else {
                // Subscribe to all stations
                for (const station of state.stations) {
                    try {
                        await state.connection.invoke('SubscribeStation', station.id);
                    } catch (err) {
                        console.warn(`Không thể subscribe station ${station.id}`, err);
                    }
                }
            }
        } catch (err) {
            console.warn('Không thể subscribe stations', err);
        }
    };

    const subscribeToActiveSessions = async () => {
        if (!state.connection || state.connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        // Subscribe to all active sessions for progress updates
        for (const session of state.activeSessions) {
            if (!state.subscribedSessions.has(session.id)) {
                try {
                    await state.connection.invoke('SubscribeSession', session.id);
                    state.subscribedSessions.add(session.id);
                    console.log(`Subscribed to session ${session.id}`);
                } catch (err) {
                    console.warn(`Không thể subscribe session ${session.id}`, err);
                }
            }
        }

        // Unsubscribe from sessions that are no longer active
        const activeSessionIds = new Set(state.activeSessions.map(s => s.id));
        for (const sessionId of state.subscribedSessions) {
            if (!activeSessionIds.has(sessionId)) {
                try {
                    await state.connection.invoke('UnsubscribeSession', sessionId);
                    state.subscribedSessions.delete(sessionId);
                    console.log(`Unsubscribed from session ${sessionId}`);
                } catch (err) {
                    console.warn(`Không thể unsubscribe session ${sessionId}`, err);
                }
            }
        }
    };

    const updateSessionProgress = (sessionId, progress) => {
        if (!activeTableBody || !progress) return;

        // Cập nhật trong state
        const session = state.activeSessions.find(s => s.id === sessionId);
        if (session) {
            session.progress = progress;
            if (progress.energyDeliveredKwh != null) {
                session.energyDeliveredKwh = progress.energyDeliveredKwh;
            }
            
            // Reset simulation data với giá trị mới từ SignalR
            const powerKw = progress.currentPowerKw || session.chargingSpotPower || 120;
            state.sessionSimulations.set(sessionId, {
                startTime: Date.now(),
                initialEnergy: progress.energyDeliveredKwh || 0,
                powerKw: powerKw
            });
            
            console.log(`SignalR updated session ${sessionId}: energy=${progress.energyDeliveredKwh}, power=${powerKw}`);
        }

        // UI sẽ được cập nhật bởi updateAllEnergies() và updateAllDurations() mỗi giây
    };

    const initSignalR = async () => {
        if (!window.signalR) return;

        state.connection = new signalR.HubConnectionBuilder()
            .withAutomaticReconnect()
            .withUrl('/hubs/station')
            .build();

        // Listen for session updates - reload active sessions immediately
        state.connection.on('SessionUpdated', (sessionId) => {
            console.log('SessionUpdated received:', sessionId);
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        });

        // Listen for charging progress updates - update UI in real-time
        state.connection.on('ChargingProgressUpdated', (sessionId, progress) => {
            console.log('ChargingProgressUpdated received:', sessionId, progress);
            
            // Cập nhật session trong state
            const session = state.activeSessions.find(s => s.id === sessionId);
            if (session && progress) {
                // Cập nhật progress object
                session.progress = progress;
                
                // Cập nhật energyDeliveredKwh
                if (progress.energyDeliveredKwh != null) {
                    session.energyDeliveredKwh = progress.energyDeliveredKwh;
                }
                
                // Cập nhật lastUpdatedAt
                if (progress.lastUpdatedAt) {
                    session.lastUpdatedAt = progress.lastUpdatedAt;
                }
            }
            
            // Cập nhật UI ngay lập tức
            updateSessionProgress(sessionId, progress);
        });

        // Listen for station availability updates
        state.connection.on('StationAvailabilityUpdated', (stationId, totalSpots, availableSpots) => {
            console.log('StationAvailabilityUpdated:', stationId);
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        });

        // Handle reconnection
        state.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
        });

        state.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            subscribeToStations();
            subscribeToActiveSessions();
        });

        try {
            await state.connection.start();
            console.log('SignalR connected for staff sessions');
            await subscribeToStations();
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    };

    const updateAllDurations = () => {
        if (!activeTableBody) return;
        
        // Cập nhật thời gian cho tất cả session đang hoạt động (InProgress)
        state.activeSessions.forEach(session => {
            if (getStatusCode(session.status) !== 1) return; // Chỉ update cho InProgress
            
            const row = activeTableBody.querySelector(`tr[data-session-id="${session.id}"]`);
            if (!row) return;

            const durationCell = row.querySelector(`td[data-duration="${session.id}"]`);
            if (durationCell && session.sessionStartTime) {
                const startTime = new Date(session.sessionStartTime);
                const now = new Date();
                const duration = Math.floor((now - startTime) / 60000); // minutes
                const durationText = duration >= 60 
                    ? `${Math.floor(duration / 60)}h ${duration % 60}m`
                    : `${duration}m`;
                durationCell.textContent = durationText;
            }
        });
    };

    const updateAllEnergies = () => {
        if (!activeTableBody) return;
        
        console.log('[updateAllEnergies] Running...');
        
        // Cập nhật năng lượng cho tất cả session đang hoạt động (InProgress)
        state.activeSessions.forEach(session => {
            if (getStatusCode(session.status) !== 1) return; // Chỉ update cho InProgress
            
            const row = activeTableBody.querySelector(`tr[data-session-id="${session.id}"]`);
            if (!row) {
                console.log(`[updateAllEnergies] Row not found for session ${session.id}`);
                return;
            }

            const energyCell = row.querySelector(`td[data-energy="${session.id}"]`);
            if (!energyCell) {
                console.log(`[updateAllEnergies] Energy cell not found for session ${session.id}`);
                return;
            }

            // Lấy simulation data cho session này
            let simData = state.sessionSimulations.get(session.id);
            
            // Nếu chưa có simulation data, khởi tạo
            if (!simData) {
                const powerKw = session.chargingSpotPower || session.progress?.currentPowerKw || 120; // Default 120kW
                const initialEnergy = session.energyDeliveredKwh || session.progress?.energyDeliveredKwh || 0;
                simData = {
                    startTime: Date.now(),
                    initialEnergy: initialEnergy,
                    powerKw: powerKw
                };
                state.sessionSimulations.set(session.id, simData);
                console.log(`[updateAllEnergies] Initialized sim for ${session.id}: energy=${initialEnergy}, power=${powerKw}`);
            }

            // Tính năng lượng dựa trên thời gian đã trôi qua và công suất
            const elapsedSeconds = (Date.now() - simData.startTime) / 1000;
            const elapsedHours = elapsedSeconds / 3600;
            const energyDelivered = simData.initialEnergy + (simData.powerKw * elapsedHours);
            
            console.log(`[updateAllEnergies] Session ${session.id}: elapsed=${elapsedSeconds.toFixed(1)}s, energy=${energyDelivered.toFixed(2)} kWh`);
            
            // Cập nhật UI
            energyCell.textContent = `${utils.formatNumber ? utils.formatNumber(energyDelivered, 2) : energyDelivered.toFixed(2)} kWh`;
            
            // Cập nhật trong session object
            session.energyDeliveredKwh = energyDelivered;
            if (session.progress) {
                session.progress.energyDeliveredKwh = energyDelivered;
            }
        });
    };

    const setupAutoRefresh = () => {
        // Clear existing intervals
        if (state.refreshInterval) {
            clearInterval(state.refreshInterval);
        }
        if (state.durationInterval) {
            clearInterval(state.durationInterval);
        }

        // Cập nhật thời gian mỗi giây
        state.durationInterval = setInterval(() => {
            if (activeTab && activeTab.classList.contains('active')) {
                updateAllDurations();
            }
        }, 1000);

        // Cập nhật năng lượng mỗi giây (simulation)
        state.energyInterval = setInterval(() => {
            if (activeTab && activeTab.classList.contains('active')) {
                updateAllEnergies();
            }
        }, 1000);

        // Reload toàn bộ danh sách mỗi 30 giây để đảm bảo sync
        state.refreshInterval = setInterval(() => {
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        }, 30000);
    };

    const bindEvents = () => {
        if (stationFilter) {
            stationFilter.addEventListener('change', async (e) => {
                state.stationId = e.target.value || null;
                
                // Update SignalR subscriptions
                if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
                    await subscribeToStations();
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
                // Clear intervals khi chuyển sang tab lịch sử
                if (state.refreshInterval) {
                    clearInterval(state.refreshInterval);
                }
                if (state.durationInterval) {
                    clearInterval(state.durationInterval);
                }
                loadHistorySessions();
            });
        }

        if (savePaymentBtn) {
            savePaymentBtn.addEventListener('click', processPayment);
        }
    };

    const init = async () => {
        bindEvents();
        await loadStations();
        await initSignalR();
        loadActiveSessions();
        setupAutoRefresh();
    };

    init().catch(err => console.error('Initialization error:', err));
})();

