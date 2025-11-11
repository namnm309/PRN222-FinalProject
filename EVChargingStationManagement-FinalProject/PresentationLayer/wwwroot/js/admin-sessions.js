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
        dateFrom: null,
        dateTo: null,
        currentPage: 1,
        pageSize: 50,
        totalPages: 1,
        connection: null,
        usageChart: null,
        refreshInterval: null,
        durationInterval: null, // Timer để cập nhật thời gian
        energyInterval: null, // Timer để cập nhật năng lượng
        activeSessions: [], // Lưu danh sách session đang hoạt động
        subscribedSessions: new Set(), // Track các session đã subscribe
        sessionSimulations: new Map(), // Track simulation data cho mỗi session {startTime, initialEnergy, powerKw}
        previousStationId: null
    };

    // DOM Elements
    const stationFilter = document.getElementById('SessionStationFilter');
    const statusFilter = document.getElementById('SessionStatusFilter');
    const dateFromInput = document.getElementById('SessionDateFrom');
    const dateToInput = document.getElementById('SessionDateTo');
    const activeTableBody = document.getElementById('activeSessionsTableBody');
    const historyTableBody = document.getElementById('historySessionsTableBody');
    const historyPagination = document.getElementById('historyPagination');
    const historyPaginationInfo = document.getElementById('historyPaginationInfo');
    const activeTab = document.getElementById('active-tab');
    const historyTab = document.getElementById('history-tab');

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
            console.log('Loading stations...');
            const data = await utils.fetchJson('/api/ChargingStation');
            console.log('Stations data:', data);
            state.stations = data || [];
            renderStations();
            console.log('Stations loaded:', state.stations.length);
            
            // Subscribe to stations after loading
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
        if (!stationFilter) {
            console.error('stationFilter element not found');
            return;
        }
        stationFilter.innerHTML = '<option value="">Tất cả trạm</option>';
        state.stations.forEach(station => {
            const opt = document.createElement('option');
            opt.value = station.id;
            opt.textContent = station.name;
            stationFilter.appendChild(opt);
        });
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

    const loadProgressForSession = async (sessionId) => {
        try {
            const progress = await utils.fetchJson(`/api/ChargingSession/${sessionId}/progress`);
            return progress;
        } catch (err) {
            return null;
        }
    };

    const loadActiveSessions = async () => {
        try {
            const params = {};
            if (state.stationId) params.stationId = state.stationId;
            
            const url = `/api/ChargingSession/active${buildQueryString(params)}`;
            console.log('Loading active sessions from:', url);
            const data = await utils.fetchJson(url);
            state.activeSessions = data || [];
            
            // Load progress cho từng session để lấy giá trị mới nhất
            const progressPromises = state.activeSessions.map(session => 
                loadProgressForSession(session.id).then(progress => ({ session, progress }))
            );
            
            const results = await Promise.allSettled(progressPromises);
            
            // Cập nhật energyDeliveredKwh từ progress nếu có
            results.forEach(result => {
                if (result.status === 'fulfilled') {
                    const { session, progress } = result.value;
                    if (progress) {
                        session.progress = progress;
                        if (progress.energyDeliveredKwh !== null && progress.energyDeliveredKwh !== undefined) {
                            session.energyDeliveredKwh = progress.energyDeliveredKwh;
                        }
                    }
                }
            });
            
            renderActiveSessions(state.activeSessions);
            
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
                }
            });
        } catch (err) {
            console.error('Error loading active sessions:', err);
            if (activeTableBody) {
                activeTableBody.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Không thể tải dữ liệu: ' + (err.message || 'Lỗi không xác định') + '</td></tr>';
            }
        }
    };

    const renderActiveSessions = (sessions) => {
        if (!sessions || sessions.length === 0) {
            activeTableBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Không có phiên sạc đang hoạt động</td></tr>';
            state.activeSessions = [];
            subscribeToActiveSessions();
            return;
        }

        // Lưu sessions vào state
        state.activeSessions = sessions;

        activeTableBody.innerHTML = sessions.map(session => {
            const duration = session.sessionEndTime 
                ? Math.floor((new Date(session.sessionEndTime) - new Date(session.sessionStartTime)) / 60000)
                : Math.floor((new Date() - new Date(session.sessionStartTime)) / 60000);
            const durationText = duration >= 60 
                ? `${Math.floor(duration / 60)}h ${duration % 60}m`
                : `${duration}m`;

            // Lấy năng lượng: ưu tiên progress.energyDeliveredKwh, sau đó session.energyDeliveredKwh
            let energyKwh = 0;
            if (session.progress && session.progress.energyDeliveredKwh != null) {
                energyKwh = session.progress.energyDeliveredKwh;
            } else if (session.energyDeliveredKwh != null) {
                energyKwh = session.energyDeliveredKwh;
            }

            return `
                <tr data-session-id="${session.id}">
                    <td>${utils.formatDateTime(session.sessionStartTime)}</td>
                    <td><strong>${session.chargingStationName || '--'}</strong><br/><small>Điểm: ${session.chargingSpotNumber || '--'}</small></td>
                    <td>${session.userName || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td data-energy="${session.id}">${utils.formatNumber(energyKwh, 2)} kWh</td>
                    <td data-duration="${session.id}">${durationText}</td>
                    <td>${utils.renderStatusBadge(session.status)}</td>
                </tr>
            `;
        }).join('');

        // Subscribe to active sessions for progress updates
        subscribeToActiveSessions();
        
        // Start duration update interval
        startDurationUpdate();
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
        }

        // UI sẽ được cập nhật bởi updateAllEnergies() và updateAllDurations() mỗi giây
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
                const duration = Math.floor((now - startTime) / 60000);
                const durationText = duration >= 60 
                    ? `${Math.floor(duration / 60)}h ${duration % 60}m`
                    : `${duration}m`;
                durationCell.textContent = durationText;
            }
        });
    };

    const updateAllEnergies = () => {
        if (!activeTableBody) return;
        
        // Cập nhật năng lượng cho tất cả session đang hoạt động (InProgress)
        state.activeSessions.forEach(session => {
            if (getStatusCode(session.status) !== 1) return; // Chỉ update cho InProgress
            
            const row = activeTableBody.querySelector(`tr[data-session-id="${session.id}"]`);
            if (!row) return;

            const energyCell = row.querySelector(`td[data-energy="${session.id}"]`);
            if (!energyCell) return;

            // Lấy simulation data cho session này
            let simData = state.sessionSimulations.get(session.id);
            
            // Nếu chưa có simulation data, khởi tạo
            if (!simData) {
                const powerKw = session.chargingSpotPower || session.progress?.currentPowerKw || 120;
                const initialEnergy = session.energyDeliveredKwh || session.progress?.energyDeliveredKwh || 0;
                simData = {
                    startTime: Date.now(),
                    initialEnergy: initialEnergy,
                    powerKw: powerKw
                };
                state.sessionSimulations.set(session.id, simData);
            }

            // Tính năng lượng dựa trên thời gian đã trôi qua và công suất
            const elapsedSeconds = (Date.now() - simData.startTime) / 1000;
            const elapsedHours = elapsedSeconds / 3600;
            const energyDelivered = simData.initialEnergy + (simData.powerKw * elapsedHours);
            
            // Cập nhật UI
            energyCell.textContent = `${utils.formatNumber(energyDelivered, 2)} kWh`;
            
            // Cập nhật trong session object
            session.energyDeliveredKwh = energyDelivered;
            if (session.progress) {
                session.progress.energyDeliveredKwh = energyDelivered;
            }
        });
    };

    const startDurationUpdate = () => {
        if (state.durationInterval) {
            clearInterval(state.durationInterval);
        }
        if (state.energyInterval) {
            clearInterval(state.energyInterval);
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

    const subscribeToStations = async () => {
        if (!state.connection || state.connection.state !== signalR.HubConnectionState.Connected) {
            return;
        }

        try {
            if (state.stationId) {
                // Unsubscribe from previous station
                if (state.previousStationId && state.previousStationId !== state.stationId) {
                    await state.connection.invoke('UnsubscribeStation', state.previousStationId);
                }
                // Subscribe to specific station
                await state.connection.invoke('SubscribeStation', state.stationId);
                state.previousStationId = state.stationId;
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

    const loadHistorySessions = async () => {
        try {
            const params = {
                page: state.currentPage,
                pageSize: state.pageSize
            };
            if (state.stationId) params.stationId = state.stationId;
            if (state.status !== null && state.status !== '') params.status = parseInt(state.status);
            if (state.dateFrom) params.startDate = new Date(state.dateFrom).toISOString();
            if (state.dateTo) {
                const endDate = new Date(state.dateTo);
                endDate.setHours(23, 59, 59, 999);
                params.endDate = endDate.toISOString();
            }

            const url = `/api/dashboard/sessions/all${buildQueryString(params)}`;
            console.log('Loading history sessions from:', url);
            const response = await utils.fetchJson(url);
            console.log('History sessions response:', response);
            renderHistorySessions(response.data || [], response.totalCount || 0, response.totalPages || 1);
        } catch (err) {
            console.error('Error loading history sessions:', err);
            if (historyTableBody) {
                historyTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Không thể tải dữ liệu: ' + (err.message || 'Lỗi không xác định') + '</td></tr>';
            }
        }
    };

    const renderHistorySessions = (sessions, totalCount, totalPages) => {
        state.totalPages = totalPages;

        if (!sessions || sessions.length === 0) {
            historyTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted">Không có dữ liệu</td></tr>';
            historyPaginationInfo.textContent = 'Hiển thị 0 - 0 của 0';
            historyPagination.innerHTML = '';
            return;
        }

        const start = (state.currentPage - 1) * state.pageSize + 1;
        const end = Math.min(start + sessions.length - 1, totalCount);
        historyPaginationInfo.textContent = `Hiển thị ${start} - ${end} của ${totalCount}`;

        historyTableBody.innerHTML = sessions.map(session => {
            const startTime = utils.formatDateTime(session.sessionStartTime);
            const endTime = session.sessionEndTime ? utils.formatDateTime(session.sessionEndTime) : '--';
            const duration = session.durationMinutes 
                ? (session.durationMinutes >= 60 
                    ? `${Math.floor(session.durationMinutes / 60)}h ${session.durationMinutes % 60}m`
                    : `${session.durationMinutes}m`)
                : '--';

            return `
                <tr>
                    <td>
                        ${startTime}
                        ${session.sessionEndTime ? `<br/><small class="text-muted">Kết thúc: ${endTime}</small>` : ''}
                    </td>
                    <td><strong>${session.stationName || '--'}</strong><br/><small>Điểm: ${session.spotNumber || '--'}</small></td>
                    <td>${session.userName || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td>${utils.formatNumber(session.energyDeliveredKwh ?? session.energyRequestedKwh ?? 0, 2)}</td>
                    <td>${utils.formatCurrency(session.cost)}</td>
                    <td>${duration}</td>
                    <td>${utils.renderStatusBadge(session.status)}</td>
                </tr>
            `;
        }).join('');

        renderPagination(totalPages);
    };

    const renderPagination = (totalPages) => {
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

    const initUsageChart = () => {
        const ctx = document.getElementById('usageTrendChart');
        if (!ctx) return;

        if (state.usageChart) {
            state.usageChart.destroy();
        }

        state.usageChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Số phiên sạc',
                    data: [],
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });

        loadUsageTrend();
    };

    const loadUsageTrend = async () => {
        try {
            const endDate = new Date();
            const startDate = new Date();
            startDate.setDate(startDate.getDate() - 7);

            const params = {
                startDate: startDate.toISOString().split('T')[0],
                endDate: endDate.toISOString().split('T')[0]
            };
            if (state.stationId) params.stationId = state.stationId;

            const data = await utils.fetchJson(`/api/dashboard/sessions/all${buildQueryString(params)}`);
            
            if (data && data.data) {
                const sessionsByDate = {};
                data.data.forEach(session => {
                    const date = new Date(session.sessionStartTime).toLocaleDateString('vi-VN');
                    sessionsByDate[date] = (sessionsByDate[date] || 0) + 1;
                });

                const labels = Object.keys(sessionsByDate).sort();
                const values = labels.map(date => sessionsByDate[date]);

                if (state.usageChart) {
                    state.usageChart.data.labels = labels;
                    state.usageChart.data.datasets[0].data = values;
                    state.usageChart.update();
                }
            }
        } catch (err) {
            console.error('Error loading usage trend:', err);
        }
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
            loadUsageTrend();
        });

        // Listen for charging progress updates - update UI in real-time
        state.connection.on('ChargingProgressUpdated', (sessionId, progress) => {
            console.log('ChargingProgressUpdated received:', sessionId, progress);
            
            // Cập nhật session trong state
            const session = state.activeSessions.find(s => s.id === sessionId);
            if (session && progress) {
                session.progress = progress;
                if (progress.energyDeliveredKwh != null) {
                    session.energyDeliveredKwh = progress.energyDeliveredKwh;
                }
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

        state.connection.onreconnected(async () => {
            console.log('SignalR reconnected');
            await subscribeToStations();
            await subscribeToActiveSessions();
        });

        try {
            await state.connection.start();
            await subscribeToStations();
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    };

    const setupAutoRefresh = () => {
        if (state.refreshInterval) {
            clearInterval(state.refreshInterval);
        }
        if (state.durationInterval) {
            clearInterval(state.durationInterval);
        }
        if (state.energyInterval) {
            clearInterval(state.energyInterval);
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
        stationFilter.addEventListener('change', async (e) => {
            state.stationId = e.target.value || null;
            if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
                await subscribeToStations();
            }
            if (activeTab && activeTab.classList.contains('active')) {
                loadActiveSessions();
            } else {
                state.currentPage = 1;
                loadHistorySessions();
            }
            loadUsageTrend();
        });

        document.querySelector('[data-action="apply-filters"]')?.addEventListener('click', () => {
            state.status = statusFilter.value;
            state.dateFrom = dateFromInput.value;
            state.dateTo = dateToInput.value;
            state.currentPage = 1;
            loadHistorySessions();
            loadUsageTrend();
        });

        document.querySelector('[data-action="reset-filters"]')?.addEventListener('click', () => {
            stationFilter.value = '';
            statusFilter.value = '';
            dateFromInput.value = '';
            dateToInput.value = '';
            state.stationId = null;
            state.status = null;
            state.dateFrom = null;
            state.dateTo = null;
            state.currentPage = 1;
            loadHistorySessions();
            loadUsageTrend();
        });

        document.querySelector('[data-action="refresh-active"]')?.addEventListener('click', loadActiveSessions);
        document.querySelector('[data-action="refresh-history"]')?.addEventListener('click', () => {
            state.currentPage = 1;
            loadHistorySessions();
        });

        activeTab.addEventListener('shown.bs.tab', () => {
            loadActiveSessions();
            setupAutoRefresh();
        });

        historyTab.addEventListener('shown.bs.tab', () => {
            if (state.refreshInterval) {
                clearInterval(state.refreshInterval);
            }
            if (state.durationInterval) {
                clearInterval(state.durationInterval);
            }
            if (state.energyInterval) {
                clearInterval(state.energyInterval);
            }
        });
    };

    const init = async () => {
        try {
            // Check if required DOM elements exist
            if (!activeTableBody || !historyTableBody) {
                console.error('Required DOM elements not found');
                return;
            }

            console.log('Initializing admin-sessions...');
            await loadStations();
            bindEvents();
            initUsageChart();
            await initSignalR();
            loadActiveSessions();
            setupAutoRefresh();
            
            // Subscribe to stations after loading
            if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
                await subscribeToStations();
            }
            console.log('Admin-sessions initialized successfully');
        } catch (err) {
            console.error('Initialization error:', err);
        }
    };

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            init().catch(err => console.error('Initialization error:', err));
        });
    } else {
        init().catch(err => console.error('Initialization error:', err));
    }
})();

