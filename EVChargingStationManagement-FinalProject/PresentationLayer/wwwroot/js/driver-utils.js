// Driver Utilities JavaScript

// Format SOC percentage
function formatSocPercentage(soc) {
    if (soc == null || soc === undefined) return '--';
    return soc.toFixed(1) + '%';
}

// Format energy (kWh)
function formatEnergy(energy) {
    if (energy == null || energy === undefined) return '0 kWh';
    return energy.toFixed(2) + ' kWh';
}

// Format time remaining
function formatTimeRemaining(minutes) {
    if (minutes == null || minutes === undefined) return '--';
    if (minutes <= 0) return 'Hoàn thành';
    
    const hours = Math.floor(minutes / 60);
    const mins = Math.floor(minutes % 60);
    
    if (hours > 0) {
        return `${hours}h ${mins}m`;
    }
    return `${mins}m`;
}

// Format currency (VND)
function formatCurrency(amount) {
    if (amount == null || amount === undefined) return '0 VND';
    return new Intl.NumberFormat('vi-VN').format(amount) + ' VND';
}

// Format date time
function formatDateTime(dateString) {
    if (!dateString) return '--';
    return new Date(dateString).toLocaleString('vi-VN');
}

// Get status badge HTML
function getStatusBadge(status) {
    const badges = {
        'InProgress': '<span class="badge bg-warning">Đang sạc</span>',
        'Completed': '<span class="badge bg-success">Hoàn thành</span>',
        'Cancelled': '<span class="badge bg-secondary">Đã hủy</span>',
        'Failed': '<span class="badge bg-danger">Thất bại</span>',
        'Pending': '<span class="badge bg-warning">Chờ xác nhận</span>',
        'Confirmed': '<span class="badge bg-success">Đã xác nhận</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

// Get payment status badge HTML
function getPaymentStatusBadge(status) {
    const badges = {
        'Pending': '<span class="badge bg-warning">Chờ thanh toán</span>',
        'Captured': '<span class="badge bg-success">Đã thanh toán</span>',
        'Failed': '<span class="badge bg-danger">Thất bại</span>',
        'Refunded': '<span class="badge bg-info">Đã hoàn tiền</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

// Calculate distance between two coordinates (Haversine formula)
function calculateDistanceKm(lat1, lon1, lat2, lon2) {
    const R = 6371; // Earth's radius in kilometers
    const dLat = toRadians(lat2 - lat1);
    const dLon = toRadians(lon2 - lon1);

    const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
              Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
              Math.sin(dLon / 2) * Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
}

function toRadians(degrees) {
    return degrees * Math.PI / 180;
}

// Format distance
function formatDistance(km) {
    if (km == null || km === undefined) return '--';
    if (km < 1) {
        return Math.round(km * 1000) + ' m';
    }
    return km.toFixed(1) + ' km';
}

