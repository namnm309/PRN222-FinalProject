// Driver Dashboard JavaScript

document.addEventListener('DOMContentLoaded', function() {
    loadActiveSession();
    loadUpcomingReservations();
    loadRecentSessions();
    loadStats();
});

async function loadActiveSession() {
    try {
        const response = await fetch('/api/ChargingSession/me/active', {
            credentials: 'include'
        });

        if (response.ok) {
            const session = await response.json();
            displayActiveSession(session);
        } else if (response.status === 404) {
            document.getElementById('active-session-panel').style.display = 'none';
        }
    } catch (error) {
        console.error('Error loading active session:', error);
        document.getElementById('active-session-panel').style.display = 'none';
    }
}

function displayActiveSession(session) {
    const panel = document.getElementById('active-session-panel');
    const content = document.getElementById('active-session-content');
    
    panel.style.display = 'block';
    
    const stationName = session.chargingStationName || 'N/A';
    const spotNumber = session.chargingSpotNumber || 'N/A';
    const startTime = new Date(session.sessionStartTime).toLocaleString('vi-VN');
    
    content.innerHTML = `
        <div class="session-card">
            <div class="session-info">
                <h4>${stationName}</h4>
                <p>Cổng sạc: ${spotNumber}</p>
                <p>Bắt đầu: ${startTime}</p>
            </div>
            <div class="session-actions">
                <a href="/Driver/SessionDetail/${session.id}" class="btn btn-primary">Xem chi tiết</a>
            </div>
        </div>
    `;
}

async function loadUpcomingReservations() {
    try {
        const response = await fetch('/api/Reservation/me?from=' + new Date().toISOString(), {
            credentials: 'include'
        });

        if (response.ok) {
            const reservations = await response.json();
            displayUpcomingReservations(reservations.filter(r => 
                r.status === 'Confirmed' || r.status === 'Pending'
            ).slice(0, 5));
        }
    } catch (error) {
        console.error('Error loading reservations:', error);
        document.getElementById('upcoming-reservations').innerHTML = 
            '<div class="text-center text-muted">Không thể tải dữ liệu</div>';
    }
}

function displayUpcomingReservations(reservations) {
    const container = document.getElementById('upcoming-reservations');
    
    if (reservations.length === 0) {
        container.innerHTML = '<div class="text-center text-muted">Không có đặt chỗ sắp tới</div>';
        return;
    }

    container.innerHTML = reservations.map(r => {
        const startTime = new Date(r.scheduledStartTime).toLocaleString('vi-VN');
        const stationName = r.chargingStationName || 'N/A';
        return `
            <div class="reservation-item">
                <div>
                    <strong>${stationName}</strong>
                    <p class="text-muted small">${startTime}</p>
                </div>
                <a href="/Driver/Reservations" class="btn btn-sm btn-outline-primary">Xem</a>
            </div>
        `;
    }).join('');
}

async function loadRecentSessions() {
    try {
        const response = await fetch('/api/ChargingSession/me?limit=5', {
            credentials: 'include'
        });

        if (response.ok) {
            const sessions = await response.json();
            displayRecentSessions(sessions);
        }
    } catch (error) {
        console.error('Error loading sessions:', error);
        document.getElementById('recent-sessions').innerHTML = 
            '<div class="text-center text-muted">Không thể tải dữ liệu</div>';
    }
}

function displayRecentSessions(sessions) {
    const container = document.getElementById('recent-sessions');
    
    if (sessions.length === 0) {
        container.innerHTML = '<div class="text-center text-muted">Chưa có phiên sạc nào</div>';
        return;
    }

    container.innerHTML = sessions.map(s => {
        const startTime = new Date(s.sessionStartTime).toLocaleString('vi-VN');
        const stationName = s.chargingStationName || 'N/A';
        const status = s.status;
        const cost = s.cost ? new Intl.NumberFormat('vi-VN').format(s.cost) + ' VND' : 'Chưa thanh toán';
        
        return `
            <div class="session-item">
                <div>
                    <strong>${stationName}</strong>
                    <p class="text-muted small">${startTime} - ${status}</p>
                    <p class="text-muted small">${cost}</p>
                </div>
                <a href="/Driver/SessionDetail/${s.id}" class="btn btn-sm btn-outline-primary">Xem</a>
            </div>
        `;
    }).join('');
}

async function loadStats() {
    try {
        const response = await fetch('/api/ChargingSession/me?limit=100', {
            credentials: 'include'
        });

        if (response.ok) {
            const sessions = await response.json();
            calculateStats(sessions);
        }
    } catch (error) {
        console.error('Error loading stats:', error);
    }
}

function calculateStats(sessions) {
    const totalSessions = sessions.length;
    const totalEnergy = sessions.reduce((sum, s) => sum + (s.energyDeliveredKwh || 0), 0);
    const totalCost = sessions.reduce((sum, s) => sum + (s.cost || 0), 0);

    document.getElementById('total-sessions').textContent = totalSessions;
    document.getElementById('total-energy').textContent = totalEnergy.toFixed(2);
    document.getElementById('total-cost').textContent = new Intl.NumberFormat('vi-VN').format(totalCost);
}

