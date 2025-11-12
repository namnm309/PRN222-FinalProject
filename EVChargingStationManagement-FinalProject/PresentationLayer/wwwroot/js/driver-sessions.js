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

            await displaySessions(sessions);
        } else {
            document.getElementById('sessions-table-body').innerHTML = 
                '<tr><td colspan="8" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
        }
    } catch (error) {
        console.error('Error loading sessions:', error);
        document.getElementById('sessions-table-body').innerHTML = 
            '<tr><td colspan="8" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
    }
}

async function displaySessions(sessions) {
    const tbody = document.getElementById('sessions-table-body');
    
    if (sessions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted">Không có phiên sạc nào</td></tr>';
        return;
    }

    // Load progress for each session
    const sessionsWithProgress = await Promise.all(sessions.map(async (s) => {
        if (s.status === 'InProgress') {
            try {
                const progressResponse = await fetch(`/api/ChargingSession/${s.id}/progress`, {
                    credentials: 'include'
                });
                if (progressResponse.ok) {
                    s.progress = await progressResponse.json();
                }
            } catch (err) {
                console.error(`Error loading progress for session ${s.id}:`, err);
            }
        }
        return s;
    }));

    tbody.innerHTML = sessionsWithProgress.map(s => {
        const startTime = new Date(s.sessionStartTime).toLocaleString('vi-VN');
        const stationName = s.chargingStationName || 'N/A';
        const spotNumber = s.chargingSpotNumber || 'N/A';
        const energy = s.energyDeliveredKwh ? s.energyDeliveredKwh.toFixed(2) : '-';
        const cost = s.cost ? new Intl.NumberFormat('vi-VN').format(s.cost) + ' VND' : '-';
        const status = getStatusBadge(s.status);
        
        // Calculate charging progress percentage
        let progressGauge = '-';
        if (s.status === 'InProgress' && s.progress) {
            const progress = calculateChargingProgress(s);
            progressGauge = createCircularGauge(progress.percentage, progressGaugeId(s.id));
        } else if (s.status === 'Completed') {
            progressGauge = createCircularGauge(100, progressGaugeId(s.id));
        }
        
        return `
            <tr>
                <td>${startTime}</td>
                <td>${stationName}</td>
                <td>${spotNumber}</td>
                <td>${progressGauge}</td>
                <td>${energy}</td>
                <td>${cost}</td>
                <td>${status}</td>
                <td>
                    <a href="/Driver/SessionDetail/${s.id}" class="btn btn-sm btn-outline-primary">Xem</a>
                </td>
            </tr>
        `;
    }).join('');
    
    // Update gauges for active sessions
    sessionsWithProgress.forEach(s => {
        if (s.status === 'InProgress' && s.progress) {
            const progress = calculateChargingProgress(s);
            updateCircularGauge(progressGaugeId(s.id), progress.percentage);
        }
    });
}

function progressGaugeId(sessionId) {
    return `progress-gauge-${sessionId}`;
}

function calculateChargingProgress(session) {
    if (!session.progress) {
        return { percentage: 0, label: '0%' };
    }
    
    const initialSoc = session.progress.initialSocPercentage || 0;
    const currentSoc = session.progress.currentSocPercentage || initialSoc;
    const targetSoc = session.progress.targetSocPercentage || 100;
    
    // Calculate percentage based on SOC progress
    if (targetSoc > initialSoc) {
        const socProgress = ((currentSoc - initialSoc) / (targetSoc - initialSoc)) * 100;
        const percentage = Math.max(0, Math.min(100, socProgress));
        return { percentage: percentage, label: percentage.toFixed(1) + '%' };
    }
    
    // Fallback: calculate based on time if reservation exists
    if (session.reservationId && session.scheduledStartTime && session.scheduledEndTime) {
        const startTime = new Date(session.scheduledStartTime);
        const endTime = new Date(session.scheduledEndTime);
        const now = new Date();
        const totalDuration = endTime - startTime;
        const elapsed = now - startTime;
        const timeProgress = (elapsed / totalDuration) * 100;
        const percentage = Math.max(0, Math.min(100, timeProgress));
        return { percentage: percentage, label: percentage.toFixed(1) + '%' };
    }
    
    return { percentage: 0, label: '0%' };
}

function createCircularGauge(percentage, id) {
    const size = 60;
    const strokeWidth = 6;
    const radius = (size - strokeWidth) / 2;
    const circumference = 2 * Math.PI * radius;
    const offset = circumference - (percentage / 100) * circumference;
    const color = percentage >= 100 ? '#10b981' : percentage >= 50 ? '#3b82f6' : '#f59e0b';
    
    return `
        <div style="position: relative; display: inline-flex; align-items: center; justify-content: center; width: ${size}px; height: ${size}px;">
            <svg width="${size}" height="${size}" style="transform: rotate(-90deg); position: absolute;">
                <circle
                    cx="${size/2}"
                    cy="${size/2}"
                    r="${radius}"
                    fill="none"
                    stroke="#e5e7eb"
                    stroke-width="${strokeWidth}"
                />
                <circle
                    id="${id}"
                    cx="${size/2}"
                    cy="${size/2}"
                    r="${radius}"
                    fill="none"
                    stroke="${color}"
                    stroke-width="${strokeWidth}"
                    stroke-dasharray="${circumference}"
                    stroke-dashoffset="${offset}"
                    stroke-linecap="round"
                    style="transition: stroke-dashoffset 0.3s ease;"
                />
            </svg>
            <div id="${id}-label" style="position: absolute; font-size: 11px; font-weight: 600; color: ${color}; text-align: center; line-height: ${size}px; width: ${size}px;">
                ${percentage.toFixed(0)}%
            </div>
        </div>
    `;
}

function updateCircularGauge(id, percentage) {
    const gauge = document.getElementById(id);
    if (!gauge) return;
    
    const size = 60;
    const strokeWidth = 6;
    const radius = (size - strokeWidth) / 2;
    const circumference = 2 * Math.PI * radius;
    const offset = circumference - (percentage / 100) * circumference;
    const color = percentage >= 100 ? '#10b981' : percentage >= 50 ? '#3b82f6' : '#f59e0b';
    
    gauge.style.strokeDashoffset = offset;
    gauge.style.stroke = color;
    
    // Update label
    const label = document.getElementById(id + '-label');
    if (label) {
        label.textContent = percentage.toFixed(0) + '%';
        label.style.color = color;
    }
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

