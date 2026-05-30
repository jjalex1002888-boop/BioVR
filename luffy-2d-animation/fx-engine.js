/**
 * ==========================================
 * GEAR CHRONICLES - SPECIAL EFFECTS ENGINE
 * ==========================================
 * Procedural particle simulations, dynamic custom backgrounds, camera
 * shake controllers, and expanding combat shockwaves.
 */

class Particle {
    constructor(x, y, options = {}) {
        this.x = x;
        this.y = y;
        this.vx = options.vx || (Math.random() - 0.5) * 2;
        this.vy = options.vy || (Math.random() - 0.5) * 2;
        
        this.size = options.size || Math.random() * 8 + 2;
        this.color = options.color || '#ffffff';
        this.alpha = options.alpha || 1.0;
        this.life = 0;
        this.maxLife = options.maxLife || Math.random() * 30 + 15;
        this.decay = options.decay || 0.02;
        this.gravity = options.gravity || 0;
        this.drag = options.drag || 0.98;
        
        this.type = options.type || 'smoke'; // 'smoke', 'fire', 'haki', 'spark', 'petal', 'snow'
        this.angle = Math.random() * Math.PI * 2;
        this.spin = (Math.random() - 0.5) * 0.1;
    }
    
    update() {
        this.x += this.vx;
        this.y += this.vy;
        this.vx *= this.drag;
        this.vy *= this.drag;
        this.vy += this.gravity;
        
        this.life++;
        this.alpha = Math.max(0, 1 - (this.life / this.maxLife));
        this.angle += this.spin;
        
        if (this.type === 'smoke') {
            this.size += 0.4; // Puffs expand
        } else if (this.type === 'fire') {
            this.size = Math.max(0.1, this.size - 0.1); // Flame tapers
        }
    }
    
    draw(ctx) {
        ctx.save();
        ctx.globalAlpha = this.alpha;
        ctx.translate(this.x, this.y);
        ctx.rotate(this.angle);
        
        if (this.type === 'smoke') {
            ctx.fillStyle = this.color;
            ctx.beginPath();
            ctx.arc(0, 0, this.size, 0, Math.PI * 2);
            ctx.fill();
        } 
        else if (this.type === 'fire') {
            let grad = ctx.createRadialGradient(0, 0, 0, 0, 0, this.size);
            grad.addColorStop(0, this.color);
            grad.addColorStop(0.3, '#f97316'); // Orange
            grad.addColorStop(1, 'rgba(239, 68, 68, 0)'); // Red fade
            
            ctx.fillStyle = grad;
            ctx.beginPath();
            ctx.arc(0, 0, this.size * 1.5, 0, Math.PI * 2);
            ctx.fill();
        } 
        else if (this.type === 'haki' || this.type === 'spark') {
            // Draw angular glowing sparks
            ctx.fillStyle = this.color;
            ctx.shadowColor = this.color;
            ctx.shadowBlur = 8;
            ctx.beginPath();
            ctx.moveTo(0, -this.size);
            ctx.lineTo(this.size * 0.4, -this.size * 0.4);
            ctx.lineTo(this.size, 0);
            ctx.lineTo(this.size * 0.4, this.size * 0.4);
            ctx.lineTo(0, this.size);
            ctx.lineTo(-this.size * 0.4, this.size * 0.4);
            ctx.lineTo(-this.size, 0);
            ctx.lineTo(-this.size * 0.4, -this.size * 0.4);
            ctx.closePath();
            ctx.fill();
        }
        else if (this.type === 'petal') {
            // Cherry Blossom Petals (Wano)
            ctx.fillStyle = '#fbcfe8'; // Pink
            ctx.beginPath();
            ctx.ellipse(0, 0, this.size, this.size * 0.6, Math.PI / 4, 0, Math.PI * 2);
            ctx.fill();
        }
        else if (this.type === 'snow') {
            ctx.fillStyle = '#ffffff';
            ctx.beginPath();
            ctx.arc(0, 0, this.size, 0, Math.PI * 2);
            ctx.fill();
        }
        
        ctx.restore();
    }
}

class Shockwave {
    constructor(x, y, options = {}) {
        this.x = x;
        this.y = y;
        this.radius = options.radius || 10;
        this.maxRadius = options.maxRadius || 300;
        this.speed = options.speed || 8;
        this.color = options.color || 'rgba(255, 255, 255, 0.8)';
        this.lineWidth = options.lineWidth || 4;
        this.alpha = 1.0;
        this.active = true;
    }
    
    update() {
        this.radius += this.speed;
        this.alpha = 1 - (this.radius / this.maxRadius);
        if (this.radius >= this.maxRadius) {
            this.active = false;
        }
    }
    
    draw(ctx) {
        if (!this.active) return;
        ctx.save();
        ctx.globalAlpha = this.alpha;
        ctx.strokeStyle = this.color;
        ctx.lineWidth = this.lineWidth;
        ctx.beginPath();
        ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
    }
}

class FxEngine {
    constructor(canvas) {
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
        
        this.particles = [];
        this.shockwaves = [];
        
        // Background preset
        this.scenePreset = 'wano';
        
        // Camera Shake controller
        this.shakeTime = 0;
        this.shakeIntensity = 0;
        this.shakeOffset = { x: 0, y: 0 };
        
        // lightning bolts for Gear 5
        this.lightningBolts = [];
    }
    
    setScene(scene) {
        this.scenePreset = scene;
    }
    
    triggerShake(intensity, durationFrames) {
        this.shakeIntensity = intensity;
        this.shakeTime = durationFrames;
    }
    
    triggerShockwave(x, y, options) {
        this.shockwaves.push(new Shockwave(x, y, options));
    }
    
    spawnSteam(x, y, count = 2) {
        for (let i = 0; i < count; i++) {
            this.particles.push(new Particle(x + (Math.random() - 0.5) * 30, y + (Math.random() - 0.5) * 60, {
                vx: (Math.random() - 0.5) * 1.5,
                vy: -Math.random() * 2 - 1,
                size: Math.random() * 5 + 4,
                color: Math.random() > 0.3 ? '#ffd1dc' : '#ffffff', // Gear 2 pink/white steam
                maxLife: Math.random() * 40 + 20,
                type: 'smoke',
                decay: 0.015
            }));
        }
    }
    
    spawnHakiFlames(x, y, count = 2) {
        for (let i = 0; i < count; i++) {
            // Spiraling haki flames curling up
            this.particles.push(new Particle(x + (Math.random() - 0.5) * 40, y + (Math.random() - 0.5) * 40, {
                vx: (Math.random() - 0.5) * 3,
                vy: -Math.random() * 3 - 2,
                size: Math.random() * 10 + 4,
                color: Math.random() > 0.4 ? '#86198f' : '#db2777', // Dark purple / Pinkish haki
                maxLife: Math.random() * 25 + 15,
                type: 'haki',
                drag: 0.95
            }));
        }
    }
    
    spawnRedHawkFire(x, y, count = 10, isBlast = false) {
        for (let i = 0; i < count; i++) {
            let angle = Math.random() * Math.PI * 2;
            let speed = isBlast ? Math.random() * 14 + 6 : Math.random() * 4 + 2;
            let vx = Math.cos(angle) * speed + (isBlast ? 4 : 0);
            let vy = Math.sin(angle) * speed - (isBlast ? 1 : 0);
            
            this.particles.push(new Particle(x, y, {
                vx: vx,
                vy: vy,
                size: Math.random() * 12 + 6,
                color: Math.random() > 0.4 ? '#facc15' : '#ef4444', // Gold / Crimson
                maxLife: Math.random() * 50 + 20,
                type: 'fire',
                drag: 0.96,
                gravity: -0.05 // Rise slightly like actual fire
            }));
        }
    }
    
    spawnGear5Lightning(x, y) {
        // Strike dynamic lightning
        let bolt = [];
        let curX = x + (Math.random() - 0.5) * 60;
        let curY = y - 400; // Strike from sky
        bolt.push({ x: curX, y: curY });
        
        while (curY < y + 100) {
            curX += (Math.random() - 0.5) * 60;
            curY += Math.random() * 40 + 20;
            bolt.push({ x: curX, y: curY });
        }
        
        this.lightningBolts.push({
            segments: bolt,
            life: 12,
            maxLife: 12,
            color: Math.random() > 0.5 ? '#facc15' : '#e0f2fe' // Golden Nika or pure white lightning
        });
        
        // Spawn radial sparks at the impact zone!
        for (let i = 0; i < 8; i++) {
            this.particles.push(new Particle(x, y, {
                vx: (Math.random() - 0.5) * 12,
                vy: (Math.random() - 0.5) * 12 - 2,
                size: Math.random() * 8 + 4,
                color: '#facc15',
                maxLife: 30,
                type: 'spark',
                drag: 0.94
            }));
        }
    }
    
    update(time, gear) {
        // Handle Camera Shake mechanics
        if (this.shakeTime > 0) {
            this.shakeOffset.x = (Math.random() - 0.5) * this.shakeIntensity;
            this.shakeOffset.y = (Math.random() - 0.5) * this.shakeIntensity;
            this.shakeTime--;
            this.shakeIntensity *= 0.95; // Smooth decay
        } else {
            this.shakeOffset.x = 0;
            this.shakeOffset.y = 0;
        }
        
        // Update Particles
        this.particles.forEach(p => p.update());
        this.particles = this.particles.filter(p => p.alpha > 0);
        
        // Update Shockwaves
        this.shockwaves.forEach(s => s.update());
        this.shockwaves = this.shockwaves.filter(s => s.active);
        
        // Update lightning bolts
        this.lightningBolts.forEach(b => b.life--);
        this.lightningBolts = this.lightningBolts.filter(b => b.life > 0);
        
        // Spawn environmental weather particles slowly based on backgrounds
        if (this.scenePreset === 'wano' && Math.random() < 0.07) {
            // Floating Cherry blossoms
            this.particles.push(new Particle(Math.random() * this.canvas.width, -20, {
                vx: -Math.random() * 2 - 1,
                vy: Math.random() * 1.5 + 1.0,
                size: Math.random() * 5 + 3,
                color: '#fbcfe8',
                maxLife: 350,
                type: 'petal',
                drag: 0.99
            }));
        } 
        else if (this.scenePreset === 'marineford' && Math.random() < 0.12) {
            // Light snow
            this.particles.push(new Particle(Math.random() * this.canvas.width, -10, {
                vx: (Math.random() - 0.5) * 1.0,
                vy: Math.random() * 1.2 + 0.8,
                size: Math.random() * 3 + 1,
                color: '#ffffff',
                maxLife: 400,
                type: 'snow',
                drag: 0.99
            }));
        }
    }
    
    drawBackground(time, gear) {
        let ctx = this.ctx;
        let w = this.canvas.width;
        let h = this.canvas.height;
        
        ctx.save();
        
        if (this.scenePreset === 'dark') {
            // Gradient Focus Void
            let grad = ctx.createRadialGradient(w/2, h/2, 50, w/2, h/2, w/2 + 200);
            if (gear === '2') {
                grad.addColorStop(0, '#310a12');
            } else if (gear === '4') {
                grad.addColorStop(0, '#2e0828');
            } else if (gear === '5') {
                grad.addColorStop(0, '#102035');
            } else {
                grad.addColorStop(0, '#111116');
            }
            grad.addColorStop(1, '#030305');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, w, h);
        }
        else if (this.scenePreset === 'cyber') {
            // Cyber Grid
            ctx.fillStyle = '#06060c';
            ctx.fillRect(0, 0, w, h);
            
            // Grid lines stretching in 3D perspective
            ctx.strokeStyle = 'rgba(255, 46, 86, 0.08)';
            if (gear === '5') ctx.strokeStyle = 'rgba(224, 242, 254, 0.08)';
            ctx.lineWidth = 1;
            
            let gridSpacing = 40;
            let timeShift = (time * 40) % gridSpacing;
            
            // Vertical radiating grid rays
            let centerX = w / 2;
            let horizonY = h / 2 - 100;
            
            for (let angle = -w; angle <= w * 2; angle += 80) {
                ctx.beginPath();
                ctx.moveTo(centerX, horizonY);
                ctx.lineTo(angle, h);
                ctx.stroke();
            }
            
            // Horizontal perspective lines
            for (let y = horizonY; y < h; y += 15) {
                let ratio = (y - horizonY) / (h - horizonY);
                ctx.strokeStyle = `rgba(255, 46, 86, ${0.02 + ratio * 0.15})`;
                if (gear === '5') ctx.strokeStyle = `rgba(224, 242, 254, ${0.02 + ratio * 0.15})`;
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(w, y);
                ctx.stroke();
            }
        }
        else if (this.scenePreset === 'sunny') {
            // Thousand Sunny Deck
            // Sky
            let skyGrad = ctx.createLinearGradient(0, 0, 0, h);
            skyGrad.addColorStop(0, '#0284c7');
            skyGrad.addColorStop(0.5, '#bae6fd');
            skyGrad.addColorStop(1, '#38bdf8');
            ctx.fillStyle = skyGrad;
            ctx.fillRect(0, 0, w, h);
            
            // Ocean at horizon
            ctx.fillStyle = '#0369a1';
            ctx.fillRect(0, h/2 - 50, w, 60);
            
            // Deck lawn floor
            ctx.fillStyle = '#15803d'; // Green lawn
            ctx.fillRect(0, h/2 + 10, w, h);
            
            // Wooden fence boundary
            ctx.fillStyle = '#ca8a04';
            ctx.fillRect(0, h/2 + 10, w, 12);
            ctx.fillStyle = '#854d0e';
            ctx.fillRect(0, h/2 + 22, w, 8);
        }
        else if (this.scenePreset === 'wano') {
            // Wano Roof (Full Moon)
            // Night sky gradient
            let nightGrad = ctx.createLinearGradient(0, 0, 0, h);
            nightGrad.addColorStop(0, '#090d16');
            nightGrad.addColorStop(0.6, '#181b29');
            nightGrad.addColorStop(1, '#0e0e15');
            ctx.fillStyle = nightGrad;
            ctx.fillRect(0, 0, w, h);
            
            // Giant glowing moon in middle
            let moonRadius = Math.min(w, h) * 0.28;
            let moonX = w / 2;
            let moonY = h / 2 - 40;
            
            let moonGrad = ctx.createRadialGradient(moonX, moonY, 10, moonX, moonY, moonRadius);
            moonGrad.addColorStop(0, '#fef08a'); // Soft warm white/yellow
            moonGrad.addColorStop(0.7, '#fef9c3');
            moonGrad.addColorStop(1, 'rgba(254, 240, 138, 0)');
            
            ctx.fillStyle = moonGrad;
            ctx.beginPath();
            ctx.arc(moonX, moonY, moonRadius, 0, Math.PI*2);
            ctx.fill();
            
            // Clouds passing moon
            ctx.fillStyle = 'rgba(255, 255, 255, 0.05)';
            ctx.beginPath();
            ctx.ellipse(moonX - 80 + Math.sin(time*0.2)*50, moonY - 30, moonRadius * 0.9, 12, 0, 0, Math.PI*2);
            ctx.ellipse(moonX + 60 - Math.sin(time*0.1)*30, moonY + 40, moonRadius * 1.1, 15, 0, 0, Math.PI*2);
            ctx.fill();
            
            // Wano Tiled Roof floor
            ctx.fillStyle = '#1e293b'; // Slate roof
            ctx.beginPath();
            ctx.moveTo(0, h/2 + 130);
            ctx.lineTo(w, h/2 + 130);
            ctx.lineTo(w, h);
            ctx.lineTo(0, h);
            ctx.closePath();
            ctx.fill();
            
            // Tiled lines
            ctx.strokeStyle = '#0f172a';
            ctx.lineWidth = 4;
            for (let rx = -50; rx < w + 50; rx += 35) {
                ctx.beginPath();
                ctx.moveTo(rx, h/2 + 130);
                ctx.lineTo(rx + 80, h);
                ctx.stroke();
            }
        }
        else if (this.scenePreset === 'marineford') {
            // Marineford Icefield
            let skyGrad = ctx.createLinearGradient(0, 0, 0, h);
            skyGrad.addColorStop(0, '#1e293b');
            skyGrad.addColorStop(0.6, '#334155');
            skyGrad.addColorStop(1, '#475569');
            ctx.fillStyle = skyGrad;
            ctx.fillRect(0, 0, w, h);
            
            // Horizon
            ctx.fillStyle = '#0f172a';
            ctx.fillRect(0, h/2 - 10, w, 20);
            
            // Ice Field
            let iceGrad = ctx.createLinearGradient(0, h/2 + 10, 0, h);
            iceGrad.addColorStop(0, '#7dd3fc'); // Cyan ice
            iceGrad.addColorStop(1, '#0284c7');
            ctx.fillStyle = iceGrad;
            ctx.fillRect(0, h/2 + 10, w, h);
            
            // Ice cracks
            ctx.strokeStyle = '#e0f2fe';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(w * 0.2, h/2 + 30);
            ctx.lineTo(w * 0.25, h/2 + 90);
            ctx.lineTo(w * 0.15, h/2 + 140);
            
            ctx.moveTo(w * 0.75, h/2 + 40);
            ctx.lineTo(w * 0.68, h/2 + 100);
            ctx.lineTo(w * 0.78, h/2 + 160);
            ctx.stroke();
        }
        
        ctx.restore();
    }
    
    drawOverlayEffects(cameraShakeOffset = {x: 0, y: 0}) {
        let ctx = this.ctx;
        
        ctx.save();
        ctx.translate(cameraShakeOffset.x, cameraShakeOffset.y);
        
        // Draw lightning bolts first
        this.lightningBolts.forEach(bolt => {
            ctx.save();
            ctx.strokeStyle = bolt.color;
            ctx.shadowColor = bolt.color;
            ctx.shadowBlur = 20;
            ctx.lineWidth = Math.random() * 4 + 2;
            
            ctx.beginPath();
            let segs = bolt.segments;
            ctx.moveTo(segs[0].x, segs[0].y);
            for (let i = 1; i < segs.length; i++) {
                ctx.lineTo(segs[i].x, segs[i].y);
            }
            ctx.stroke();
            ctx.restore();
        });
        
        // Draw active shockwaves
        this.shockwaves.forEach(s => s.draw(ctx));
        
        // Draw particle systems
        this.particles.forEach(p => p.draw(ctx));
        
        ctx.restore();
    }
}
