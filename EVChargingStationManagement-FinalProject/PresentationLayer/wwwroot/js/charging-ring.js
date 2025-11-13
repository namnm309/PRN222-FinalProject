/**
 * ChargingRing Component - Xiaomi Style Charging Animation
 * A reusable circular gauge component with electric current animation
 */

class ChargingRing {
    /**
     * Creates a new ChargingRing instance
     * @param {HTMLElement} container - The container element
     * @param {Object} options - Configuration options
     * @param {number} options.value - Current value (0-100 for battery, 0-max for power)
     * @param {number} options.min - Minimum value (default: 0)
     * @param {number} options.max - Maximum value (default: 100)
     * @param {string} options.unit - Unit text ("%" or "kW")
     * @param {string} options.mainLabel - Main label text (e.g., "MỨC PIN")
     * @param {string} options.status - Status text (e.g., "ĐANG SẠC")
     * @param {string} options.type - Type: "battery" or "power"
     */
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            value: options.value || 0,
            min: options.min || 0,
            max: options.max || 100,
            unit: options.unit || '%',
            mainLabel: options.mainLabel || '',
            status: options.status || 'ĐANG SẠC',
            type: options.type || 'battery'
        };
        
        this.radius = 120; // SVG radius
        this.circumference = 2 * Math.PI * this.radius;
        
        this.init();
    }
    
    /**
     * Initialize the component
     */
    init() {
        // Set container attributes
        this.container.setAttribute('data-type', this.options.type);
        this.container.classList.add('charging-ring-container');
        
        // Create SVG structure
        this.createSVG();
        
        // Create center content
        this.createContent();
        
        // Create glow effect
        this.createGlow();
        
        // Create particles container
        this.createParticlesContainer();
        
        // Update with initial value
        this.update(this.options.value, this.options.status);
        
        // Start particle animation
        this.startParticleAnimation();
    }
    
    /**
     * Create the SVG structure with gradients and filters
     */
    createSVG() {
        // Create wrapper
        const wrapper = document.createElement('div');
        wrapper.className = 'charging-ring-wrapper';
        this.container.appendChild(wrapper);
        
        // Create SVG
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('class', 'charging-ring-svg');
        svg.setAttribute('viewBox', '0 0 300 300');
        svg.setAttribute('width', '100%');
        svg.setAttribute('height', '100%');
        
        // Create defs for gradients and filters
        const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
        
        // Glow filter for progress ring
        const ringGlow = document.createElementNS('http://www.w3.org/2000/svg', 'filter');
        ringGlow.setAttribute('id', `ringGlow-${this.container.id || 'default'}`);
        const ringBlur = document.createElementNS('http://www.w3.org/2000/svg', 'feGaussianBlur');
        ringBlur.setAttribute('stdDeviation', '4');
        ringBlur.setAttribute('result', 'coloredBlur');
        const ringMerge = document.createElementNS('http://www.w3.org/2000/svg', 'feMerge');
        const ringMergeNode1 = document.createElementNS('http://www.w3.org/2000/svg', 'feMergeNode');
        ringMergeNode1.setAttribute('in', 'coloredBlur');
        const ringMergeNode2 = document.createElementNS('http://www.w3.org/2000/svg', 'feMergeNode');
        ringMergeNode2.setAttribute('in', 'SourceGraphic');
        ringMerge.appendChild(ringMergeNode1);
        ringMerge.appendChild(ringMergeNode2);
        ringGlow.appendChild(ringBlur);
        ringGlow.appendChild(ringMerge);
        defs.appendChild(ringGlow);
        
        // Glow filter for electric current
        const currentGlow = document.createElementNS('http://www.w3.org/2000/svg', 'filter');
        currentGlow.setAttribute('id', `currentGlow-${this.container.id || 'default'}`);
        const currentBlur = document.createElementNS('http://www.w3.org/2000/svg', 'feGaussianBlur');
        currentBlur.setAttribute('stdDeviation', '6');
        currentBlur.setAttribute('result', 'coloredBlur');
        const currentMerge = document.createElementNS('http://www.w3.org/2000/svg', 'feMerge');
        const currentMergeNode1 = document.createElementNS('http://www.w3.org/2000/svg', 'feMergeNode');
        currentMergeNode1.setAttribute('in', 'coloredBlur');
        const currentMergeNode2 = document.createElementNS('http://www.w3.org/2000/svg', 'feMergeNode');
        currentMergeNode2.setAttribute('in', 'SourceGraphic');
        currentMerge.appendChild(currentMergeNode1);
        currentMerge.appendChild(currentMergeNode2);
        currentGlow.appendChild(currentBlur);
        currentGlow.appendChild(currentMerge);
        defs.appendChild(currentGlow);
        
        // Create gradients based on type
        if (this.options.type === 'battery') {
            this.createBatteryGradients(defs);
        } else {
            this.createPowerGradients(defs);
        }
        
        svg.appendChild(defs);
        
        // Background circle
        const bgCircle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        bgCircle.setAttribute('cx', '150');
        bgCircle.setAttribute('cy', '150');
        bgCircle.setAttribute('r', String(this.radius));
        bgCircle.setAttribute('class', 'charging-ring-bg');
        svg.appendChild(bgCircle);
        
        // Progress ring (filled arc)
        const progressCircle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        progressCircle.setAttribute('cx', '150');
        progressCircle.setAttribute('cy', '150');
        progressCircle.setAttribute('r', String(this.radius));
        progressCircle.setAttribute('class', 'charging-ring-progress');
        progressCircle.setAttribute('stroke-dasharray', String(this.circumference));
        progressCircle.setAttribute('stroke-dashoffset', String(this.circumference));
        progressCircle.setAttribute('filter', `url(#ringGlow-${this.container.id || 'default'})`);
        svg.appendChild(progressCircle);
        this.progressCircle = progressCircle;
        
        // Electric current ring (animated highlight)
        const currentCircle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        currentCircle.setAttribute('cx', '150');
        currentCircle.setAttribute('cy', '150');
        currentCircle.setAttribute('r', String(this.radius));
        currentCircle.setAttribute('class', 'charging-ring-current');
        currentCircle.setAttribute('filter', `url(#currentGlow-${this.container.id || 'default'})`);
        svg.appendChild(currentCircle);
        this.currentCircle = currentCircle;
        
        wrapper.appendChild(svg);
        this.wrapper = wrapper;
        this.svg = svg;
    }
    
    /**
     * Create battery gradients (red, orange, yellow, green)
     */
    createBatteryGradients(defs) {
        // Low (red): 0-20%
        const lowGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        lowGrad.setAttribute('id', `batteryLowGradient-${this.container.id || 'default'}`);
        lowGrad.setAttribute('x1', '0%');
        lowGrad.setAttribute('y1', '0%');
        lowGrad.setAttribute('x2', '100%');
        lowGrad.setAttribute('y2', '0%');
        const lowStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop1.setAttribute('offset', '0%');
        lowStop1.setAttribute('style', 'stop-color:#f87171;stop-opacity:1');
        const lowStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop2.setAttribute('offset', '50%');
        lowStop2.setAttribute('style', 'stop-color:#ef4444;stop-opacity:1');
        const lowStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop3.setAttribute('offset', '100%');
        lowStop3.setAttribute('style', 'stop-color:#dc2626;stop-opacity:1');
        lowGrad.appendChild(lowStop1);
        lowGrad.appendChild(lowStop2);
        lowGrad.appendChild(lowStop3);
        defs.appendChild(lowGrad);
        
        // Medium (orange/yellow): 20-50%
        const medGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        medGrad.setAttribute('id', `batteryMedGradient-${this.container.id || 'default'}`);
        medGrad.setAttribute('x1', '0%');
        medGrad.setAttribute('y1', '0%');
        medGrad.setAttribute('x2', '100%');
        medGrad.setAttribute('y2', '0%');
        const medStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop1.setAttribute('offset', '0%');
        medStop1.setAttribute('style', 'stop-color:#fbbf24;stop-opacity:1');
        const medStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop2.setAttribute('offset', '50%');
        medStop2.setAttribute('style', 'stop-color:#f59e0b;stop-opacity:1');
        const medStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop3.setAttribute('offset', '100%');
        medStop3.setAttribute('style', 'stop-color:#d97706;stop-opacity:1');
        medGrad.appendChild(medStop1);
        medGrad.appendChild(medStop2);
        medGrad.appendChild(medStop3);
        defs.appendChild(medGrad);
        
        // High (blue): 50-80%
        const highGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        highGrad.setAttribute('id', `batteryHighGradient-${this.container.id || 'default'}`);
        highGrad.setAttribute('x1', '0%');
        highGrad.setAttribute('y1', '0%');
        highGrad.setAttribute('x2', '100%');
        highGrad.setAttribute('y2', '0%');
        const highStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop1.setAttribute('offset', '0%');
        highStop1.setAttribute('style', 'stop-color:#60a5fa;stop-opacity:1');
        const highStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop2.setAttribute('offset', '50%');
        highStop2.setAttribute('style', 'stop-color:#3b82f6;stop-opacity:1');
        const highStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop3.setAttribute('offset', '100%');
        highStop3.setAttribute('style', 'stop-color:#2563eb;stop-opacity:1');
        highGrad.appendChild(highStop1);
        highGrad.appendChild(highStop2);
        highGrad.appendChild(highStop3);
        defs.appendChild(highGrad);
        
        // Full (green): 80-100%
        const fullGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        fullGrad.setAttribute('id', `batteryFullGradient-${this.container.id || 'default'}`);
        fullGrad.setAttribute('x1', '0%');
        fullGrad.setAttribute('y1', '0%');
        fullGrad.setAttribute('x2', '100%');
        fullGrad.setAttribute('y2', '0%');
        const fullStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        fullStop1.setAttribute('offset', '0%');
        fullStop1.setAttribute('style', 'stop-color:#4ade80;stop-opacity:1');
        const fullStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        fullStop2.setAttribute('offset', '50%');
        fullStop2.setAttribute('style', 'stop-color:#22c55e;stop-opacity:1');
        const fullStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        fullStop3.setAttribute('offset', '100%');
        fullStop3.setAttribute('style', 'stop-color:#16a34a;stop-opacity:1');
        fullGrad.appendChild(fullStop1);
        fullGrad.appendChild(fullStop2);
        fullGrad.appendChild(fullStop3);
        defs.appendChild(fullGrad);
    }
    
    /**
     * Create power gradients (blue, cyan, green)
     */
    createPowerGradients(defs) {
        // Low (blue): 0-33%
        const lowGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        lowGrad.setAttribute('id', `powerLowGradient-${this.container.id || 'default'}`);
        lowGrad.setAttribute('x1', '0%');
        lowGrad.setAttribute('y1', '0%');
        lowGrad.setAttribute('x2', '100%');
        lowGrad.setAttribute('y2', '0%');
        const lowStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop1.setAttribute('offset', '0%');
        lowStop1.setAttribute('style', 'stop-color:#60a5fa;stop-opacity:1');
        const lowStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop2.setAttribute('offset', '50%');
        lowStop2.setAttribute('style', 'stop-color:#3b82f6;stop-opacity:1');
        const lowStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        lowStop3.setAttribute('offset', '100%');
        lowStop3.setAttribute('style', 'stop-color:#2563eb;stop-opacity:1');
        lowGrad.appendChild(lowStop1);
        lowGrad.appendChild(lowStop2);
        lowGrad.appendChild(lowStop3);
        defs.appendChild(lowGrad);
        
        // Medium (cyan): 33-66%
        const medGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        medGrad.setAttribute('id', `powerMedGradient-${this.container.id || 'default'}`);
        medGrad.setAttribute('x1', '0%');
        medGrad.setAttribute('y1', '0%');
        medGrad.setAttribute('x2', '100%');
        medGrad.setAttribute('y2', '0%');
        const medStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop1.setAttribute('offset', '0%');
        medStop1.setAttribute('style', 'stop-color:#22d3ee;stop-opacity:1');
        const medStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop2.setAttribute('offset', '50%');
        medStop2.setAttribute('style', 'stop-color:#06b6d4;stop-opacity:1');
        const medStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        medStop3.setAttribute('offset', '100%');
        medStop3.setAttribute('style', 'stop-color:#0891b2;stop-opacity:1');
        medGrad.appendChild(medStop1);
        medGrad.appendChild(medStop2);
        medGrad.appendChild(medStop3);
        defs.appendChild(medGrad);
        
        // High (green): 66-100%
        const highGrad = document.createElementNS('http://www.w3.org/2000/svg', 'linearGradient');
        highGrad.setAttribute('id', `powerHighGradient-${this.container.id || 'default'}`);
        highGrad.setAttribute('x1', '0%');
        highGrad.setAttribute('y1', '0%');
        highGrad.setAttribute('x2', '100%');
        highGrad.setAttribute('y2', '0%');
        const highStop1 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop1.setAttribute('offset', '0%');
        highStop1.setAttribute('style', 'stop-color:#4ade80;stop-opacity:1');
        const highStop2 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop2.setAttribute('offset', '50%');
        highStop2.setAttribute('style', 'stop-color:#22c55e;stop-opacity:1');
        const highStop3 = document.createElementNS('http://www.w3.org/2000/svg', 'stop');
        highStop3.setAttribute('offset', '100%');
        highStop3.setAttribute('style', 'stop-color:#16a34a;stop-opacity:1');
        highGrad.appendChild(highStop1);
        highGrad.appendChild(highStop2);
        highGrad.appendChild(highStop3);
        defs.appendChild(highGrad);
    }
    
    /**
     * Create center content (value, unit, labels)
     */
    createContent() {
        const content = document.createElement('div');
        content.className = 'charging-ring-content';
        
        const value = document.createElement('div');
        value.className = 'charging-ring-value';
        value.textContent = '0';
        this.valueElement = value;
        
        const unit = document.createElement('div');
        unit.className = 'charging-ring-unit';
        unit.textContent = this.options.unit;
        this.unitElement = unit;
        
        const label = document.createElement('div');
        label.className = 'charging-ring-label';
        label.textContent = this.options.mainLabel;
        this.labelElement = label;
        
        const status = document.createElement('div');
        status.className = 'charging-ring-status';
        status.textContent = this.options.status;
        this.statusElement = status;
        
        content.appendChild(value);
        content.appendChild(unit);
        content.appendChild(label);
        content.appendChild(status);
        
        this.wrapper.appendChild(content);
    }
    
    /**
     * Create pulsing glow effect
     */
    createGlow() {
        const glow = document.createElement('div');
        glow.className = 'charging-ring-glow';
        this.glowElement = glow;
        this.wrapper.appendChild(glow);
    }
    
    /**
     * Create particles container
     */
    createParticlesContainer() {
        const particles = document.createElement('div');
        particles.className = 'charging-ring-particles';
        this.particlesContainer = particles;
        this.wrapper.appendChild(particles);
    }
    
    /**
     * Get color level based on value
     */
    getColorLevel(value) {
        const percent = ((value - this.options.min) / (this.options.max - this.options.min)) * 100;
        
        if (this.options.type === 'battery') {
            if (percent < 20) return 'low';
            if (percent < 50) return 'medium';
            if (percent < 80) return 'high';
            return 'full';
        } else {
            // Power type
            if (percent < 33) return 'low';
            if (percent < 66) return 'medium';
            return 'high';
        }
    }
    
    /**
     * Get gradient ID based on color level
     */
    getGradientId(level) {
        const prefix = this.options.type === 'battery' ? 'battery' : 'power';
        const suffix = level.charAt(0).toUpperCase() + level.slice(1);
        return `${prefix}${suffix}Gradient-${this.container.id || 'default'}`;
    }
    
    /**
     * Update the gauge with new value
     * @param {number} value - New value
     * @param {string} status - Status text (optional)
     */
    update(value, status = null) {
        // Clamp value
        const clampedValue = Math.max(this.options.min, Math.min(this.options.max, value));
        
        // Calculate percentage
        const percent = ((clampedValue - this.options.min) / (this.options.max - this.options.min)) * 100;
        
        // Update progress ring
        const offset = this.circumference - (percent / 100) * this.circumference;
        this.progressCircle.setAttribute('stroke-dashoffset', String(offset));
        
        // Update color level
        const level = this.getColorLevel(clampedValue);
        this.container.setAttribute('data-level', level);
        
        // Update gradient
        const gradientId = this.getGradientId(level);
        this.progressCircle.setAttribute('stroke', `url(#${gradientId})`);
        this.currentCircle.setAttribute('stroke', `url(#${gradientId})`);
        
        // Update center value with animation
        this.animateValue(this.valueElement, parseFloat(this.valueElement.textContent) || 0, clampedValue, 500);
        
        // Update status
        if (status !== null) {
            this.statusElement.textContent = status;
            
            // Update status class
            this.statusElement.className = 'charging-ring-status';
            if (status.includes('ĐANG SẠC') || status.includes('Đang sạc')) {
                this.statusElement.classList.add('charging');
            } else if (status.includes('HOÀN TẤT') || status.includes('Hoàn thành')) {
                this.statusElement.classList.add('completed');
            } else if (status.includes('THẤP') || status.includes('Thấp')) {
                this.statusElement.classList.add('low');
            }
        }
        
        // Update electric current animation (only if charging)
        if (status && (status.includes('ĐANG SẠC') || status.includes('Đang sạc'))) {
            this.currentCircle.style.display = 'block';
        } else {
            this.currentCircle.style.display = 'none';
        }
    }
    
    /**
     * Animate value change
     */
    animateValue(element, start, end, duration) {
        const startTime = performance.now();
        const difference = end - start;
        
        const update = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            
            // Easing function (ease-out cubic)
            const easeOut = 1 - Math.pow(1 - progress, 3);
            const current = start + (difference * easeOut);
            
            // Format based on unit
            if (this.options.unit === '%') {
                element.textContent = current.toFixed(1);
            } else {
                element.textContent = current.toFixed(1);
            }
            
            if (progress < 1) {
                requestAnimationFrame(update);
            } else {
                element.textContent = end.toFixed(1);
            }
        };
        
        requestAnimationFrame(update);
    }
    
    /**
     * Start particle animation (emits particles occasionally)
     */
    startParticleAnimation() {
        setInterval(() => {
            if (this.statusElement && this.statusElement.textContent.includes('ĐANG SẠC')) {
                this.emitParticle();
            }
        }, 2000 + Math.random() * 2000); // Random interval between 2-4 seconds
    }
    
    /**
     * Emit a single particle
     */
    emitParticle() {
        const particle = document.createElement('div');
        particle.className = 'charging-ring-particle';
        
        // Random position on the ring
        const angle = Math.random() * 360;
        const rad = (angle - 90) * Math.PI / 180;
        const centerX = 50; // Percentage
        const centerY = 50;
        const radius = 45; // Percentage from center
        
        const x = centerX + radius * Math.cos(rad);
        const y = centerY + radius * Math.sin(rad);
        
        particle.style.left = x + '%';
        particle.style.top = y + '%';
        
        // Get current color
        const level = this.container.getAttribute('data-level');
        const colors = {
            low: '#ef4444',
            medium: '#f59e0b',
            high: '#3b82f6',
            full: '#10b981'
        };
        particle.style.backgroundColor = colors[level] || '#3b82f6';
        
        // Random particle movement
        const moveX = (Math.random() - 0.5) * 30;
        const moveY = (Math.random() - 0.5) * 30;
        particle.style.setProperty('--particle-x', moveX + 'px');
        particle.style.setProperty('--particle-y', moveY + 'px');
        
        // Random delay
        particle.style.animationDelay = Math.random() * 0.5 + 's';
        
        this.particlesContainer.appendChild(particle);
        
        // Remove after animation
        setTimeout(() => {
            if (particle.parentNode) {
                particle.parentNode.removeChild(particle);
            }
        }, 2000);
    }
}

// Export for use in modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ChargingRing;
}

