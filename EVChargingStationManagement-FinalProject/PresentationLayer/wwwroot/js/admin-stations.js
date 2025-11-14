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

    // Variables for "Add My Station" modal
    let myStationMap = null;
    let myStationModal = null;
    let myStationForm = null;
    let myStationMarker = null;
    let mySpots = [];
    let mySpotCounter = 0;

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

        // Initialize "Add My Station" modal
        initMyStationModal();
    }

    function initMyStationModal() {
        const modalElement = document.getElementById('addMyStationModal');
        if (!modalElement) {
            console.warn('Add My Station modal not found');
            return;
        }

        myStationModal = new bootstrap.Modal(modalElement);
        myStationForm = document.getElementById('myStationForm');

        // Event listener for modal show
        modalElement.addEventListener('shown.bs.modal', function() {
            initMyStationMap();
        });

        // Event listener for modal hide
        modalElement.addEventListener('hidden.bs.modal', function() {
            if (myStationMap) {
                myStationMap.remove();
                myStationMap = null;
            }
            if (myStationMarker) {
                myStationMarker = null;
            }
            if (myStationForm) {
                myStationForm.reset();
            }
            mySpots = [];
            mySpotCounter = 0;
            renderMySpots();
        });

        // Add spot button
        const btnAddSpot = document.getElementById('btn-add-my-spot');
        if (btnAddSpot) {
            btnAddSpot.addEventListener('click', addMySpot);
        }

        // Save button
        const saveBtn = document.getElementById('saveMyStationBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', saveMyStation);
        }

        // Location button
        const btnLocation = document.getElementById('btn-my-location');
        if (btnLocation) {
            btnLocation.addEventListener('click', requestMyLocation);
        }
    }

    function initMyStationMap() {
        const mapEl = document.getElementById('myStationMap');
        if (!mapEl || typeof L === 'undefined') {
            console.error('Map element or Leaflet not found');
            return;
        }

        const DEFAULT_COORDS = { lat: 21.0285, lng: 105.8542 };

        myStationMap = L.map('myStationMap').setView([DEFAULT_COORDS.lat, DEFAULT_COORDS.lng], 13);
        
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(myStationMap);

        // Fix map size after initialization
        setTimeout(function() {
            if (myStationMap && myStationMap.invalidateSize) {
                myStationMap.invalidateSize();
            }
        }, 300);

        // Add click event listener
        myStationMap.on('click', function(e) {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;

            // Update form fields
            const latInput = document.getElementById('myStationLatitude');
            const lngInput = document.getElementById('myStationLongitude');
            if (latInput) latInput.value = parseFloat(lat).toFixed(6);
            if (lngInput) lngInput.value = parseFloat(lng).toFixed(6);

            // Remove old marker if exists
            if (myStationMarker) {
                myStationMap.removeLayer(myStationMarker);
            }

            // Add new marker
            const blueIcon = L.divIcon({
                className: 'custom-marker',
                html: '<div style="background:#3B82F6;width:20px;height:20px;border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.4);"></div>',
                iconSize: [20, 20],
                iconAnchor: [10, 10]
            });

            myStationMarker = L.marker([lat, lng], { icon: blueIcon }).addTo(myStationMap);
            myStationMarker.bindPopup('Vị trí trạm sạc<br/>' + lat.toFixed(6) + ', ' + lng.toFixed(6)).openPopup();
        });

        // Request user location
        requestMyLocation();
    }

    function requestMyLocation() {
        if (!myStationMap || !navigator.geolocation) {
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function(position) {
                const lat = position.coords.latitude;
                const lng = position.coords.longitude;
                myStationMap.setView([lat, lng], 15);
            },
            function(error) {
                console.warn('Geolocation error:', error);
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 60000
            }
        );
    }

    function addMySpot() {
        mySpotCounter++;
        const spot = {
            id: 'my_spot_' + mySpotCounter,
            spotNumber: '',
            connectorType: '',
            powerOutput: null,
            pricePerKwh: null,
            status: 0
        };
        mySpots.push(spot);
        renderMySpots();
    }

    function removeMySpot(spotId) {
        mySpots = mySpots.filter(function(s) { return s.id !== spotId; });
        renderMySpots();
    }

    function renderMySpots() {
        const container = document.getElementById('mySpotsContainer');
        const emptyMsg = document.getElementById('mySpotsEmpty');
        
        if (!container) return;

        if (mySpots.length === 0) {
            container.innerHTML = '';
            if (emptyMsg) emptyMsg.style.display = 'block';
            return;
        }

        if (emptyMsg) emptyMsg.style.display = 'none';

        container.innerHTML = mySpots.map(function(spot, index) {
            return `
                <div class="spot-item card mb-3" data-spot-id="${spot.id}">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h6 class="mb-0">Cổng sạc #${index + 1}</h6>
                            <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.removeMySpot('${spot.id}')">
                                <i class="bi bi-trash"></i> Xóa
                            </button>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Số cổng <span class="text-danger">*</span></label>
                                <input type="text" class="form-control my-spot-number" data-spot-id="${spot.id}" 
                                       placeholder="VD: 01, 02, A1" value="${escapeHtml(spot.spotNumber)}" required>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Loại cổng sạc <span class="text-danger">*</span></label>
                                <select class="form-select my-spot-connector-type" data-spot-id="${spot.id}" required>
                                    <option value="">-- Chọn loại cổng --</option>
                                    <option value="CCS" ${spot.connectorType === 'CCS' ? 'selected' : ''}>CCS (Combined Charging System)</option>
                                    <option value="CHAdeMO" ${spot.connectorType === 'CHAdeMO' ? 'selected' : ''}>CHAdeMO</option>
                                    <option value="AC" ${spot.connectorType === 'AC' ? 'selected' : ''}>AC/Type2</option>
                                    <option value="Type2" ${spot.connectorType === 'Type2' ? 'selected' : ''}>Type2</option>
                                </select>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-4 mb-3">
                                <label class="form-label">Công suất (kW) <span class="text-danger">*</span></label>
                                <input type="number" step="0.1" min="0" class="form-control my-spot-power" 
                                       data-spot-id="${spot.id}" placeholder="VD: 50" 
                                       value="${spot.powerOutput || ''}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label">Giá (VND/kWh) <span class="text-danger">*</span></label>
                                <input type="number" step="100" min="0" class="form-control my-spot-price" 
                                       data-spot-id="${spot.id}" placeholder="VD: 4500" 
                                       value="${spot.pricePerKwh || ''}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label">Trạng thái <span class="text-danger">*</span></label>
                                <select class="form-select my-spot-status" data-spot-id="${spot.id}" required>
                                    <option value="0" ${spot.status === 0 ? 'selected' : ''}>Available</option>
                                    <option value="1" ${spot.status === 1 ? 'selected' : ''}>Occupied</option>
                                    <option value="2" ${spot.status === 2 ? 'selected' : ''}>Maintenance</option>
                                    <option value="3" ${spot.status === 3 ? 'selected' : ''}>OutOfOrder</option>
                                </select>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        // Attach event listeners
        attachMySpotEventListeners();
    }

    function attachMySpotEventListeners() {
        const spotInputs = document.querySelectorAll('.my-spot-number, .my-spot-connector-type, .my-spot-power, .my-spot-price, .my-spot-status');
        spotInputs.forEach(function(input) {
            input.addEventListener('change', function() {
                const spotId = this.getAttribute('data-spot-id');
                const spot = mySpots.find(function(s) { return s.id === spotId; });
                if (!spot) return;

                if (this.classList.contains('my-spot-number')) {
                    spot.spotNumber = this.value.trim();
                } else if (this.classList.contains('my-spot-connector-type')) {
                    spot.connectorType = this.value;
                } else if (this.classList.contains('my-spot-power')) {
                    spot.powerOutput = this.value ? parseFloat(this.value) : null;
                } else if (this.classList.contains('my-spot-price')) {
                    spot.pricePerKwh = this.value ? parseFloat(this.value) : null;
                } else if (this.classList.contains('my-spot-status')) {
                    spot.status = parseInt(this.value, 10);
                }
            });
        });
    }

    // Make removeMySpot available globally
    window.removeMySpot = removeMySpot;

    async function saveMyStation() {
        if (!myStationForm) {
            console.error('Form not initialized');
            return;
        }
        
        if (!myStationForm.checkValidity()) {
            myStationForm.reportValidity();
            return;
        }

        // Validate spots
        if (mySpots.length === 0) {
            alert('Vui lòng thêm ít nhất một cổng sạc.');
            return;
        }

        // Collect spot data from inputs
        const spotInputs = document.querySelectorAll('#mySpotsContainer .spot-item');
        const spotsData = [];
        let hasError = false;

        spotInputs.forEach(function(spotItem) {
            const spotId = spotItem.getAttribute('data-spot-id');
            const spot = mySpots.find(function(s) { return s.id === spotId; });
            if (!spot) return;

            const spotNumber = spotItem.querySelector('.my-spot-number').value.trim();
            const connectorType = spotItem.querySelector('.my-spot-connector-type').value;
            const powerOutput = spotItem.querySelector('.my-spot-power').value;
            const pricePerKwh = spotItem.querySelector('.my-spot-price').value;
            const status = spotItem.querySelector('.my-spot-status').value;

            if (!spotNumber || !connectorType || !powerOutput || !pricePerKwh) {
                hasError = true;
                return;
            }

            spotsData.push({
                spotNumber: spotNumber,
                connectorType: connectorType || null,
                powerOutput: powerOutput ? parseFloat(powerOutput) : null,
                pricePerKwh: pricePerKwh ? parseFloat(pricePerKwh) : null,
                status: parseInt(status, 10)
            });
        });

        if (hasError) {
            alert('Vui lòng điền đầy đủ thông tin cho tất cả các cổng sạc.');
            return;
        }

        const formData = new FormData(myStationForm);
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
            closingTime: formData.get('closingTime') || null,
            spots: spotsData
        };

        // Validate coordinates
        if (!data.latitude || !data.longitude) {
            alert('Vui lòng click trên map để chọn vị trí trạm sạc.');
            return;
        }

        const saveBtn = document.getElementById('saveMyStationBtn');
        if (saveBtn) {
            saveBtn.disabled = true;
            saveBtn.textContent = 'Đang lưu...';
        }

        try {
            const response = await fetch('/api/ChargingStation', {
                method: 'POST',
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

            alert('Thêm trạm sạc thành công!');
            if (myStationModal) {
                myStationModal.hide();
            }
            loadStations();
        } catch (error) {
            console.error('Error saving station:', error);
            alert('Lỗi khi lưu trạm sạc: ' + error.message);
        } finally {
            if (saveBtn) {
                saveBtn.disabled = false;
                saveBtn.textContent = 'Lưu trạm sạc';
            }
        }
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

