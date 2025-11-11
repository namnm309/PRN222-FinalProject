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
        refreshInterval: null
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

    const loadActiveSessions = async () => {
        try {
            const params = {};
            if (state.stationId) params.stationId = state.stationId;
            
            const url = `/api/ChargingSession/active${buildQueryString(params)}`;
            console.log('Loading active sessions from:', url);
            const data = await utils.fetchJson(url);
            console.log('Active sessions data:', data);
            renderActiveSessions(data || []);
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
            return;
        }

        activeTableBody.innerHTML = sessions.map(session => {
            const duration = session.sessionEndTime 
                ? Math.floor((new Date(session.sessionEndTime) - new Date(session.sessionStartTime)) / 60000)
                : Math.floor((new Date() - new Date(session.sessionStartTime)) / 60000);
            const durationText = duration >= 60 
                ? `${Math.floor(duration / 60)}h ${duration % 60}m`
                : `${duration}m`;

            return `
                <tr>
                    <td>${utils.formatDateTime(session.sessionStartTime)}</td>
                    <td><strong>${session.chargingStationName || '--'}</strong><br/><small>Điểm: ${session.chargingSpotNumber || '--'}</small></td>
                    <td>${session.userName || 'N/A'}</td>
                    <td>${session.vehicleName || '--'}</td>
                    <td>${utils.formatNumber(session.energyDeliveredKwh ?? session.energyRequestedKwh ?? 0, 2)}</td>
                    <td>${durationText}</td>
                    <td>${utils.renderStatusBadge(session.status)}</td>
                </tr>
            `;
        }).join('');
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

        state.connection.on('SessionUpdated', () => {
            if (activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
            loadUsageTrend();
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
            if (activeTab.classList.contains('active')) {
                loadActiveSessions();
            }
        }, 10000); // Refresh mỗi 10 giây
    };

    const bindEvents = () => {
        stationFilter.addEventListener('change', (e) => {
            state.stationId = e.target.value || null;
            if (state.connection && state.stationId) {
                state.connection.invoke('SubscribeStation', state.stationId).catch(console.error);
            }
            if (activeTab.classList.contains('active')) {
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

