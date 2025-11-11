(function () {
    const utils = window.dashboardUtils;
    if (!utils) {
        console.error('dashboardUtils not found');
        return;
    }

    const state = {
        stations: [],
        spots: {},
        stationId: null,
        status: null,
        reports: [],
        currentUserId: null
    };

    // DOM Elements
    const stationFilter = document.getElementById('ReportStationFilter');
    const statusFilter = document.getElementById('ReportStatusFilter');
    const reportsTableBody = document.getElementById('reportsTableBody');
    const createReportModal = document.getElementById('createReportModal') ? new bootstrap.Modal(document.getElementById('createReportModal')) : null;
    const reportForm = document.getElementById('reportForm');
    const reportStation = document.getElementById('reportStation');
    const reportSpot = document.getElementById('reportSpot');
    const saveReportBtn = document.getElementById('saveReportBtn');

    const loadStations = async () => {
        try {
            const data = await utils.fetchJson('/api/ChargingStation');
            state.stations = data || [];
            renderStationFilters();
        } catch (err) {
            console.error('Error loading stations:', err);
            if (stationFilter) {
                stationFilter.innerHTML = '<option value="">Lỗi tải trạm</option>';
            }
        }
    };

    const renderStationFilters = () => {
        // Render filter dropdown
        if (stationFilter) {
            stationFilter.innerHTML = '<option value="">Tất cả trạm</option>';
            state.stations.forEach(station => {
                const opt = document.createElement('option');
                opt.value = station.id;
                opt.textContent = station.name;
                stationFilter.appendChild(opt);
            });
        }

        // Render create report modal station dropdown
        if (reportStation) {
            reportStation.innerHTML = '<option value="">-- Chọn trạm sạc --</option>';
            state.stations.forEach(station => {
                const opt = document.createElement('option');
                opt.value = station.id;
                opt.textContent = station.name;
                reportStation.appendChild(opt);
            });
        }
    };

    const loadSpotsForStation = async (stationId) => {
        if (!stationId) {
            if (reportSpot) {
                reportSpot.innerHTML = '<option value="">-- Chọn cổng sạc --</option>';
            }
            return;
        }

        try {
            const station = state.stations.find(s => s.id === stationId);
            if (station && station.chargingSpots) {
                state.spots[stationId] = station.chargingSpots;
            } else {
                // Load full station details
                const response = await fetch(`/api/ChargingStation/${stationId}`, {
                    credentials: 'include'
                });
                if (response.ok) {
                    const fullStation = await response.json();
                    state.spots[stationId] = fullStation.chargingSpots || [];
                }
            }

            if (reportSpot) {
                reportSpot.innerHTML = '<option value="">-- Chọn cổng sạc --</option>';
                const spots = state.spots[stationId] || [];
                spots.forEach(spot => {
                    const opt = document.createElement('option');
                    const spotNumber = spot.spotNumber || spot.number || 'N/A';
                    const connectorType = spot.connectorType || '';
                    const power = spot.powerOutput || spot.power || spot.powerKw || '';
                    let displayText = `Cổng ${spotNumber}`;
                    if (connectorType) {
                        displayText += ` (${connectorType})`;
                    }
                    if (power) {
                        displayText += ` - ${power}kW`;
                    }
                    opt.value = spot.id;
                    opt.textContent = displayText;
                    reportSpot.appendChild(opt);
                });
            }
        } catch (error) {
            console.error('Error loading spots:', error);
        }
    };

    const loadReports = async () => {
        try {
            let url = '/api/StationError';
            const params = {};
            
            if (state.stationId) {
                url = `/api/StationError/station/${state.stationId}`;
            }
            
            if (state.status !== null && state.status !== '') {
                url = `/api/StationError/status/${state.status}`;
            }

            const data = await utils.fetchJson(url);
            state.reports = Array.isArray(data) ? data : [];
            
            // Apply filters
            let filteredReports = state.reports;
            if (state.stationId && state.status !== null && state.status !== '') {
                filteredReports = state.reports.filter(r => 
                    r.chargingStationId === state.stationId && 
                    r.status === parseInt(state.status)
                );
            } else if (state.stationId) {
                filteredReports = state.reports.filter(r => r.chargingStationId === state.stationId);
            } else if (state.status !== null && state.status !== '') {
                filteredReports = state.reports.filter(r => r.status === parseInt(state.status));
            }

            renderReports(filteredReports);
        } catch (err) {
            console.error('Error loading reports:', err);
            if (reportsTableBody) {
                reportsTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
            }
        }
    };

    const renderReports = (reports) => {
        if (!reportsTableBody) return;

        if (!reports || reports.length === 0) {
            reportsTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-muted">Không có báo cáo sự cố</td></tr>';
            return;
        }

        reportsTableBody.innerHTML = reports.map(report => {
            const reportedAt = report.reportedAt 
                ? new Date(report.reportedAt).toLocaleString('vi-VN')
                : new Date(report.createdAt).toLocaleString('vi-VN');
            
            const statusBadge = getStatusBadge(report.status);
            const severityBadge = getSeverityBadge(report.severity);

            return `
                <tr>
                    <td>${reportedAt}</td>
                    <td>${escapeHtml(report.chargingStationName || '--')}</td>
                    <td>${report.chargingSpotNumber ? `Cổng ${report.chargingSpotNumber}` : 'Toàn trạm'}</td>
                    <td><strong>${escapeHtml(report.title || '--')}</strong></td>
                    <td>${escapeHtml(report.description ? (report.description.length > 50 ? report.description.substring(0, 50) + '...' : report.description) : '--')}</td>
                    <td>${severityBadge}</td>
                    <td>${statusBadge}</td>
                    <td>${escapeHtml(report.reportedByUserName || '--')}</td>
                </tr>
            `;
        }).join('');
    };

    const getStatusBadge = (status) => {
        const statusMap = {
            0: '<span class="badge bg-warning">Mới báo cáo</span>',
            1: '<span class="badge bg-info">Đang xử lý</span>',
            2: '<span class="badge bg-success">Đã xử lý</span>',
            3: '<span class="badge bg-secondary">Đã đóng</span>'
        };
        return statusMap[status] || '<span class="badge bg-secondary">Unknown</span>';
    };

    const getSeverityBadge = (severity) => {
        if (!severity) return '<span class="badge bg-secondary">--</span>';
        const severityMap = {
            'Low': '<span class="badge bg-success">Thấp</span>',
            'Medium': '<span class="badge bg-warning">Trung bình</span>',
            'High': '<span class="badge bg-danger">Cao</span>',
            'Critical': '<span class="badge bg-dark">Nghiêm trọng</span>'
        };
        return severityMap[severity] || `<span class="badge bg-secondary">${severity}</span>`;
    };

    const getCurrentUserId = async () => {
        if (state.currentUserId) return state.currentUserId;

        try {
            // Try to get user info from a session or API endpoint
            // For now, we'll get it from the payment endpoint or create a helper
            // Since we need userId, we'll try to get it from the first available endpoint
            // Or we can make it optional and let backend handle it
            // For simplicity, we'll try to get from user profile or session
            const response = await fetch('/api/ChargingSession/me', {
                credentials: 'include'
            });
            
            if (response.ok) {
                const sessions = await response.json();
                if (sessions && sessions.length > 0 && sessions[0].userId) {
                    state.currentUserId = sessions[0].userId;
                    return state.currentUserId;
                }
            }
        } catch (error) {
            console.warn('Could not get current user ID, backend will handle it', error);
        }

        return null;
    };

    const createReport = async () => {
        if (!reportForm || !reportForm.checkValidity()) {
            reportForm.reportValidity();
            return;
        }

        const formData = new FormData(reportForm);
        const stationId = formData.get('stationId');
        const spotId = formData.get('spotId');
        const title = formData.get('title');
        const errorCode = formData.get('errorCode');
        const description = formData.get('description');
        const severity = formData.get('severity');

        if (!stationId) {
            alert('Vui lòng chọn trạm sạc');
            return;
        }

        if (saveReportBtn) {
            saveReportBtn.disabled = true;
            saveReportBtn.textContent = 'Đang tạo...';
        }

        try {
            // Get current user ID
            const userId = await getCurrentUserId();

            const reportData = {
                chargingStationId: stationId,
                chargingSpotId: spotId || null,
                reportedByUserId: userId || '00000000-0000-0000-0000-000000000000', // Will be set by backend from User context if empty
                status: 0, // Reported
                errorCode: errorCode || '',
                title: title,
                description: description,
                severity: severity || 'Medium'
            };

            const response = await fetch('/api/StationError', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify(reportData)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Không thể tạo báo cáo sự cố');
            }

            alert('Đã tạo báo cáo sự cố thành công!');
            if (createReportModal) {
                createReportModal.hide();
            }
            reportForm.reset();
            loadReports();
        } catch (error) {
            console.error('Error creating report:', error);
            alert('Lỗi khi tạo báo cáo sự cố: ' + error.message);
        } finally {
            if (saveReportBtn) {
                saveReportBtn.disabled = false;
                saveReportBtn.textContent = 'Tạo báo cáo';
            }
        }
    };

    const bindEvents = () => {
        document.querySelector('[data-action="create-report"]')?.addEventListener('click', () => {
            if (createReportModal) {
                createReportModal.show();
            }
        });

        document.querySelector('[data-action="apply-filters"]')?.addEventListener('click', () => {
            if (stationFilter) state.stationId = stationFilter.value || null;
            if (statusFilter) state.status = statusFilter.value || null;
            loadReports();
        });

        document.querySelector('[data-action="reset-filters"]')?.addEventListener('click', () => {
            if (stationFilter) stationFilter.value = '';
            if (statusFilter) statusFilter.value = '';
            state.stationId = null;
            state.status = null;
            loadReports();
        });

        document.querySelector('[data-action="refresh-reports"]')?.addEventListener('click', loadReports);

        if (reportStation) {
            reportStation.addEventListener('change', (e) => {
                loadSpotsForStation(e.target.value);
            });
        }

        if (saveReportBtn) {
            saveReportBtn.addEventListener('click', createReport);
        }
    };

    const escapeHtml = (text) => {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    };

    const init = async () => {
        await loadStations();
        bindEvents();
        await getCurrentUserId();
        loadReports();
    };

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

