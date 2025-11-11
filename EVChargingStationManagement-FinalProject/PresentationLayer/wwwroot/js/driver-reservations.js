// Driver Reservations JavaScript

document.addEventListener('DOMContentLoaded', function() {
    loadReservations();
});

async function loadReservations() {
    const statusFilter = document.getElementById('statusFilter').value;

    try {
        const response = await fetch('/api/Reservation/me', {
            credentials: 'include'
        });

        if (response.ok) {
            let reservations = await response.json();
            
            // Apply filter
            if (statusFilter) {
                reservations = reservations.filter(r => r.status === statusFilter);
            }

            // Sort by scheduled start time
            reservations.sort((a, b) => new Date(a.scheduledStartTime) - new Date(b.scheduledStartTime));

            displayReservations(reservations);
        } else {
            document.getElementById('reservations-table-body').innerHTML = 
                '<tr><td colspan="6" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
        }
    } catch (error) {
        console.error('Error loading reservations:', error);
        document.getElementById('reservations-table-body').innerHTML = 
            '<tr><td colspan="6" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
    }
}

function displayReservations(reservations) {
    const tbody = document.getElementById('reservations-table-body');
    const countEl = document.getElementById('reservation-count');
    
    if (countEl) {
        countEl.textContent = reservations.length;
    }
    
    if (reservations.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center py-5">
                    <div class="text-muted">
                        <i class="bi bi-calendar-x" style="font-size: 3rem; opacity: 0.3;"></i>
                        <p class="mt-3 mb-0">Không có đặt chỗ nào</p>
                        <small>Hãy đặt chỗ trạm sạc từ trang chủ</small>
                    </div>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = reservations.map(r => {
        const startTime = new Date(r.scheduledStartTime);
        const endTime = new Date(r.scheduledEndTime);
        const startTimeStr = startTime.toLocaleString('vi-VN', { 
            day: '2-digit', 
            month: '2-digit', 
            year: 'numeric',
            hour: '2-digit', 
            minute: '2-digit' 
        });
        const endTimeStr = endTime.toLocaleString('vi-VN', { 
            hour: '2-digit', 
            minute: '2-digit' 
        });
        const stationName = r.chargingStationName || 'N/A';
        const spotNumber = r.chargingSpotNumber || 'N/A';
        const vehicleName = r.vehicleName || 'N/A';
        const status = getStatusBadge(r.status);
        
        // Check if can start charging (cho phép bắt đầu sớm nếu có reservation)
        const now = new Date();
        const scheduledStart = new Date(r.scheduledStartTime);
        // Cho phép bắt đầu nếu có reservation và có spot (có thể bắt đầu sớm)
        const canStart = (r.status === 'Confirmed' || r.status === 'Pending') && r.chargingSpotId;
        
        // Check if reservation is upcoming (not yet started)
        const isUpcoming = now < scheduledStart && r.status === 'Confirmed';

        let actions = '';
        if (canStart) {
            actions = `
                <a href="/Driver/StartCharging?reservationId=${r.id}" class="btn btn-sm btn-success">
                    <i class="bi bi-lightning-charge"></i> Bắt đầu sạc
                </a>
            `;
        } else if (isUpcoming) {
            const timeUntil = Math.ceil((scheduledStart - now) / (1000 * 60)); // minutes
            if (timeUntil <= 15) {
                actions = `
                    <span class="badge bg-info">Có thể bắt đầu trong ${timeUntil} phút</span>
                `;
            } else {
                actions = `
                    <span class="badge bg-secondary">Chưa đến giờ</span>
                `;
            }
        }
        
        if (r.status === 'Pending' || r.status === 'Confirmed') {
            actions += `
                <button class="btn btn-sm btn-outline-danger ms-2" onclick="cancelReservation('${r.id}')">
                    <i class="bi bi-x-circle"></i> Hủy
                </button>
            `;
        }

        return `
            <tr>
                <td>
                    <div class="fw-semibold">${startTimeStr}</div>
                    <small class="text-muted">
                        <i class="bi bi-clock"></i> Đến: ${endTimeStr}
                    </small>
                </td>
                <td>
                    <div class="fw-semibold">${escapeHtml(stationName)}</div>
                </td>
                <td>
                    <span class="badge bg-light text-dark">${escapeHtml(spotNumber)}</span>
                </td>
                <td>${escapeHtml(vehicleName)}</td>
                <td>${status}</td>
                <td>
                    <div class="d-flex align-items-center gap-2">
                        ${actions || '<span class="text-muted">-</span>'}
                    </div>
                </td>
            </tr>
        `;
    }).join('');
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function getStatusBadge(status) {
    const badges = {
        'Pending': '<span class="badge bg-warning">Chờ xác nhận</span>',
        'Confirmed': '<span class="badge bg-success">Đã xác nhận</span>',
        'Completed': '<span class="badge bg-info">Hoàn thành</span>',
        'Cancelled': '<span class="badge bg-secondary">Đã hủy</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

async function cancelReservation(reservationId) {
    if (!confirm('Bạn có chắc chắn muốn hủy đặt chỗ này?')) {
        return;
    }

    try {
        const response = await fetch(`/api/Reservation/${reservationId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                notes: 'Hủy bởi người dùng'
            })
        });

        if (response.ok || response.status === 204) {
            // Show success message
            const alertDiv = document.createElement('div');
            alertDiv.className = 'alert alert-success alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
            alertDiv.style.zIndex = '9999';
            alertDiv.innerHTML = `
                <i class="bi bi-check-circle"></i> Đã hủy đặt chỗ thành công
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            document.body.appendChild(alertDiv);
            setTimeout(() => alertDiv.remove(), 3000);
            
            loadReservations();
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể hủy đặt chỗ'));
        }
    } catch (error) {
        console.error('Error cancelling reservation:', error);
        alert('Có lỗi xảy ra. Vui lòng thử lại.');
    }
}

function resetFilters() {
    document.getElementById('statusFilter').value = '';
    loadReservations();
}

