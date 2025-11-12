(function () {
    'use strict';

    var mapInstance = null;
    var DEFAULT_COORDS = { lat: 21.0285, lng: 105.8542 };
    var serpMarkers = [];
    var currentStationMarker = null;
    var selectedMarker = null;
    var selectedStationData = null;
    var currentStationId = null;

    var stationForm = document.getElementById('stationForm');
    var searchInput = document.getElementById('stationSearchInput');
    var saveStationBtn = document.getElementById('saveStationBtn');
    var searchDebounceTimer = null;
    var spots = []; // Array to store charging spots
    var spotCounter = 0; // Counter for unique spot IDs

    // Get station ID from URL
    function getStationIdFromUrl() {
        var urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('id');
    }

    // Initialize when DOM is ready
    function init() {
        if (typeof L === 'undefined') {
            console.error('Leaflet is not loaded');
            return;
        }

        currentStationId = getStationIdFromUrl();
        if (!currentStationId) {
            alert('Không tìm thấy ID trạm sạc');
            window.location.href = '/Admin/Stations';
            return;
        }

        // Initialize map
        initMap();

        // Load station data
        loadStationData();

        // Setup search input
        if (searchInput) {
            searchInput.addEventListener('input', handleSearchInput);
            searchInput.addEventListener('keypress', function(e) {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    if (searchDebounceTimer) {
                        clearTimeout(searchDebounceTimer);
                    }
                    performSearch(searchInput.value.trim());
                }
            });
        }

        // Setup form submit
        if (stationForm) {
            stationForm.addEventListener('submit', handleFormSubmit);
        }

        // Setup add spot button
        var btnAddSpot = document.getElementById('btn-add-spot');
        if (btnAddSpot) {
            btnAddSpot.addEventListener('click', addSpot);
        }

        // Setup buttons
        var btnLocation = document.getElementById('btn-current-location');
        var btnSearchNearby = document.getElementById('btn-search-nearby');

        if (btnLocation) {
            btnLocation.addEventListener('click', requestUserLocation);
        }

        if (btnSearchNearby) {
            btnSearchNearby.addEventListener('click', function() {
                searchNearbyStations();
            });
        }
    }

    function initMap() {
        var mapEl = document.getElementById('map');
        if (!mapEl) {
            console.error('Map element not found');
            return;
        }

        mapInstance = L.map('map').setView([DEFAULT_COORDS.lat, DEFAULT_COORDS.lng], 13);
        
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(mapInstance);

        // Fix map size after initialization
        setTimeout(function() {
            if (mapInstance && mapInstance.invalidateSize) {
                mapInstance.invalidateSize();
            }
        }, 300);
    }

    async function loadStationData() {
        try {
            var response = await fetch('/api/ChargingStation/' + currentStationId, {
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error('Failed to load station data');
            }

            var station = await response.json();
            populateForm(station);
            showStationOnMap(station);
            
            // Load spots information
            await loadSpotsInfo();
        } catch (error) {
            console.error('Error loading station:', error);
            alert('Lỗi khi tải dữ liệu trạm sạc: ' + error.message);
            window.location.href = '/Admin/Stations';
        }
    }

    async function loadSpotsInfo() {
        try {
            var response = await fetch('/api/ChargingSpot/station/' + currentStationId, {
                credentials: 'include'
            });

            if (response.ok) {
                var spotsData = await response.json();
                spots = [];
                spotCounter = 0;
                
                if (spotsData && Array.isArray(spotsData) && spotsData.length > 0) {
                    // Load existing spots into the spots array
                    spotsData.forEach(function(spot) {
                        spotCounter++;
                        // Ensure status is a number (handle both number and string enum values)
                        var statusValue = 0;
                        if (spot.status !== undefined && spot.status !== null) {
                            statusValue = typeof spot.status === 'number' ? spot.status : parseInt(spot.status, 10);
                            if (isNaN(statusValue)) statusValue = 0;
                        }
                        
                        spots.push({
                            id: spot.id || 'spot_' + spotCounter,
                            spotId: spot.id, // Keep original ID for update
                            spotNumber: spot.spotNumber || '',
                            connectorType: spot.connectorType || '',
                            powerOutput: spot.powerOutput || null,
                            pricePerKwh: spot.pricePerKwh || null,
                            status: statusValue
                        });
                    });
                }
            } else {
                // If response is not ok, log warning but still render empty spots
                console.warn('Failed to load spots:', response.status, response.statusText);
                spots = [];
                spotCounter = 0;
            }
            
            // Always render spots, even if empty or on error
            renderSpots();
        } catch (error) {
            console.error('Error loading spots:', error);
            // Reset spots array and render empty state
            spots = [];
            spotCounter = 0;
            renderSpots();
        }
    }

    function addSpot() {
        spotCounter++;
        var spot = {
            id: 'spot_' + spotCounter,
            spotId: null, // New spot, no ID yet
            spotNumber: '',
            connectorType: '',
            powerOutput: null,
            pricePerKwh: null,
            status: 0 // Available
        };
        spots.push(spot);
        renderSpots();
    }

    function removeSpot(spotId) {
        spots = spots.filter(function(s) { return s.id !== spotId; });
        renderSpots();
    }

    function renderSpots() {
        var container = document.getElementById('spots-container');
        var emptyMsg = document.getElementById('spots-empty');
        
        if (!container) return;

        if (spots.length === 0) {
            container.innerHTML = '';
            if (emptyMsg) emptyMsg.style.display = 'block';
            return;
        }

        if (emptyMsg) emptyMsg.style.display = 'none';

        container.innerHTML = spots.map(function(spot, index) {
            return `
                <div class="spot-item card mb-3" data-spot-id="${spot.id}">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center mb-3">
                            <h6 class="mb-0">Cổng sạc #${index + 1}${spot.spotId ? ' (ID: ' + spot.spotId.substring(0, 8) + '...)' : ' (Mới)'}</h6>
                            <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.removeSpot('${spot.id}')">
                                <i class="bi bi-trash"></i> Xóa
                            </button>
                        </div>
                        <div class="row">
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Số cổng <span class="text-danger">*</span></label>
                                <input type="text" class="form-control spot-number" data-spot-id="${spot.id}" 
                                       placeholder="VD: 01, 02, A1" value="${escapeHtml(spot.spotNumber)}" required>
                            </div>
                            <div class="col-md-6 mb-3">
                                <label class="form-label">Loại cổng sạc <span class="text-danger">*</span></label>
                                <select class="form-select spot-connector-type" data-spot-id="${spot.id}" required>
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
                                <input type="number" step="0.1" min="0" class="form-control spot-power" 
                                       data-spot-id="${spot.id}" placeholder="VD: 50" 
                                       value="${spot.powerOutput || ''}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label">Giá (VND/kWh) <span class="text-danger">*</span></label>
                                <input type="number" step="100" min="0" class="form-control spot-price" 
                                       data-spot-id="${spot.id}" placeholder="VD: 4500" 
                                       value="${spot.pricePerKwh || ''}" required>
                            </div>
                            <div class="col-md-4 mb-3">
                                <label class="form-label">Trạng thái <span class="text-danger">*</span></label>
                                <select class="form-select spot-status" data-spot-id="${spot.id}" required>
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
        attachSpotEventListeners();
    }

    function attachSpotEventListeners() {
        // Attach change listeners to all spot inputs
        var spotInputs = document.querySelectorAll('.spot-number, .spot-connector-type, .spot-power, .spot-price, .spot-status');
        spotInputs.forEach(function(input) {
            input.addEventListener('change', function() {
                var spotId = this.getAttribute('data-spot-id');
                var spot = spots.find(function(s) { return s.id === spotId; });
                if (!spot) return;

                if (this.classList.contains('spot-number')) {
                    spot.spotNumber = this.value.trim();
                } else if (this.classList.contains('spot-connector-type')) {
                    spot.connectorType = this.value;
                } else if (this.classList.contains('spot-power')) {
                    spot.powerOutput = this.value ? parseFloat(this.value) : null;
                } else if (this.classList.contains('spot-price')) {
                    spot.pricePerKwh = this.value ? parseFloat(this.value) : null;
                } else if (this.classList.contains('spot-status')) {
                    spot.status = parseInt(this.value, 10);
                }
            });
        });
    }

    // Make removeSpot available globally
    window.removeSpot = removeSpot;

    function populateForm(station) {
        if (!stationForm) return;

        setValue('stationId', station.id);
        setValue('stationName', station.name || '');
        setValue('stationAddress', station.address || '');
        setValue('stationCity', station.city || '');
        setValue('stationProvince', station.province || '');
        setValue('stationPostalCode', station.postalCode || '');
        setValue('stationLatitude', station.latitude ? parseFloat(station.latitude).toFixed(6) : '');
        setValue('stationLongitude', station.longitude ? parseFloat(station.longitude).toFixed(6) : '');
        setValue('stationPhone', station.phone || '');
        setValue('stationEmail', station.email || '');
        setValue('stationStatus', station.status !== undefined ? station.status.toString() : '0');
        setValue('stationDescription', station.description || '');
        setChecked('stationIs24Hours', station.is24Hours);

        if (station.openingTime) {
            var openingTime = station.openingTime.split(':').slice(0, 2).join(':');
            setValue('stationOpeningTime', openingTime);
        }
        if (station.closingTime) {
            var closingTime = station.closingTime.split(':').slice(0, 2).join(':');
            setValue('stationClosingTime', closingTime);
        }

        // SerpAPI info
        setValue('serpApiPlaceIdInput', station.serpApiPlaceId || '');
        setValue('externalRatingInput', station.externalRating || '');
        setValue('externalReviewCountInput', station.externalReviewCount || '');

        // Display SerpAPI info
        var placeIdEl = document.getElementById('serpApiPlaceId');
        var ratingEl = document.getElementById('serpApiRating');
        var reviewsEl = document.getElementById('serpApiReviews');

        if (placeIdEl) placeIdEl.textContent = station.serpApiPlaceId || '--';
        if (ratingEl) ratingEl.textContent = station.externalRating ? parseFloat(station.externalRating).toFixed(1) : '--';
        if (reviewsEl) reviewsEl.textContent = station.externalReviewCount || '--';
        
        // Set total spots from station DTO if available
        if (station.totalSpots !== undefined) {
            var totalSpotsEl = document.getElementById('totalSpots');
            if (totalSpotsEl) {
                totalSpotsEl.value = station.totalSpots || 0;
            }
        }
    }

    function showStationOnMap(station) {
        if (!mapInstance || !station.latitude || !station.longitude) return;

        var lat = parseFloat(station.latitude);
        var lng = parseFloat(station.longitude);

        // Center map on station
        mapInstance.setView([lat, lng], 15);

        // Remove old marker if exists
        if (currentStationMarker) {
            mapInstance.removeLayer(currentStationMarker);
        }

        // Add marker for current station
        var stationIcon = L.divIcon({
            className: 'custom-marker',
            html: '<div style="background:#155DFC;width:20px;height:20px;border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.4);"></div>',
            iconSize: [20, 20],
            iconAnchor: [10, 10]
        });

        currentStationMarker = L.marker([lat, lng], { icon: stationIcon }).addTo(mapInstance);
        
        var popup = '<b>' + escapeHtml(station.name || 'Trạm sạc') + '</b>';
        if (station.address) popup += '<br/>' + escapeHtml(station.address);
        popup += '<br/><small style="color:#666;">Trạm hiện tại</small>';

        currentStationMarker.bindPopup(popup);
        currentStationMarker.openPopup();

        // Auto search nearby after showing station
        setTimeout(function() {
            searchNearbyStations();
        }, 500);
    }

    function requestUserLocation() {
        if (!navigator.geolocation) {
            console.warn('Geolocation not supported');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function(position) {
                var lat = position.coords.latitude;
                var lng = position.coords.longitude;
                
                if (mapInstance) {
                    mapInstance.setView([lat, lng], 15);
                }
            },
            function(error) {
                console.warn('Geolocation error:', error);
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    }

    function handleSearchInput(e) {
        var query = e.target.value.trim();

        if (searchDebounceTimer) {
            clearTimeout(searchDebounceTimer);
        }

        if (!query) {
            clearSerpMarkers();
            return;
        }

        searchDebounceTimer = setTimeout(function() {
            performSearch(query);
        }, 500);
    }

    function performSearch(searchQuery) {
        if (!mapInstance || !searchQuery.trim()) return;

        var center = mapInstance.getCenter();
        var url = '/api/maps/search?query=' + encodeURIComponent(searchQuery) + 
                  '&lat=' + encodeURIComponent(center.lat) + 
                  '&lng=' + encodeURIComponent(center.lng) + 
                  '&zoom=' + encodeURIComponent(mapInstance.getZoom() || 14);

        fetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Request failed');
                return r.json();
            })
            .then(function(list) {
                clearSerpMarkers();
                
                if (Array.isArray(list) && list.length > 0) {
                    for (var i = 0; i < list.length; i++) {
                        var place = list[i];
                        if (!place || typeof place !== 'object') continue;

                        var item = {
                            title: place.title || place.Title || '',
                            address: place.address || place.Address || '',
                            latitude: place.latitude || place.Latitude || 0,
                            longitude: place.longitude || place.Longitude || 0,
                            rating: place.rating || place.Rating || null,
                            reviews: place.reviews || place.Reviews || null,
                            placeId: place.placeId || place.PlaceId || null
                        };

                        if (typeof item.latitude === 'string') item.latitude = parseFloat(item.latitude);
                        if (typeof item.longitude === 'string') item.longitude = parseFloat(item.longitude);
                        if (typeof item.rating === 'string') item.rating = parseFloat(item.rating);
                        if (typeof item.reviews === 'string') item.reviews = parseInt(item.reviews, 10);

                        if (typeof item.latitude === 'number' && !isNaN(item.latitude) &&
                            typeof item.longitude === 'number' && !isNaN(item.longitude)) {
                            var marker = addSerpMarker(item);
                            if (marker) {
                                marker.stationData = item;
                            }
                        }
                    }
                }
            })
            .catch(function(err) {
                console.error('Search error:', err);
            });
    }

    function searchNearbyStations() {
        if (!mapInstance) return;

        var center = mapInstance.getCenter();
        var url = '/api/maps/search?query=' + encodeURIComponent('EV charging station') + 
                  '&lat=' + encodeURIComponent(center.lat) + 
                  '&lng=' + encodeURIComponent(center.lng) + 
                  '&zoom=' + encodeURIComponent(mapInstance.getZoom() || 14);

        fetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Request failed');
                return r.json();
            })
            .then(function(list) {
                clearSerpMarkers();
                
                if (Array.isArray(list) && list.length > 0) {
                    for (var i = 0; i < list.length; i++) {
                        var place = list[i];
                        if (!place || typeof place !== 'object') continue;

                        var item = {
                            title: place.title || place.Title || '',
                            address: place.address || place.Address || '',
                            latitude: place.latitude || place.Latitude || 0,
                            longitude: place.longitude || place.Longitude || 0,
                            rating: place.rating || place.Rating || null,
                            reviews: place.reviews || place.Reviews || null,
                            placeId: place.placeId || place.PlaceId || null
                        };

                        if (typeof item.latitude === 'string') item.latitude = parseFloat(item.latitude);
                        if (typeof item.longitude === 'string') item.longitude = parseFloat(item.longitude);
                        if (typeof item.rating === 'string') item.rating = parseFloat(item.rating);
                        if (typeof item.reviews === 'string') item.reviews = parseInt(item.reviews, 10);

                        if (typeof item.latitude === 'number' && !isNaN(item.latitude) &&
                            typeof item.longitude === 'number' && !isNaN(item.longitude)) {
                            var marker = addSerpMarker(item);
                            if (marker) {
                                marker.stationData = item;
                            }
                        }
                    }
                }
            })
            .catch(function(err) {
                console.error('Search nearby error:', err);
            });
    }

    function clearSerpMarkers() {
        for (var i = 0; i < serpMarkers.length; i++) {
            try {
                mapInstance.removeLayer(serpMarkers[i]);
            } catch (e) {
                console.warn('Error removing marker:', e);
            }
        }
        serpMarkers = [];
    }

    function addSerpMarker(item) {
        if (!mapInstance || typeof item.latitude !== 'number' || typeof item.longitude !== 'number' ||
            isNaN(item.latitude) || isNaN(item.longitude)) {
            return null;
        }

        var color = '#10B981'; // Green color for available stations
        var icon = L.divIcon({
            className: 'custom-marker',
            html: '<div style="background:' + color + ';width:16px;height:16px;border-radius:50%;border:3px solid white;box-shadow:0 2px 4px rgba(0,0,0,0.3);"></div>',
            iconSize: [16, 16],
            iconAnchor: [8, 8]
        });

        var marker = L.marker([item.latitude, item.longitude], { icon: icon }).addTo(mapInstance);
        
        var popup = '<b>' + escapeHtml(item.title || 'Trạm sạc') + '</b>';
        if (item.address) popup += '<br/>' + escapeHtml(item.address);
        if (item.rating) {
            popup += '<br/>Đánh giá: ' + escapeHtml(item.rating.toFixed(1));
            if (item.reviews) popup += ' (' + escapeHtml(item.reviews) + ')';
        }
        popup += '<br/><small style="color:#666;">Click để cập nhật thông tin từ đây</small>';

        marker.bindPopup(popup, { autoClose: false, closeOnClick: false });
        
        marker.on('click', function() {
            selectedMarker = marker;
            selectedStationData = item;
            updateFormFromSerpData(item);
            marker.openPopup();
        });

        serpMarkers.push(marker);
        return marker;
    }

    function updateFormFromSerpData(data) {
        if (!stationForm) return;

        // Update form fields with SerpAPI data
        setValue('stationName', data.title || '');
        setValue('stationAddress', data.address || '');
        setValue('stationLatitude', data.latitude ? parseFloat(data.latitude).toFixed(6) : '');
        setValue('stationLongitude', data.longitude ? parseFloat(data.longitude).toFixed(6) : '');

        // Update SerpAPI info
        setValue('serpApiPlaceIdInput', data.placeId || '');
        setValue('externalRatingInput', data.rating || '');
        setValue('externalReviewCountInput', data.reviews || '');

        // Display SerpAPI info
        var placeIdEl = document.getElementById('serpApiPlaceId');
        var ratingEl = document.getElementById('serpApiRating');
        var reviewsEl = document.getElementById('serpApiReviews');

        if (placeIdEl) placeIdEl.textContent = data.placeId || '--';
        if (ratingEl) ratingEl.textContent = data.rating ? data.rating.toFixed(1) : '--';
        if (reviewsEl) reviewsEl.textContent = data.reviews || '--';

        // Update map marker position
        if (currentStationMarker && data.latitude && data.longitude) {
            var newLat = parseFloat(data.latitude);
            var newLng = parseFloat(data.longitude);
            currentStationMarker.setLatLng([newLat, newLng]);
            mapInstance.setView([newLat, newLng], 15);
        }
    }

    function setValue(id, value) {
        var el = document.getElementById(id);
        if (el) {
            el.value = value || '';
        }
    }

    function setChecked(id, checked) {
        var el = document.getElementById(id);
        if (el) {
            el.checked = checked || false;
        }
    }

    function handleFormSubmit(e) {
        e.preventDefault();

        if (!stationForm || !currentStationId) return;

        if (!stationForm.checkValidity()) {
            stationForm.reportValidity();
            return;
        }

        // Collect spot data from inputs
        var spotInputs = document.querySelectorAll('.spot-item');
        var spotsData = [];
        var hasError = false;

        spotInputs.forEach(function(spotItem) {
            var spotId = spotItem.getAttribute('data-spot-id');
            var spot = spots.find(function(s) { return s.id === spotId; });
            if (!spot) return;

            var spotNumber = spotItem.querySelector('.spot-number').value.trim();
            var connectorType = spotItem.querySelector('.spot-connector-type').value;
            var powerOutput = spotItem.querySelector('.spot-power').value;
            var pricePerKwh = spotItem.querySelector('.spot-price').value;
            var status = spotItem.querySelector('.spot-status').value;

            if (!spotNumber || !connectorType || !powerOutput || !pricePerKwh) {
                hasError = true;
                return;
            }

            spotsData.push({
                id: spot.spotId ? spot.spotId : null, // Include ID if exists (for update), null for new spots
                spotNumber: spotNumber,
                connectorType: connectorType,
                powerOutput: parseFloat(powerOutput),
                pricePerKwh: parseFloat(pricePerKwh),
                status: parseInt(status, 10)
            });
        });

        if (hasError) {
            alert('Vui lòng điền đầy đủ thông tin cho tất cả các cổng sạc.');
            return;
        }

        var formData = new FormData(stationForm);
        var data = {
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
            serpApiPlaceId: formData.get('serpApiPlaceId') || null,
            externalRating: formData.get('externalRating') ? parseFloat(formData.get('externalRating')) : null,
            externalReviewCount: formData.get('externalReviewCount') ? parseInt(formData.get('externalReviewCount'), 10) : null,
            // Charging spots - danh sách cổng sạc chi tiết
            spots: spotsData
        };

        // Disable submit button
        if (saveStationBtn) {
            saveStationBtn.disabled = true;
            saveStationBtn.textContent = 'Đang cập nhật...';
        }

        fetch('/api/ChargingStation/' + currentStationId, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || 'Failed to update station');
                });
            }
            return response.json();
        })
        .then(function(result) {
            alert('Cập nhật trạm sạc thành công!');
            window.location.href = '/Admin/Stations';
        })
        .catch(function(error) {
            console.error('Error updating station:', error);
            alert('Lỗi khi cập nhật trạm sạc: ' + error.message);
        })
        .finally(function() {
            if (saveStationBtn) {
                saveStationBtn.disabled = false;
                saveStationBtn.textContent = 'Cập nhật trạm sạc';
            }
        });
    }

    function escapeHtml(str) {
        if (typeof str !== 'string') {
            if (str === null || str === undefined) return '';
            str = String(str);
        }
        return str.replace(/[&<>"']/g, function(m) {
            switch (m) {
                case '&': return '&amp;';
                case '<': return '&lt;';
                case '>': return '&gt;';
                case '"': return '&quot;';
                case '\'': return '&#39;';
                default: return m;
            }
        });
    }

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

