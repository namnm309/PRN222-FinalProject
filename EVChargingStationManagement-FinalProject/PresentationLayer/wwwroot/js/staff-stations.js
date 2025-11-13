(function () {
    'use strict';

    const utils = window.dashboardUtils;
    if (!utils) {
        console.error('dashboardUtils not found');
        return;
    }

    let stationsTableBody;
    let statusFilter;
    let searchInput;
    let spotsDetailModal;
    let spotsDetailTableBody;
    let spotsDetailModalTitle;

    let stations = [];
    let connection = null;

    // Initialize when DOM is ready
    function init() {
        stationsTableBody = document.getElementById('stationsTableBody');
        statusFilter = document.getElementById('StationStatusFilter');
        searchInput = document.getElementById('StationSearch');
        
        // Initialize modal elements
        const modalElement = document.getElementById('spotsDetailModal');
        if (!modalElement) {
            console.error('Modal element spotsDetailModal not found');
        } else {
            try {
                spotsDetailModal = new bootstrap.Modal(modalElement);
                spotsDetailModalTitle = document.getElementById('spotsDetailModalTitle');
                spotsDetailTableBody = document.getElementById('spotsDetailTableBody');
                
                if (!spotsDetailModalTitle) {
                    console.error('Modal title element not found');
                }
                if (!spotsDetailTableBody) {
                    console.error('Modal table body element not found');
                }
            } catch (error) {
                console.error('Error initializing modal:', error);
            }
        }

        // Event listeners
        const refreshBtn = document.querySelector('[data-action="refresh-stations"]');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', (e) => {
                e.preventDefault();
                loadStations();
            });
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', filterStations);
        }

        if (searchInput) {
            searchInput.addEventListener('input', debounce(filterStations, 300));
        }

        // Load stations on page load
        loadStations();
        initSignalR();
    }

    // Wait for DOM to be ready and Bootstrap to be loaded
    function waitForBootstrap() {
        if (typeof bootstrap === 'undefined') {
            console.warn('Bootstrap not loaded yet, retrying...');
            setTimeout(waitForBootstrap, 100);
            return;
        }
        init();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', waitForBootstrap);
    } else {
        waitForBootstrap();
    }

    async function loadStations() {
        if (!stationsTableBody) return;
        
        try {
            const response = await fetch('/api/ChargingStation', {
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error('Failed to load stations');
            }

            stations = await response.json();
            renderStations();
        } catch (error) {
            console.error('Error loading stations:', error);
            stationsTableBody.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
        }
    }

    function renderStations() {
        if (!stationsTableBody) return;
        let filteredStations = stations;

        // Filter by status
        const statusValue = statusFilter?.value;
        if (statusValue !== undefined && statusValue !== '') {
            const statusNum = parseInt(statusValue);
            filteredStations = filteredStations.filter(s => {
                return normalizeStatus(s.status) === statusNum;
            });
        }

        // Filter by search
        const searchValue = searchInput?.value.toLowerCase();
        if (searchValue) {
            filteredStations = filteredStations.filter(s =>
                s.name.toLowerCase().includes(searchValue) ||
                (s.address && s.address.toLowerCase().includes(searchValue))
            );
        }

        if (filteredStations.length === 0) {
            stationsTableBody.innerHTML = '<tr><td colspan="7" class="text-center">Không có dữ liệu</td></tr>';
            return;
        }

        stationsTableBody.innerHTML = filteredStations.map(station => {
            const totalSpots = station.totalSpots || station.chargingSpots?.length || 0;
            const availableSpots = station.availableSpots || (station.chargingSpots?.filter(s => s.status === 0).length || 0);
            const inUseSpots = totalSpots - availableSpots - (station.chargingSpots?.filter(s => s.status === 2).length || 0);

            return `
                <tr>
                    <td><strong>${escapeHtml(station.name)}</strong></td>
                    <td>${escapeHtml(station.address || '')}</td>
                    <td>${totalSpots}</td>
                    <td><span class="badge bg-success">${availableSpots}</span></td>
                    <td><span class="badge bg-primary">${inUseSpots}</span></td>
                    <td><span class="badge ${getStatusBadgeClass(station.status)}">${getStatusText(station.status)}</span></td>
                    <td>
                        <button class="btn btn-sm btn-outline-info" data-station-id="${station.id}" data-action="view-spots" title="Xem chi tiết điểm sạc">
                            <i class="bi bi-eye"></i> Xem chi tiết
                        </button>
                    </td>
                </tr>
            `;
        }).join('');

        // Attach event listeners to view spots buttons
        stationsTableBody.querySelectorAll('[data-action="view-spots"]').forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const stationId = this.getAttribute('data-station-id');
                console.log('View spots button clicked, stationId:', stationId);
                if (stationId) {
                    viewStationSpots(stationId);
                } else {
                    console.error('Station ID not found on button');
                    alert('Không tìm thấy ID trạm sạc');
                }
            });
        });
    }

    async function viewStationSpots(stationId) {
        console.log('viewStationSpots called with stationId:', stationId);
        console.log('Available stations:', stations.map(s => ({ id: s.id, name: s.name })));
        
        // Try to find station by string or GUID comparison
        const station = stations.find(s => {
            const sId = String(s.id || '');
            const searchId = String(stationId || '');
            return sId === searchId || sId.toLowerCase() === searchId.toLowerCase();
        });
        
        if (!station) {
            console.error('Station not found with ID:', stationId);
            alert('Không tìm thấy trạm sạc với ID: ' + stationId);
            return;
        }
        
        console.log('Found station:', station);

        // Re-initialize modal if not already initialized
        if (!spotsDetailModal) {
            const modalElement = document.getElementById('spotsDetailModal');
            if (modalElement && typeof bootstrap !== 'undefined') {
                try {
                    spotsDetailModal = new bootstrap.Modal(modalElement);
                } catch (error) {
                    console.error('Error creating modal instance:', error);
                    alert('Không thể mở modal. Vui lòng thử lại.');
                    return;
                }
            } else {
                console.error('Modal element or Bootstrap not available');
                alert('Không thể mở modal. Vui lòng tải lại trang.');
                return;
            }
        }

        // Re-initialize modal elements if needed
        if (!spotsDetailModalTitle) {
            spotsDetailModalTitle = document.getElementById('spotsDetailModalTitle');
        }
        if (!spotsDetailTableBody) {
            spotsDetailTableBody = document.getElementById('spotsDetailTableBody');
        }

        if (!spotsDetailModal || !spotsDetailTableBody || !spotsDetailModalTitle) {
            console.error('Modal elements not initialized', {
                modal: !!spotsDetailModal,
                title: !!spotsDetailModalTitle,
                body: !!spotsDetailTableBody
            });
            alert('Không thể mở modal. Vui lòng tải lại trang.');
            return;
        }

        spotsDetailModalTitle.textContent = `Chi tiết điểm sạc - ${station.name}`;

        // Show loading state
        spotsDetailTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Đang tải...</td></tr>';

        // Load full station details with spots if not already loaded
        let spots = station.chargingSpots || [];
        if (!spots || spots.length === 0) {
            try {
                const response = await fetch(`/api/ChargingStation/${stationId}`, {
                    credentials: 'include'
                });
                if (response.ok) {
                    const fullStation = await response.json();
                    spots = fullStation.chargingSpots || [];
                } else {
                    console.error('Failed to load station details:', response.status, response.statusText);
                }
            } catch (error) {
                console.error('Error loading station details:', error);
            }
        }

        if (!spots || spots.length === 0) {
            spotsDetailTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Không có điểm sạc</td></tr>';
        } else {
            spotsDetailTableBody.innerHTML = spots.map(spot => {
                const spotStatus = getSpotStatusText(spot.status);
                const spotStatusClass = getSpotStatusBadgeClass(spot.status);
                const isOnline = spot.isOnline !== false; // Default to online if not specified
                
                return `
                    <tr>
                        <td><strong>${escapeHtml(String(spot.spotNumber || spot.number || '--'))}</strong></td>
                        <td><span class="badge ${spotStatusClass}">${escapeHtml(spotStatus)}</span></td>
                        <td>${escapeHtml(String(spot.powerKw || spot.power || '--'))} kW</td>
                        <td>${escapeHtml(String(spot.connectorType || '--'))}</td>
                        <td><span class="badge ${isOnline ? 'bg-success' : 'bg-danger'}">${isOnline ? 'Online' : 'Offline'}</span></td>
                    </tr>
                `;
            }).join('');
        }

        try {
            spotsDetailModal.show();
        } catch (error) {
            console.error('Error showing modal:', error);
            alert('Không thể hiển thị modal. Vui lòng thử lại.');
        }
    }

    function getSpotStatusText(status) {
        const normalized = normalizeStatus(status);
        switch (normalized) {
            case 0: return 'Available';
            case 1: return 'InUse';
            case 2: return 'Maintenance';
            case 3: return 'Faulty';
            default: return 'Unknown';
        }
    }

    function getSpotStatusBadgeClass(status) {
        const normalized = normalizeStatus(status);
        switch (normalized) {
            case 0: return 'bg-success';
            case 1: return 'bg-primary';
            case 2: return 'bg-warning';
            case 3: return 'bg-danger';
            default: return 'bg-secondary';
        }
    }

    // Helper function to normalize status to number
    function normalizeStatus(status) {
        if (status === null || status === undefined) return -1;
        if (typeof status === 'number') return status;
        if (typeof status === 'string') {
            if (status === 'Active' || status === 'Available') return 0;
            if (status === 'Inactive' || status === 'InUse') return 1;
            if (status === 'Maintenance') return 2;
            if (status === 'Closed' || status === 'Faulty') return 3;
            const num = parseInt(status);
            if (!isNaN(num)) return num;
        }
        return -1;
    }

    function getStatusBadgeClass(status) {
        const normalized = normalizeStatus(status);
        switch (normalized) {
            case 0: return 'bg-success';
            case 1: return 'bg-secondary';
            case 2: return 'bg-warning';
            case 3: return 'bg-danger';
            default: return 'bg-secondary';
        }
    }

    function getStatusText(status) {
        const normalized = normalizeStatus(status);
        switch (normalized) {
            case 0: return 'Active';
            case 1: return 'Inactive';
            case 2: return 'Maintenance';
            case 3: return 'Closed';
            default: return 'Unknown';
        }
    }

    function filterStations() {
        renderStations();
    }

    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    async function initSignalR() {
        if (!window.signalR) return;

        connection = new signalR.HubConnectionBuilder()
            .withAutomaticReconnect()
            .withUrl('/hubs/station')
            .build();

        connection.on('StationUpdated', (stationId) => {
            // Reload stations when station is updated
            loadStations();
        });

        connection.on('SpotUpdated', (spotId, stationId) => {
            // Reload stations when spot is updated
            loadStations();
        });

        try {
            await connection.start();
            console.log('SignalR connected for staff stations');
        } catch (err) {
            console.warn('Không thể kết nối SignalR', err);
        }
    }
})();

