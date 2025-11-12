(function () {
    'use strict';

    let stationsTableBody;
    let stationModal;
    let stationForm;
    let saveStationBtn;
    let stationModalTitle;
    let statusFilter;
    let searchInput;

    let stations = [];
    let editingStationId = null;

    // Initialize when DOM is ready
    function init() {
        stationsTableBody = document.getElementById('stationsTableBody');
        const modalElement = document.getElementById('stationModal');
        if (!modalElement) {
            console.error('Modal element not found');
            return;
        }
        stationModal = new bootstrap.Modal(modalElement);
        stationForm = document.getElementById('stationForm');
        saveStationBtn = document.getElementById('saveStationBtn');
        stationModalTitle = document.getElementById('stationModalTitle');
        statusFilter = document.getElementById('StationStatusFilter');
        searchInput = document.getElementById('StationSearch');

        // Event listeners
        const createBtn = document.querySelector('[data-action="create-station"]');
        if (createBtn) {
            createBtn.addEventListener('click', (e) => {
                e.preventDefault();
                openCreateModal();
            });
        } else {
            console.error('Create button not found');
        }

        const refreshBtn = document.querySelector('[data-action="refresh-stations"]');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', (e) => {
                e.preventDefault();
                loadStations();
            });
        }

        if (saveStationBtn) {
            saveStationBtn.addEventListener('click', saveStation);
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', filterStations);
        }

        if (searchInput) {
            searchInput.addEventListener('input', debounce(filterStations, 300));
        }

        // Load stations on page load
        loadStations();
    }

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
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

        stationsTableBody.innerHTML = filteredStations.map(station => `
            <tr>
                <td>${escapeHtml(station.name)}</td>
                <td>${escapeHtml(station.address || '')}</td>
                <td>${station.totalSpots || 0}</td>
                <td>${station.availableSpots || 0}</td>
                <td><span class="badge ${getStatusBadgeClass(station.status)}">${getStatusText(station.status)}</span></td>
                <td>${station.isFromSerpApi ? '<span class="badge bg-info">Có</span>' : '<span class="badge bg-secondary">Không</span>'}</td>
                <td>
                    <div style="display: flex; gap: 8px; align-items: center; justify-content: center;">
                        <button class="btn btn-outline-success btn-toggle-status" data-station-id="${station.id}" title="${isActiveStatus(station.status) ? 'Tắt trạm' : 'Bật trạm'}" style="min-width: 42px; height: 38px; padding: 8px 12px; display: inline-flex; align-items: center; justify-content: center; border-radius: 6px; transition: all 0.2s; font-size: 16px;">
                            <i class="bi ${isActiveStatus(station.status) ? 'bi-toggle-on' : 'bi-toggle-off'}"></i>
                        </button>
                        <button class="btn btn-outline-primary btn-edit-station" data-station-id="${station.id}" title="Sửa" style="min-width: 42px; height: 38px; padding: 8px 12px; display: inline-flex; align-items: center; justify-content: center; border-radius: 6px; transition: all 0.2s; font-size: 16px;">
                            <i class="bi bi-pencil-fill"></i>
                        </button>
                        <button class="btn btn-outline-danger btn-delete-station" data-station-id="${station.id}" title="Xóa" style="min-width: 42px; height: 38px; padding: 8px 12px; display: inline-flex; align-items: center; justify-content: center; border-radius: 6px; transition: all 0.2s; font-size: 16px;">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        // Attach event listeners to buttons
        stationsTableBody.querySelectorAll('.btn-toggle-status').forEach(btn => {
            btn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const stationId = this.getAttribute('data-station-id');
                if (stationId) {
                    toggleStationStatus(stationId);
                } else {
                    console.error('Station ID not found');
                }
            });
        });

        stationsTableBody.querySelectorAll('.btn-edit-station').forEach(btn => {
            btn.addEventListener('click', function() {
                const stationId = this.getAttribute('data-station-id');
                window.location.href = '/Admin/Stations/EditStation?id=' + stationId;
            });
        });

        stationsTableBody.querySelectorAll('.btn-delete-station').forEach(btn => {
            btn.addEventListener('click', function() {
                const stationId = this.getAttribute('data-station-id');
                deleteStation(stationId);
            });
        });
    }

    // Helper function to normalize status to number
    function normalizeStatus(status) {
        if (status === null || status === undefined) return -1;
        if (typeof status === 'number') return status;
        if (typeof status === 'string') {
            if (status === 'Active') return 0;
            if (status === 'Inactive') return 1;
            if (status === 'Maintenance') return 2;
            if (status === 'Closed') return 3;
            const num = parseInt(status);
            if (!isNaN(num)) return num;
        }
        return -1;
    }

    // Helper function to check if station is active
    function isActiveStatus(status) {
        return normalizeStatus(status) === 0;
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

    function openCreateModal() {
        if (!stationModal || !stationForm || !stationModalTitle) {
            console.error('Modal elements not initialized');
            return;
        }
        editingStationId = null;
        stationModalTitle.textContent = 'Thêm trạm sạc';
        stationForm.reset();
        const stationIdInput = document.getElementById('stationId');
        if (stationIdInput) stationIdInput.value = '';
        const statusSelect = document.getElementById('stationStatus');
        if (statusSelect) statusSelect.value = '0';
        stationModal.show();
    }

    function editStation(id) {
        if (!stationModal || !stationModalTitle) {
            console.error('Modal not initialized');
            return;
        }
        
        const station = stations.find(s => s.id === id);
        if (!station) return;

        editingStationId = id;
        stationModalTitle.textContent = 'Sửa trạm sạc';
        
        const setValue = (id, value) => {
            const el = document.getElementById(id);
            if (el) el.value = value || '';
        };
        
        const setChecked = (id, checked) => {
            const el = document.getElementById(id);
            if (el) el.checked = checked || false;
        };
        
        setValue('stationId', station.id);
        setValue('stationName', station.name);
        setValue('stationAddress', station.address);
        setValue('stationCity', station.city);
        setValue('stationProvince', station.province);
        setValue('stationPostalCode', station.postalCode);
        setValue('stationLatitude', station.latitude);
        setValue('stationLongitude', station.longitude);
        setValue('stationPhone', station.phone);
        setValue('stationEmail', station.email);
        setValue('stationStatus', station.status || '0');
        setValue('stationDescription', station.description);
        setChecked('stationIs24Hours', station.is24Hours);
        setValue('stationSerpApiPlaceId', station.serpApiPlaceId);
        setValue('stationExternalRating', station.externalRating);
        setValue('stationExternalReviewCount', station.externalReviewCount);

        if (station.openingTime) {
            const openingTime = station.openingTime.split(':').slice(0, 2).join(':');
            setValue('stationOpeningTime', openingTime);
        }
        if (station.closingTime) {
            const closingTime = station.closingTime.split(':').slice(0, 2).join(':');
            setValue('stationClosingTime', closingTime);
        }

        stationModal.show();
    }

    async function toggleStationStatus(id) {
        try {
            const response = await fetch(`/api/ChargingStation/${id}/toggle-status`, {
                method: 'PATCH',
                credentials: 'include'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to toggle station status');
            }

            const updatedStation = await response.json();
            const isActive = isActiveStatus(updatedStation.status);
            alert(`Đã ${isActive ? 'bật' : 'tắt'} trạm sạc thành công!`);
            loadStations();
        } catch (error) {
            console.error('Error toggling station status:', error);
            alert('Lỗi khi chuyển trạng thái trạm sạc: ' + error.message);
        }
    }

    async function deleteStation(id) {
        if (!confirm('Bạn có chắc chắn muốn xóa trạm sạc này?')) {
            return;
        }

        try {
            const response = await fetch(`/api/ChargingStation/${id}`, {
                method: 'DELETE',
                credentials: 'include'
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to delete station');
            }

            alert('Xóa trạm sạc thành công!');
            loadStations();
        } catch (error) {
            console.error('Error deleting station:', error);
            alert('Lỗi khi xóa trạm sạc: ' + error.message);
        }
    };

    async function saveStation() {
        if (!stationForm) {
            console.error('Form not initialized');
            return;
        }
        
        if (!stationForm.checkValidity()) {
            stationForm.reportValidity();
            return;
        }

        const formData = new FormData(stationForm);
        const data = {
            name: formData.get('name'),
            address: formData.get('address'),
            city: formData.get('city') || null,
            province: formData.get('province') || null,
            postalCode: formData.get('postalCode') || null,
            latitude: formData.get('latitude') ? parseFloat(formData.get('latitude')) : null,
            longitude: formData.get('longitude') ? parseFloat(formData.get('longitude')) : null,
            phone: formData.get('phone') || null,
            email: formData.get('email') || null,
            status: parseInt(formData.get('status')),
            description: formData.get('description') || null,
            is24Hours: formData.get('is24Hours') === 'on',
            openingTime: formData.get('openingTime') || null,
            closingTime: formData.get('closingTime') || null
        };

        try {
            const url = editingStationId 
                ? `/api/ChargingStation/${editingStationId}`
                : '/api/ChargingStation';
            
            const method = editingStationId ? 'PUT' : 'POST';

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to save station');
            }

            alert(editingStationId ? 'Cập nhật trạm sạc thành công!' : 'Thêm trạm sạc thành công!');
            if (stationModal) {
                stationModal.hide();
            }
            loadStations();
        } catch (error) {
            console.error('Error saving station:', error);
            alert('Lỗi khi lưu trạm sạc: ' + error.message);
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
})();

