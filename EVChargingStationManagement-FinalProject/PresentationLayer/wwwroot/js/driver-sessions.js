// Driver Sessions JavaScript

let progressRefreshInterval = null;

document.addEventListener('DOMContentLoaded', function() {
    loadSessions();
});

// Clean up interval when page unloads
window.addEventListener('beforeunload', function() {
    if (progressRefreshInterval) {
        clearInterval(progressRefreshInterval);
    }
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
        if (s.status === 'InProgress') {
            // Try to calculate progress even if progress API data is not available
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
        if (s.status === 'InProgress') {
            const progress = calculateChargingProgress(s);
            updateCircularGauge(progressGaugeId(s.id), progress.percentage);
        }
    });
    
    // Clear existing interval if any
    if (progressRefreshInterval) {
        clearInterval(progressRefreshInterval);
        progressRefreshInterval = null;
    }
    
    // Set up auto-refresh for active sessions every 10 seconds
    const hasActiveSessions = sessionsWithProgress.some(s => s.status === 'InProgress');
    if (hasActiveSessions) {
        progressRefreshInterval = setInterval(async () => {
            // Find all active session gauges in the DOM
            const activeGauges = document.querySelectorAll('[id^="progress-gauge-"]');
            activeGauges.forEach(async (gaugeEl) => {
                const gaugeId = gaugeEl.id;
                const sessionId = gaugeId.replace('progress-gauge-', '');
                
                try {
                    const progressResponse = await fetch(`/api/ChargingSession/${sessionId}/progress`, {
                        credentials: 'include'
                    });
                    if (progressResponse.ok) {
                        const progress = await progressResponse.json();
                        // Create a minimal session object for calculation
                        const sessionObj = {
                            progress: progress,
                            reservationId: null, // Will be fetched if needed
                            scheduledStartTime: null,
                            scheduledEndTime: null,
                            sessionStartTime: null,
                            sessionEndTime: null,
                            energyDeliveredKwh: null,
                            energyRequestedKwh: null
                        };
                        const calculatedProgress = calculateChargingProgress(sessionObj);
                        updateCircularGauge(gaugeId, calculatedProgress.percentage);
                    }
                } catch (err) {
                    console.error(`Error refreshing progress for session ${sessionId}:`, err);
                }
            });
        }, 10000); // Refresh every 10 seconds
    }
}

function progressGaugeId(sessionId) {
    return `progress-gauge-${sessionId}`;
}

function calculateChargingProgress(session) {
    // Priority 1: Calculate based on SOC progress if available
    if (session.progress) {
        const initialSoc = session.progress.initialSocPercentage || 0;
        const currentSoc = session.progress.currentSocPercentage ?? initialSoc;
        const targetSoc = session.progress.targetSocPercentage || 100;
        
        // Calculate percentage based on SOC progress
        if (targetSoc > initialSoc) {
            const socProgress = ((currentSoc - initialSoc) / (targetSoc - initialSoc)) * 100;
            const percentage = Math.max(0, Math.min(100, socProgress));
            return { percentage: percentage, label: percentage.toFixed(0) + '%' };
        } else if (targetSoc === initialSoc && currentSoc >= targetSoc) {
            // Already at target
            return { percentage: 100, label: '100%' };
        }
    }
    
    // Priority 2: Calculate based on scheduled time if reservation exists
    if (session.reservationId && session.scheduledStartTime && session.scheduledEndTime) {
        const startTime = new Date(session.scheduledStartTime);
        const endTime = new Date(session.scheduledEndTime);
        const now = new Date();
        
        // Ensure times are valid
        if (isNaN(startTime.getTime()) || isNaN(endTime.getTime())) {
            return { percentage: 0, label: '0%' };
        }
        
        const totalDuration = endTime - startTime;
        if (totalDuration <= 0) {
            return { percentage: 100, label: '100%' };
        }
        
        const elapsed = now - startTime;
        const timeProgress = (elapsed / totalDuration) * 100;
        const percentage = Math.max(0, Math.min(100, timeProgress));
        return { percentage: percentage, label: percentage.toFixed(0) + '%' };
    }
    
    // Priority 3: Calculate based on actual session time if available
    if (session.sessionStartTime) {
        const startTime = new Date(session.sessionStartTime);
        const now = new Date();
        
        if (isNaN(startTime.getTime())) {
            return { percentage: 0, label: '0%' };
        }
        
        // If session has end time, calculate based on that
        if (session.sessionEndTime) {
            const endTime = new Date(session.sessionEndTime);
            if (!isNaN(endTime.getTime())) {
                const totalDuration = endTime - startTime;
                if (totalDuration > 0) {
                    const elapsed = now - startTime;
                    const timeProgress = (elapsed / totalDuration) * 100;
                    const percentage = Math.max(0, Math.min(100, timeProgress));
                    return { percentage: percentage, label: percentage.toFixed(0) + '%' };
                }
            }
        }
        
        // If no end time, estimate based on energy delivered vs requested
        if (session.energyDeliveredKwh != null && session.energyRequestedKwh != null && session.energyRequestedKwh > 0) {
            const energyProgress = (session.energyDeliveredKwh / session.energyRequestedKwh) * 100;
            const percentage = Math.max(0, Math.min(100, energyProgress));
            return { percentage: percentage, label: percentage.toFixed(0) + '%' };
        }
    }
    
    // Default: no progress data available
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

