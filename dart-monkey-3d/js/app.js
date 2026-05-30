/**
 * Dart Monkey 3D WebGL Engine Controller
 * Integrates Three.js, procedural assets, sound engine, and interactive game loops.
 */
class MonkeyApp {
    constructor() {
        this.container = document.getElementById('canvas-container');
        this.clock = new THREE.Clock();
        
        // Scene state
        this.monkey = null;
        this.bloons = [];
        this.darts = [];
        this.particles = [];
        this.mousePos = new THREE.Vector2();
        this.raycaster = new THREE.Raycaster();
        
        // Game stats
        this.stats = {
            popped: 0,
            activeDarts: 0,
            activeBloons: 0
        };

        // Upgrade state
        this.activeTier = 0;
        this.autoAim = false;

        // Interaction helper: plane to raycast mouse coordinate in 3D
        this.aimPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0); // Flat plane at y=0

        // Initialize core engine
        this.initEngine();
        this.initLighting();
        this.initEnvironment();
        this.initMonkey();
        this.initUI();
        
        // Spawn initial balloons
        for (let i = 0; i < 5; i++) {
            this.spawnBloon(true);
        }

        // Start 60FPS Render loop
        this.animate();

        // Handle resizing
        window.addEventListener('resize', () => this.onWindowResize());
    }

    // --- 1. Three.js Engine Setup ---
    initEngine() {
        // Create Scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0e1224);
        this.scene.fog = new THREE.FogExp2(0x0e1224, 0.04);

        // Create Camera
        this.camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 100);
        this.camera.position.set(0, 3.2, 7.5);

        // Create WebGL Renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
        this.renderer.setSize(window.innerWidth, window.innerHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)); // Lock ratio for performance
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.container.appendChild(this.renderer.domElement);

        // Orbit Camera Controls
        this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.minDistance = 3.5;
        this.controls.maxDistance = 14;
        this.controls.maxPolarAngle = Math.PI / 2 - 0.05; // Prevent camera clipping through floor
        this.controls.target.set(0, 0.9, 0); // Focus camera on monkey torso/head
    }

    // --- 2. Multi-Mode Responsive Lighting ---
    initLighting() {
        // Ambient fill lighting
        this.ambientLight = new THREE.AmbientLight(0xffffff, 0.35);
        this.scene.add(this.ambientLight);

        // Main shadows Directional light (Simulates Sun)
        this.dirLight = new THREE.DirectionalLight(0xfff3e0, 1.25);
        this.dirLight.position.set(5, 8, 4);
        this.dirLight.castShadow = true;
        this.dirLight.shadow.mapSize.width = 1024;
        this.dirLight.shadow.mapSize.height = 1024;
        this.dirLight.shadow.camera.near = 0.5;
        this.dirLight.shadow.camera.far = 25;
        this.dirLight.shadow.camera.left = -6;
        this.dirLight.shadow.camera.right = 6;
        this.dirLight.shadow.camera.top = 6;
        this.dirLight.shadow.camera.bottom = -6;
        this.dirLight.shadow.bias = -0.0005;
        this.scene.add(this.dirLight);

        // Rim/Back Lighting for premium visual pop
        this.rimLight = new THREE.DirectionalLight(0x3bb2ff, 0.7);
        this.rimLight.position.set(-5, 4, -4);
        this.scene.add(this.rimLight);

        // Soft spotlight reflecting from floor
        this.floorLight = new THREE.PointLight(0xffeedd, 0.3, 10);
        this.floorLight.position.set(0, 0.1, 0);
        this.scene.add(this.floorLight);
    }

    // --- 3. Playground Environment & Platform ---
    initEnvironment() {
        // Grassy Floor
        const floorGeo = new THREE.PlaneGeometry(50, 50);
        const floorMat = new THREE.MeshStandardMaterial({ 
            color: 0x14203d, 
            roughness: 0.9,
            metalness: 0.1
        });
        const floor = new THREE.Mesh(floorGeo, floorMat);
        floor.rotation.x = -Math.PI / 2;
        floor.receiveShadow = true;
        this.scene.add(floor);

        // Stylized Wooden Pedestal Platform for Monkey (Standard BTD6 Stand)
        const standGroup = new THREE.Group();
        this.scene.add(standGroup);

        const cylinderGeo = new THREE.CylinderGeometry(1.4, 1.5, 0.24, 32);
        const woodMat = new THREE.MeshStandardMaterial({ color: 0x6e4726, roughness: 0.7 });
        const standBase = new THREE.Mesh(cylinderGeo, woodMat);
        standBase.position.y = 0.12;
        standBase.castShadow = true;
        standBase.receiveShadow = true;
        standGroup.add(standBase);

        // Ring border around pedestal
        const ringGeo = new THREE.TorusGeometry(1.4, 0.05, 8, 48);
        const metalMat = new THREE.MeshStandardMaterial({ color: 0xe5ac3c, roughness: 0.3, metalness: 0.8 }); // Golden trim
        const ring = new THREE.Mesh(ringGeo, metalMat);
        ring.rotation.x = Math.PI / 2;
        ring.position.y = 0.22;
        ring.castShadow = true;
        standGroup.add(ring);

        // Floating sparkles particle system
        this.initSparkles();
    }

    initSparkles() {
        const sparkleGeo = new THREE.BufferGeometry();
        const sparkleCount = 60;
        const positions = new Float32Array(sparkleCount * 3);
        
        for(let i=0; i < sparkleCount * 3; i += 3) {
            positions[i] = (Math.random() - 0.5) * 12;      // X
            positions[i+1] = Math.random() * 5 + 0.1;       // Y (Above floor)
            positions[i+2] = (Math.random() - 0.5) * 12;    // Z
        }
        
        sparkleGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        
        // Soft round particle texture using code canvas
        const canvas = document.createElement('canvas');
        canvas.width = 16;
        canvas.height = 16;
        const ctx = canvas.getContext('2d');
        const grad = ctx.createRadialGradient(8, 8, 0, 8, 8, 8);
        grad.addColorStop(0, 'rgba(255, 255, 255, 1)');
        grad.addColorStop(1, 'rgba(255, 255, 255, 0)');
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, 16, 16);
        
        const texture = new THREE.CanvasTexture(canvas);
        const sparkleMat = new THREE.PointsMaterial({
            color: 0xffe6a3,
            size: 0.12,
            map: texture,
            transparent: true,
            blending: THREE.AdditiveBlending,
            depthWrite: false
        });
        
        this.sparkles = new THREE.Points(sparkleGeo, sparkleMat);
        this.scene.add(this.sparkles);
    }

    // --- 4. Instantiate Monkey ---
    initMonkey() {
        this.monkey = new window.DartMonkey3D();
        this.monkey.position.set(0, 0.24, 0); // Sit on top of pedestal
        this.scene.add(this.monkey);
    }

    // --- 5. Spawn Balloon/Bloon Physics Mesh ---
    spawnBloon(isInitial = false) {
        const bloon = new THREE.Group();
        
        // Bloon colors from BTD6
        const colors = [
            { hex: 0xff2525, weight: 4 }, // Red (Standard)
            { hex: 0x2570ff, weight: 3 }, // Blue (Faster)
            { hex: 0x1ecc40, weight: 2 }, // Green (Very fast)
            { hex: 0xffcd1c, weight: 1 }  // Yellow (Hyper active)
        ];
        
        // Select random color biased by weighting
        let totalWeight = colors.reduce((acc, c) => acc + c.weight, 0);
        let randVal = Math.random() * totalWeight;
        let selectedColor = colors[0];
        
        for (const c of colors) {
            if (randVal < c.weight) {
                selectedColor = c;
                break;
            }
            randVal -= c.weight;
        }

        const bloonMat = new THREE.MeshStandardMaterial({
            color: selectedColor.hex,
            roughness: 0.15,
            metalness: 0.1,
            clearcoat: 0.5 // High glossy rubber
        });

        // 1. Balloon main sphere
        const sphereGeo = new THREE.SphereGeometry(0.3, 16, 16);
        const sphere = new THREE.Mesh(sphereGeo, bloonMat);
        sphere.scale.set(1.0, 1.22, 1.0); // Elongated pear shape
        sphere.castShadow = true;
        bloon.add(sphere);

        // 2. Balloon knot at bottom
        const knotGeo = new THREE.ConeGeometry(0.06, 0.1, 4);
        const knot = new THREE.Mesh(knotGeo, bloonMat);
        knot.position.y = -0.34;
        knot.rotation.x = Math.PI;
        bloon.add(knot);

        // 3. String hanging down
        const stringGeo = new THREE.CylinderGeometry(0.005, 0.005, 0.5, 4);
        const stringMat = new THREE.MeshBasicMaterial({ color: 0xcccccc });
        const string = new THREE.Mesh(stringGeo, stringMat);
        string.position.y = -0.6;
        bloon.add(string);

        // Set physics position
        // Distribute spherically around platform to prevent overlap
        const angle = Math.random() * Math.PI * 2;
        const radius = Math.random() * 2.2 + 2.2; // Keep within 2.2m to 4.4m range
        const x = Math.cos(angle) * radius;
        const z = Math.sin(angle) * radius;
        const y = isInitial ? (Math.random() * 1.5 + 0.8) : -1.0; // Float up if newly spawned

        bloon.position.set(x, y, z);
        this.scene.add(bloon);

        // Setup custom physics vectors attached to the object
        bloon.userData = {
            baseY: Math.random() * 1.2 + 0.8,
            speedY: Math.random() * 1.5 + 1.0,
            swaySpeed: Math.random() * 2 + 1,
            swayRange: Math.random() * 0.2 + 0.1,
            phase: Math.random() * Math.PI,
            isPopping: false,
            color: selectedColor.hex,
            radius: 0.35
        };

        this.bloons.push(bloon);
        this.updateStats();
    }

    // --- 6. Interaction: Clicking to Throw Darts ---
    shootDart(target3D) {
        if (!this.monkey) return;

        // Auto-aim lock-on helper
        let shootVector = target3D.clone();
        if (this.autoAim && this.bloons.length > 0) {
            // Find closest active balloon
            let closest = null;
            let minDist = Infinity;
            
            this.bloons.forEach(b => {
                const d = b.position.distanceTo(this.monkey.position);
                if (d < minDist) {
                    minDist = d;
                    closest = b;
                }
            });

            if (closest) {
                shootVector.copy(closest.position);
                shootVector.y += 0.25; // Aim at center of mass
            }
        }

        // Trigger monkey throw swing action
        this.monkey.triggerThrow();
        
        // Play Throw Swoosh synth audio
        window.MonkeyAudio.playThrow();

        // Delay projectile spawn slightly to match keyframe arm swipe forward release
        setTimeout(() => {
            const projectile = new THREE.Group();
            
            // Get hand position in world space
            const handWorldPos = new THREE.Vector3();
            this.monkey.rightHand.getWorldPosition(handWorldPos);
            
            projectile.position.copy(handWorldPos);
            this.scene.add(projectile);

            // Construct 3D mesh representation matching upgraded skin
            let meshRadius = 0.15;
            let isSpikedBall = false;

            if (this.activeTier >= 3) {
                // Giant Spiked Ball
                isSpikedBall = true;
                meshRadius = 0.38;
                
                const ball = new THREE.Mesh(new THREE.SphereGeometry(0.24, 12, 12), this.monkey.materials.spikeBall);
                ball.castShadow = true;
                projectile.add(ball);
                
                // Little spikes
                const numSpikes = 8;
                const spikeGeo = new THREE.ConeGeometry(0.05, 0.16, 4);
                for(let i = 0; i < numSpikes; i++) {
                    const spike = new THREE.Mesh(spikeGeo, this.monkey.materials.spikeBall);
                    const phi = Math.acos(-1 + (2 * i) / numSpikes);
                    const theta = Math.sqrt(numSpikes * Math.PI) * phi;
                    spike.position.setFromSphericalCoords(0.24, phi, theta);
                    spike.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0), spike.position.clone().normalize());
                    spike.castShadow = true;
                    projectile.add(spike);
                }
            } else {
                // Standard Wooden Dart (wooden shaft, metal tip, red fletching)
                const dartGeo = new THREE.Group();
                dartGeo.rotation.x = Math.PI / 2; // Point forward along travel axis
                
                const shaft = new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.02, 0.35, 8), this.monkey.materials.dartWood);
                shaft.castShadow = true;
                dartGeo.add(shaft);

                const tip = new THREE.Mesh(new THREE.ConeGeometry(0.045, 0.12, 8), this.monkey.materials.dartMetal);
                tip.position.y = 0.225;
                tip.castShadow = true;
                dartGeo.add(tip);

                for (let i = 0; i < 3; i++) {
                    const fin = new THREE.Mesh(new THREE.BoxGeometry(0.005, 0.1, 0.05), this.monkey.materials.dartFeather);
                    fin.position.y = -0.15;
                    fin.rotation.y = (Math.PI * 2 / 3) * i;
                    fin.translateZ(0.025);
                    fin.castShadow = true;
                    dartGeo.add(fin);
                }
                
                projectile.add(dartGeo);
            }

            // Direction calculation
            const direction = new THREE.Vector3().subVectors(shootVector, handWorldPos).normalize();
            
            // Orient projectile to face direction of travel
            const targetQuat = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 0, -1), direction);
            projectile.quaternion.copy(targetQuat);

            // Determine speed based on Green Headband upgrade
            const travelSpeed = (this.activeTier >= 1) ? 14.5 : 9.5;

            projectile.userData = {
                velocity: direction.multiplyScalar(travelSpeed),
                radius: meshRadius,
                isSpikedBall: isSpikedBall,
                life: 2.2 // Terminate after 2.2 seconds to free heap
            };

            this.darts.push(projectile);
            this.updateStats();

        }, 220); // Sync release delay in milliseconds
    }

    // --- 7. Collisions & Pop Particle Sprays ---
    createPopExplosion(pos, color) {
        const particleCount = 14;
        const partGeo = new THREE.SphereGeometry(0.05, 8, 8);
        const partMat = new THREE.MeshBasicMaterial({ color: color, transparent: true, opacity: 1.0 });

        for(let i=0; i < particleCount; i++) {
            const p = new THREE.Mesh(partGeo, partMat);
            p.position.copy(pos);
            this.scene.add(p);

            // Explode velocities outwards spherically
            const velocity = new THREE.Vector3(
                (Math.random() - 0.5) * 4.5,
                (Math.random() - 0.3) * 5.0, // Slight upward bias
                (Math.random() - 0.5) * 4.5
            );

            this.particles.push({
                mesh: p,
                vel: velocity,
                life: 0.6 + Math.random() * 0.3, // Duration
                maxLife: 0.9
            });
        }
    }

    // --- 8. UI Interactions & Upgrades ---
    initUI() {
        // Upgrade button click bindings
        const btnLongRange = document.getElementById('upgrade-long-range');
        const btnEyesight = document.getElementById('upgrade-eyesight');
        const btnSpike = document.getElementById('upgrade-spike');
        const btnReset = document.getElementById('reset-upgrades');
        
        btnLongRange.addEventListener('click', () => {
            if (this.activeTier < 1) {
                this.activeTier = 1;
                this.monkey.setTier(1);
                btnLongRange.classList.add('active');
                btnEyesight.removeAttribute('disabled'); // Unlock next path tier
                this.showUnlockSplash("LONG RANGE DARTS UNLOCKED!");
                window.MonkeyAudio.playUpgrade();
            }
        });

        btnEyesight.addEventListener('click', () => {
            if (this.activeTier < 2 && !btnEyesight.disabled) {
                this.activeTier = 2;
                this.monkey.setTier(2);
                btnEyesight.classList.add('active');
                this.autoAim = true;
                this.showUnlockSplash("ENHANCED EYESIGHT UNLOCKED!");
                window.MonkeyAudio.playUpgrade();
            }
        });

        btnSpike.addEventListener('click', () => {
            if (this.activeTier < 3) {
                this.activeTier = 3;
                this.monkey.setTier(3);
                btnSpike.classList.add('active');
                // Auto active standard components
                btnLongRange.classList.add('active');
                btnEyesight.classList.add('active');
                btnEyesight.removeAttribute('disabled');
                this.showUnlockSplash("SPIKE-O-PULT CATAPULT ACTIVE!");
                window.MonkeyAudio.playUpgrade();
            }
        });

        btnReset.addEventListener('click', () => {
            this.activeTier = 0;
            this.autoAim = false;
            this.monkey.setTier(0);
            
            btnLongRange.classList.remove('active');
            btnEyesight.classList.remove('active');
            btnEyesight.setAttribute('disabled', 'true');
            btnSpike.classList.remove('active');
            
            this.showUnlockSplash("MONKEY UPGRADES RESET");
            window.MonkeyAudio.playUpgrade();
        });

        // Scene click integration (raycasting trigger to shoot)
        window.addEventListener('pointerdown', (e) => {
            // Block triggers if clicking glass card panels
            if (e.target.closest('.side-panel') || e.target.closest('.app-header') || e.target.closest('.app-footer')) {
                return;
            }
            
            // Map pointer to NDC coords (-1 to 1)
            this.mousePos.x = (e.clientX / window.innerWidth) * 2 - 1;
            this.mousePos.y = -(e.clientY / window.innerHeight) * 2 + 1;

            this.raycaster.setFromCamera(this.mousePos, this.camera);
            
            const targetIntersection = new THREE.Vector3();
            this.raycaster.ray.intersectPlane(this.aimPlane, targetIntersection);
            
            // Validate bounding distance limit so monkey doesn't throw infinitely far
            if (targetIntersection.length() < 20) {
                this.shootDart(targetIntersection);
            }
        });

        // Track cursor pointer to update look-at vectors
        window.addEventListener('pointermove', (e) => {
            this.mousePos.x = (e.clientX / window.innerWidth) * 2 - 1;
            this.mousePos.y = -(e.clientY / window.innerHeight) * 2 + 1;
        });

        // Lighting Control Buttons
        document.querySelectorAll('.light-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                document.querySelectorAll('.light-btn').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                this.changeLightingMode(btn.dataset.light);
            });
        });

        // Audio Control Buttons
        const fxBtn = document.getElementById('btn-sound-fx');
        const bgmBtn = document.getElementById('btn-sound-bgm');

        fxBtn.addEventListener('click', () => {
            fxBtn.classList.toggle('active');
            window.MonkeyAudio.toggleFX(fxBtn.classList.contains('active'));
        });

        bgmBtn.addEventListener('click', () => {
            bgmBtn.classList.toggle('active');
            window.MonkeyAudio.toggleBGM(bgmBtn.classList.contains('active'));
        });
    }

    showUnlockSplash(msg) {
        const splash = document.getElementById('announcement');
        splash.innerText = msg;
        splash.classList.remove('hidden');
        splash.classList.add('show');
        
        setTimeout(() => {
            splash.classList.remove('show');
            setTimeout(() => splash.classList.add('hidden'), 400);
        }, 1800);
    }

    // Dynamic light environment transformation
    changeLightingMode(mode) {
        const duration = 1.0;
        
        if (mode === 'sunny') {
            this.scene.background.setHex(0x0e1224);
            this.scene.fog.color.setHex(0x0e1224);
            this.ambientLight.color.setHex(0xffffff);
            this.ambientLight.intensity = 0.35;
            this.dirLight.color.setHex(0xfff3e0);
            this.dirLight.intensity = 1.25;
            this.rimLight.color.setHex(0x3bb2ff);
            this.rimLight.intensity = 0.7;
        } 
        else if (mode === 'sunset') {
            this.scene.background.setHex(0x240e1d);
            this.scene.fog.color.setHex(0x240e1d);
            this.ambientLight.color.setHex(0xaa88aa);
            this.ambientLight.intensity = 0.25;
            this.dirLight.color.setHex(0xff5511); // Rich solar orange key
            this.dirLight.intensity = 1.6;
            this.rimLight.color.setHex(0x8a2be2); // Deep sunset purple
            this.rimLight.intensity = 0.9;
        } 
        else if (mode === 'neon') {
            this.scene.background.setHex(0x02040b);
            this.scene.fog.color.setHex(0x02040b);
            this.ambientLight.color.setHex(0x0d2b45);
            this.ambientLight.intensity = 0.1;
            this.dirLight.color.setHex(0xff00ff); // Hot pink key
            this.dirLight.intensity = 0.9;
            this.rimLight.color.setHex(0x00ffff); // Electric cyan fill
            this.rimLight.intensity = 1.2;
        }
        
        window.MonkeyAudio.playPop(); // Little micro click sound feedback
    }

    updateStats() {
        document.getElementById('pop-counter').innerText = this.stats.popped;
        document.getElementById('active-darts-counter').innerText = this.darts.length;
        document.getElementById('bloon-counter').innerText = this.bloons.length;
    }

    onWindowResize() {
        this.camera.aspect = window.innerWidth / window.innerHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(window.innerWidth, window.innerHeight);
    }

    // --- 9. Core 60FPS Physics and Animation Render Loop ---
    animate() {
        requestAnimationFrame(() => this.animate());

        const delta = this.clock.getDelta();
        const time = this.clock.getElapsedTime();

        // 1. Damping orbit camera controls
        this.controls.update();

        // 2. Slow particle space rotation
        if(this.sparkles) {
            this.sparkles.rotation.y = time * 0.03;
            this.sparkles.rotation.x = Math.sin(time * 0.05) * 0.05;
        }

        // 3. Aim target calculation (intersect cursor with flat ground plane at torso height y=0.85)
        this.raycaster.setFromCamera(this.mousePos, this.camera);
        const lookTarget = new THREE.Vector3();
        
        if (this.autoAim && this.bloons.length > 0) {
            // Point head directly at closest target
            let closest = this.bloons[0];
            let minDist = Infinity;
            
            this.bloons.forEach(b => {
                const dist = b.position.distanceTo(this.monkey.position);
                if (dist < minDist) {
                    minDist = dist;
                    closest = b;
                }
            });
            lookTarget.copy(closest.position);
        } else {
            // Look at cursor intersect plane
            const torsoPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), -0.95);
            this.raycaster.ray.intersectPlane(torsoPlane, lookTarget);
        }

        // Update Monkey character bone rotation mesh structures
        if (this.monkey) {
            this.monkey.update(time, lookTarget);
        }

        // 4. Update Balloons floating path physics
        for (let i = this.bloons.length - 1; i >= 0; i--) {
            const b = this.bloons[i];
            const uData = b.userData;

            // Float up into active viewport boundary if newly spawned
            if (b.position.y < uData.baseY) {
                b.position.y += delta * 2.0; // Quick rise
            } else {
                // Gentle floating amplitude cycle
                b.position.y = uData.baseY + Math.sin(time * uData.speedY + uData.phase) * 0.16;
            }

            // Gentle wind sway drifting
            b.position.x += Math.sin(time * uData.swaySpeed + uData.phase) * uData.swayRange * delta;
            b.position.z += Math.cos(time * uData.swaySpeed * 0.8 + uData.phase) * uData.swayRange * delta;
        }

        // 5. Update Flying Projectiles physics
        for (let i = this.darts.length - 1; i >= 0; i--) {
            const d = this.darts[i];
            const vel = d.userData.velocity;

            d.position.addScaledVector(vel, delta);
            d.userData.life -= delta;

            // Roll spiked ball along ground or spin darts
            if (d.userData.isSpikedBall) {
                d.rotation.x += delta * 6.5;
                d.rotation.z += delta * 3.0;
                
                // Roll height check (spiked balls stay close to ground)
                if (d.position.y > 0.4) {
                    d.position.y -= delta * 3.5;
                }
            }

            // Boundary clean up
            if (d.userData.life <= 0 || d.position.length() > 25) {
                this.scene.remove(d);
                this.darts.splice(i, 1);
                continue;
            }

            // Check collision with balloons
            let dartPopped = false;
            for (let j = this.bloons.length - 1; j >= 0; j--) {
                const b = this.bloons[j];
                const distance = d.position.distanceTo(b.position);

                // Simple sphere bounding hit-box verification
                if (distance < (d.userData.radius + b.userData.radius)) {
                    
                    // Trigger explosive pop splash particles
                    this.createPopExplosion(b.position, b.userData.color);

                    // Play satisfying pop synthesized sound effect
                    window.MonkeyAudio.playPop();

                    // Remove elements
                    this.scene.remove(b);
                    this.bloons.splice(j, 1);

                    this.stats.popped++;
                    
                    // Spiked balls pierce through multiple bloons, standard darts are destroyed on hit
                    if (!d.userData.isSpikedBall) {
                        dartPopped = true;
                        break;
                    }
                }
            }

            if (dartPopped) {
                this.scene.remove(d);
                this.darts.splice(i, 1);
            }
        }

        // Auto-replenish balloons to keep standard population steady
        if (this.bloons.length < 5) {
            this.spawnBloon();
        }

        // 6. Update Exploding pop particle physics (gravity fall & fade-out)
        for (let i = this.particles.length - 1; i >= 0; i--) {
            const p = this.particles[i];
            
            p.mesh.position.addScaledVector(p.vel, delta);
            p.vel.y -= delta * 9.8; // Gravity fall acceleration
            
            p.life -= delta;
            p.mesh.material.opacity = Math.max(0, p.life / p.maxLife);

            // Scale down particle size as it decays
            const sc = Math.max(0.01, p.life / p.maxLife);
            p.mesh.scale.set(sc, sc, sc);

            if (p.life <= 0) {
                this.scene.remove(p.mesh);
                p.mesh.geometry.dispose();
                p.mesh.material.dispose();
                this.particles.splice(i, 1);
            }
        }

        this.updateStats();

        // Render main view
        this.renderer.render(this.scene, this.camera);
    }
}

// Start application when DOM has mounted
window.addEventListener('DOMContentLoaded', () => {
    window.App = new MonkeyApp();
});
