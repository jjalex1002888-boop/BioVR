/**
 * ==========================================
 * GEAR CHRONICLES - MAIN APP COORDINATOR
 * ==========================================
 * Orchestrates 60fps render loop, UI event mappings,
 * telemetry calculations, and the 10-second chronological choreography.
 */

document.addEventListener('DOMContentLoaded', () => {
    // Canvas setup
    const canvas = document.getElementById('luffy-canvas');
    const wrapper = canvas.parentElement;
    
    function resizeCanvas() {
        canvas.width = wrapper.clientWidth;
        canvas.height = wrapper.clientHeight;
    }
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);

    // Engine instances
    const sfx = new AudioSynth();
    const fx = new FxEngine(canvas);
    const rig = new LuffyRig(canvas);

    // UI elements
    const playPauseBtn = document.getElementById('play-pause-btn');
    const timelineScrubber = document.getElementById('timeline-scrubber');
    const timeDisplay = document.getElementById('time-display');
    const sfxToggleBtn = document.getElementById('sfx-toggle-btn');
    const scenePreset = document.getElementById('scene-preset');
    const canvasOverlayText = document.getElementById('canvas-overlay-text');
    const logConsole = document.getElementById('log-console');
    
    // Telemetry DOM
    const statHaki = document.getElementById('stat-haki');
    const barHaki = document.getElementById('bar-haki');
    const statElasticity = document.getElementById('stat-elasticity');
    const barElasticity = document.getElementById('bar-elasticity');
    const statBpm = document.getElementById('stat-bpm');
    const barBpm = document.getElementById('bar-bpm');
    const statLiberation = document.getElementById('stat-liberation');
    const barLiberation = document.getElementById('bar-liberation');

    // App state variables
    let isPlaying = false;
    let timelineTime = 0.0; // 0.0s to 10.0s
    let lastTime = 0;
    
    // Timeline keyframe track history to avoid repeating SFX during playback
    let triggeredKeyframes = {
        gear2: false,
        redhawkWindup: false,
        redhawkImpact: false,
        gear5: false
    };

    // Logging helpers
    function appendLog(type, text) {
        const line = document.createElement('div');
        line.className = `log-line ${type}`;
        line.innerText = text;
        logConsole.appendChild(line);
        logConsole.scrollTop = logConsole.scrollHeight; // Auto-scroll to bottom
        
        // Cap lines at 20
        while (logConsole.childElementCount > 20) {
            logConsole.removeChild(logConsole.firstChild);
        }
    }

    // ----------------------------------------
    // TELEMETRY UPDATER
    // ----------------------------------------
    function updateTelemetry(gear) {
        let haki = 0, elasticity = 100, bpm = 72, liberation = 0;
        
        if (gear === 'base') {
            haki = 0; elasticity = 100; bpm = 72; liberation = 0;
        } else if (gear === '2') {
            haki = 15; elasticity = 150; bpm = 240; liberation = 0;
        } else if (gear === '4') {
            haki = 95; elasticity = 260; bpm = 135; liberation = 5;
        } else if (gear === '5') {
            haki = 60; elasticity = 600; bpm = 300; liberation = 100;
        }
        
        // Write to UI
        statHaki.innerText = `${haki}%`;
        barHaki.style.width = `${haki}%`;
        
        statElasticity.innerText = `${elasticity}%`;
        barElasticity.style.width = `${Math.min(100, elasticity / 6)}%`;
        
        statBpm.innerText = `${bpm} BPM`;
        barBpm.style.width = `${Math.min(100, bpm / 3)}%`;
        
        statLiberation.innerText = `${liberation}%`;
        barLiberation.style.width = `${liberation}%`;
    }

    // ----------------------------------------
    // CORE GEAR & ACTION TRIGGERS
    // ----------------------------------------
    function triggerGearChange(gearVal) {
        rig.setGear(gearVal);
        updateTelemetry(gearVal);
        
        // Remove active class from gear buttons
        document.querySelectorAll('[data-gear]').forEach(btn => {
            btn.classList.remove('active');
        });
        
        const activeBtn = document.querySelector(`[data-gear="${gearVal}"]`);
        if (activeBtn) activeBtn.classList.add('active');
        
        // Scene / FX adaptations
        if (gearVal === 'base') {
            canvasOverlayText.innerText = "Base Form: Restless Spirit";
            canvasOverlayText.style.borderColor = 'var(--color-primary)';
            appendLog('system', '[SYS] Luffy reset to Base Form.');
            appendLog('character', "[LUFFY] I'm hungry! Meat! 🍖");
        } 
        else if (gearVal === '2') {
            canvasOverlayText.innerText = "Gear 2: Jet Acceleration";
            canvasOverlayText.style.borderColor = 'var(--color-pink)';
            appendLog('system', '[SYS] Blood pressure spiking! Jet Steam active.');
            appendLog('combat', '[GEAR 2] Leg pump complete. Speed increased by 300%.');
            sfx.playSteamHiss(1.5);
            fx.triggerShake(12, 15);
            fx.triggerShockwave(canvas.width / 2, canvas.height / 2 + 30, {
                color: 'rgba(255, 112, 166, 0.6)',
                maxRadius: 280,
                speed: 10
            });
            
            // Screen flash pink/red
            document.querySelector('.canvas-wrapper').classList.add('screen-flash-red');
            setTimeout(() => {
                document.querySelector('.canvas-wrapper').classList.remove('screen-flash-red');
            }, 400);
        } 
        else if (gearVal === '4') {
            canvasOverlayText.innerText = "Gear 4: Boundman Heavyweight";
            canvasOverlayText.style.borderColor = 'var(--color-primary)';
            appendLog('system', '[SYS] Haki concentrating. Compression force ready.');
            appendLog('combat', '[GEAR 4] Armament Haki hardens muscles! Bouncing activated.');
            sfx.playWhoosh(1.2, 0.6);
            fx.triggerShake(18, 20);
            fx.triggerShockwave(canvas.width / 2, canvas.height / 2 + 10, {
                color: 'rgba(239, 68, 68, 0.7)',
                maxRadius: 350,
                speed: 12,
                lineWidth: 6
            });
            
            document.querySelector('.canvas-wrapper').classList.add('screen-flash-red');
            setTimeout(() => {
                document.querySelector('.canvas-wrapper').classList.remove('screen-flash-red');
            }, 450);
        } 
        else if (gearVal === '5') {
            canvasOverlayText.innerText = "Gear 5: Joyboy Sun God";
            canvasOverlayText.style.borderColor = 'var(--color-gold)';
            appendLog('gear5', '[GEAR 5] Drums of Liberation pounding! Freedom ratio maximum!');
            appendLog('character', '[NIKA] Hahaha! This is so fun! I can do anything!');
            sfx.playDrumsOfLiberation();
            sfx.playWhoosh(1.4, 1.5);
            fx.triggerShake(24, 25);
            fx.triggerShockwave(canvas.width / 2, canvas.height / 2 - 40, {
                color: 'rgba(254, 210, 63, 0.8)',
                maxRadius: 400,
                speed: 14,
                lineWidth: 8
            });
            
            document.querySelector('.canvas-wrapper').classList.add('screen-flash-gold');
            setTimeout(() => {
                document.querySelector('.canvas-wrapper').classList.remove('screen-flash-gold');
            }, 500);
        }
    }

    function triggerAttackMove(atkType) {
        if (rig.isAttacking) return; // Prevent spamming overlaying attacks
        
        rig.triggerAttack(atkType);
        
        const btn = document.querySelector(`[data-attack="${atkType}"]`);
        if (btn) {
            btn.classList.add('executing');
            setTimeout(() => btn.classList.remove('executing'), 600);
        }
        
        if (atkType === 'pistol') {
            appendLog('combat', '[COMBAT] Luffy fires Gum-Gum Pistol!');
            sfx.playWhoosh(0.7);
            
            // Spawn standard shockwave at arm stretch path
            setTimeout(() => {
                fx.triggerShockwave(canvas.width / 2 + 150, canvas.height / 2, {
                    color: 'rgba(255, 255, 255, 0.4)',
                    maxRadius: 180,
                    speed: 12
                });
                fx.triggerShake(6, 8);
            }, 250);
        } 
        else if (atkType === 'gatling') {
            appendLog('combat', '[COMBAT] Luffy fires Gum-Gum Gatling barrage!');
            
            // Rapid small shake whooshes
            let count = 0;
            let interval = setInterval(() => {
                if (count >= 12) {
                    clearInterval(interval);
                    return;
                }
                sfx.playWhoosh(0.3, 1.2 - count * 0.05);
                fx.triggerShake(4, 5);
                
                fx.triggerShockwave(canvas.width / 2 + 120 + (Math.random() - 0.5)*80, canvas.height / 2 + (Math.random() - 0.5)*60, {
                    color: 'rgba(255, 255, 255, 0.3)',
                    maxRadius: 120,
                    speed: 14
                });
                count++;
            }, 60);
        } 
        else if (atkType === 'redhawk') {
            appendLog('combat', '[COMBAT] Luffy executes GUM-GUM RED HAWK! 🔥');
            sfx.playWhoosh(0.9, 0.7); // Haki heat winding
            
            // Part 1: Wind up (spawning flame points on fist)
            let windUpCount = 0;
            let windUpInterval = setInterval(() => {
                if (windUpCount >= 8 || !rig.isAttacking) {
                    clearInterval(windUpInterval);
                    return;
                }
                let fistX = canvas.width / 2 + 35 + rig.stretchRightArmX;
                let fistY = canvas.height / 2 - 10 + rig.stretchRightArmY + rig.bobY;
                fx.spawnRedHawkFire(fistX, fistY, 8, false);
                windUpCount++;
            }, 50);
            
            // Part 2: Impact explosion
            setTimeout(() => {
                let fistX = canvas.width / 2 + 35 + rig.stretchRightArmX;
                let fistY = canvas.height / 2 - 10 + rig.stretchRightArmY + rig.bobY;
                
                fx.spawnRedHawkFire(fistX, fistY, 45, true);
                fx.triggerShockwave(fistX, fistY, {
                    color: 'rgba(239, 46, 86, 0.9)',
                    maxRadius: 360,
                    speed: 15,
                    lineWidth: 10
                });
                
                fx.triggerShake(28, 30);
                sfx.playExplosion();
                
                // Red flash
                document.querySelector('.canvas-wrapper').classList.add('screen-flash-red');
                setTimeout(() => {
                    document.querySelector('.canvas-wrapper').classList.remove('screen-flash-red');
                }, 400);
                
                appendLog('combat', '[COMBAT] Direct hit! Massive thermal shockwave lands.');
            }, 300);
        }
    }

    // ----------------------------------------
    // 10-SECOND CINEMATIC TIMELINE CHOREOGRAPHY
    // ----------------------------------------
    function processTimelineKeyframes(time) {
        // Chronological events based on timeline progress
        
        // Event 1: Idle Base (0.0s - 2.0s)
        if (time >= 0.0 && time < 2.0) {
            if (rig.gear !== 'base') {
                triggerGearChange('base');
            }
        }
        
        // Event 2: Gear 2 Jet Spark (2.0s)
        if (time >= 2.0 && time < 4.0) {
            if (rig.gear !== '2') {
                triggerGearChange('2');
                triggeredKeyframes.gear2 = true;
            }
        }
        
        // Event 3: Gum-Gum Red Hawk Wind-up (4.0s - 5.5s)
        if (time >= 4.0 && time < 5.5) {
            if (rig.gear !== '2') {
                triggerGearChange('2');
            }
            if (!triggeredKeyframes.redhawkWindup) {
                rig.isAttacking = true;
                rig.attackType = 'redhawk';
                rig.attackProgress = 0.05; // Lock it into windup stretch
                
                sfx.playWhoosh(1.1, 0.65);
                triggeredKeyframes.redhawkWindup = true;
                appendLog('combat', '[TIMELINE] 4.0s: Luffy concentrates Haki in his fist! 🔥');
            }
            
            // Continuous heavy fire particles from fist in windup
            let fistX = canvas.width / 2 + 35 + rig.stretchRightArmX;
            let fistY = canvas.height / 2 - 10 + rig.stretchRightArmY + rig.bobY;
            fx.spawnRedHawkFire(fistX, fistY, 4, false);
            
            // Map timeline fraction (4.0 to 5.5) directly to windup stretching
            let localT = (time - 4.0) / 1.5; // 0 to 1
            rig.stretchRightArmX = -120 * localT;
            rig.stretchRightArmY = -30 * localT;
        }
        
        // Event 4: Red Hawk Blast Impact! (5.5s)
        if (time >= 5.5 && time < 7.0) {
            if (!triggeredKeyframes.redhawkImpact) {
                // Snap punch forward
                rig.stretchRightArmX = 340;
                rig.stretchRightArmY = 10;
                
                let fistX = canvas.width / 2 + 35 + rig.stretchRightArmX;
                let fistY = canvas.height / 2 - 10 + rig.stretchRightArmY + rig.bobY;
                
                fx.spawnRedHawkFire(fistX, fistY, 50, true);
                fx.triggerShockwave(fistX, fistY, {
                    color: 'rgba(239, 46, 86, 0.95)',
                    maxRadius: 400,
                    speed: 16,
                    lineWidth: 12
                });
                
                fx.triggerShake(30, 25);
                sfx.playExplosion();
                
                // Screen flash white-red
                document.querySelector('.canvas-wrapper').classList.add('screen-flash-white');
                setTimeout(() => {
                    document.querySelector('.canvas-wrapper').classList.remove('screen-flash-white');
                }, 500);
                
                triggeredKeyframes.redhawkImpact = true;
                appendLog('combat', '[TIMELINE] 5.5s: RED HAWK BLASTS FORWARD!');
            }
            
            // Retract phase mapping between 5.8s and 6.5s
            if (time > 5.8) {
                let retractT = Math.min(1.0, (time - 5.8) / 0.8);
                rig.stretchRightArmX = 340 - (340 * retractT);
                rig.stretchRightArmY = 10 - (10 * retractT);
                rig.isAttacking = false;
            }
        }
        
        // Event 5: Gear 5 Nika Peak Liberation! (7.0s - 10.0s)
        if (time >= 7.0) {
            if (rig.gear !== '5') {
                triggerGearChange('5');
                triggeredKeyframes.gear5 = true;
                appendLog('gear5', '[TIMELINE] 7.0s: GEAR 5 LIBERATED! Pure Joyboy arises!');
                
                // Giant cloud shockwave
                fx.triggerShockwave(canvas.width / 2, canvas.height / 2 - 40, {
                    color: 'rgba(255, 255, 255, 0.95)',
                    maxRadius: 450,
                    speed: 13,
                    lineWidth: 14
                });
                
                // Celestial flash
                document.querySelector('.canvas-wrapper').classList.add('screen-flash-gold');
                setTimeout(() => {
                    document.querySelector('.canvas-wrapper').classList.remove('screen-flash-gold');
                }, 500);
            }
            
            // Dynamic golden lightning strikes striking periodically
            if (Math.random() < 0.05) {
                fx.spawnGear5Lightning(canvas.width / 2, canvas.height / 2);
                sfx.playWhoosh(0.4, 2.0); // Zapping noise
                fx.triggerShake(8, 6);
            }
            
            // Periodic Drums beats synchronized every 1.5s
            let beatFrac = (time - 7.0) % 1.5;
            if (beatFrac < 0.03) {
                sfx.playDrumsOfLiberation();
                fx.triggerShake(5, 5);
                appendLog('character', '[NIKA] Thump-thump! 🥁');
            }
        }
    }

    // Reset timeline triggers
    function resetTimelineKeyframeTriggers() {
        triggeredKeyframes = {
            gear2: false,
            redhawkWindup: false,
            redhawkImpact: false,
            gear5: false
        };
        rig.isAttacking = false;
        rig.stretchRightArmX = 0;
        rig.stretchRightArmY = 0;
    }

    // ----------------------------------------
    // MAIN ENGINE RENDER LOOP
    // ----------------------------------------
    function mainLoop(timestamp) {
        if (!lastTime) lastTime = timestamp;
        let delta = (timestamp - lastTime) / 1000.0;
        lastTime = timestamp;
        
        // Handle timeline sequence increment
        if (isPlaying) {
            timelineTime += delta;
            if (timelineTime >= 10.0) {
                timelineTime = 0.0; // Loop back
                resetTimelineKeyframeTriggers();
                appendLog('system', '[TIMELINE] 10.0s Sequence complete. Restarting...');
            }
            
            timelineScrubber.value = timelineTime;
            timeDisplay.innerText = `${timelineTime.toFixed(1)}s / 10.0s`;
            
            processTimelineKeyframes(timelineTime);
        }
        
        // 1. Draw Environment background
        fx.drawBackground(timestamp / 1000.0, rig.gear);
        
        // 2. Generate active state particle fumes on Luffy's joints
        let bodyX = canvas.width / 2;
        let bodyY = canvas.height / 2 + 50 + rig.bobY;
        
        if (rig.gear === '2') {
            fx.spawnSteam(bodyX, bodyY - 10, 1.5);
        } 
        else if (rig.gear === '4') {
            fx.spawnHakiFlames(bodyX - 35, bodyY - 10, 1);
            fx.spawnHakiFlames(bodyX + 35, bodyY - 10, 1);
            
            // Boundman bouncing puff puff smoke on foot floor impact
            if (Math.sin(timestamp / 160) > 0.9) {
                fx.spawnSteam(bodyX - 25, bodyY + 50, 1);
                fx.spawnSteam(bodyX + 25, bodyY + 50, 1);
            }
        }
        else if (rig.gear === '5') {
            // Flowy celestial cloud particles
            if (Math.random() < 0.25) {
                fx.particles.push(new Particle(bodyX + (Math.random() - 0.5)*120, bodyY - 80 + (Math.random() - 0.5)*40, {
                    vx: (Math.random() - 0.5) * 1,
                    vy: -Math.random() * 1.5 - 0.5,
                    size: Math.random() * 8 + 6,
                    color: '#ffffff',
                    maxLife: 45,
                    type: 'smoke',
                    decay: 0.02
                }));
            }
            // Golden sparks
            if (Math.random() < 0.15) {
                fx.particles.push(new Particle(bodyX + (Math.random() - 0.5)*140, bodyY + (Math.random() - 0.5)*120, {
                    vx: (Math.random() - 0.5) * 4,
                    vy: (Math.random() - 0.5) * 4,
                    size: Math.random() * 5 + 2,
                    color: '#facc15',
                    maxLife: 30,
                    type: 'spark',
                    decay: 0.03
                }));
            }
        }
        
        // 3. Update engines
        rig.update(timestamp / 1000.0, sfx);
        fx.update(timestamp / 1000.0, rig.gear);
        
        // 4. Render Luffy Vector Rig with camera shake offsets
        rig.draw(fx.shakeOffset);
        
        // 5. Render overlays (Particles, lightning, shockwaves)
        fx.drawOverlayEffects(fx.shakeOffset);
        
        requestAnimationFrame(mainLoop);
    }
    
    // Start loop
    requestAnimationFrame(mainLoop);

    // ----------------------------------------
    // INTERACTION EVENT MAPPINGS
    // ----------------------------------------
    
    // Gear select events
    document.getElementById('gear-base-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerGearChange('base');
    });
    
    document.getElementById('gear-2-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerGearChange('2');
    });
    
    document.getElementById('gear-4-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerGearChange('4');
    });
    
    document.getElementById('gear-5-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerGearChange('5');
    });
    
    // Attack actions
    document.getElementById('atk-pistol-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerAttackMove('pistol');
    });
    
    document.getElementById('atk-gatling-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerAttackMove('gatling');
    });
    
    document.getElementById('atk-redhawk-btn').addEventListener('click', () => {
        if (isPlaying) stopTimelineSequence();
        triggerAttackMove('redhawk');
    });
    
    // Scene change
    scenePreset.addEventListener('change', (e) => {
        fx.setScene(e.target.value);
        appendLog('system', `[SYS] Background set to: ${e.target.value.toUpperCase()}`);
    });
    
    // SFX toggle
    sfxToggleBtn.addEventListener('click', () => {
        // Initializing audio on user interaction
        sfx.init();
        let isMuted = sfx.toggleMute();
        
        if (isMuted) {
            sfxToggleBtn.classList.add('muted');
            sfxToggleBtn.querySelector('.icon').innerText = "🔇";
            sfxToggleBtn.querySelector('.label').innerText = "Sound FX: OFF";
            appendLog('system', '[SYS] Sound FX Muted.');
        } else {
            sfxToggleBtn.classList.remove('muted');
            sfxToggleBtn.querySelector('.icon').innerText = "🔊";
            sfxToggleBtn.querySelector('.label').innerText = "Sound FX: ON";
            appendLog('system', '[SYS] Sound FX Enabled.');
            sfx.playWhoosh(0.5); // Feedback sound
        }
    });

    // Timeline control triggers
    function startTimelineSequence() {
        isPlaying = true;
        sfx.init(); // Initialize audio context on play click
        playPauseBtn.classList.add('playing');
        playPauseBtn.querySelector('.icon').innerText = "⏸";
        playPauseBtn.querySelector('.label').innerText = "Pause Showpiece";
        appendLog('system', '[TIMELINE] Chronological sequence started. Auto-playback active.');
    }
    
    function stopTimelineSequence() {
        isPlaying = false;
        playPauseBtn.classList.remove('playing');
        playPauseBtn.querySelector('.icon').innerText = "▶";
        playPauseBtn.querySelector('.label').innerText = "Play Showpiece";
        appendLog('system', '[TIMELINE] Auto-playback paused.');
    }

    playPauseBtn.addEventListener('click', () => {
        if (isPlaying) {
            stopTimelineSequence();
        } else {
            startTimelineSequence();
        }
    });
    
    // Timeline scrubber dragging
    timelineScrubber.addEventListener('input', (e) => {
        timelineTime = parseFloat(e.target.value);
        timeDisplay.innerText = `${timelineTime.toFixed(1)}s / 10.0s`;
        
        // Reset dynamic trigger locks so when user scrubs backward they can trigger again
        resetTimelineKeyframeTriggers();
        
        // Evaluate keyframe states immediately during scrub
        processTimelineKeyframes(timelineTime);
    });
});
