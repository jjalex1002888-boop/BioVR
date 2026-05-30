import { state } from './state.js';
import { audio } from './audio.js';
import {
    ParallaxBackground,
    MonkeyCharacter2D,
    HumanChaser2D,
    drawLog,
    drawArch,
    drawSpikes,
    drawLava,
    drawBanana,
    drawPowerup
} from './entities.js';

/**
 * Banana Run 2D - Core Game Loop & Gameplay Engine
 */
class GameEngine2D {
    constructor() {
        this.canvas = null;
        this.ctx = null;
        this.container = null;
        
        // Size bounds
        this.width = 800;
        this.height = 450;
        
        // Characters
        this.monkey = null;
        this.chaser = null;
        
        // Environment layers
        this.background = null;
        this.scrollX = 0;
        
        // Procedural track lists
        this.obstacles = [];
        this.bananas = [];
        this.particles = [];
        
        // World coordinates tracking
        this.monkeyWorldX = 0;
        this.spawnDistanceThreshold = 0;
        
        // Easing values for player lane transition
        this.targetLane = 0; // -1: Top, 0: Mid, 1: Bottom
        this.currentLane = 0;
        
        this.monkeyScreenX = 0; // fixed horizontal screen point
        this.monkeyY = 0;      // active lane Y tracker
        this.monkeyScale = 1.0;
        
        // Action physics
        this.actionState = 'run'; // run, jump, slide
        this.actionTimer = 0;
        this.jumpDuration = 700; // ms
        this.slideDuration = 650; // ms
        
        // Power-ups and timers
        this.powerupBannerText = '';
        this.powerupBannerOpacity = 0;
        
        // Clock variables
        this.lastTime = 0;
        this.elapsedTime = 0;
        this.previewActive = true;
        
        // Swipe controller markers
        this.touchStart = { x: 0, y: 0 };
    }

    init(canvasId) {
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) return;
        this.ctx = this.canvas.getContext('2d');
        this.container = this.canvas.parentElement;

        // Easing screen sizing
        this.resize();
        window.addEventListener('resize', this.resize.bind(this));

        // Setup Controls
        this.setupControls();

        // Instantiate components
        this.resetEnvironment();

        // Start Loop
        requestAnimationFrame((t) => this.animateLoop(t));
    }

    resize() {
        this.width = this.container.clientWidth;
        this.height = this.container.clientHeight;
        this.canvas.width = this.width;
        this.canvas.height = this.height;

        // Recalculate fixed positions based on screen bounds
        this.monkeyScreenX = this.width * 0.32;
        if (this.previewActive) {
            this.monkeyY = this.height * 0.65;
        } else {
            this.monkeyY = this.getLaneY(this.currentLane);
        }
    }

    resetEnvironment() {
        const activeSkin = state.skinsCatalog[state.activeSkin] || state.skinsCatalog.default;
        
        // Load clean rendering objects
        this.background = new ParallaxBackground();
        this.monkey = new MonkeyCharacter2D(activeSkin);
        this.chaser = new HumanChaser2D();
        
        // Reset coordinates
        this.scrollX = 0;
        this.monkeyWorldX = 0;
        this.spawnDistanceThreshold = 250; // spawn first obstacle 250px ahead
        this.targetLane = 0;
        this.currentLane = 0;
        
        this.obstacles = [];
        this.bananas = [];
        this.particles = [];
        
        this.actionState = 'run';
        this.actionTimer = 0;
        
        // Position placement
        this.resize();

        // Prepopulate environment with initial fireflies
        this.generateFireflies();
    }

    generateFireflies() {
        const count = state.graphicsQuality === 'low' ? 15 : 35;
        for (let i = 0; i < count; i++) {
            this.particles.push({
                x: Math.random() * this.width,
                y: 50 + Math.random() * (this.height * 0.5),
                vx: -0.4 - Math.random() * 0.8,
                vy: (Math.random() - 0.5) * 0.3,
                size: 1.5 + Math.random() * 2,
                color: Math.random() > 0.5 ? 'rgba(0, 255, 204, 0.7)' : 'rgba(255, 204, 0, 0.7)',
                type: 'firefly',
                life: 9999,
                maxLife: 9999
            });
        }
    }

    getLaneY(laneIndex) {
        const baseY = this.height * 0.81;
        const laneSpacing = this.height * 0.08;
        return baseY + laneIndex * laneSpacing;
    }

    getLaneScale(laneIndex) {
        // Pseudo 3D perspective scales
        if (laneIndex === -1) return 0.82; // background lane
        if (laneIndex === 1) return 1.18;  // foreground lane
        return 1.0; // center lane
    }

    setupControls() {
        // Keyboard Controls
        window.addEventListener('keydown', (e) => {
            if (!state.isPlaying || state.isPaused || state.isGameOver) return;

            if (e.key === 'ArrowUp' || e.key === 'w' || e.key === 'W') {
                e.preventDefault();
                // If sliding, cancel slide to shift lane or jump
                if (this.actionState === 'slide') {
                    this.actionState = 'run';
                }
                this.shiftLane(-1); // Shifting lane up screen (into background track)
            }
            if (e.key === 'ArrowDown' || e.key === 's' || e.key === 'S') {
                e.preventDefault();
                // Slide if running, else shift lane down screen
                if (this.actionState === 'run') {
                    this.triggerSlide();
                } else {
                    this.shiftLane(1); // Shifting lane down screen (into foreground track)
                }
            }
            if (e.key === 'ArrowLeft' || e.key === 'a' || e.key === 'A') {
                // Retro runner can jump or adjust lane
                this.triggerJump();
            }
            if (e.key === 'ArrowRight' || e.key === 'd' || e.key === 'D') {
                // Alternative key jump mapping
                this.triggerJump();
            }
            if (e.key === ' ') {
                e.preventDefault();
                this.triggerJump();
            }
        });

        // Swipe Controls for Mobile/Trackpads
        this.canvas.addEventListener('touchstart', (e) => {
            this.touchStart.x = e.touches[0].clientX;
            this.touchStart.y = e.touches[0].clientY;
        }, { passive: true });

        this.canvas.addEventListener('touchend', (e) => {
            if (!state.isPlaying || state.isPaused || state.isGameOver) return;

            const diffX = e.changedTouches[0].clientX - this.touchStart.x;
            const diffY = e.changedTouches[0].clientY - this.touchStart.y;
            const thresh = 35;

            if (Math.abs(diffX) > Math.abs(diffY)) {
                // Horizontal Swipes trigger Jumps
                if (Math.abs(diffX) > thresh) this.triggerJump();
            } else {
                // Vertical Swipes trigger Lane Shifts and slides
                if (diffY < -thresh) {
                    if (this.actionState === 'slide') this.actionState = 'run';
                    this.shiftLane(-1);
                }
                if (diffY > thresh) {
                    if (this.actionState === 'run') {
                        this.triggerSlide();
                    } else {
                        this.shiftLane(1);
                    }
                }
            }
        }, { passive: true });
    }

    shiftLane(direction) {
        // bound lane shifting index to -1, 0, 1
        this.targetLane = Math.max(-1, Math.min(1, this.targetLane + direction));
    }

    triggerJump() {
        if (this.actionState !== 'run') return;
        this.actionState = 'jump';
        this.actionTimer = this.jumpDuration;
        audio.playJump();

        // Jump lift particles
        this.spawnImpactParticles(this.monkeyScreenX, this.monkeyY, 8, '#dfb195');
    }

    triggerSlide() {
        if (this.actionState !== 'run') return;
        this.actionState = 'slide';
        this.actionTimer = this.slideDuration;
        audio.playSlide();
    }

    spawnImpactParticles(x, y, count, color) {
        if (state.graphicsQuality === 'low') return;
        for (let i = 0; i < count; i++) {
            this.particles.push({
                x,
                y,
                vx: (Math.random() - 0.6) * 4,
                vy: -Math.random() * 3 - 1,
                size: 2 + Math.random() * 3,
                color,
                type: 'impact',
                life: 300,
                maxLife: 300
            });
        }
    }

    startGame() {
        state.reset();
        audio.init();
        audio.startMusic();
        
        this.resetEnvironment();
        this.previewActive = false;
        state.isPlaying = true;

        // Synchronize display UI overlays
        document.getElementById('start-screen').classList.add('hidden');
        document.getElementById('gameover-screen').classList.add('hidden');
        document.getElementById('hud').classList.remove('hidden');

        // Reset clocks
        this.lastTime = performance.now();
    }

    togglePause() {
        if (!state.isPlaying || state.isGameOver) return;
        state.isPaused = !state.isPaused;
        
        if (state.isPaused) {
            audio.stopMusic();
            document.getElementById('pause-screen').classList.remove('hidden');
        } else {
            this.lastTime = performance.now(); // reset clock delta accumulator
            audio.startMusic();
            document.getElementById('pause-screen').classList.add('hidden');
        }
    }

    triggerGameOver() {
        state.isGameOver = true;
        state.isPlaying = false;
        audio.stopMusic();
        audio.playCrash();

        // Trigger heavy splash blast of colors!
        this.spawnImpactParticles(this.monkeyScreenX, this.monkeyY, 35, '#ff3333');
        this.spawnImpactParticles(this.monkeyScreenX, this.monkeyY, 20, '#ffcc00');

        // Shake viewport intensely
        this.screenShakeIntensity = 22;

        document.getElementById('hud').classList.add('hidden');
        document.getElementById('gameover-screen').classList.remove('hidden');

        // Refresh stats elements
        document.getElementById('final-score').innerText = state.score;
        document.getElementById('final-distance').innerText = Math.floor(state.distance) + 'm';
        document.getElementById('final-bananas').innerText = state.bananasCollectedThisRun;
        document.getElementById('gameover-highscore').innerText = state.highScore;

        state.addBananas(0); // force LocalStorage saving triggers
        state.saveLocalStorage();

        document.getElementById('total-bananas-menu').innerText = state.totalBananas;
    }

    // SPANNING PROCEDURAL ELEMENTS ENDLESSLY
    proceduralGenerationUpdate(dt, movementSpeed) {
        // World coordinates increment based on motion speed
        this.monkeyWorldX += movementSpeed * 35 * dt;
        
        // Spawn elements ahead of monkey
        if (this.monkeyWorldX > this.spawnDistanceThreshold) {
            const roll = Math.random();
            if (roll < 0.35) {
                this.spawnObstacle();
                this.spawnDistanceThreshold += 350 + Math.random() * 250; // spacing spacing
            } else {
                this.spawnBananaRow();
                this.spawnDistanceThreshold += 280 + Math.random() * 180;
            }
        }
    }

    spawnObstacle() {
        const types = ['log', 'arch', 'barrier', 'lava'];
        const type = types[Math.floor(Math.random() * types.length)];
        
        let width = 50;
        let height = 50;
        let lanes = [0];

        if (type === 'log') {
            width = 110;
            height = 20;
            lanes = [-1, 0, 1]; // Blocks all tracks horizontally! Requires JUMP
        } else if (type === 'arch') {
            width = 95;
            height = 70; // high portal structure
            lanes = [-1, 0, 1]; // Blocks columns on left/right, portal requires SLIDE
        } else if (type === 'barrier') {
            width = 55;
            height = 42;
            // Blocks two random lanes
            const openLane = Math.floor(Math.random() * 3) - 1;
            lanes = [-1, 0, 1].filter(l => l !== openLane);
        } else if (type === 'lava') {
            width = 75;
            height = 14;
            // Blocks one single lane
            lanes = [Math.floor(Math.random() * 3) - 1];
        }

        this.obstacles.push({
            type,
            worldX: this.monkeyWorldX + this.width + 100, // spawn off screen
            lanes,
            width,
            height,
            passed: false
        });
    }

    spawnBananaRow() {
        const lane = Math.floor(Math.random() * 3) - 1;
        const count = 3 + Math.floor(Math.random() * 3);
        const hasPowerup = Math.random() < 0.12; // 12% power-up rate
        const powerupType = ['magnet', 'shield', 'boost'][Math.floor(Math.random() * 3)];

        for (let i = 0; i < count; i++) {
            const worldX = this.monkeyWorldX + this.width + 100 + i * 36;
            let isPowerup = false;
            let pType = null;

            if (hasPowerup && i === Math.floor(count / 2)) {
                isPowerup = true;
                pType = powerupType;
            }

            this.bananas.push({
                worldX,
                lane,
                isPowerup,
                powerupType: pType,
                yBobOffset: Math.sin(i * 0.7) * 8
            });
        }
    }

    showPowerupBanner(type) {
        this.powerupBannerText = type.toUpperCase() + ' ACTIVATED!';
        this.powerupBannerOpacity = 1.0;
        
        // Re-inject alert fade
        if (this.bannerTimeout) clearTimeout(this.bannerTimeout);
        this.bannerTimeout = setTimeout(() => {
            this.powerupBannerOpacity = 0.0;
        }, 1500);
    }

    triggerShieldFlash() {
        const flash = document.getElementById('damage-flash');
        flash.style.backgroundColor = 'rgba(255, 0, 255, 0.4)';
        flash.style.opacity = 1;
        setTimeout(() => flash.style.opacity = 0, 300);
        this.screenShakeIntensity = 8;
    }

    triggerStumbleFlash() {
        const flash = document.getElementById('damage-flash');
        flash.style.backgroundColor = 'rgba(255, 0, 0, 0.45)';
        flash.style.opacity = 1;
        setTimeout(() => flash.style.opacity = 0, 450);
        this.screenShakeIntensity = 12;
    }

    updateHUD() {
        document.getElementById('hud-score').innerText = state.score;
        document.getElementById('hud-distance').innerText = Math.floor(state.distance) + 'm';
        document.getElementById('hud-bananas').innerText = state.bananasCollectedThisRun;

        // Power-ups indicators updates
        const magBar = document.getElementById('magnet-progress');
        if (state.activePowerups.magnet > 0) {
            magBar.parentElement.classList.remove('hidden');
            magBar.style.width = (state.activePowerups.magnet / state.powerupDurations.magnet * 100) + '%';
        } else {
            magBar.parentElement.classList.add('hidden');
        }

        const bstBar = document.getElementById('boost-progress');
        if (state.activePowerups.boost > 0) {
            bstBar.parentElement.classList.remove('hidden');
            bstBar.style.width = (state.activePowerups.boost / state.powerupDurations.boost * 100) + '%';
        } else {
            bstBar.parentElement.classList.add('hidden');
        }

        const shldInd = document.getElementById('hud-shield-indicator');
        if (state.activePowerups.shield) {
            shldInd.classList.remove('hidden');
        } else {
            shldInd.classList.add('hidden');
        }

        // Pulse warning vignette
        const warning = document.getElementById('stumble-warning');
        if (state.stumbled) {
            warning.classList.remove('hidden');
            warning.style.opacity = 0.55 + Math.sin(this.elapsedTime * 8) * 0.4;
        } else {
            warning.classList.add('hidden');
        }
    }

    // PARTICLE ENGINE EMITTER
    updateParticles(dt) {
        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];
            p.life -= dt * 1000;
            
            if (p.life <= 0) {
                // If fireflies die, wrap them back to screen edge to save allocation allocations!
                if (p.type === 'firefly') {
                    p.x = this.width + 10;
                    p.y = 50 + Math.random() * (this.height * 0.55);
                    p.life = 9999;
                } else {
                    this.particles.splice(i, 1);
                }
                continue;
            }

            p.x += p.vx;
            p.y += p.vy;

            // gravity details for impact dust sparks
            if (p.type === 'impact') {
                p.vy += 0.12; // slow fall down
            }
        }
    }

    spawnSkinTrail(x, y) {
        if (state.graphicsQuality === 'low') return;
        const skinId = state.activeSkin;
        let color = null;
        let shape = 'circle';

        if (skinId === 'cyber') {
            color = 'rgba(0, 255, 204, 0.8)';
            shape = 'square';
        } else if (skinId === 'fire') {
            color = Math.random() > 0.5 ? 'rgba(255, 85, 0, 0.8)' : 'rgba(255, 204, 0, 0.8)';
        } else if (skinId === 'gold') {
            color = 'rgba(255, 215, 0, 0.85)';
            shape = 'diamond';
        }

        if (!color) return;

        this.particles.push({
            x: x - 10,
            y: y - 5 + (Math.random() - 0.5) * 15,
            vx: -2 - Math.random() * 2,
            vy: (Math.random() - 0.5) * 1.5,
            size: 2.5 + Math.random() * 3,
            color,
            shape,
            type: 'trail',
            life: 250 + Math.random() * 200,
            maxLife: 450
        });
    }

    drawParticles() {
        this.particles.forEach(p => {
            this.ctx.save();
            this.ctx.globalAlpha = Math.max(0, p.life / p.maxLife);
            this.ctx.fillStyle = p.color;

            // Draw glowing halo borders if needed
            if (p.type === 'firefly') {
                this.ctx.shadowColor = p.color;
                this.ctx.shadowBlur = 8;
            }

            if (p.shape === 'square') {
                this.ctx.fillRect(p.x - p.size/2, p.y - p.size/2, p.size, p.size);
            } else if (p.shape === 'diamond') {
                this.ctx.beginPath();
                this.ctx.moveTo(p.x, p.y - p.size);
                this.ctx.lineTo(p.x + p.size, p.y);
                this.ctx.lineTo(p.x, p.y + p.size);
                this.ctx.lineTo(p.x - p.size, p.y);
                this.ctx.closePath();
                this.ctx.fill();
            } else {
                this.ctx.beginPath();
                this.ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                this.ctx.fill();
            }
            this.ctx.restore();
        });
    }

    // DRAW THE PATH ROADWAYS & GOLDEN RUINS BORDERS
    drawRoad(dt) {
        // Draw primary asphalt band
        const baseY = this.height * 0.81;
        const laneSpacing = this.height * 0.08;

        const roadTopY = baseY - laneSpacing * 1.6;
        const roadBotY = baseY + laneSpacing * 1.6;
        const roadHeight = roadBotY - roadTopY;

        // Dark Mossy Roadway Gradient
        const rGrad = this.ctx.createLinearGradient(0, roadTopY, 0, roadBotY);
        rGrad.addColorStop(0, '#101d14'); // forest shadow top
        rGrad.addColorStop(0.3, '#1d2e23');
        rGrad.addColorStop(0.7, '#1d2e23');
        rGrad.addColorStop(1, '#0c1610'); // shadow base bottom
        this.ctx.fillStyle = rGrad;
        this.ctx.fillRect(0, roadTopY, this.width, roadHeight);

        // Ancient golden borders (thick tracks)
        this.ctx.strokeStyle = '#5a3e1a';
        this.ctx.lineWidth = 6;
        this.ctx.beginPath();
        this.ctx.moveTo(0, roadTopY + 3);
        this.ctx.lineTo(this.width, roadTopY + 3);
        this.ctx.moveTo(0, roadBotY - 3);
        this.ctx.lineTo(this.width, roadBotY - 3);
        this.ctx.stroke();

        // Subtly draw lane splitting guide lines
        this.ctx.strokeStyle = 'rgba(255, 255, 255, 0.06)';
        this.ctx.lineWidth = 2;
        this.ctx.setLineDash([12, 18]);
        this.ctx.beginPath();
        this.ctx.moveTo(0, baseY - laneSpacing * 0.5);
        this.ctx.lineTo(this.width, baseY - laneSpacing * 0.5);
        this.ctx.moveTo(0, baseY + laneSpacing * 0.5);
        this.ctx.lineTo(this.width, baseY + laneSpacing * 0.5);
        this.ctx.stroke();
        this.ctx.setLineDash([]); // reset
    }

    checkCollisions() {
        if (state.isGameOver) return;

        // 1. BANANAS OVERLAPPING CHECK
        const magnetActive = state.activePowerups.magnet > 0;

        for (let i = this.bananas.length - 1; i >= 0; i--) {
            const b = this.bananas[i];
            const bananaScreenX = b.worldX - this.monkeyWorldX + this.monkeyScreenX;

            // Banana magnet pull lerp
            if (magnetActive && bananaScreenX < this.width + 100 && bananaScreenX > -50) {
                const distToMonkey = Math.hypot(bananaScreenX - this.monkeyScreenX, this.getLaneY(b.lane) - this.monkeyY);
                if (distToMonkey < 180) {
                    // Fly towards player!
                    b.worldX += (this.monkeyWorldX - b.worldX) * 0.22;
                    b.lane += (this.currentLane - b.lane) * 0.22;
                }
            }

            const currentBananaScreenX = b.worldX - this.monkeyWorldX + this.monkeyScreenX;
            const currentBananaScreenY = this.getLaneY(b.lane) - 22 + b.yBobOffset;

            // Simple distance check in 2D space
            const dist = Math.hypot(currentBananaScreenX - this.monkeyScreenX, currentBananaScreenY - (this.monkeyY - 24));
            if (dist < 32) {
                // Collect!
                this.bananas.splice(i, 1);
                
                if (b.isPowerup) {
                    state.activatePowerup(b.powerupType);
                    if (b.powerupType === 'magnet') audio.playMagnet();
                    if (b.powerupType === 'shield') audio.playShield();
                    if (b.powerupType === 'boost') audio.playBoost();
                    
                    this.showPowerupBanner(b.powerupType);
                } else {
                    state.addBananas(1);
                    audio.playCollect();
                }

                // Collect sparks trigger
                const sparkColor = b.isPowerup ? '#ff00ff' : '#ffcc00';
                this.spawnImpactParticles(currentBananaScreenX, currentBananaScreenY, 6, sparkColor);
                continue;
            }

            // Prune off-screen elements
            if (bananaScreenX < -60) {
                this.bananas.splice(i, 1);
            }
        }

        // 2. OBSTACLES OVERLAPPING CHECK
        const boostActive = state.activePowerups.boost > 0;

        for (let i = this.obstacles.length - 1; i >= 0; i--) {
            const obs = this.obstacles[i];
            const obsScreenX = obs.worldX - this.monkeyWorldX + this.monkeyScreenX;
            
            // Check collision only when horizontal overlapping is within narrow bounds
            if (Math.abs(obsScreenX - this.monkeyScreenX) < (obs.width / 2 + 15)) {
                // Are we in a blocked lane?
                const isBlockedLane = obs.lanes.includes(this.currentLane);
                if (isBlockedLane && !obs.passed) {
                    let isHitting = false;

                    if (boostActive) {
                        // Boost is active - fly above everything! No crash.
                        isHitting = false;
                    } else if (obs.type === 'arch') {
                        // Arch requires slide
                        if (this.actionState !== 'slide') {
                            isHitting = true;
                        }
                    } else if (obs.type === 'log' || obs.type === 'barrier') {
                        // Hurdles require jump
                        if (this.actionState !== 'jump') {
                            isHitting = true;
                        } else {
                            // If jumped, check height clearance
                            const jumpProgress = 1.0 - (this.actionTimer / this.jumpDuration);
                            const jumpY = Math.sin(jumpProgress * Math.PI) * 75; // vertical lift
                            
                            // Barrier requires max height clearance
                            if (obs.type === 'barrier' && jumpY < 48) {
                                isHitting = true;
                            }
                        }
                    } else if (obs.type === 'lava') {
                        // Lava requires jump
                        if (this.actionState !== 'jump') {
                            isHitting = true;
                        }
                    }

                    if (isHitting) {
                        obs.passed = true; // prevent multi-collisions in same frames
                        const result = state.triggerStumble();

                        this.obstacles.splice(i, 1); // remove
                        
                        if (result === 'shield_break') {
                            audio.playShieldBreak();
                            this.triggerShieldFlash();
                        } else if (result === 'stumbled') {
                            audio.playStumble();
                            this.triggerStumbleFlash();
                        } else if (result === 'caught') {
                            this.triggerGameOver();
                        }
                        break;
                    }
                }
            }

            // Prune obstacles falling off left boundary
            if (obsScreenX < -obs.width - 50) {
                this.obstacles.splice(i, 1);
            }
        }
    }

    animateLoop(timestamp) {
        requestAnimationFrame((t) => this.animateLoop(t));

        if (!this.lastTime) this.lastTime = timestamp;
        let dt = (timestamp - this.lastTime) / 1000;
        this.lastTime = timestamp;

        // cap frame accumulator to prevent background-tab warp crashes
        dt = Math.min(0.08, dt);
        this.elapsedTime += dt;

        // Expose global speed factor to sync sound sequencer rhythm
        window.gameSpeedFactor = state.activePowerups.boost > 0 ? 2.5 : (state.speed / 15);

        // CLEAR VIEWPORT
        this.ctx.fillStyle = '#020503';
        this.ctx.fillRect(0, 0, this.width, this.height);

        // Apply dynamic screen shakes on impacts
        this.ctx.save();
        if (this.screenShakeIntensity > 0.1) {
            const dx = (Math.random() - 0.5) * this.screenShakeIntensity;
            const dy = (Math.random() - 0.5) * this.screenShakeIntensity;
            this.ctx.translate(dx, dy);
            this.screenShakeIntensity *= 0.88; // fade shake out
        }

        // 1. ANIME BACKGROUND PARALLAX
        this.background.draw(this.ctx, this.width, this.height, this.scrollX);

        // 2. MAIN MENU MODE preview SPIN / IDLE
        if (this.previewActive) {
            // Draw menu preview idle
            this.monkeyY = this.height * 0.62;
            this.monkeyScale = 1.6;
            this.monkey.draw(this.ctx, this.width * 0.5, this.monkeyY, this.monkeyScale, 'idle', 'run', 0, this.elapsedTime * 0.15);
            
            // Draw fireflies drifting
            this.updateParticles(dt);
            this.drawParticles();
            
            this.ctx.restore(); // pop screen shake save
            return;
        }

        // PAUSED STATE DRAWING
        if (state.isPaused) {
            this.drawRoad(dt);
            this.drawBananasAndObstacles();
            this.monkey.draw(this.ctx, this.monkeyScreenX, this.monkeyY, this.monkeyScale, 'paused', this.actionState, this.actionTimer, this.elapsedTime);
            this.chaser.draw(this.ctx, this.monkeyScreenX - 180, this.monkeyY, 1.25, state.speed, this.elapsedTime);
            this.drawParticles();
            this.ctx.restore();
            return;
        }

        // GAME OVER HOLD STATE DRAWING
        if (state.isGameOver) {
            this.drawRoad(dt);
            this.drawBananasAndObstacles();
            this.drawParticles();
            this.ctx.restore();
            return;
        }

        // 3. RUNNING CALCULATIONS
        let movementSpeed = state.speed;
        if (state.activePowerups.boost > 0) {
            movementSpeed = state.speed * 2.2;
        }

        // Increment background scrolls
        this.scrollX += movementSpeed * 35 * dt;

        // Distance & Powerups updates
        state.updateDistance(dt);
        state.updatePowerups(dt * 1000);

        // Procedural spawner tick
        this.proceduralGenerationUpdate(dt, movementSpeed);

        // 4. ROADWAYS BACKGROUND GRID
        this.drawRoad(dt);

        // 5. BANANAS AND OBSTACLES PLACEMENT
        this.drawBananasAndObstacles();

        // 6. PLAYER POSITION PHYSICS & INTERPOLATION
        // Easing interpolation for lane vertical coordinates
        const targetY = this.getLaneY(this.targetLane);
        const targetScale = this.getLaneScale(this.targetLane);
        
        this.monkeyY += (targetY - this.monkeyY) * 16 * dt;
        this.monkeyScale += (targetScale - this.monkeyScale) * 16 * dt;
        this.currentLane = Math.round((this.monkeyY - this.height * 0.81) / (this.height * 0.08));

        // Jump vertical gravitational offset calculus
        let actionYOffset = 0;
        if (this.actionState === 'jump') {
            this.actionTimer -= dt * 1000;
            const progress = 1.0 - (this.actionTimer / this.jumpDuration);
            // Parabolic arc formula
            actionYOffset = Math.sin(progress * Math.PI) * 75; // vertical height limit
            
            if (this.actionTimer <= 0) {
                this.actionState = 'run';
            }
        } else if (this.actionState === 'slide') {
            this.actionTimer -= dt * 1000;
            if (this.actionTimer <= 0) {
                this.actionState = 'run';
            }
            
            // Kick up slide friction dust sparks
            if (state.graphicsQuality === 'high' && Math.random() > 0.4) {
                this.particles.push({
                    x: this.monkeyScreenX,
                    y: this.monkeyY,
                    vx: -2 - Math.random() * 4,
                    vy: -Math.random() * 1.5,
                    size: 1.5 + Math.random() * 2,
                    color: 'rgba(255, 255, 255, 0.45)',
                    type: 'friction',
                    life: 200,
                    maxLife: 200
                });
            }
        }

        // Active Boost flies automatically
        if (state.activePowerups.boost > 0) {
            actionYOffset = 110; // hover high
        }

        // Emit skin trails
        this.spawnSkinTrail(this.monkeyScreenX, this.monkeyY - actionYOffset);

        // Draw Player Monkey
        this.monkey.draw(
            this.ctx, 
            this.monkeyScreenX, 
            this.monkeyY - actionYOffset, 
            this.monkeyScale, 
            'run', 
            state.activePowerups.boost > 0 ? 'run' : this.actionState, 
            this.actionTimer, 
            this.elapsedTime
        );

        // 7. ACTIVE SHIELD SPHERE BUBBLE EFFECT
        if (state.activePowerups.shield) {
            this.ctx.save();
            this.ctx.globalAlpha = 0.45 + Math.sin(this.elapsedTime * 9) * 0.15;
            this.ctx.strokeStyle = '#ff00ff';
            this.ctx.lineWidth = 3.5;
            ctx.shadowColor = '#ff00ff';
            ctx.shadowBlur = 12;
            this.ctx.beginPath();
            this.ctx.arc(this.monkeyScreenX, this.monkeyY - actionYOffset - 18, 38, 0, Math.PI * 2);
            this.ctx.stroke();
            this.ctx.restore();
        }

        // 8. HUMAN CHASER PHYSICS
        // Angry chaser lungs closer on stumble, or falls far behind on Sprint Boost
        let targetChaserDistance = state.stumbled ? 110 : 250;
        if (state.activePowerups.boost > 0) {
            targetChaserDistance = 450; // drops off screen
        }

        this.chaserScreenDistance = this.chaserScreenDistance || 250;
        const interpSpeed = state.stumbled ? 10 * dt : 2.5 * dt;
        this.chaserScreenDistance += (targetChaserDistance - this.chaserScreenDistance) * interpSpeed;

        const chaserX = this.monkeyScreenX - this.chaserScreenDistance;
        const chaserScale = this.monkeyScale * 1.15; // chaser is slightly larger than monkey
        
        // Chaser runs trail on similar lane vertical alignment
        this.chaser.draw(this.ctx, chaserX, this.monkeyY, chaserScale, movementSpeed, this.elapsedTime);

        // 9. UPDATE PARTICLES & DRAW HUD ELEMENTS
        this.updateParticles(dt);
        this.drawParticles();

        // 10. COLLISION CHECKS
        this.checkCollisions();
        this.updateHUD();

        // Draw powerup alert banner text overlay
        if (this.powerupBannerOpacity > 0.05) {
            this.ctx.save();
            this.ctx.globalAlpha = this.powerupBannerOpacity;
            this.ctx.font = 'bold 22px Outfit, Poppins, sans-serif';
            this.ctx.fillStyle = '#00ff66';
            this.ctx.shadowColor = '#00ff66';
            this.ctx.shadowBlur = 8;
            this.ctx.textAlign = 'center';
            this.ctx.fillText(this.powerupBannerText, this.width / 2, this.height * 0.28);
            this.ctx.restore();
        }

        this.ctx.restore(); // restore viewport shake save
    }

    drawBananasAndObstacles() {
        // Draw lane elements
        // Sort elements by Y coordinate depth levels (top track is drawn first, foreground overlays it!)
        
        // 1. Draw top lane objects (-1)
        this.drawTrackObjectsForLane(-1);
        
        // 2. Draw middle lane objects (0)
        this.drawTrackObjectsForLane(0);
        
        // 3. Draw bottom lane objects (1)
        this.drawTrackObjectsForLane(1);
    }

    drawTrackObjectsForLane(laneIndex) {
        const laneY = this.getLaneY(laneIndex);
        const laneScale = this.getLaneScale(laneIndex);

        // A. Draw Bananas in this lane
        this.bananas.forEach(b => {
            if (b.lane === laneIndex) {
                const bX = b.worldX - this.monkeyWorldX + this.monkeyScreenX;
                const bY = laneY - 22 + b.yBobOffset;
                
                if (b.isPowerup) {
                    drawPowerup(this.ctx, bX, bY, b.powerupType, this.elapsedTime);
                } else {
                    drawBanana(this.ctx, bX, bY, laneScale);
                }
            }
        });

        // B. Draw Obstacles in this lane
        this.obstacles.forEach(obs => {
            // Log & stone arch occupy all lanes
            const blocksThisLane = obs.lanes.includes(laneIndex);
            
            // Render the obstacle once, bound it to the highest priority track layer it spans
            const primaryLanes = obs.lanes;
            const primaryLane = primaryLanes[primaryLanes.length - 1]; // bottom lane is drawn last, overlays correctly!
            
            if (blocksThisLane && laneIndex === primaryLane) {
                const oX = obs.worldX - this.monkeyWorldX + this.monkeyScreenX;
                
                if (obs.type === 'log') {
                    // Logs are centered horizontally on screen road
                    drawLog(this.ctx, oX, laneY, obs.width, obs.height);
                } else if (obs.type === 'arch') {
                    drawArch(this.ctx, oX, laneY, obs.width, obs.height);
                } else if (obs.type === 'barrier') {
                    drawSpikes(this.ctx, oX, laneY, obs.width, obs.height);
                } else if (obs.type === 'lava') {
                    drawLava(this.ctx, oX, laneY, obs.width, obs.height, this.elapsedTime);
                }
            }
        });
    }
}

export const game = new GameEngine2D();
window.gameEngine = game; // Expose globally for HTML DOM calls
