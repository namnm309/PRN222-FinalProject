// Driver Sessions JavaScript

document.addEventListener('DOMContentLoaded', function() {
    loadSessions();
});

async function loadSessions() {
    const statusFilter = document.getElementById('statusFilter').value;
    const dateFrom = document.getElementById('dateFrom').value;
    const dateTo = document.getElementById('dateTo').value;

    try {
        let url = '/api/ChargingSession/me?limit=100';
        const response = await fetch(url, {
            credentials: 'include'
        });

        if (response.ok) {
            let sessions = await response.json();
            
            // Apply filters
            if (statusFilter) {
                sessions = sessions.filter(s => s.status === statusFilter);
            }
            if (dateFrom) {
                const fromDate = new Date(dateFrom);
                sessions = sessions.filter(s => new Date(s.sessionStartTime) >= fromDate);
            }
            if (dateTo) {
                const toDate = new Date(dateTo);
                toDate.setHours(23, 59, 59, 999);
                sessions = sessions.filter(s => new Date(s.sessionStartTime) <= toDate);
            }

            displaySessions(sessions);
        } else {
            document.getElementById('sessions-table-body').innerHTML = 
                '<tr><td colspan="7" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
        }
    } catch (error) {
        console.error('Error loading sessions:', error);
        document.getElementById('sessions-table-body').innerHTML = 
            '<tr><td colspan="7" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
    }
}

function displaySessions(sessions) {
    const tbody = document.getElementById('sessions-table-body');
    
    if (sessions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Không có phiên sạc nào</td></tr>';
        return;
    }

    tbody.innerHTML = sessions.map(s => {
        const startTime = new Date(s.sessionStartTime).toLocaleString('vi-VN');
        const stationName = s.chargingStationName || 'N/A';
        const spotNumber = s.chargingSpotNumber || 'N/A';
        const energy = s.energyDeliveredKwh ? s.energyDeliveredKwh.toFixed(2) : '-';
        const cost = s.cost ? new Intl.NumberFormat('vi-VN').format(s.cost) + ' VND' : '-';
        const status = getStatusBadge(s.status);
        
        return `
            <tr>
                <td>${startTime}</td>
                <td>${stationName}</td>
                <td>${spotNumber}</td>
                <td>${energy}</td>
                <td>${cost}</td>
                <td>${status}</td>
                <td>
                    <a href="/Driver/SessionDetail/${s.id}" class="btn btn-sm btn-outline-primary">Xem</a>
                </td>
            </tr>
        `;
    }).join('');
}

function getStatusBadge(status) {
    const badges = {
        'InProgress': '<span class="badge bg-warning">Đang sạc</span>',
        'Completed': '<span class="badge bg-success">Hoàn thành</span>',
        'Cancelled': '<span class="badge bg-secondary">Đã hủy</span>',
        'Failed': '<span class="badge bg-danger">Thất bại</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

function resetFilters() {
    document.getElementById('statusFilter').value = '';
    document.getElementById('dateFrom').value = '';
    document.getElementById('dateTo').value = '';
    loadSessions();
}

