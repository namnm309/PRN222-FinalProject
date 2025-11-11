(function () {
    const stationSelect = document.getElementById('StaffStationFilter');
    if (!stationSelect) return;

    const utils = window.dashboardUtils;
    const kpiRoot = document.querySelector('.dashboard-kpis');
    const sessionTable = document.querySelector('[data-session-list]');
    const reservationTable = document.querySelector('[data-reservation-list]');
    const errorList = document.querySelector('[data-error-list]');
    const maintenanceList = document.querySelector('[data-maintenance-list]');

    const state = {
        stationId: null,
        stations: [],
        connection: null,
        previousStationId: null
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
        subscribeStation();
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

    const updateKpis = ({ overview, errors, maintenances }) => {
        if (overview) {
            utils.setKpiValue(kpiRoot, 'available-spots', overview.availableSpots);
            utils.setKpiValue(kpiRoot, 'active-sessions', overview.activeSessions);
        }

        if (errors) {
            const openCount = errors.filter(item => {
                const status = String(item.status || '').toLowerCase();
                return ['reported', 'investigating', '0', '1'].includes(status);
            }).length;
            utils.setKpiValue(kpiRoot, 'open-errors', openCount);
        }

        if (maintenances) {
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            const todayCount = maintenances.filter(item => {
                if (!item.scheduledDate) return false;
                const dt = new Date(item.scheduledDate);
                dt.setHours(0, 0, 0, 0);
                return dt.getTime() === today.getTime();
            }).length;
            utils.setKpiValue(kpiRoot, 'maintenance-today', todayCount);
        }
    };

    const loadOverview = async () => {
        const data = await utils.fetchJson(`/api/dashboard/overview${state.stationId ? `?stationId=${state.stationId}` : ''}`);
        return data;
    };

    const loadSessions = async () => {
        try {
            const rows = await utils.fetchJson(`/api/ChargingSession/active${state.stationId ? `?stationId=${state.stationId}` : ''}`);
            if (!rows || rows.length === 0) {
                sessionTable.innerHTML = `<tr><td colspan="5" class="text-center text-muted">Không có phiên sạc nào đang diễn ra.</td></tr>`;
                return rows;
            }

            sessionTable.innerHTML = rows.map(item => `
                <tr>
                    <td>${utils.formatDateTime(item.sessionStartTime)}</td>
                    <td>${item.chargingSpotNumber || '--'}<br/><small>${item.chargingStationName || '--'}</small></td>
                    <td>${item.vehicleName || item.userId}</td>
                    <td>${utils.formatNumber(item.energyRequestedKwh, 1)}</td>
                    <td>${item.notes || ''}</td>
                </tr>
            `).join('');

            return rows;
        } catch (err) {
            console.error(err);
            sessionTable.innerHTML = `<tr><td colspan="5" class="text-center text-danger">Không thể tải dữ liệu phiên sạc.</td></tr>`;
            return [];
        }
    };

    const loadReservations = async () => {
        if (!state.stationId) {
            reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Hãy chọn trạm để xem đặt chỗ.</td></tr>`;
            return [];
        }
        try {
            const data = await utils.fetchJson(`/api/reservation/station/${state.stationId}`);
            const upcoming = (data || []).filter(item => new Date(item.scheduledStartTime) > new Date()).slice(0, 10);
            if (upcoming.length === 0) {
                reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Chưa có đặt chỗ sắp tới.</td></tr>`;
                return [];
            }
            reservationTable.innerHTML = upcoming.map(item => `
                <tr>
                    <td>${utils.formatDateTime(item.scheduledStartTime)}</td>
                    <td>${item.userFullName || item.userId}</td>
                    <td>${item.chargingSpotNumber || '--'}</td>
                    <td>${utils.renderStatusBadge(item.status)}</td>
                </tr>
            `).join('');
            return upcoming;
        } catch (err) {
            console.error(err);
            reservationTable.innerHTML = `<tr><td colspan="4" class="text-center text-danger">Không thể tải dữ liệu đặt chỗ.</td></tr>`;
            return [];
        }
    };

    const loadErrors = async () => {
        if (!state.stationId) {
            errorList.innerHTML = `<li class="notification-item muted">Hãy chọn trạm để xem sự cố.</li>`;
            return [];
        }
        try {
            const data = await utils.fetchJson(`/api/StationError/station/${state.stationId}`);
            const open = (data || []).filter(item => {
                const status = String(item.status || '').toLowerCase();
                return ['reported', 'investigating', '0', '1'].includes(status);
            }).slice(0, 6);
            if (open.length === 0) {
                errorList.innerHTML = `<li class="notification-item muted">Không có sự cố mở.</li>`;
                return [];
            }
            errorList.innerHTML = open.map(item => `
                <li class="notification-item">
                    <div class="fw-semibold">${item.title || item.errorCode || 'Sự cố'}</div>
                    <div class="small text-muted">${utils.formatDateTime(item.reportedAt)}</div>
                    <div>${item.description || ''}</div>
                </li>
            `).join('');
            return open;
        } catch (err) {
            console.error(err);
            errorList.innerHTML = `<li class="notification-item muted text-danger">Không thể tải danh sách sự cố.</li>`;
            return [];
        }
    };

    const loadMaintenances = async () => {
        if (!state.stationId) {
            maintenanceList.innerHTML = `<li class="notification-item muted">Hãy chọn trạm để xem bảo trì.</li>`;
            return [];
        }
        try {
            const data = await utils.fetchJson(`/api/StationMaintenance/station/${state.stationId}`);
            const upcoming = (data || []).filter(item => {
                const status = String(item.status || '').toLowerCase();
                return !['completed', '2', 'closed'].includes(status);
            }).slice(0, 6);
            if (upcoming.length === 0) {
                maintenanceList.innerHTML = `<li class="notification-item muted">Không có lịch bảo trì.</li>`;
                return [];
            }
            maintenanceList.innerHTML = upcoming.map(item => `
                <li class="notification-item">
                    <div class="fw-semibold">${item.title || 'Bảo trì định kỳ'}</div>
                    <div class="small text-muted">Lịch: ${utils.formatDateTime(item.scheduledDate || item.startDate)}</div>
                    <div>${item.description || ''}</div>
                </li>
            `).join('');
            return upcoming;
        } catch (err) {
            console.error(err);
            maintenanceList.innerHTML = `<li class="notification-item muted text-danger">Không thể tải lịch bảo trì.</li>`;
            return [];
        }
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

        const reload = () => refreshAll(false);
        state.connection.on('SpotStatusUpdated', reload);
        state.connection.on('SessionUpdated', reload);
        state.connection.on('ReservationUpdated', reload);

        try {
            await state.connection.start();
            await subscribeStation();
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    };

    const refreshAll = async (updateStations = true) => {
        if (updateStations) {
            await loadStations();
        }

        const [overview, sessions, reservations, errors, maintenances] = await Promise.all([
            loadOverview(),
            loadSessions(),
            loadReservations(),
            loadErrors(),
            loadMaintenances()
        ]);

        updateKpis({ overview, errors, maintenances, sessions, reservations });
    };

    const bindEvents = () => {
        stationSelect.addEventListener('change', async (e) => {
            state.stationId = e.target.value || null;
            await subscribeStation();
            await refreshAll(false);
        });

        document.querySelector('[data-refresh-sessions]')?.addEventListener('click', () => loadSessions());
        document.querySelector('[data-refresh-reservations]')?.addEventListener('click', () => loadReservations());
    };

    const init = async () => {
        await refreshAll(true);
        bindEvents();
        await initSignalR();
    };

    init().catch(err => console.error(err));
})();

