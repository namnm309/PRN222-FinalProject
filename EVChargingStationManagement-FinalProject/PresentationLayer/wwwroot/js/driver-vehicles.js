// Driver Vehicles JavaScript

let vehicles = [];
let editingVehicleId = null;

document.addEventListener('DOMContentLoaded', function() {
    loadVehicles();
    
    const vehicleForm = document.getElementById('vehicleForm');
    const btnSaveVehicle = document.getElementById('btn-save-vehicle');
    const vehicleModal = document.getElementById('vehicleModal');
    
    if (btnSaveVehicle) {
        btnSaveVehicle.addEventListener('click', handleSaveVehicle);
    }
    
    if (vehicleModal) {
        vehicleModal.addEventListener('hidden.bs.modal', function() {
            resetForm();
        });
    }
});

async function loadVehicles() {
    try {
        const response = await fetch('/api/Vehicle', {
            credentials: 'include'
        });

        if (response.ok) {
            vehicles = await response.json();
            displayVehicles(vehicles);
            updateVehicleCount(vehicles.length);
        } else {
            showError('Không thể tải danh sách xe');
        }
    } catch (error) {
        console.error('Error loading vehicles:', error);
        showError('Lỗi khi tải danh sách xe');
    }
}

function displayVehicles(vehiclesList) {
    const container = document.getElementById('vehicles-list');
    
    if (!container) return;
    
    if (vehiclesList.length === 0) {
        container.innerHTML = `
            <div class="text-center py-5">
                <div class="text-muted">
                    <i class="bi bi-car-front" style="font-size: 3rem; opacity: 0.3;"></i>
                    <p class="mt-3 mb-0">Chưa có xe nào</p>
                    <small>Hãy thêm xe đầu tiên của bạn</small>
                </div>
            </div>
        `;
        return;
    }

    container.innerHTML = vehiclesList.map(v => {
        const isPrimary = v.isPrimary ? '<span class="badge bg-success">Mặc định</span>' : '';
        const vehicleType = getVehicleTypeLabel(v.vehicleType);
        
        return `
            <div class="vehicle-card" data-vehicle-id="${v.id}">
                <div class="vehicle-card-header">
                    <div>
                        <h4 class="vehicle-name">${escapeHtml(v.nickname || `${v.make} ${v.model}`)}</h4>
                        <div class="vehicle-info">
                            <span class="badge bg-light text-dark">${escapeHtml(v.make)} ${escapeHtml(v.model)}</span>
                            <span class="badge bg-secondary">${v.modelYear}</span>
                            <span class="badge bg-info">${escapeHtml(v.licensePlate)}</span>
                            ${isPrimary}
                        </div>
                    </div>
                    <div class="vehicle-actions">
                        ${!v.isPrimary ? `<button class="btn btn-sm btn-outline-primary" onclick="setPrimaryVehicle('${v.id}')" title="Đặt làm mặc định">
                            <i class="bi bi-star"></i>
                        </button>` : ''}
                        <button class="btn btn-sm btn-outline-primary" onclick="editVehicle('${v.id}')" title="Sửa">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteVehicle('${v.id}')" title="Xóa">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
                <div class="vehicle-card-body">
                    <div class="row">
                        <div class="col-md-6">
                            <div class="vehicle-detail">
                                <span class="detail-label">Loại xe:</span>
                                <span class="detail-value">${escapeHtml(vehicleType)}</span>
                            </div>
                            <div class="vehicle-detail">
                                <span class="detail-label">Màu sắc:</span>
                                <span class="detail-value">${escapeHtml(v.color || 'N/A')}</span>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="vehicle-detail">
                                <span class="detail-label">Dung lượng pin:</span>
                                <span class="detail-value">${v.batteryCapacityKwh || 0} kWh</span>
                            </div>
                            <div class="vehicle-detail">
                                <span class="detail-label">Công suất sạc tối đa:</span>
                                <span class="detail-value">${v.maxChargingPowerKw || 0} kW</span>
                            </div>
                        </div>
                    </div>
                    ${v.chargePortLocation ? `
                        <div class="vehicle-detail">
                            <span class="detail-label">Vị trí cổng sạc:</span>
                            <span class="detail-value">${escapeHtml(getPortLocationLabel(v.chargePortLocation))}</span>
                        </div>
                    ` : ''}
                    ${v.notes ? `
                        <div class="vehicle-detail">
                            <span class="detail-label">Ghi chú:</span>
                            <span class="detail-value">${escapeHtml(v.notes)}</span>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    }).join('');
}

function updateVehicleCount(count) {
    const countEl = document.getElementById('vehicle-count');
    if (countEl) {
        countEl.textContent = count;
    }
}

function openAddVehicleModal() {
    editingVehicleId = null;
    document.getElementById('vehicleModalLabel').textContent = 'Thêm xe mới';
    resetForm();
}

function editVehicle(vehicleId) {
    const vehicle = vehicles.find(v => v.id === vehicleId);
    if (!vehicle) return;

    editingVehicleId = vehicleId;
    document.getElementById('vehicleModalLabel').textContent = 'Sửa thông tin xe';
    
    document.getElementById('vehicle-id').value = vehicle.id;
    document.getElementById('vehicle-make').value = vehicle.make || '';
    document.getElementById('vehicle-model').value = vehicle.model || '';
    document.getElementById('vehicle-year').value = vehicle.modelYear || '';
    document.getElementById('vehicle-license').value = vehicle.licensePlate || '';
    document.getElementById('vehicle-type').value = vehicle.vehicleType || '';
    document.getElementById('vehicle-color').value = vehicle.color || '';
    document.getElementById('vehicle-battery').value = vehicle.batteryCapacityKwh || '';
    document.getElementById('vehicle-power').value = vehicle.maxChargingPowerKw || '';
    document.getElementById('vehicle-nickname').value = vehicle.nickname || '';
    document.getElementById('vehicle-port-location').value = vehicle.chargePortLocation || '';
    document.getElementById('vehicle-notes').value = vehicle.notes || '';
    document.getElementById('vehicle-is-primary').checked = vehicle.isPrimary || false;
    
    const modal = new bootstrap.Modal(document.getElementById('vehicleModal'));
    modal.show();
}

async function handleSaveVehicle() {
    const form = document.getElementById('vehicleForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const yearValue = document.getElementById('vehicle-year').value.trim();
    const batteryValue = document.getElementById('vehicle-battery').value.trim();
    const powerValue = document.getElementById('vehicle-power').value.trim();
    const vehicleTypeValue = document.getElementById('vehicle-type').value.trim();
    
    // Validate required fields
    if (!vehicleTypeValue) {
        showError('Vui lòng chọn loại xe');
        return;
    }
    
    const vehicleData = {
        make: document.getElementById('vehicle-make').value.trim(),
        model: document.getElementById('vehicle-model').value.trim(),
        modelYear: yearValue ? parseInt(yearValue) : null,
        licensePlate: document.getElementById('vehicle-license').value.trim() || null,
        vehicleType: vehicleTypeValue, // Ensure it's not empty
        color: document.getElementById('vehicle-color').value.trim() || null,
        batteryCapacityKwh: batteryValue ? parseFloat(batteryValue) : null,
        maxChargingPowerKw: powerValue ? parseFloat(powerValue) : null,
        nickname: document.getElementById('vehicle-nickname').value.trim() || null,
        chargePortLocation: document.getElementById('vehicle-port-location').value || null,
        notes: document.getElementById('vehicle-notes').value.trim() || null,
        isPrimary: document.getElementById('vehicle-is-primary').checked
    };

    const btnSave = document.getElementById('btn-save-vehicle');
    btnSave.disabled = true;
    btnSave.textContent = 'Đang lưu...';

    try {
        let response;
        if (editingVehicleId) {
            response = await fetch(`/api/Vehicle/${editingVehicleId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify(vehicleData)
            });
        } else {
            response = await fetch('/api/Vehicle', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify(vehicleData)
            });
        }

        if (response.ok) {
            const modal = bootstrap.Modal.getInstance(document.getElementById('vehicleModal'));
            modal.hide();
            showSuccess(editingVehicleId ? 'Cập nhật xe thành công' : 'Thêm xe thành công');
            loadVehicles();
        } else {
            let errorMessage = 'Không thể lưu xe';
            try {
                const error = await response.json();
                if (error.message) {
                    errorMessage = error.message;
                } else if (error.errors) {
                    // Handle validation errors
                    const errorMessages = [];
                    for (const key in error.errors) {
                        if (error.errors[key]) {
                            errorMessages.push(error.errors[key].join(', '));
                        }
                    }
                    errorMessage = errorMessages.length > 0 ? errorMessages.join('; ') : errorMessage;
                } else if (typeof error === 'string') {
                    errorMessage = error;
                }
            } catch (e) {
                console.error('Error parsing error response:', e);
                errorMessage = `Lỗi ${response.status}: ${response.statusText}`;
            }
            showError(errorMessage);
        }
    } catch (error) {
        console.error('Error saving vehicle:', error);
        showError('Có lỗi xảy ra. Vui lòng thử lại.');
    } finally {
        btnSave.disabled = false;
        btnSave.textContent = 'Lưu';
    }
}

async function deleteVehicle(vehicleId) {
    if (!confirm('Bạn có chắc chắn muốn xóa xe này?')) {
        return;
    }

    try {
        const response = await fetch(`/api/Vehicle/${vehicleId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        if (response.ok || response.status === 204) {
            showSuccess('Đã xóa xe thành công');
            loadVehicles();
        } else {
            const error = await response.json();
            showError(error.message || 'Không thể xóa xe');
        }
    } catch (error) {
        console.error('Error deleting vehicle:', error);
        showError('Có lỗi xảy ra. Vui lòng thử lại.');
    }
}

async function setPrimaryVehicle(vehicleId) {
    try {
        const response = await fetch(`/api/Vehicle/${vehicleId}/primary`, {
            method: 'POST',
            credentials: 'include'
        });

        if (response.ok) {
            showSuccess('Đã đặt làm xe mặc định');
            loadVehicles();
        } else {
            showError('Không thể đặt xe mặc định');
        }
    } catch (error) {
        console.error('Error setting primary vehicle:', error);
        showError('Có lỗi xảy ra. Vui lòng thử lại.');
    }
}

function resetForm() {
    editingVehicleId = null;
    document.getElementById('vehicleForm').reset();
    document.getElementById('vehicle-id').value = '';
}

function getVehicleTypeLabel(type) {
    const labels = {
        'Car': 'Ô tô',
        'Motorcycle': 'Xe máy',
        'Van': 'Xe tải nhỏ',
        'Bus': 'Xe buýt',
        'Truck': 'Xe tải',
        'Unknown': 'Không xác định'
    };
    return labels[type] || type;
}

function getPortLocationLabel(location) {
    const labels = {
        'Front': 'Phía trước',
        'Rear': 'Phía sau',
        'Left': 'Bên trái',
        'Right': 'Bên phải',
        'Top': 'Phía trên'
    };
    return labels[location] || location;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showSuccess(message) {
    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-success alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        <i class="bi bi-check-circle"></i> ${escapeHtml(message)}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => alertDiv.remove(), 3000);
}

function showError(message) {
    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-danger alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        <i class="bi bi-exclamation-circle"></i> ${escapeHtml(message)}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => alertDiv.remove(), 5000);
}

