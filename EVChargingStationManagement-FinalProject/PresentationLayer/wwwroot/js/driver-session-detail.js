// Driver Session Detail JavaScript

let sessionData = null;
let progressPollInterval = null;
let chargingTimer = null;
let sessionStartTime = null;
let simulatedPowerKw = 0;
const BASE_FEE = 10000; // Phí cơ bản 10k
let pricePerKwh = 0;
let previousStatus = null; // Track previous status to detect changes
let powerVariationTimer = null; // Timer for power variation effect
let basePowerKw = 0; // Base power for variation

// ChargingRing component instances
let powerGauge = null;
let socGauge = null;

document.addEventListener('DOMContentLoaded', function() {
    const sessionId = document.getElementById('session-id').value;
    
    // Initialize ChargingRing components
    initializeChargingRings();
    
    loadSession(sessionId);
    loadProgress(sessionId);
    setupSignalR(sessionId);
    
    // Setup polling backup (every 5 seconds)
    progressPollInterval = setInterval(() => {
        loadProgress(sessionId);
    }, 5000);

    // Setup buttons
    document.getElementById('btn-stop-charging')?.addEventListener('click', () => {
        stopCharging(sessionId);
    });
    document.getElementById('btn-payment')?.addEventListener('click', () => {
        goToPayment(sessionId);
    });
});

/**
 * Initialize ChargingRing components
 */
function initializeChargingRings() {
    // Power gauge
    const powerContainer = document.getElementById('power-gauge-container');
    if (powerContainer && typeof ChargingRing !== 'undefined') {
        powerGauge = new ChargingRing(powerContainer, {
            value: 0,
            min: 0,
            max: 150,
            unit: 'kW',
            mainLabel: 'CÔNG SUẤT SẠC',
            status: 'ĐANG SẠC',
            type: 'power'
        });
    }
    
    // SOC gauge
    const socContainer = document.getElementById('soc-gauge-container');
    if (socContainer && typeof ChargingRing !== 'undefined') {
        socGauge = new ChargingRing(socContainer, {
            value: 0,
            min: 0,
            max: 100,
            unit: '%',
            mainLabel: 'MỨC PIN',
            status: 'ĐANG SẠC',
            type: 'battery'
        });
    }
}

// Function to stop charging simulation
function stopChargingSimulation() {
    if (chargingTimer) {
        clearInterval(chargingTimer);
        chargingTimer = null;
        console.log('Charging simulation stopped');
    }
    stopPowerVariation();
}

async function loadSession(sessionId) {
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}`, {
            credentials: 'include'
        });

        if (response.ok) {
            const newSessionData = await response.json();
            const statusChanged = previousStatus && previousStatus === 'InProgress' && newSessionData.status === 'Completed';
            
            sessionData = newSessionData;
            sessionStartTime = new Date(sessionData.sessionStartTime);
            pricePerKwh = sessionData.pricePerKwh || 0;
            
            displaySessionInfo(sessionData);
            
            // Initialize SOC gauge với giá trị ban đầu nếu có
            if (sessionData.initialSocPercentage !== undefined && socGauge) {
                socGauge.update(sessionData.initialSocPercentage, 'ĐANG SẠC');
            }
            
            // Show buttons based on session status
            const actionsPanel = document.getElementById('session-actions');
            const btnStop = document.getElementById('btn-stop-charging');
            const btnPayment = document.getElementById('btn-payment');
            
            if (sessionData.status === 'InProgress') {
                actionsPanel.style.display = 'block';
                btnStop.style.display = 'block';
                btnPayment.style.display = 'none';
                // Bắt đầu simulation
                startChargingSimulation(sessionId);
                // Start power variation effect
                startPowerVariation();
            } else if (sessionData.status === 'Completed') {
                // Stop power variation
                stopPowerVariation();
                // Dừng simulation nếu đang chạy (khi staff dừng session)
                stopChargingSimulation();
                
                actionsPanel.style.display = 'block';
                btnStop.style.display = 'none';
                btnPayment.style.display = 'block';
                
                // Reload progress để lấy giá trị cuối cùng
                loadProgress(sessionId);
                
                // Chỉ hiển thị alert nếu đang từ InProgress chuyển sang Completed (staff dừng)
                if (statusChanged) {
                    alert('Phiên sạc đã được dừng bởi nhân viên trạm sạc. Vui lòng thanh toán.');
                }
            } else {
                actionsPanel.style.display = 'none';
            }
            
            // Update previous status
            previousStatus = sessionData.status;
        } else {
            document.getElementById('session-info').innerHTML = 
                '<div class="text-center text-danger">Không tìm thấy phiên sạc</div>';
        }
    } catch (error) {
        console.error('Error loading session:', error);
        document.getElementById('session-info').innerHTML = 
            '<div class="text-center text-danger">Lỗi khi tải dữ liệu</div>';
    }
}

function displaySessionInfo(session) {
    const stationName = session.chargingStationName || 'N/A';
    const spotNumber = session.chargingSpotNumber || 'N/A';
    const startTime = new Date(session.sessionStartTime).toLocaleString('vi-VN');
    const status = getStatusBadge(session.status);
    
    // Hiển thị cost, nếu chưa có thì hiển thị base fee
    let costDisplay = '10,000 VND'; // Base fee mặc định
    if (session.cost) {
        costDisplay = new Intl.NumberFormat('vi-VN').format(session.cost) + ' VND';
    } else if (session.status === 'InProgress') {
        costDisplay = '10,000 VND (phí cơ bản)';
    }

    document.getElementById('session-info').innerHTML = `
        <div class="session-details">
            <div class="detail-row">
                <strong>Trạm sạc:</strong> ${stationName}
            </div>
            <div class="detail-row">
                <strong>Cổng sạc:</strong> ${spotNumber}
            </div>
            <div class="detail-row">
                <strong>Thời gian bắt đầu:</strong> ${startTime}
            </div>
            <div class="detail-row">
                <strong>Trạng thái:</strong> ${status}
            </div>
            <div class="detail-row">
                <strong>Chi phí:</strong> ${costDisplay}
            </div>
        </div>
    `;
}

async function loadProgress(sessionId) {
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}/progress`, {
            credentials: 'include'
        });

        if (response.ok) {
            const progress = await response.json();
            console.log('Progress data loaded:', progress); // Debug log
            updateProgressUI(progress);
        } else {
            console.warn('Failed to load progress:', response.status);
        }
    } catch (error) {
        console.error('Error loading progress:', error);
    }
}

function updateProgressUI(progress) {
    // Update SOC
    const currentSoc = progress.currentSocPercentage || 0;
    const initialSoc = progress.initialSocPercentage || 0;
    const targetSoc = progress.targetSocPercentage || 100;

    // Update old progress bar elements if they exist (for backward compatibility)
    const socPercentageEl = document.getElementById('soc-percentage');
    const initialSocEl = document.getElementById('initial-soc');
    const targetSocEl = document.getElementById('target-soc');
    const progressBar = document.getElementById('soc-progress-bar');
    
    if (socPercentageEl) {
        socPercentageEl.textContent = currentSoc.toFixed(1) + '%';
    }
    if (initialSocEl) {
        initialSocEl.textContent = initialSoc.toFixed(1);
    }
    if (targetSocEl) {
        targetSocEl.textContent = targetSoc.toFixed(1);
    }
    if (progressBar) {
        const progressPercent = ((currentSoc - initialSoc) / (targetSoc - initialSoc)) * 100;
        progressBar.style.width = Math.max(0, Math.min(100, progressPercent)) + '%';
        progressBar.textContent = currentSoc.toFixed(1) + '%';
    }

    // Update energy
    const energy = progress.energyDeliveredKwh || 0;
    document.getElementById('energy-delivered').textContent = energy.toFixed(2) + ' kWh';

    // Update power
    const power = progress.currentPowerKw || 0;
    basePowerKw = power; // Set base power for variation
    
    // Update power gauge using ChargingRing
    if (powerGauge) {
        const powerStatus = sessionData?.status === 'InProgress' ? 'ĐANG SẠC' : 'ĐÃ HOÀN TẤT';
        powerGauge.update(power, powerStatus);
    }
    
    // Update SOC gauge using ChargingRing
    if (socGauge) {
        let socStatus = 'ĐANG SẠC';
        if (sessionData?.status === 'Completed') {
            socStatus = 'ĐÃ HOÀN TẤT';
        } else if (currentSoc < 20) {
            socStatus = 'PIN THẤP';
        } else if (currentSoc >= 100) {
            socStatus = 'ĐÃ HOÀN TẤT';
        }
        socGauge.update(currentSoc, socStatus);
    }

    // Update time remaining
    const timeRemaining = progress.estimatedTimeRemainingMinutes;
    if (timeRemaining != null) {
        const hours = Math.floor(timeRemaining / 60);
        const minutes = Math.floor(timeRemaining % 60);
        document.getElementById('time-remaining').textContent = 
            hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
    } else {
        document.getElementById('time-remaining').textContent = '--';
    }
}

// Old gauge functions removed - now using ChargingRing component

// Power variation effect (random up/down)
function startPowerVariation() {
    if (powerVariationTimer) {
        clearInterval(powerVariationTimer);
    }
    
    powerVariationTimer = setInterval(() => {
        if (sessionData && sessionData.status === 'InProgress' && basePowerKw > 0 && powerGauge) {
            // Random variation: ±2 kW around base power
            const variation = (Math.random() - 0.5) * 4; // -2 to +2 kW
            const variedPower = Math.max(0, Math.min(150, basePowerKw + variation));
            powerGauge.update(variedPower, 'ĐANG SẠC');
        }
    }, 800); // Update every 800ms for smooth variation
}

function stopPowerVariation() {
    if (powerVariationTimer) {
        clearInterval(powerVariationTimer);
        powerVariationTimer = null;
    }
}

// Old helper functions removed - now handled by ChargingRing component

async function setupSignalR(sessionId) {
    if (!window.signalRManager) {
        console.warn('signalRManager not available');
        return;
    }

    try {
        // Connect to SignalR
        await window.signalRManager.connect();
        
        if (!window.signalRManager.connection) {
            console.error('SignalR connection failed');
            return;
        }

        // Subscribe to session for progress updates
        await window.signalRManager.subscribeSession(sessionId, (progress) => {
            updateProgressUI(progress);
        });
        
        // Remove existing listener to avoid duplicates
        const connection = window.signalRManager.connection;
        if (connection) {
            connection.off('SessionUpdated');
            
            // Listen for session status changes (when staff stops the session)
            // SessionUpdated is sent to user-{userId} group (automatically added on connect)
            connection.on('SessionUpdated', async (updatedSessionId) => {
                console.log('SessionUpdated event received:', updatedSessionId, 'for session:', sessionId);
                if (updatedSessionId === sessionId) {
                    console.log('SessionUpdated for current session, stopping simulation and reloading...');
                    // Dừng simulation ngay lập tức
                    stopChargingSimulation();
                    // Reload session để lấy status mới
                    await loadSession(sessionId);
                }
            });
        }
        
        console.log('SignalR setup completed for session:', sessionId);
    } catch (error) {
        console.error('Error setting up SignalR:', error);
    }
}

function startChargingSimulation(sessionId) {
    // Dừng timer cũ nếu có
    stopChargingSimulation();
    
    // Lấy power từ spot hoặc mặc định 50kW
    simulatedPowerKw = sessionData?.chargingSpotPower || 50; // kW từ spot hoặc mặc định
    
    // Cập nhật mỗi giây
    chargingTimer = setInterval(() => {
        // Kiểm tra status trước khi update
        if (!sessionData || sessionData.status !== 'InProgress') {
            stopChargingSimulation();
            return;
        }
        
        // Tính thời gian đã sạc (giây)
        const now = new Date();
        const elapsedSeconds = Math.floor((now - sessionStartTime) / 1000);
        
        // Cập nhật timer
        updateChargingTimer(elapsedSeconds);
        
        // Tính năng lượng đã sạc (kWh) = (power * time_in_hours)
        const elapsedHours = elapsedSeconds / 3600;
        const energyDeliveredKwh = simulatedPowerKw * elapsedHours;
        
        // Tính cost = (energy * price) + base fee
        const cost = (energyDeliveredKwh * pricePerKwh) + BASE_FEE;
        
        // Tính SOC trước (giả sử tăng đều từ initial đến target)
        const initialSoc = sessionData.initialSocPercentage || 0;
        const targetSoc = sessionData.targetSocPercentage || 100;
        const socRange = targetSoc - initialSoc;
        
        // Giả sử sạc từ 0% đến 100% mất 1 giờ với power hiện tại
        const estimatedTotalHours = 1; // 1 giờ để sạc đầy
        const socProgress = Math.min(1, elapsedHours / estimatedTotalHours);
        const currentSoc = initialSoc + (socRange * socProgress);
        
        // Cập nhật UI
        document.getElementById('energy-delivered').textContent = energyDeliveredKwh.toFixed(2) + ' kWh';
        document.getElementById('current-cost').textContent = new Intl.NumberFormat('vi-VN').format(Math.round(cost)) + ' VND';
        
        // Update power gauge with simulated power using ChargingRing
        basePowerKw = simulatedPowerKw; // Update base for variation
        if (powerGauge) {
            powerGauge.update(simulatedPowerKw, 'ĐANG SẠC');
        }
        
        // Update SOC gauge với giá trị đã tính toán using ChargingRing
        if (socGauge) {
            let socStatus = 'ĐANG SẠC';
            if (currentSoc >= 100) {
                socStatus = 'ĐÃ HOÀN TẤT';
            } else if (currentSoc < 20) {
                socStatus = 'PIN THẤP';
            }
            socGauge.update(currentSoc, socStatus);
        }
        
        // Cập nhật progress UI
        updateProgressUI({
            currentSocPercentage: currentSoc,
            initialSocPercentage: initialSoc,
            targetSocPercentage: targetSoc,
            currentPowerKw: simulatedPowerKw,
            energyDeliveredKwh: energyDeliveredKwh
        });
        
        // Gửi update lên server mỗi 10 giây
        if (elapsedSeconds % 10 === 0) {
            updateProgressOnServer(sessionId, energyDeliveredKwh, currentSoc);
        }
    }, 1000); // Cập nhật mỗi giây
}

function updateChargingTimer(totalSeconds) {
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    
    const timeString = 
        String(hours).padStart(2, '0') + ':' +
        String(minutes).padStart(2, '0') + ':' +
        String(seconds).padStart(2, '0');
    
    document.getElementById('charging-time').textContent = timeString;
}

async function updateProgressOnServer(sessionId, energyKwh, currentSoc) {
    try {
        await fetch(`/api/ChargingSession/${sessionId}/progress`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                socPercentage: currentSoc,
                powerKw: simulatedPowerKw,
                energyDeliveredKwh: energyKwh,
                estimatedTimeRemainingMinutes: null
            })
        });
    } catch (error) {
        console.error('Error updating progress:', error);
    }
}

async function stopCharging(sessionId) {
    if (!confirm('Bạn có chắc chắn muốn ngưng sạc?')) {
        return;
    }

    // Dừng timer
    stopChargingSimulation();

    try {
        // Lấy giá trị năng lượng từ UI (loại bỏ " kWh")
        const energyText = document.getElementById('energy-delivered').textContent;
        const energyDelivered = parseFloat(energyText.replace(' kWh', '')) || 0;
        
        // Tính cost = (energy * price) + base fee
        const cost = (energyDelivered * pricePerKwh) + BASE_FEE;

        const response = await fetch(`/api/ChargingSession/${sessionId}/complete`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                energyDeliveredKwh: energyDelivered,
                cost: cost,
                pricePerKwh: pricePerKwh || sessionData.pricePerKwh
            })
        });

        if (response.ok) {
            const completedSession = await response.json();
            sessionData = completedSession;
            alert('Đã ngưng sạc. Vui lòng thanh toán.');
            
            // Cập nhật UI
            displaySessionInfo(completedSession);
            const actionsPanel = document.getElementById('session-actions');
            const btnStop = document.getElementById('btn-stop-charging');
            const btnPayment = document.getElementById('btn-payment');
            actionsPanel.style.display = 'block';
            btnStop.style.display = 'none';
            btnPayment.style.display = 'block';
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể ngưng sạc'));
        }
    } catch (error) {
        console.error('Error stopping charging:', error);
        alert('Có lỗi xảy ra. Vui lòng thử lại.');
    }
}

function goToPayment(sessionId) {
    window.location.href = `/Driver/Payment?sessionId=${sessionId}`;
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

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (chargingTimer) {
        clearInterval(chargingTimer);
    }
    if (progressPollInterval) {
        clearInterval(progressPollInterval);
    }
    const sessionId = document.getElementById('session-id')?.value;
    if (sessionId && window.signalRManager) {
        window.signalRManager.unsubscribeSession(sessionId);
    }
});

