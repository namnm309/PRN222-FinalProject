(function () {
    const stationSelect = document.getElementById('DashboardStationFilter');
    if (!stationSelect) return;

    const utils = window.dashboardUtils;
    const rangeSelect = document.querySelector('[data-range-filter]');
    const kpiRoot = document.querySelector('.dashboard-kpis');
    const sessionChartCanvas = document.querySelector('[data-session-chart]');
    const sessionChartEmpty = document.querySelector('[data-session-chart-empty]');
    const reservationChartCanvas = document.querySelector('[data-reservation-chart]');
    const reservationChartEmpty = document.querySelector('[data-reservation-chart-empty]');
    const notificationList = document.querySelector('[data-notification-list]');
    const mapContainer = document.querySelector('[data-map-container]');

    const state = {
        stationId: null,
        stations: [],
        connection: null,
        previousStationId: null,
        dateRange: 'today', // today, yesterday, 7days, 30days
        sessionsChart: null,
        reservationsChart: null
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

    const getStatusName = (status) => {
        const statusMap = {
            'Scheduled': 'Đã lên lịch',
            'InProgress': 'Đang chạy',
            'Completed': 'Hoàn thành',
            'Cancelled': 'Đã hủy',
            'Failed': 'Thất bại'
        };
        return statusMap[status] || status;
    };

    const getStatusColor = (status) => {
        const colorMap = {
            'Scheduled': 'rgba(128, 128, 128, 1)',      // Xám
            'InProgress': 'rgba(255, 193, 7, 1)',       // Vàng
            'Completed': 'rgba(40, 167, 69, 1)',        // Xanh lá
            'Cancelled': 'rgba(220, 53, 69, 1)',        // Đỏ
            'Failed': 'rgba(220, 53, 69, 1)'            // Đỏ
        };
        return colorMap[status] || 'rgba(128, 128, 128, 1)';
    };

    const groupSessionsByStatus = (sessions) => {
        const grouped = {};
        sessions.forEach(session => {
            const status = session.status || 'Scheduled';
            if (!grouped[status]) {
                grouped[status] = 0;
            }
            grouped[status]++;
        });
        return grouped;
    };

    const updateSessions = (rows) => {
        if (!sessionChartCanvas) return;

        // Hiển thị message nếu không có dữ liệu
        if (!rows || rows.length === 0) {
            if (state.sessionsChart) {
                state.sessionsChart.destroy();
                state.sessionsChart = null;
            }
            sessionChartCanvas.style.display = 'none';
            if (sessionChartEmpty) sessionChartEmpty.style.display = 'block';
            return;
        }

        sessionChartCanvas.style.display = 'block';
        if (sessionChartEmpty) sessionChartEmpty.style.display = 'none';

        // Nhóm sessions theo trạng thái
        const grouped = groupSessionsByStatus(rows);
        const labels = Object.keys(grouped).map(status => getStatusName(status));
        const data = Object.values(grouped);
        const backgroundColors = Object.keys(grouped).map(status => getStatusColor(status));
        const borderColors = backgroundColors.map(color => color.replace('1)', '1)'));

        // Destroy chart cũ nếu có
        if (state.sessionsChart) {
            state.sessionsChart.destroy();
        }

        // Tạo biểu đồ cột mới
        state.sessionsChart = new Chart(sessionChartCanvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số phiên sạc',
                    data: data,
                    backgroundColor: backgroundColors.map(color => color.replace('1)', '0.7)')),
                    borderColor: borderColors,
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.dataset.label || '';
                                const value = context.parsed.y || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return `${label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            precision: 0
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    };

    const groupReservationsByDate = (reservations, dateRange) => {
        const { startDate, endDate } = getDateRange(dateRange);
        const grouped = {};
        
        // Tạo mảng tất cả các ngày trong khoảng thời gian
        const dates = [];
        const currentDate = new Date(startDate);
        while (currentDate <= endDate) {
            const dateKey = currentDate.toISOString().split('T')[0];
            dates.push(dateKey);
            grouped[dateKey] = 0;
            currentDate.setDate(currentDate.getDate() + 1);
        }

        // Filter và đếm reservations theo ngày trong khoảng thời gian
        reservations.forEach(reservation => {
            const reservationDate = new Date(reservation.scheduledStartTime);
            // Chỉ đếm reservations trong khoảng thời gian được chọn
            if (reservationDate >= startDate && reservationDate <= endDate) {
                const dateKey = reservationDate.toISOString().split('T')[0];
                if (grouped.hasOwnProperty(dateKey)) {
                    grouped[dateKey]++;
                }
            }
        });

        return { dates, counts: dates.map(date => grouped[date] || 0) };
    };

    const formatDateLabel = (dateString) => {
        const date = new Date(dateString);
        const today = new Date();
        const yesterday = new Date(today);
        yesterday.setDate(yesterday.getDate() - 1);

        if (date.toDateString() === today.toDateString()) {
            return 'Hôm nay';
        } else if (date.toDateString() === yesterday.toDateString()) {
            return 'Hôm qua';
        } else {
            return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
        }
    };

    const updateReservations = async () => {
        if (!reservationChartCanvas) return;

        if (!state.stationId) {
            if (state.reservationsChart) {
                state.reservationsChart.destroy();
                state.reservationsChart = null;
            }
            reservationChartCanvas.style.display = 'none';
            if (reservationChartEmpty) {
                reservationChartEmpty.textContent = 'Hãy chọn trạm để xem lịch đặt chỗ.';
                reservationChartEmpty.style.display = 'block';
            }
            return;
        }

        try {
            const data = await utils.fetchJson(`/api/reservation/station/${state.stationId}`);
            
            // Hiển thị message nếu không có dữ liệu
            if (!data || data.length === 0) {
                if (state.reservationsChart) {
                    state.reservationsChart.destroy();
                    state.reservationsChart = null;
                }
                reservationChartCanvas.style.display = 'none';
                if (reservationChartEmpty) {
                    reservationChartEmpty.textContent = 'Chưa có đặt chỗ.';
                    reservationChartEmpty.style.display = 'block';
                }
                return;
            }

            reservationChartCanvas.style.display = 'block';
            if (reservationChartEmpty) reservationChartEmpty.style.display = 'none';

            // Nhóm reservations theo ngày
            const { dates, counts } = groupReservationsByDate(data, state.dateRange);
            const labels = dates.map(date => formatDateLabel(date));

            // Destroy chart cũ nếu có
            if (state.reservationsChart) {
                state.reservationsChart.destroy();
            }

            // Tạo biểu đồ đường mới
            state.reservationsChart = new Chart(reservationChartCanvas, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: 'Số đặt chỗ',
                        data: counts,
                        borderColor: 'rgba(75, 192, 192, 1)',
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        tension: 0.4,
                        fill: true,
                        pointRadius: 5,
                        pointHoverRadius: 7,
                        pointBackgroundColor: 'rgba(75, 192, 192, 1)',
                        pointBorderColor: '#fff',
                        pointBorderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: true,
                            position: 'top'
                        },
                        tooltip: {
                            mode: 'index',
                            intersect: false,
                            callbacks: {
                                label: function(context) {
                                    return `Số đặt chỗ: ${context.parsed.y}`;
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                stepSize: 1,
                                precision: 0
                            }
                        },
                        x: {
                            grid: {
                                display: false
                            }
                        }
                    }
                }
            });
        } catch (err) {
            console.error(err);
            if (state.reservationsChart) {
                state.reservationsChart.destroy();
                state.reservationsChart = null;
            }
            reservationChartCanvas.style.display = 'none';
            if (reservationChartEmpty) {
                reservationChartEmpty.textContent = 'Không thể tải dữ liệu đặt chỗ.';
                reservationChartEmpty.style.display = 'block';
            }
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
                pageSize: 1000 // Tăng pageSize để lấy đủ dữ liệu cho biểu đồ
            };
            
            const response = await utils.fetchJson(`/api/dashboard/sessions/all${buildQueryString(params)}`);
            const sessions = response?.data || response || [];
            updateSessions(sessions);
        } catch (err) {
            console.error('sessions', err);
            if (sessionChartCanvas) {
                sessionChartCanvas.style.display = 'none';
                if (sessionChartEmpty) {
                    sessionChartEmpty.textContent = 'Không thể tải dữ liệu phiên sạc.';
                    sessionChartEmpty.style.display = 'block';
                }
            }
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
                await Promise.all([
                    loadSessions(),
                    updateReservations()
                ]);
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

