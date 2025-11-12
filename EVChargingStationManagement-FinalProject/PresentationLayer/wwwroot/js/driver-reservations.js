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
        
        // Convert status to string để xử lý cả enum (số) và string
        // ReservationStatus enum: Pending=0, Confirmed=1, CheckedIn=2, Completed=3, Cancelled=4, Expired=5, NoShow=6
        const statusValue = String(r.status).toLowerCase();
        const statusNum = typeof r.status === 'number' ? r.status : 
                         (statusValue === 'pending' ? 0 :
                          statusValue === 'confirmed' ? 1 :
                          statusValue === 'checkedin' ? 2 :
                          statusValue === 'completed' ? 3 :
                          statusValue === 'cancelled' ? 4 :
                          statusValue === 'expired' ? 5 :
                          statusValue === 'noshow' ? 6 : -1);
        
        const status = getStatusBadge(r.status);
        
        // Check if can start charging (cho phép bắt đầu sớm nếu có reservation)
        const now = new Date();
        const scheduledStart = new Date(r.scheduledStartTime);
        // Cho phép bắt đầu nếu có reservation và có spot (có thể bắt đầu sớm)
        // So sánh cả số và string
        const isPending = statusNum === 0 || statusValue === 'pending';
        const isConfirmed = statusNum === 1 || statusValue === 'confirmed';
        const isCheckedIn = statusNum === 2 || statusValue === 'checkedin';
        const canStart = (isConfirmed || isPending) && r.chargingSpotId;
        
        // Check if reservation is upcoming (not yet started)
        const isUpcoming = now < scheduledStart && isConfirmed;

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
        
        // Hiển thị nút hủy cho các trạng thái có thể hủy (Pending, Confirmed, CheckedIn)
        if (isPending || isConfirmed || isCheckedIn) {
            actions += `
                <button class="btn btn-sm btn-outline-danger ms-2" onclick="cancelReservation('${r.id}', '${escapeHtml(stationName)}', '${escapeHtml(spotNumber)}')">
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
    // Xử lý cả enum (số) và string
    // ReservationStatus enum: Pending=0, Confirmed=1, CheckedIn=2, Completed=3, Cancelled=4, Expired=5, NoShow=6
    let statusKey = status;
    
    if (typeof status === 'number') {
        const statusMap = {
            0: 'Pending',
            1: 'Confirmed',
            2: 'CheckedIn',
            3: 'Completed',
            4: 'Cancelled',
            5: 'Expired',
            6: 'NoShow'
        };
        statusKey = statusMap[status] || status;
    } else {
        // Convert string to proper case
        const statusStr = String(status);
        if (statusStr === '0' || statusStr.toLowerCase() === 'pending') statusKey = 'Pending';
        else if (statusStr === '1' || statusStr.toLowerCase() === 'confirmed') statusKey = 'Confirmed';
        else if (statusStr === '2' || statusStr.toLowerCase() === 'checkedin') statusKey = 'CheckedIn';
        else if (statusStr === '3' || statusStr.toLowerCase() === 'completed') statusKey = 'Completed';
        else if (statusStr === '4' || statusStr.toLowerCase() === 'cancelled') statusKey = 'Cancelled';
        else if (statusStr === '5' || statusStr.toLowerCase() === 'expired') statusKey = 'Expired';
        else if (statusStr === '6' || statusStr.toLowerCase() === 'noshow') statusKey = 'NoShow';
    }
    
    const badges = {
        'Pending': '<span class="badge bg-warning">Chờ xác nhận</span>',
        'Confirmed': '<span class="badge bg-success">Đã xác nhận</span>',
        'CheckedIn': '<span class="badge bg-primary">Đã check-in</span>',
        'Completed': '<span class="badge bg-info">Hoàn thành</span>',
        'Cancelled': '<span class="badge bg-secondary">Đã hủy</span>',
        'Expired': '<span class="badge bg-dark">Hết hạn</span>',
        'NoShow': '<span class="badge bg-danger">Không đến</span>'
    };
    return badges[statusKey] || `<span class="badge bg-secondary">${status}</span>`;
}

async function cancelReservation(reservationId, stationName = '', spotNumber = '') {
    // Thông báo xác nhận chi tiết hơn
    const confirmMessage = stationName && spotNumber 
        ? `Bạn có chắc chắn muốn hủy đặt chỗ tại trạm "${stationName}" - Cổng ${spotNumber}?\n\nChỗ sạc sẽ được trả về trống ngay lập tức.`
        : 'Bạn có chắc chắn muốn hủy đặt chỗ này?\n\nChỗ sạc sẽ được trả về trống ngay lập tức.';
    
    if (!confirm(confirmMessage)) {
        return;
    }

    // Tìm button đang được click để hiển thị loading state
    const buttons = document.querySelectorAll(`button[onclick*="cancelReservation('${reservationId}'"]`);
    const originalButtons = [];
    buttons.forEach(btn => {
        originalButtons.push({
            element: btn,
            originalHTML: btn.innerHTML,
            originalDisabled: btn.disabled
        });
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang hủy...';
    });

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
            // Show success message với thông tin chi tiết
            const alertDiv = document.createElement('div');
            alertDiv.className = 'alert alert-success alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
            alertDiv.style.zIndex = '9999';
            alertDiv.style.minWidth = '300px';
            alertDiv.innerHTML = `
                <div class="d-flex align-items-center">
                    <i class="bi bi-check-circle-fill me-2" style="font-size: 1.2rem;"></i>
                    <div>
                        <strong>Đã hủy đặt chỗ thành công!</strong>
                        <div class="small">Chỗ sạc đã được trả về trống.</div>
                    </div>
                    <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            `;
            document.body.appendChild(alertDiv);
            setTimeout(() => {
                if (alertDiv.parentNode) {
                    alertDiv.remove();
                }
            }, 5000);
            
            // Reload danh sách reservations
            await loadReservations();
        } else {
            // Khôi phục button
            originalButtons.forEach(btn => {
                btn.element.disabled = btn.originalDisabled;
                btn.element.innerHTML = btn.originalHTML;
            });

            const errorData = await response.json().catch(() => ({}));
            const errorMessage = errorData.message || 'Không thể hủy đặt chỗ';
            
            // Show error message
            const errorAlertDiv = document.createElement('div');
            errorAlertDiv.className = 'alert alert-danger alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
            errorAlertDiv.style.zIndex = '9999';
            errorAlertDiv.style.minWidth = '300px';
            errorAlertDiv.innerHTML = `
                <div class="d-flex align-items-center">
                    <i class="bi bi-exclamation-triangle-fill me-2" style="font-size: 1.2rem;"></i>
                    <div>
                        <strong>Lỗi!</strong>
                        <div class="small">${escapeHtml(errorMessage)}</div>
                    </div>
                    <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            `;
            document.body.appendChild(errorAlertDiv);
            setTimeout(() => {
                if (errorAlertDiv.parentNode) {
                    errorAlertDiv.remove();
                }
            }, 5000);
        }
    } catch (error) {
        console.error('Error cancelling reservation:', error);
        
        // Khôi phục button
        originalButtons.forEach(btn => {
            btn.element.disabled = btn.originalDisabled;
            btn.element.innerHTML = btn.originalHTML;
        });

        // Show error message
        const errorAlertDiv = document.createElement('div');
        errorAlertDiv.className = 'alert alert-danger alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3';
        errorAlertDiv.style.zIndex = '9999';
        errorAlertDiv.style.minWidth = '300px';
        errorAlertDiv.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi bi-exclamation-triangle-fill me-2" style="font-size: 1.2rem;"></i>
                <div>
                    <strong>Lỗi!</strong>
                    <div class="small">Có lỗi xảy ra. Vui lòng thử lại.</div>
                </div>
                <button type="button" class="btn-close ms-auto" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        document.body.appendChild(errorAlertDiv);
        setTimeout(() => {
            if (errorAlertDiv.parentNode) {
                errorAlertDiv.remove();
            }
        }, 5000);
    }
}

function resetFilters() {
    document.getElementById('statusFilter').value = '';
    loadReservations();
}

