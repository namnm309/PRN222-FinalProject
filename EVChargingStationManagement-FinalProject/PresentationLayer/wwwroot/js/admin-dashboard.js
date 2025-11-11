(function () {
    const stationSelect = document.getElementById('DashboardStationFilter');
    if (!stationSelect) return;

    const utils = window.dashboardUtils;
    const rangeSelect = document.querySelector('[data-range-filter]');
    const kpiRoot = document.querySelector('.dashboard-kpis');
    const sessionTable = document.querySelector('[data-session-list]');
    const reservationTable = document.querySelector('[data-reservation-list]');
    const notificationList = document.querySelector('[data-notification-list]');
    const mapContainer = document.querySelector('[data-map-container]');

    const state = {
        stationId: null,
        stations: [],
        connection: null,
        previousStationId: null,
        dateRange: 'today' // today, yesterday, 7days, 30days
    };

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

    const renderStations = () => {
        stationSelect.innerHTML = '<option value="">Tất cả trạm</option>';
        state.stations.forEach(station => {
            const opt = document.createElement('option');
            opt.value = station.id;
            opt.textContent = station.name;
            stationSelect.appendChild(opt);
        });
        if (!state.stationId && state.stations.length > 0) {
            state.stationId = state.stations[0].id;
            stationSelect.value = state.stationId;
        }
        state.previousStationId = null;
        updateMap();
        subscribeStation();
    };

    const updateKpis = (data) => {
        if (!data) return;
        utils.setKpiValue(kpiRoot, 'total-spots', data.totalSpots);
        utils.setKpiValue(kpiRoot, 'available-spots', data.availableSpots);
        utils.setKpiValue(kpiRoot, 'active-sessions', data.activeSessions);
        utils.setKpiValue(kpiRoot, 'reservations-today', data.reservationsToday);
        utils.setKpiValue(kpiRoot, 'energy-today', data.energyDeliveredToday, v => utils.formatNumber(v, 1));
        utils.setKpiValue(kpiRoot, 'revenue-today', data.revenueToday, utils.formatCurrency);
    };

    const updateSessions = (rows) => {
        if (!sessionTable) return;
        if (!rows || rows.length === 0) {
            sessionTable.innerHTML = `<tr><td colspan="5" class="text-center text-muted">Chưa có dữ liệu</td></tr>`;
            return;
        }
        sessionTable.innerHTML = rows.map(item => `
            <tr>
                <td>${utils.formatDateTime(item.sessionStartTime)}${item.sessionEndTime ? `<br/><small>Kết thúc: ${utils.formatDateTime(item.sessionEndTime)}</small>` : ''}</td>
                <td><strong>${item.stationName || '--'}</strong><br/><small>Điểm: ${item.spotNumber || '--'}</small></td>
                <td>${utils.renderStatusBadge(item.status)}</td>
                <td>${utils.formatNumber(item.energyDeliveredKwh ?? item.energyRequestedKwh, 1)}</td>
                <td>${utils.formatCurrency(item.cost)}</td>
            </tr>
        `).join('');
    };

    const updateReservations = async () => {
        if (!reservationTable) return;
        if (!state.stationId) {
            reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Hãy chọn trạm để xem lịch đặt chỗ.</td></tr>`;
            return;
        }
        try {
            const data = await utils.fetchJson(`/api/reservation/station/${state.stationId}`);
            if (!data || data.length === 0) {
                reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Chưa có đặt chỗ.</td></tr>`;
                return;
            }
            reservationTable.innerHTML = data.slice(0, 10).map(item => `
                <tr>
                    <td>${utils.formatDateTime(item.scheduledStartTime)}</td>
                    <td>${item.userFullName || item.userId}</td>
                    <td>${item.chargingStationName || '--'} • ${item.chargingSpotNumber || '--'}</td>
                    <td>${utils.renderStatusBadge(item.status)}</td>
                </tr>
            `).join('');
        } catch (err) {
            console.error(err);
            reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-danger">Không thể tải dữ liệu đặt chỗ.</td></tr>`;
        }
    };

    const updateNotifications = async () => {
        if (!notificationList) return;
        try {
            const data = await utils.fetchJson('/api/notification?unreadOnly=false');
            if (!data || data.length === 0) {
                notificationList.innerHTML = `<li class="notification-item muted">Không có thông báo nào.</li>`;
                return;
            }
            notificationList.innerHTML = data.slice(0, 6).map(item => `
                <li class="notification-item">
                    <div class="fw-semibold">${item.title}</div>
                    <div class="small text-muted">${utils.formatDateTime(item.sentAt)}</div>
                    <div>${item.message}</div>
                </li>
            `).join('');
        } catch (err) {
            console.error(err);
            notificationList.innerHTML = `<li class="notification-item muted text-danger">Không thể tải thông báo.</li>`;
        }
    };

    const updateMap = () => {
        if (!mapContainer) return;
        if (!state.stationId) {
            mapContainer.innerHTML = `<div class="placeholder">Hãy chọn trạm để hiển thị bản đồ.</div>`;
            return;
        }
        const station = state.stations.find(x => x.id === state.stationId);
        if (!station || !station.latitude || !station.longitude) {
            mapContainer.innerHTML = `<div class="placeholder">Trạm chưa có tọa độ để hiển thị bản đồ.</div>`;
            return;
        }

        const iframe = document.createElement('iframe');
        const lat = station.latitude;
        const lng = station.longitude;
        iframe.src = `https://www.google.com/maps?q=${lat},${lng}&z=15&output=embed`;
        iframe.loading = 'lazy';
        mapContainer.innerHTML = '';
        mapContainer.appendChild(iframe);
    };

    const subscribeStation = async () => {
        if (!state.connection || !state.stationId) return;
        try {
            if (state.previousStationId && state.previousStationId !== state.stationId) {
                await state.connection.invoke('UnsubscribeStation', state.previousStationId);
            }
            await state.connection.invoke('SubscribeStation', state.stationId);
            state.previousStationId = state.stationId ?? null;
        } catch (err) {
            console.warn('Không thể subscribe station hub', err);
        }
    };

    const initSignalR = async () => {
        if (!window.signalR) return;
        state.connection = new signalR.HubConnectionBuilder()
            .withAutomaticReconnect()
            .withUrl('/hubs/station')
            .build();

        state.connection.on('SpotStatusUpdated', () => loadOverview());
        state.connection.on('SessionUpdated', () => {
            loadOverview();
            loadSessions();
        });
        state.connection.on('ReservationUpdated', () => {
            loadOverview();
            updateReservations();
        });
        state.connection.on('NotificationReceived', updateNotifications);

        try {
            await state.connection.start();
            await subscribeStation();
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    };

    const loadStations = async () => {
        try {
            const data = await utils.fetchJson('/api/ChargingStation');
            state.stations = data || [];
            renderStations();
        } catch (err) {
            console.error(err);
            stationSelect.innerHTML = '<option value="">Lỗi tải trạm</option>';
        }
    };

    const loadOverview = async () => {
        try {
            const data = await utils.fetchJson(`/api/dashboard/overview${buildQueryString({ stationId: state.stationId })}`);
            updateKpis(data);
        } catch (err) {
            console.error('overview', err);
        }
    };

    const getDateRange = (range) => {
        const now = new Date();
        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        let startDate, endDate;

        switch (range) {
            case 'today':
                startDate = new Date(today);
                endDate = new Date(today);
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'yesterday':
                startDate = new Date(today);
                startDate.setDate(startDate.getDate() - 1);
                endDate = new Date(today);
                endDate.setDate(endDate.getDate() - 1);
                endDate.setHours(23, 59, 59, 999);
                break;
            case '7days':
                startDate = new Date(today);
                startDate.setDate(startDate.getDate() - 7);
                endDate = new Date(today);
                endDate.setHours(23, 59, 59, 999);
                break;
            case '30days':
                startDate = new Date(today);
                startDate.setDate(startDate.getDate() - 30);
                endDate = new Date(today);
                endDate.setHours(23, 59, 59, 999);
                break;
            default:
                startDate = new Date(today);
                endDate = new Date(today);
                endDate.setHours(23, 59, 59, 999);
        }

        return { startDate, endDate };
    };

    const loadSessions = async () => {
        try {
            const { startDate, endDate } = getDateRange(state.dateRange);
            const params = {
                stationId: state.stationId,
                startDate: startDate.toISOString(),
                endDate: endDate.toISOString(),
                page: 1,
                pageSize: 50
            };
            
            const response = await utils.fetchJson(`/api/dashboard/sessions/all${buildQueryString(params)}`);
            const sessions = response?.data || response || [];
            updateSessions(sessions);
        } catch (err) {
            console.error('sessions', err);
            if (sessionTable) sessionTable.innerHTML = `<tr><td colspan="5" class="text-center text-danger">Không thể tải dữ liệu phiên sạc.</td></tr>`;
        }
    };

    const bindEvents = () => {
        stationSelect.addEventListener('change', async (e) => {
            state.stationId = e.target.value || null;
            updateMap();
            await subscribeStation();
            await Promise.all([
                loadOverview(),
                loadSessions(),
                updateReservations()
            ]);
        });

        if (rangeSelect) {
            rangeSelect.addEventListener('change', async (e) => {
                state.dateRange = e.target.value || 'today';
                await loadSessions();
            });
        }

        document.querySelector('[data-refresh-sessions]')?.addEventListener('click', loadSessions);
        document.querySelector('[data-refresh-reservations]')?.addEventListener('click', updateReservations);
    };

    const init = async () => {
        await loadStations();
        bindEvents();
        await Promise.all([
            loadOverview(),
            loadSessions(),
            updateReservations(),
            updateNotifications()
        ]);
        await initSignalR();
    };

    init().catch(err => console.error(err));
})();

