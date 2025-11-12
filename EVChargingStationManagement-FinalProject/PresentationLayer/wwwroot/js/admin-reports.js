(function () {
    const utils = window.dashboardUtils;
    if (!utils) {
        console.error('dashboardUtils not found');
        return;
    }

    const state = {
        stations: [],
        stationId: null,
        dateFrom: null,
        dateTo: null,
        selectedStationId: null,
        revenueChart: null,
        usageByHourChart: null,
        stationRevenueChart: null,
        stationUsageByHourChart: null
    };

    // DOM Elements
    const dateFromInput = document.getElementById('ReportDateFrom');
    const dateToInput = document.getElementById('ReportDateTo');
    const stationFilter = document.getElementById('ReportStationFilter');
    const stationSelect = document.getElementById('StationReportSelect');
    const overviewTab = document.getElementById('overview-tab');
    const stationTab = document.getElementById('station-tab');
    const stationReportContent = document.getElementById('stationReportContent');
    const stationReportPlaceholder = document.getElementById('stationReportPlaceholder');

    // Set default date range (last 30 days)
    const setDefaultDateRange = () => {
        const today = new Date();
        const thirtyDaysAgo = new Date();
        thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

        dateToInput.value = today.toISOString().split('T')[0];
        dateFromInput.value = thirtyDaysAgo.toISOString().split('T')[0];
        state.dateFrom = dateFromInput.value;
        state.dateTo = dateToInput.value;
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
        }
    };

    const renderStations = () => {
        if (stationFilter) {
            stationFilter.innerHTML = '<option value="">Tất cả trạm</option>';
            state.stations.forEach(station => {
                const opt = document.createElement('option');
                opt.value = station.id;
                opt.textContent = station.name;
                stationFilter.appendChild(opt);
            });
        }
        
        if (stationSelect) {
            stationSelect.innerHTML = '<option value="">-- Chọn trạm --</option>';
            state.stations.forEach(station => {
                const opt = document.createElement('option');
                opt.value = station.id;
                opt.textContent = station.name;
                stationSelect.appendChild(opt);
            });
        }
    };

    const loadOverviewReport = async () => {
        if (!state.dateFrom || !state.dateTo) {
            setDefaultDateRange();
        }

        try {
            const params = {
                startDate: state.dateFrom,
                endDate: state.dateTo
            };
            if (state.stationId) params.stationId = state.stationId;

            const revenueUrl = `/api/Reporting/revenue${buildQueryString(params)}`;
            const usageUrl = `/api/Reporting/usage-statistics${buildQueryString(params)}`;
            console.log('Loading revenue from:', revenueUrl);
            console.log('Loading usage from:', usageUrl);

            const [revenueData, usageData] = await Promise.all([
                utils.fetchJson(revenueUrl),
                utils.fetchJson(usageUrl)
            ]);

            console.log('Revenue data:', revenueData);
            console.log('Usage data:', usageData);

            renderOverviewKPIs(revenueData, usageData);
            renderRevenueChart(revenueData);
            renderUsageByHourChart(usageData);
            renderRevenueTable(revenueData);
        } catch (err) {
            console.error('Error loading overview report:', err);
            // Show error message
            const kpiRoot = document.querySelector('#overview-pane .dashboard-kpis');
            if (kpiRoot) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'alert alert-danger';
                errorDiv.textContent = 'Không thể tải dữ liệu báo cáo: ' + (err.message || 'Lỗi không xác định');
                kpiRoot.appendChild(errorDiv);
            }
        }
    };

    const renderOverviewKPIs = (revenueData, usageData) => {
        const kpiRoot = document.querySelector('#overview-pane .dashboard-kpis');
        if (!kpiRoot) {
            console.error('KPI root element not found');
            return;
        }

        console.log('Rendering KPIs with data:', { revenueData, usageData });
        utils.setKpiValue(kpiRoot, 'total-revenue', revenueData?.totalRevenue || 0, utils.formatCurrency);
        utils.setKpiValue(kpiRoot, 'total-sessions', revenueData?.totalSessions || 0);
        utils.setKpiValue(kpiRoot, 'total-energy', revenueData?.totalEnergyDeliveredKwh || 0, v => utils.formatNumber(v, 1));
        utils.setKpiValue(kpiRoot, 'peak-hour', usageData?.peakHour !== null && usageData?.peakHour !== undefined 
            ? `${usageData.peakHour}:00` 
            : '--');
        utils.setKpiValue(kpiRoot, 'avg-duration', usageData?.averageSessionDurationMinutes || 0, v => utils.formatNumber(v, 1));
    };

    const renderRevenueChart = (data) => {
        const ctx = document.getElementById('revenueChart');
        if (!ctx || !data) return;

        if (state.revenueChart) {
            state.revenueChart.destroy();
        }

        const labels = data.dailyRevenues?.map(d => {
            const date = new Date(d.date);
            return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
        }) || [];
        const revenues = data.dailyRevenues?.map(d => d.revenue) || [];

        state.revenueChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Doanh thu (VND)',
                    data: revenues,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1,
                    fill: true
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
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + utils.formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return (value / 1000000).toFixed(1) + 'M';
                            }
                        }
                    }
                }
            }
        });
    };

    const renderUsageByHourChart = (data) => {
        const ctx = document.getElementById('usageByHourChart');
        if (!ctx || !data) return;

        if (state.usageByHourChart) {
            state.usageByHourChart.destroy();
        }

        const sessionsByHour = data.sessionsByHour || {};
        const labels = Array.from({ length: 24 }, (_, i) => `${i}:00`);
        const values = labels.map((_, hour) => sessionsByHour[hour] || 0);

        state.usageByHourChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số phiên sạc',
                    data: values,
                    backgroundColor: 'rgba(54, 162, 235, 0.5)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    };

    const renderRevenueTable = (data) => {
        const tbody = document.getElementById('revenueTableBody');
        if (!tbody) return;

        if (!data || !data.dailyRevenues || data.dailyRevenues.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Không có dữ liệu</td></tr>';
            return;
        }

        tbody.innerHTML = data.dailyRevenues.map(daily => {
            const date = new Date(daily.date);
            return `
                <tr>
                    <td>${date.toLocaleDateString('vi-VN')}</td>
                    <td>${utils.formatCurrency(daily.revenue)}</td>
                    <td>${daily.sessions}</td>
                    <td>${utils.formatNumber(daily.energyDeliveredKwh, 2)}</td>
                </tr>
            `;
        }).join('');
    };

    const loadStationReport = async () => {
        if (!state.selectedStationId || !state.dateFrom || !state.dateTo) {
            return;
        }

        try {
            const params = {
                startDate: state.dateFrom,
                endDate: state.dateTo,
                stationId: state.selectedStationId
            };

            const [revenueData, usageData, stationReports] = await Promise.all([
                utils.fetchJson(`/api/Reporting/revenue${buildQueryString(params)}`),
                utils.fetchJson(`/api/Reporting/usage-statistics${buildQueryString(params)}`),
                utils.fetchJson(`/api/Reporting/stations/${state.selectedStationId}${buildQueryString({ startDate: state.dateFrom, endDate: state.dateTo })}`)
            ]);

            renderStationKPIs(revenueData, usageData);
            renderStationRevenueChart(revenueData);
            renderStationUsageByHourChart(usageData);
            renderStationReportTable(stationReports);
        } catch (err) {
            console.error('Error loading station report:', err);
        }
    };

    const renderStationKPIs = (revenueData, usageData) => {
        const kpiRoot = document.querySelector('#station-pane .dashboard-kpis');
        if (!kpiRoot) return;

        utils.setKpiValue(kpiRoot, 'station-revenue', revenueData?.totalRevenue || 0, utils.formatCurrency);
        utils.setKpiValue(kpiRoot, 'station-sessions', revenueData?.totalSessions || 0);
        utils.setKpiValue(kpiRoot, 'station-energy', revenueData?.totalEnergyDeliveredKwh || 0, v => utils.formatNumber(v, 1));
        utils.setKpiValue(kpiRoot, 'station-peak-hour', usageData?.peakHour !== null && usageData?.peakHour !== undefined 
            ? `${usageData.peakHour}:00` 
            : '--');
        utils.setKpiValue(kpiRoot, 'station-avg-duration', usageData?.averageSessionDurationMinutes || 0, v => utils.formatNumber(v, 1));
    };

    const renderStationRevenueChart = (data) => {
        const ctx = document.getElementById('stationRevenueChart');
        if (!ctx || !data) return;

        if (state.stationRevenueChart) {
            state.stationRevenueChart.destroy();
        }

        const labels = data.dailyRevenues?.map(d => {
            const date = new Date(d.date);
            return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
        }) || [];
        const revenues = data.dailyRevenues?.map(d => d.revenue) || [];

        state.stationRevenueChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Doanh thu (VND)',
                    data: revenues,
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.2)',
                    tension: 0.1,
                    fill: true
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
                        callbacks: {
                            label: function(context) {
                                return 'Doanh thu: ' + utils.formatCurrency(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return (value / 1000000).toFixed(1) + 'M';
                            }
                        }
                    }
                }
            }
        });
    };

    const renderStationUsageByHourChart = (data) => {
        const ctx = document.getElementById('stationUsageByHourChart');
        if (!ctx || !data) return;

        if (state.stationUsageByHourChart) {
            state.stationUsageByHourChart.destroy();
        }

        const sessionsByHour = data.sessionsByHour || {};
        const labels = Array.from({ length: 24 }, (_, i) => `${i}:00`);
        const values = labels.map((_, hour) => sessionsByHour[hour] || 0);

        state.stationUsageByHourChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số phiên sạc',
                    data: values,
                    backgroundColor: 'rgba(255, 159, 64, 0.5)',
                    borderColor: 'rgba(255, 159, 64, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    };

    const renderStationReportTable = (reports) => {
        const tbody = document.getElementById('stationReportTableBody');
        if (!tbody) return;

        if (!reports || reports.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Không có dữ liệu</td></tr>';
            return;
        }

        tbody.innerHTML = reports.map(report => {
            const date = new Date(report.reportDate);
            return `
                <tr>
                    <td>${date.toLocaleDateString('vi-VN')}</td>
                    <td>${report.totalSessions}</td>
                    <td>${utils.formatNumber(report.totalEnergyDeliveredKwh, 2)}</td>
                    <td>${utils.formatCurrency(report.totalRevenue)}</td>
                    <td>${report.peakHour !== null && report.peakHour !== undefined ? `${report.peakHour}:00` : '--'}</td>
                    <td>${utils.formatNumber(report.averageSessionDurationMinutes || 0, 1)}</td>
                </tr>
            `;
        }).join('');
    };

    const bindEvents = () => {
        document.querySelector('[data-action="apply-filters"]')?.addEventListener('click', () => {
            state.dateFrom = dateFromInput.value;
            state.dateTo = dateToInput.value;
            state.stationId = stationFilter.value || null;
            
            if (overviewTab.classList.contains('active')) {
                loadOverviewReport();
            } else if (state.selectedStationId) {
                loadStationReport();
            }
        });

        document.querySelector('[data-action="reset-filters"]')?.addEventListener('click', () => {
            setDefaultDateRange();
            stationFilter.value = '';
            state.stationId = null;
            loadOverviewReport();
        });

        stationSelect.addEventListener('change', (e) => {
            state.selectedStationId = e.target.value || null;
            if (state.selectedStationId) {
                stationReportContent.style.display = 'block';
                stationReportPlaceholder.style.display = 'none';
                loadStationReport();
            } else {
                stationReportContent.style.display = 'none';
                stationReportPlaceholder.style.display = 'block';
            }
        });

        overviewTab.addEventListener('shown.bs.tab', () => {
            loadOverviewReport();
        });

        stationTab.addEventListener('shown.bs.tab', () => {
            if (state.selectedStationId) {
                loadStationReport();
            }
        });
    };

    const init = async () => {
        try {
            // Check if required DOM elements exist
            if (!dateFromInput || !dateToInput) {
                console.error('Required DOM elements not found');
                return;
            }

            console.log('Initializing admin-reports...');
            setDefaultDateRange();
            await loadStations();
            bindEvents();
            loadOverviewReport();
            console.log('Admin-reports initialized successfully');
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

