// Staff Sessions Management JavaScript
let allSessions = [];
let filteredSessions = [];
let currentPage = 1;
const itemsPerPage = 10;

// Load data on page load
document.addEventListener('DOMContentLoaded', function() {
    loadStations();
    loadUsers();
    loadSessions();
    updateStats();
    
    // Auto refresh every 30 seconds
    setInterval(function() {
        loadSessions();
        updateStats();
    }, 30000);
});

// Load stations for filter and modal
async function loadStations() {
    try {
        const response = await fetch('/api/ChargingStation');
        if (!response.ok) throw new Error('Failed to fetch stations');
        
        const stations = await response.json();
        
        // Populate filter dropdown
        const filterSelect = document.getElementById('filterStation');
        filterSelect.innerHTML = '<option value="">Tất cả trạm</option>';
        stations.forEach(station => {
            filterSelect.innerHTML += `<option value="${station.id}">${station.name}</option>`;
        });
        
        // Populate modal dropdown
        const modalSelect = document.getElementById('stationId');
        modalSelect.innerHTML = '<option value="">Chọn trạm sạc</option>';
        stations.forEach(station => {
            modalSelect.innerHTML += `<option value="${station.id}">${station.name}</option>`;
        });
    } catch (error) {
        console.error('Error loading stations:', error);
    }
}

// Load users for modal
async function loadUsers() {
    try {
        // In a real app, you'd have an API endpoint for users
        // For now, we'll just show a placeholder
        const userSelect = document.getElementById('userId');
        userSelect.innerHTML = '<option value="">Chọn khách hàng</option>';
        // userSelect.innerHTML += `<option value="user-id">Khách hàng mẫu</option>`;
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

// Load available spots when station is selected
async function loadAvailableSpots() {
    const stationId = document.getElementById('stationId').value;
    const spotSelect = document.getElementById('spotId');
    
    if (!stationId) {
        spotSelect.innerHTML = '<option value="">Chọn trạm sạc trước</option>';
        return;
    }
    
    try {
        const response = await fetch(`/api/ChargingSpot/station/${stationId}/available`);
        if (!response.ok) throw new Error('Failed to fetch spots');
        
        const spots = await response.json();
        spotSelect.innerHTML = '<option value="">Chọn điểm sạc</option>';
        
        if (spots.length === 0) {
            spotSelect.innerHTML = '<option value="">Không có điểm sạc khả dụng</option>';
            return;
        }
        
        spots.forEach(spot => {
            const powerInfo = spot.powerOutput ? ` - ${spot.powerOutput}kW` : '';
            const priceInfo = spot.pricePerKwh ? ` - ${spot.pricePerKwh}đ/kWh` : '';
            spotSelect.innerHTML += `<option value="${spot.id}">${spot.spotNumber}${powerInfo}${priceInfo}</option>`;
        });
    } catch (error) {
        console.error('Error loading spots:', error);
        spotSelect.innerHTML = '<option value="">Lỗi tải dữ liệu</option>';
    }
}

// Load all sessions
async function loadSessions() {
    try {
        const response = await fetch('/api/ChargingSession');
        if (!response.ok) throw new Error('Failed to fetch sessions');
        
        allSessions = await response.json();
        filteredSessions = [...allSessions];
        applyFilters();
    } catch (error) {
        console.error('Error loading sessions:', error);
        showError('Không thể tải danh sách phiên sạc');
    }
}

// Apply filters
function applyFilters() {
    let filtered = [...allSessions];
    
    // Status filter
    const statusFilter = document.getElementById('filterStatus').value;
    if (statusFilter !== '') {
        filtered = filtered.filter(s => s.status == statusFilter);
    }
    
    // Station filter
    const stationFilter = document.getElementById('filterStation').value;
    if (stationFilter !== '') {
        filtered = filtered.filter(s => s.chargingStationId === stationFilter);
    }
    
    // Date filters
    const fromDate = document.getElementById('filterFromDate').value;
    const toDate = document.getElementById('filterToDate').value;
    
    if (fromDate) {
        filtered = filtered.filter(s => new Date(s.startTime) >= new Date(fromDate));
    }
    
    if (toDate) {
        const endDate = new Date(toDate);
        endDate.setHours(23, 59, 59);
        filtered = filtered.filter(s => new Date(s.startTime) <= endDate);
    }
    
    filteredSessions = filtered;
    currentPage = 1;
    renderTable();
}

// Clear filters
function clearFilters() {
    document.getElementById('filterStatus').value = '';
    document.getElementById('filterStation').value = '';
    document.getElementById('filterFromDate').value = '';
    document.getElementById('filterToDate').value = '';
    document.getElementById('searchInput').value = '';
    applyFilters();
}

// Search sessions
function searchSessions() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    
    if (!searchTerm) {
        applyFilters();
        return;
    }
    
    filteredSessions = allSessions.filter(s => 
        (s.userFullName && s.userFullName.toLowerCase().includes(searchTerm)) ||
        (s.userName && s.userName.toLowerCase().includes(searchTerm)) ||
        (s.chargingStationName && s.chargingStationName.toLowerCase().includes(searchTerm)) ||
        (s.chargingSpotNumber && s.chargingSpotNumber.toLowerCase().includes(searchTerm))
    );
    
    currentPage = 1;
    renderTable();
}

// Render table
function renderTable() {
    const container = document.getElementById('tableContainer');
    
    if (filteredSessions.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-icon">⚡</div>
                <div class="empty-text">Không tìm thấy phiên sạc nào</div>
                <div class="empty-subtext">Thử thay đổi bộ lọc hoặc tìm kiếm</div>
            </div>
        `;
        document.getElementById('paginationContainer').style.display = 'none';
        return;
    }
    
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    const pageSessions = filteredSessions.slice(startIndex, endIndex);
    
    let tableHTML = `
        <table class="sessions-table">
            <thead>
                <tr>
                    <th>Khách hàng</th>
                    <th>Trạm sạc</th>
                    <th>Điểm sạc</th>
                    <th>Thời gian</th>
                    <th>Năng lượng (kWh)</th>
                    <th>Chi phí</th>
                    <th>Trạng thái</th>
                    <th>Thao tác</th>
                </tr>
            </thead>
            <tbody>
    `;
    
    pageSessions.forEach(session => {
        const startTime = new Date(session.startTime);
        const duration = session.endTime 
            ? Math.floor((new Date(session.endTime) - startTime) / 60000)
            : Math.floor((new Date() - startTime) / 60000);
        
        const initials = session.userFullName 
            ? session.userFullName.split(' ').map(n => n[0]).join('')
            : 'U';
        
        const statusClass = getStatusClass(session.status);
        const statusText = getStatusText(session.status);
        
        const actions = getActionButtons(session);
        
        tableHTML += `
            <tr>
                <td>
                    <div class="user-cell">
                        <div class="user-avatar">${initials}</div>
                        <div>
                            <div class="user-name">${session.userFullName || 'Unknown'}</div>
                            <div class="user-username">@${session.userName || 'unknown'}</div>
                        </div>
                    </div>
                </td>
                <td>${session.chargingStationName || 'N/A'}</td>
                <td><strong>${session.chargingSpotNumber || 'N/A'}</strong></td>
                <td>
                    <div>${startTime.toLocaleDateString('vi-VN')}</div>
                    <div style="font-size: 12px; color: #6B7280;">${startTime.toLocaleTimeString('vi-VN')} (${duration} phút)</div>
                </td>
                <td><strong>${session.energyConsumed.toFixed(2)}</strong></td>
                <td><strong>${session.totalCost.toLocaleString('vi-VN')}đ</strong></td>
                <td><span class="status-badge status-${statusClass}">${statusText}</span></td>
                <td>
                    <div class="action-buttons">
                        ${actions}
                    </div>
                </td>
            </tr>
        `;
    });
    
    tableHTML += `
            </tbody>
        </table>
    `;
    
    container.innerHTML = tableHTML;
    
    // Update pagination
    updatePagination();
}

function getStatusClass(status) {
    const classes = ['active', 'completed', 'paused', 'cancelled', 'error'];
    return classes[status] || 'active';
}

function getStatusText(status) {
    const texts = ['Đang sạc', 'Hoàn thành', 'Tạm dừng', 'Đã hủy', 'Lỗi'];
    return texts[status] || 'Unknown';
}

function getActionButtons(session) {
    if (session.status === 0) { // Active
        return `
            <button class="btn-icon" onclick="pauseSession('${session.id}')" title="Tạm dừng">⏸️</button>
            <button class="btn-icon danger" onclick="stopSessionWithModal('${session.id}')" title="Dừng">⏹️</button>
        `;
    } else if (session.status === 2) { // Paused
        return `
            <button class="btn-icon" onclick="resumeSession('${session.id}')" title="Tiếp tục">▶️</button>
            <button class="btn-icon danger" onclick="stopSessionWithModal('${session.id}')" title="Dừng">⏹️</button>
        `;
    } else if (session.status === 1 || session.status === 3 || session.status === 4) { // Completed, Cancelled, Error
        return `
            <button class="btn-icon" onclick="viewSessionDetails('${session.id}')" title="Xem chi tiết">👁️</button>
        `;
    }
    return '';
}

// Pagination
function updatePagination() {
    const totalPages = Math.ceil(filteredSessions.length / itemsPerPage);
    
    if (totalPages <= 1) {
        document.getElementById('paginationContainer').style.display = 'none';
        return;
    }
    
    document.getElementById('paginationContainer').style.display = 'flex';
    
    const startItem = (currentPage - 1) * itemsPerPage + 1;
    const endItem = Math.min(currentPage * itemsPerPage, filteredSessions.length);
    
    document.getElementById('paginationInfo').textContent = 
        `Hiển thị ${startItem}-${endItem} trong tổng số ${filteredSessions.length} phiên`;
    
    let buttonsHTML = '';
    
    if (currentPage > 1) {
        buttonsHTML += `<button class="pagination-btn" onclick="goToPage(${currentPage - 1})">← Trước</button>`;
    }
    
    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || (i >= currentPage - 2 && i <= currentPage + 2)) {
            buttonsHTML += `<button class="pagination-btn ${i === currentPage ? 'active' : ''}" onclick="goToPage(${i})">${i}</button>`;
        } else if (i === currentPage - 3 || i === currentPage + 3) {
            buttonsHTML += `<span style="padding: 6px;">...</span>`;
        }
    }
    
    if (currentPage < totalPages) {
        buttonsHTML += `<button class="pagination-btn" onclick="goToPage(${currentPage + 1})">Sau →</button>`;
    }
    
    document.getElementById('paginationButtons').innerHTML = buttonsHTML;
}

function goToPage(page) {
    currentPage = page;
    renderTable();
}

// Update stats
async function updateStats() {
    try {
        const activeResponse = await fetch('/api/ChargingSession/status/0');
        const active = await activeResponse.json();
        document.getElementById('statActive').textContent = active.length;
        
        const pausedResponse = await fetch('/api/ChargingSession/status/2');
        const paused = await pausedResponse.json();
        document.getElementById('statPaused').textContent = paused.length;
        
        const completedResponse = await fetch('/api/ChargingSession/status/1');
        const completed = await completedResponse.json();
        
        const today = new Date().toDateString();
        const todayCompleted = completed.filter(s => 
            new Date(s.endTime).toDateString() === today
        );
        document.getElementById('statCompleted').textContent = todayCompleted.length;
        
        const todayRevenue = todayCompleted.reduce((sum, s) => sum + s.totalCost, 0);
        document.getElementById('statRevenue').textContent = todayRevenue.toLocaleString('vi-VN') + 'đ';
    } catch (error) {
        console.error('Error updating stats:', error);
    }
}

// Modal functions
function showStartSessionModal() {
    document.getElementById('startSessionModal').classList.add('show');
}

function hideStartSessionModal() {
    document.getElementById('startSessionModal').classList.remove('show');
    // Clear form
    document.getElementById('userId').value = '';
    document.getElementById('stationId').value = '';
    document.getElementById('spotId').value = '';
    document.getElementById('targetSoC').value = '';
    document.getElementById('notes').value = '';
}

// Start session
async function startSession() {
    const userId = document.getElementById('userId').value;
    const stationId = document.getElementById('stationId').value;
    const spotId = document.getElementById('spotId').value;
    
    if (!userId || !stationId || !spotId) {
        alert('Vui lòng điền đầy đủ thông tin bắt buộc!');
        return;
    }
    
    const targetSoC = document.getElementById('targetSoC').value;
    const notes = document.getElementById('notes').value;
    
    const data = {
        userId: userId,
        chargingStationId: stationId,
        chargingSpotId: spotId,
        targetSoC: targetSoC ? parseInt(targetSoC) : null,
        notes: notes || null
    };
    
    try {
        const response = await fetch('/api/ChargingSession', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        if (response.ok) {
            alert('Đã khởi động phiên sạc thành công!');
            hideStartSessionModal();
            loadSessions();
            updateStats();
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể khởi động phiên sạc'));
        }
    } catch (error) {
        console.error('Error starting session:', error);
        alert('Đã xảy ra lỗi: ' + error.message);
    }
}

// Session actions
async function stopSessionWithModal(sessionId) {
    const energyConsumed = prompt('Nhập năng lượng tiêu thụ (kWh):', '0');
    if (energyConsumed === null) return;
    
    const totalCost = prompt('Nhập tổng chi phí (VND):', '0');
    if (totalCost === null) return;
    
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}/stop`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                energyConsumed: parseFloat(energyConsumed) || 0,
                totalCost: parseFloat(totalCost) || 0,
                paymentMethod: 'Cash',
                notes: 'Stopped by staff'
            })
        });
        
        if (response.ok) {
            alert('Đã dừng phiên sạc thành công!');
            loadSessions();
            updateStats();
        } else {
            alert('Không thể dừng phiên sạc.');
        }
    } catch (error) {
        console.error('Error stopping session:', error);
        alert('Đã xảy ra lỗi: ' + error.message);
    }
}

async function pauseSession(sessionId) {
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}/pause`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify('Paused by staff')
        });
        
        if (response.ok) {
            alert('Đã tạm dừng phiên sạc!');
            loadSessions();
            updateStats();
        } else {
            alert('Không thể tạm dừng phiên sạc.');
        }
    } catch (error) {
        console.error('Error pausing session:', error);
        alert('Đã xảy ra lỗi: ' + error.message);
    }
}

async function resumeSession(sessionId) {
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}/resume`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify('Resumed by staff')
        });
        
        if (response.ok) {
            alert('Đã tiếp tục phiên sạc!');
            loadSessions();
            updateStats();
        } else {
            alert('Không thể tiếp tục phiên sạc.');
        }
    } catch (error) {
        console.error('Error resuming session:', error);
        alert('Đã xảy ra lỗi: ' + error.message);
    }
}

function viewSessionDetails(sessionId) {
    const session = allSessions.find(s => s.id === sessionId);
    if (!session) return;
    
    const details = `
Thông tin phiên sạc:

Khách hàng: ${session.userFullName || 'N/A'}
Trạm sạc: ${session.chargingStationName || 'N/A'}
Điểm sạc: ${session.chargingSpotNumber || 'N/A'}
Bắt đầu: ${new Date(session.startTime).toLocaleString('vi-VN')}
Kết thúc: ${session.endTime ? new Date(session.endTime).toLocaleString('vi-VN') : 'Chưa kết thúc'}
Năng lượng: ${session.energyConsumed.toFixed(2)} kWh
Chi phí: ${session.totalCost.toLocaleString('vi-VN')}đ
Trạng thái: ${getStatusText(session.status)}
${session.notes ? '\nGhi chú: ' + session.notes : ''}
    `;
    
    alert(details);
}

// Export to Excel (placeholder)
function exportSessions() {
    alert('Chức năng xuất Excel đang được phát triển!');
}

// Error display
function showError(message) {
    document.getElementById('tableContainer').innerHTML = `
        <div class="empty-state">
            <div class="empty-icon">❌</div>
            <div class="empty-text">Đã xảy ra lỗi</div>
            <div class="empty-subtext">${message}</div>
        </div>
    `;
}

// Close modal when clicking outside
document.addEventListener('click', function(event) {
    const modal = document.getElementById('startSessionModal');
    if (event.target === modal) {
        hideStartSessionModal();
    }
});

