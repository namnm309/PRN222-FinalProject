(function () {
    'use strict';

    var mapInstance = null;
    var DEFAULT_COORDS = { lat: 21.0285, lng: 105.8542 };
    var serpMarkers = [];
    var selectedMarker = null;
    var selectedStationData = null;

    var stationForm = document.getElementById('stationForm');
    var emptyState = document.getElementById('emptyState');
    var searchInput = document.getElementById('stationSearchInput');
    var saveStationBtn = document.getElementById('saveStationBtn');
    var searchDebounceTimer = null;
    var spots = []; // Array to store charging spots
    var spotCounter = 0; // Counter for unique spot IDs

    // Initialize when DOM is ready
    function init() {
        if (typeof L === 'undefined') {
            console.error('Leaflet is not loaded');
            return;
        }

        // Initialize map
        initMap();

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

        // Auto search nearby stations on load
        setTimeout(function() {
            if (mapInstance) {
                searchNearbyStations();
            }
        }, 1000);
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

        // Request user location
        requestUserLocation();
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
                    // Auto search nearby after location is set
                    setTimeout(function() {
                        searchNearbyStations();
                    }, 500);
                }
            },
            function(error) {
                console.warn('Geolocation error:', error);
                // Use default location and search nearby
                if (mapInstance) {
                    searchNearbyStations();
                }
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
            // Clear markers and reset
            clearSerpMarkers();
            hideForm();
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
                alert('Không thể thực hiện tìm kiếm. Vui lòng thử lại sau.');
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
        popup += '<br/><small style="color:#666;">Click để thêm trạm này</small>';

        marker.bindPopup(popup, { autoClose: false, closeOnClick: false });
        
        marker.on('click', function() {
            selectedMarker = marker;
            selectedStationData = item;
            showFormWithData(item);
            marker.openPopup();
        });

        serpMarkers.push(marker);
        return marker;
    }

    function showFormWithData(data) {
        if (!stationForm || !emptyState) return;

        // Hide empty state, show form
        emptyState.style.display = 'none';
        stationForm.style.display = 'block';

        // Fill form fields - round coordinates to 6 decimal places
        setValue('stationName', data.title || '');
        setValue('stationAddress', data.address || '');
        setValue('stationLatitude', data.latitude ? parseFloat(data.latitude).toFixed(6) : '');
        setValue('stationLongitude', data.longitude ? parseFloat(data.longitude).toFixed(6) : '');
        setValue('stationStatus', '0'); // Default to Active

        // SerpAPI info
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

        // Reset spots when showing form
        spots = [];
        spotCounter = 0;
        renderSpots();

        // Scroll form into view
        stationForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function hideForm() {
        if (!stationForm || !emptyState) return;
        stationForm.style.display = 'none';
        emptyState.style.display = 'block';
        selectedMarker = null;
        selectedStationData = null;
        spots = [];
        spotCounter = 0;
        renderSpots();
    }

    function addSpot() {
        spotCounter++;
        var spot = {
            id: 'spot_' + spotCounter,
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
                            <h6 class="mb-0">Cổng sạc #${index + 1}</h6>
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

    function setValue(id, value) {
        var el = document.getElementById(id);
        if (el) {
            el.value = value || '';
        }
    }

    function handleFormSubmit(e) {
        e.preventDefault();

        if (!stationForm) return;

        if (!stationForm.checkValidity()) {
            stationForm.reportValidity();
            return;
        }

        // Validate spots
        if (spots.length === 0) {
            alert('Vui lòng thêm ít nhất một cổng sạc.');
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
            saveStationBtn.textContent = 'Đang lưu...';
        }

        fetch('/api/ChargingStation', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || 'Failed to save station');
                });
            }
            return response.json();
        })
        .then(function(result) {
            alert('Thêm trạm sạc thành công!');
            window.location.href = '/Admin/Stations';
        })
        .catch(function(error) {
            console.error('Error saving station:', error);
            alert('Lỗi khi lưu trạm sạc: ' + error.message);
        })
        .finally(function() {
            if (saveStationBtn) {
                saveStationBtn.disabled = false;
                saveStationBtn.textContent = 'Lưu trạm sạc';
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

