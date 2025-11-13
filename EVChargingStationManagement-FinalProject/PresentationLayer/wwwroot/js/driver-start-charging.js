// Driver Start Charging JavaScript

let qrCodeInstance = null;
let isQrScanned = false;

document.addEventListener('DOMContentLoaded', function() {
    loadVehicles();
    initializeForm();
    
    // Event listeners
    document.getElementById('start-charging-form').addEventListener('submit', handleStartCharging);
    document.getElementById('spot-select').addEventListener('change', handleSpotSelect);
    document.getElementById('station-select').addEventListener('change', handleStationSelect);
    document.getElementById('btn-scan-qr').addEventListener('click', handleQrScanned);
    document.getElementById('manual-qr-input').addEventListener('input', handleManualQrInput);
    
    // Add vehicle modal
    const btnAddVehicle = document.getElementById('btn-add-vehicle');
    if (btnAddVehicle) {
        btnAddVehicle.addEventListener('click', openAddVehicleModal);
    }
    
    const btnSaveVehicle = document.getElementById('btn-save-vehicle');
    if (btnSaveVehicle) {
        btnSaveVehicle.addEventListener('click', handleSaveVehicle);
    }
    
    // Load spots when modal opens
    const addVehicleModal = document.getElementById('addVehicleModal');
    if (addVehicleModal) {
        addVehicleModal.addEventListener('show.bs.modal', function() {
            loadSpotsForAddVehicleModal();
        });
    }
});

async function initializeForm() {
    const reservationId = document.getElementById('reservation-id').value;
    const spotId = document.getElementById('spot-id').value;
    const stationId = document.getElementById('station-id').value;
    
    if (reservationId) {
        // Load từ reservation
        await loadFromReservation(reservationId);
    } else if (spotId) {
        // Load từ spot
        await loadSpotAndQrCode(spotId);
    } else if (stationId) {
        // Load từ station (chọn từ bản đồ)
        document.getElementById('station-id').value = stationId;
        await loadSpotsForStation(stationId);
    } else {
        // Load danh sách stations
        await loadStations();
        document.getElementById('station-select-section').style.display = 'block';
    }
}

async function loadFromReservation(reservationId) {
    try {
        const response = await fetch(`/api/Reservation/${reservationId}`, {
            credentials: 'include'
        });
        if (response.ok) {
            const reservation = await response.json();
            if (reservation.chargingSpotId && reservation.chargingStationId) {
                // Set reservation và station ID
                document.getElementById('reservation-id').value = reservationId;
                document.getElementById('station-id').value = reservation.chargingStationId;
                
                // Load spots cho station và select spot đã đặt
                await loadSpotsForStationAndSelect(reservation.chargingStationId, reservation.chargingSpotId);
            }
        }
    } catch (error) {
        console.error('Error loading reservation:', error);
    }
}

async function loadStations() {
    const select = document.getElementById('station-select');
    try {
        const response = await fetch('/api/ChargingStation', {
            credentials: 'include'
        });
        if (response.ok) {
            const stations = await response.json();
            select.innerHTML = '<option value="">-- Chọn trạm sạc --</option>';
            stations.forEach(station => {
                const option = document.createElement('option');
                option.value = station.id;
                option.textContent = `${station.name} (${station.availableSpots}/${station.totalSpots} cổng trống)`;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading stations:', error);
    }
}

async function handleStationSelect(e) {
    const stationId = e.target.value;
    if (!stationId) {
        document.getElementById('spot-select').innerHTML = '<option value="">-- Chọn cổng sạc --</option>';
        return;
    }
    
    document.getElementById('station-id').value = stationId;
    await loadSpotsForStation(stationId);
}

async function loadSpotsForStation(stationId) {
    const select = document.getElementById('spot-select');
    select.innerHTML = '<option value="">-- Đang tải --</option>';
    
    try {
        const response = await fetch(`/api/ChargingSpot/station/${stationId}/available`, {
            credentials: 'include'
        });
        if (response.ok) {
            const spots = await response.json();
            select.innerHTML = '<option value="">-- Chọn cổng sạc --</option>';
            if (spots && spots.length > 0) {
                spots.forEach(spot => {
                    const option = document.createElement('option');
                    option.value = spot.id;
                    option.textContent = `${spot.spotNumber} - ${spot.connectorType} (${spot.powerOutputKw}kW) - ${spot.pricePerKwh}đ/kWh`;
                    select.appendChild(option);
                });
            } else {
                select.innerHTML = '<option value="">-- Không có cổng sạc trống --</option>';
            }
        }
    } catch (error) {
        console.error('Error loading spots:', error);
        select.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
    }
}

async function loadSpotsForStationAndSelect(stationId, selectedSpotId) {
    const select = document.getElementById('spot-select');
    select.innerHTML = '<option value="">-- Đang tải --</option>';
    
    try {
        // Load tất cả spots của station (không chỉ available) để đảm bảo spot đã đặt luôn hiển thị
        const response = await fetch(`/api/ChargingSpot/station/${stationId}`, {
            credentials: 'include'
        });
        if (response.ok) {
            const spots = await response.json();
            select.innerHTML = '<option value="">-- Chọn cổng sạc --</option>';
            if (spots && spots.length > 0) {
                let spotFound = false;
                spots.forEach(spot => {
                    const option = document.createElement('option');
                    option.value = spot.id;
                    option.textContent = `${spot.spotNumber} - ${spot.connectorType} (${spot.powerOutputKw}kW) - ${spot.pricePerKwh}đ/kWh`;
                    // Select spot đã đặt trong reservation
                    if (spot.id === selectedSpotId) {
                        option.selected = true;
                        spotFound = true;
                    }
                    select.appendChild(option);
                });
                
                // Set spot-id và load QR code cho spot đã đặt
                if (selectedSpotId && spotFound) {
                    document.getElementById('spot-id').value = selectedSpotId;
                    await loadSpotAndQrCode(selectedSpotId);
                } else if (selectedSpotId && !spotFound) {
                    // Nếu spot đã đặt không tìm thấy, vẫn load QR code
                    document.getElementById('spot-id').value = selectedSpotId;
                    await loadSpotAndQrCode(selectedSpotId);
                }
            } else {
                select.innerHTML = '<option value="">-- Không có cổng sạc --</option>';
                // Vẫn load QR code cho spot đã đặt nếu không có spots
                if (selectedSpotId) {
                    document.getElementById('spot-id').value = selectedSpotId;
                    await loadSpotAndQrCode(selectedSpotId);
                }
            }
        }
    } catch (error) {
        console.error('Error loading spots:', error);
        select.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
        // Vẫn load QR code cho spot đã đặt nếu có lỗi
        if (selectedSpotId) {
            document.getElementById('spot-id').value = selectedSpotId;
            await loadSpotAndQrCode(selectedSpotId);
        }
    }
}

async function handleSpotSelect(e) {
    const spotId = e.target.value;
    if (!spotId) {
        hideQrCode();
        return;
    }
    
    document.getElementById('spot-id').value = spotId;
    await loadSpotAndQrCode(spotId);
    
    // Validate compatibility sau khi chọn spot
    validateVehicleSpotCompatibility();
}

async function loadSpotAndQrCode(spotId) {
    try {
        const response = await fetch(`/api/QrCode/spot/${spotId}`, {
            credentials: 'include'
        });
        if (response.ok) {
            const data = await response.json();
            if (data.qrCode) {
                displayQrCode(data.qrCode);
                document.getElementById('qr-code-value').value = data.qrCode;
                document.getElementById('manual-qr-input').value = data.qrCode;
                isQrScanned = false; // Reset trạng thái quét
                updateStartButtonState();
            }
        }
    } catch (error) {
        console.error('Error loading QR code:', error);
    }
}

function displayQrCode(qrCode) {
    const container = document.getElementById('qrcode');
    const section = document.getElementById('qr-code-section');
    
    if (container && typeof QRCode !== 'undefined') {
        container.innerHTML = '';
        qrCodeInstance = new QRCode(container, {
            text: qrCode,
            width: 256,
            height: 256,
            colorDark: '#000000',
            colorLight: '#ffffff',
            correctLevel: QRCode.CorrectLevel.H
        });
        section.style.display = 'block';
        document.getElementById('manual-qr-section').style.display = 'block';
    }
}

function hideQrCode() {
    document.getElementById('qr-code-section').style.display = 'none';
    document.getElementById('manual-qr-section').style.display = 'none';
    document.getElementById('qr-code-value').value = '';
    isQrScanned = false;
    updateStartButtonState();
}

function handleQrScanned() {
    isQrScanned = true;
    updateStartButtonState();
    alert('Đã xác nhận quét QR code. Bạn có thể bắt đầu sạc.');
}

function handleManualQrInput(e) {
    const qrCode = e.target.value.trim();
    if (qrCode && qrCode.startsWith('EVCS_')) {
        document.getElementById('qr-code-value').value = qrCode;
        isQrScanned = true;
        updateStartButtonState();
    } else {
        isQrScanned = false;
        updateStartButtonState();
    }
}

function updateStartButtonState() {
    const btnStart = document.getElementById('btn-start-charging');
    const spotId = document.getElementById('spot-id').value;
    const vehicleId = document.getElementById('vehicle-select').value;
    const qrCode = document.getElementById('qr-code-value').value;
    
    // Enable nút nếu đã chọn spot, xe và có QR code (đã quét hoặc nhập thủ công)
    const canStart = spotId && vehicleId && qrCode && qrCode.startsWith('EVCS_');
    btnStart.disabled = !canStart;
}

async function loadVehicles() {
    const select = document.getElementById('vehicle-select');
    
    try {
        const response = await fetch('/api/Vehicle', {
            credentials: 'include'
        });
        
        if (response.ok) {
            const vehicles = await response.json();
            select.innerHTML = '<option value="">-- Chọn xe --</option>';
            
            if (vehicles && vehicles.length > 0) {
                vehicles.forEach(vehicle => {
                    const option = document.createElement('option');
                    option.value = vehicle.id;
                    const displayText = vehicle.licensePlate 
                        ? `${vehicle.make} ${vehicle.model} (${vehicle.licensePlate})`
                        : `${vehicle.make} ${vehicle.model}`;
                    option.textContent = displayText;
                    // Lưu preferred spot ID vào data attribute để validate sau
                    const preferredSpotId = getPreferredSpotId(vehicle);
                    if (preferredSpotId) {
                        option.dataset.preferredSpotId = preferredSpotId;
                    }
                    // Lưu toàn bộ vehicle object vào dataset để dễ truy cập
                    option.dataset.vehicleData = JSON.stringify(vehicle);
                    select.appendChild(option);
                });
            } else {
                select.innerHTML = '<option value="">-- Bạn chưa có xe. Vui lòng thêm xe trước.</option>';
            }
        } else {
            select.innerHTML = '<option value="">-- Không thể tải danh sách xe --</option>';
        }
    } catch (error) {
        console.error('Error loading vehicles:', error);
        select.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
    }
    
    // Add change listener để update button state và validate spot
    select.addEventListener('change', function() {
        updateStartButtonState();
        validateVehicleSpotCompatibility();
    });
}

// Helper function để lấy preferred spot ID từ vehicle notes
function getPreferredSpotId(vehicle) {
    if (!vehicle || !vehicle.notes) return null;
    const match = vehicle.notes.match(/PREFERRED_SPOT_ID:([a-f0-9-]+)/i);
    return match ? match[1] : null;
}

// Validate xem xe đã chọn có compatible với cổng sạc đã chọn không
function validateVehicleSpotCompatibility() {
    const vehicleSelect = document.getElementById('vehicle-select');
    const spotSelect = document.getElementById('spot-select');
    
    if (!vehicleSelect || !spotSelect) return;
    
    const selectedVehicleOption = vehicleSelect.options[vehicleSelect.selectedIndex];
    const selectedSpotId = spotSelect.value;
    
    if (!selectedVehicleOption || !selectedVehicleOption.value || !selectedSpotId) {
        return; // Chưa chọn đủ, không validate
    }
    
    const preferredSpotId = selectedVehicleOption.dataset.preferredSpotId;
    
    if (preferredSpotId && preferredSpotId !== selectedSpotId) {
        // Xe có cổng sạc ưa thích nhưng không khớp với cổng đã chọn
        const vehicleData = JSON.parse(selectedVehicleOption.dataset.vehicleData || '{}');
        const vehicleName = vehicleData.licensePlate 
            ? `${vehicleData.make} ${vehicleData.model} (${vehicleData.licensePlate})`
            : `${vehicleData.make} ${vehicleData.model}`;
        
        alert(`Cảnh báo: Xe "${vehicleName}" chỉ tương thích với cổng sạc đã đăng ký. Vui lòng chọn cổng sạc phù hợp hoặc chọn xe khác.`);
        
        // Disable nút bắt đầu sạc
        const btnStart = document.getElementById('btn-start-charging');
        if (btnStart) {
            btnStart.disabled = true;
        }
    } else {
        // Khớp hoặc không có preferred spot, enable nút
        updateStartButtonState();
    }
}

async function handleStartCharging(e) {
    e.preventDefault();

    const form = e.target;
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const qrCode = document.getElementById('qr-code-value').value || document.getElementById('manual-qr-input').value;
    const spotId = document.getElementById('spot-id').value;
    const vehicleId = document.getElementById('vehicle-select').value;
    const reservationId = document.getElementById('reservation-id').value;
    const targetSoc = document.getElementById('target-soc').value;
    const energyRequested = document.getElementById('energy-requested').value;

    if (!qrCode || !qrCode.startsWith('EVCS_')) {
        alert('Vui lòng nhập QR code hợp lệ');
        return;
    }

    if (!vehicleId) {
        alert('Vui lòng chọn xe');
        return;
    }

    if (!spotId) {
        alert('Vui lòng chọn cổng sạc');
        return;
    }
    
    // Validate vehicle-spot compatibility trước khi submit
    const vehicleSelect = document.getElementById('vehicle-select');
    const spotSelect = document.getElementById('spot-select');
    if (vehicleSelect && spotSelect) {
        const selectedVehicleOption = vehicleSelect.options[vehicleSelect.selectedIndex];
        const selectedSpotId = spotSelect.value;
        if (selectedVehicleOption && selectedVehicleOption.value && selectedSpotId) {
            const preferredSpotId = selectedVehicleOption.dataset.preferredSpotId;
            if (preferredSpotId && preferredSpotId !== selectedSpotId) {
                const vehicleData = JSON.parse(selectedVehicleOption.dataset.vehicleData || '{}');
                const vehicleName = vehicleData.licensePlate 
                    ? `${vehicleData.make} ${vehicleData.model} (${vehicleData.licensePlate})`
                    : `${vehicleData.make} ${vehicleData.model}`;
                alert(`Không thể bắt đầu sạc: Xe "${vehicleName}" chỉ tương thích với cổng sạc đã đăng ký. Vui lòng chọn cổng sạc phù hợp hoặc chọn xe khác.`);
                return;
            }
        }
    }

    const submitBtn = document.getElementById('btn-start-charging');
    submitBtn.disabled = true;
    submitBtn.textContent = 'Đang xử lý...';

    try {
        // Sử dụng endpoint scan-qr để bắt đầu sạc
        const response = await fetch('/api/ChargingSession/scan-qr', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                qrCode: qrCode,
                vehicleId: vehicleId,
                reservationId: reservationId || null,
                targetSocPercentage: targetSoc ? parseFloat(targetSoc) : null,
                energyRequestedKwh: energyRequested ? parseFloat(energyRequested) : null
            })
        });

        if (response.ok) {
            const session = await response.json();
            alert('Bắt đầu sạc thành công!');
            window.location.href = `/Driver/SessionDetail/${session.id}`;
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể bắt đầu sạc'));
        }
    } catch (error) {
        console.error('Error starting charging:', error);
        alert('Có lỗi xảy ra. Vui lòng thử lại.');
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = 'Bắt đầu sạc';
    }
}

// Add Vehicle Modal Functions
function openAddVehicleModal() {
    const modal = new bootstrap.Modal(document.getElementById('addVehicleModal'));
    modal.show();
}

async function loadSpotsForAddVehicleModal() {
    const select = document.getElementById('add-vehicle-spot-select');
    if (!select) return;
    
    select.innerHTML = '<option value="">-- Đang tải --</option>';
    
    try {
        // Load tất cả stations và spots
        const stationsResponse = await fetch('/api/ChargingStation', {
            credentials: 'include'
        });
        
        if (stationsResponse.ok) {
            const stations = await stationsResponse.json();
            select.innerHTML = '<option value="">-- Chọn cổng sạc (tùy chọn) --</option>';
            
            // Load spots từ tất cả stations
            for (const station of stations) {
                try {
                    const spotsResponse = await fetch(`/api/ChargingSpot/station/${station.id}`, {
                        credentials: 'include'
                    });
                    if (spotsResponse.ok) {
                        const spots = await spotsResponse.json();
                        spots.forEach(spot => {
                            const option = document.createElement('option');
                            option.value = spot.id;
                            option.textContent = `${station.name} - Cổng ${spot.spotNumber} (${spot.connectorType || 'N/A'})`;
                            select.appendChild(option);
                        });
                    }
                } catch (err) {
                    console.error(`Error loading spots for station ${station.id}:`, err);
                }
            }
        }
    } catch (error) {
        console.error('Error loading spots:', error);
        select.innerHTML = '<option value="">-- Lỗi tải dữ liệu --</option>';
    }
}

async function handleSaveVehicle() {
    const form = document.getElementById('add-vehicle-form');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }
    
    const make = document.getElementById('vehicle-make').value.trim();
    const licensePlate = document.getElementById('vehicle-license-plate').value.trim();
    const model = document.getElementById('vehicle-model').value.trim() || make; // Nếu không có model, dùng make
    const spotId = document.getElementById('add-vehicle-spot-select').value;
    
    if (!make || !licensePlate) {
        alert('Vui lòng nhập đầy đủ hãng xe và biển số.');
        return;
    }
    
    const btnSave = document.getElementById('btn-save-vehicle');
    btnSave.disabled = true;
    btnSave.textContent = 'Đang lưu...';
    
    try {
        // Lưu preferred spot ID vào Notes nếu có chọn
        let notes = '';
        if (spotId) {
            notes = `PREFERRED_SPOT_ID:${spotId}`;
        }
        
        const vehicleData = {
            make: make,
            model: model,
            licensePlate: licensePlate,
            vehicleType: 'Car', // Mặc định là Car
            isPrimary: false,
            notes: notes || null
        };
        
        const response = await fetch('/api/Vehicle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(vehicleData)
        });
        
        if (response.ok) {
            const newVehicle = await response.json();
            
            // Đóng modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('addVehicleModal'));
            modal.hide();
            
            // Reset form
            form.reset();
            
            // Reload danh sách xe và chọn xe vừa thêm
            await loadVehicles();
            document.getElementById('vehicle-select').value = newVehicle.id;
            updateStartButtonState();
            
            alert('Thêm xe thành công!');
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể thêm xe'));
        }
    } catch (error) {
        console.error('Error saving vehicle:', error);
        alert('Có lỗi xảy ra. Vui lòng thử lại.');
    } finally {
        btnSave.disabled = false;
        btnSave.textContent = 'Lưu';
    }
}

// Helper function để lấy preferred spot ID từ vehicle notes
function getPreferredSpotId(vehicle) {
    if (!vehicle || !vehicle.notes) return null;
    const match = vehicle.notes.match(/PREFERRED_SPOT_ID:([a-f0-9-]+)/i);
    return match ? match[1] : null;
}
