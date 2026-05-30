/**
 * BTD6 Dart Monkey Procedural 3D Geometry Builder
 * Generates the full character model using primitive grouping for lightweight, offline-ready 3D rendering.
 */
class DartMonkey3D extends THREE.Group {
    constructor() {
        super();
        this.tier = 0;
        
        // Material definitions
        this.materials = {
            fur: new THREE.MeshStandardMaterial({ color: 0x814d24, roughness: 0.85, metalness: 0.05 }), // Chocolate brown
            skin: new THREE.MeshStandardMaterial({ color: 0xffcca3, roughness: 0.6, metalness: 0.0 }),  // Peach face/hands
            eyeWhite: new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.2 }),
            pupil: new THREE.MeshStandardMaterial({ color: 0x1f1105, roughness: 0.1 }),
            mouth: new THREE.MeshBasicMaterial({ color: 0x2b0d00 }),
            headband: new THREE.MeshStandardMaterial({ color: 0x1bb53d, roughness: 0.6, metalness: 0.1 }), // Vibrant green
            gogglesFrame: new THREE.MeshStandardMaterial({ color: 0xe52e2e, roughness: 0.4, metalness: 0.4 }), // Red metal
            gogglesStrap: new THREE.MeshStandardMaterial({ color: 0x222222, roughness: 0.8 }),
            gogglesLens: new THREE.MeshStandardMaterial({ color: 0x050a12, roughness: 0.1, metalness: 0.9, opacity: 0.8, transparent: true }),
            dartWood: new THREE.MeshStandardMaterial({ color: 0xc8985c, roughness: 0.7 }),
            dartMetal: new THREE.MeshStandardMaterial({ color: 0x999999, roughness: 0.2, metalness: 0.8 }),
            dartFeather: new THREE.MeshStandardMaterial({ color: 0xe52e2e, roughness: 0.6 }),
            spikeBall: new THREE.MeshStandardMaterial({ color: 0x3a424e, roughness: 0.4, metalness: 0.6 })
        };

        // Animation state variables
        this.throwTimer = 0;
        this.isThrowing = false;
        
        // Assemble parts
        this.buildMonkey();
    }

    buildMonkey() {
        // Base main scaling group
        this.characterGroup = new THREE.Group();
        this.add(this.characterGroup);

        // 1. Torso/Body (Capsule-like shape)
        const bodyGeo = new THREE.CylinderGeometry(0.5, 0.4, 1.1, 16);
        // Round the top and bottom of the cylinder using spheres for a smooth capsule
        const bodyTopGeo = new THREE.SphereGeometry(0.5, 16, 16, 0, Math.PI * 2, 0, Math.PI/2);
        const bodyBottomGeo = new THREE.SphereGeometry(0.4, 16, 16, 0, Math.PI * 2, Math.PI/2, Math.PI/2);

        this.torso = new THREE.Group();
        const torsoCenter = new THREE.Mesh(bodyGeo, this.materials.fur);
        torsoCenter.castShadow = true;
        torsoCenter.receiveShadow = true;
        
        const torsoTop = new THREE.Mesh(bodyTopGeo, this.materials.fur);
        torsoTop.position.y = 0.55;
        torsoTop.castShadow = true;
        
        const torsoBottom = new THREE.Mesh(bodyBottomGeo, this.materials.fur);
        torsoBottom.position.y = -0.55;
        torsoBottom.castShadow = true;

        this.torso.add(torsoCenter);
        this.torso.add(torsoTop);
        this.torso.add(torsoBottom);
        this.torso.position.y = 0.85;
        this.characterGroup.add(this.torso);

        // Belly peach-colored patch
        const bellyGeo = new THREE.SphereGeometry(0.38, 16, 16);
        const belly = new THREE.Mesh(bellyGeo, this.materials.skin);
        belly.scale.set(1.0, 1.2, 0.5); // Flatten and elongate
        belly.position.set(0, -0.05, 0.35);
        this.torso.add(belly);

        // 2. Head (Large cute cartoonish head)
        this.head = new THREE.Group();
        this.head.position.set(0, 0.85, 0); // Position relative to torso center
        this.torso.add(this.head);

        const skullGeo = new THREE.SphereGeometry(0.72, 32, 32);
        const skull = new THREE.Mesh(skullGeo, this.materials.fur);
        skull.castShadow = true;
        skull.receiveShadow = true;
        this.head.add(skull);

        // Peach muzzle/snout
        const muzzleGroup = new THREE.Group();
        muzzleGroup.position.set(0, -0.1, 0.5);
        this.head.add(muzzleGroup);

        const snoutGeo = new THREE.SphereGeometry(0.42, 24, 24);
        const snout = new THREE.Mesh(snoutGeo, this.materials.skin);
        snout.scale.set(1.15, 0.8, 0.9);
        snout.castShadow = true;
        muzzleGroup.add(snout);

        // Smiling mouth
        const mouthGeo = new THREE.TorusGeometry(0.12, 0.03, 8, 16, Math.PI);
        this.mouth = new THREE.Mesh(mouthGeo, this.materials.mouth);
        this.mouth.position.set(0, -0.1, 0.32);
        this.mouth.rotation.x = Math.PI * 0.1;
        this.mouth.rotation.z = Math.PI; // Smile face up
        muzzleGroup.add(this.mouth);

        // Cute tiny nose
        const noseGeo = new THREE.SphereGeometry(0.06, 12, 12);
        const nose = new THREE.Mesh(noseGeo, this.materials.pupil);
        nose.scale.set(1.4, 0.8, 1.0);
        nose.position.set(0, 0.05, 0.38);
        muzzleGroup.add(nose);

        // 3. Expressive Eyes
        this.buildEyes();

        // 4. Large rounded Monkey Ears
        this.buildEars();

        // 5. Upgrade Accessories (Initially hidden)
        this.buildAccessories();

        // 6. Limbs & Hands
        this.buildLimbs();

        // 7. Wiggling Tail
        this.buildTail();

        // Set initial tier to 0
        this.setTier(0);
    }

    buildEyes() {
        this.eyesGroup = new THREE.Group();
        this.eyesGroup.position.set(0, 0.15, 0.52);
        this.head.add(this.eyesGroup);

        // Large stylized cartoon eye shape (connected peach base mask)
        const maskGeo = new THREE.SphereGeometry(0.4, 16, 16);
        const eyeMaskL = new THREE.Mesh(maskGeo, this.materials.skin);
        eyeMaskL.scale.set(0.9, 0.9, 0.3);
        eyeMaskL.position.set(-0.2, -0.1, -0.1);
        
        const eyeMaskR = eyeMaskL.clone();
        eyeMaskR.position.x = 0.2;
        this.eyesGroup.add(eyeMaskL);
        this.eyesGroup.add(eyeMaskR);

        // Left Eye Globe
        const eyeGlobeGeo = new THREE.SphereGeometry(0.18, 16, 16);
        this.leftEye = new THREE.Mesh(eyeGlobeGeo, this.materials.eyeWhite);
        this.leftEye.scale.set(1.0, 1.2, 0.5);
        this.leftEye.position.set(-0.16, -0.05, 0.05);
        this.leftEye.rotation.y = Math.PI * 0.08;
        
        // Right Eye Globe
        this.rightEye = new THREE.Mesh(eyeGlobeGeo, this.materials.eyeWhite);
        this.rightEye.scale.set(1.0, 1.2, 0.5);
        this.rightEye.position.set(0.16, -0.05, 0.05);
        this.rightEye.rotation.y = -Math.PI * 0.08;
        
        this.eyesGroup.add(this.leftEye);
        this.eyesGroup.add(this.rightEye);

        // Pupils
        const pupilGeo = new THREE.SphereGeometry(0.08, 16, 16);
        this.leftPupil = new THREE.Mesh(pupilGeo, this.materials.pupil);
        this.leftPupil.scale.set(1.0, 1.1, 0.4);
        this.leftPupil.position.set(0, 0, 0.14); // Forward
        this.leftEye.add(this.leftPupil);

        this.rightPupil = new THREE.Mesh(pupilGeo, this.materials.pupil);
        this.rightPupil.scale.set(1.0, 1.1, 0.4);
        this.rightPupil.position.set(0, 0, 0.14);
        this.rightEye.add(this.rightPupil);

        // Glossy Eye Highlights (tiny white dots)
        const highlightGeo = new THREE.SphereGeometry(0.024, 8, 8);
        const pupilHighlightL = new THREE.Mesh(highlightGeo, this.materials.eyeWhite);
        pupilHighlightL.position.set(0.04, 0.04, 0.07);
        this.leftPupil.add(pupilHighlightL);

        const pupilHighlightR = new THREE.Mesh(highlightGeo, this.materials.eyeWhite);
        pupilHighlightR.position.set(0.04, 0.04, 0.07);
        this.rightPupil.add(pupilHighlightR);
    }

    buildEars() {
        const outerEarGeo = new THREE.SphereGeometry(0.35, 16, 16);
        outerEarGeo.scale(1.0, 1.0, 0.4); // Flat sphere
        const innerEarGeo = new THREE.SphereGeometry(0.24, 16, 16);
        innerEarGeo.scale(1.0, 1.0, 0.4);

        // Left Ear
        this.leftEar = new THREE.Group();
        this.leftEar.position.set(-0.75, 0.05, -0.15);
        this.leftEar.rotation.y = Math.PI * 0.18;
        this.leftEar.rotation.z = Math.PI * 0.08;
        this.head.add(this.leftEar);

        const leftOuter = new THREE.Mesh(outerEarGeo, this.materials.fur);
        leftOuter.castShadow = true;
        const leftInner = new THREE.Mesh(innerEarGeo, this.materials.skin);
        leftInner.position.z = 0.05;
        this.leftEar.add(leftOuter);
        this.leftEar.add(leftInner);

        // Right Ear
        this.rightEar = new THREE.Group();
        this.rightEar.position.set(0.75, 0.05, -0.15);
        this.rightEar.rotation.y = -Math.PI * 0.18;
        this.rightEar.rotation.z = -Math.PI * 0.08;
        this.head.add(this.rightEar);

        const rightOuter = new THREE.Mesh(outerEarGeo, this.materials.fur);
        rightOuter.castShadow = true;
        const rightInner = new THREE.Mesh(innerEarGeo, this.materials.skin);
        rightInner.position.z = 0.05;
        this.rightEar.add(rightOuter);
        this.rightEar.add(rightInner);
    }

    buildAccessories() {
        // --- 1. Green Headband (Tier 1) ---
        this.headband = new THREE.Group();
        this.head.add(this.headband);

        // Main headband loop around forehead
        const bandGeo = new THREE.TorusGeometry(0.725, 0.07, 12, 32);
        const band = new THREE.Mesh(bandGeo, this.materials.headband);
        band.rotation.x = Math.PI * 0.55;
        band.scale.set(1.02, 1.02, 0.6); // Flatten slightly
        band.position.set(0, 0.12, -0.04);
        band.castShadow = true;
        this.headband.add(band);

        // Ribbons hanging at the back
        this.ribbonLeft = new THREE.Mesh(new THREE.ConeGeometry(0.08, 0.4, 4), this.materials.headband);
        this.ribbonLeft.rotation.z = Math.PI * 0.85;
        this.ribbonLeft.position.set(-0.15, 0.0, -0.76);
        this.headband.add(this.ribbonLeft);

        this.ribbonRight = new THREE.Mesh(new THREE.ConeGeometry(0.08, 0.45, 4), this.materials.headband);
        this.ribbonRight.rotation.z = Math.PI * 1.15;
        this.ribbonRight.position.set(0.15, 0.0, -0.76);
        this.headband.add(this.ribbonRight);

        // --- 2. Red Goggles (Tier 2) ---
        this.goggles = new THREE.Group();
        this.goggles.position.set(0, 0.15, 0.49);
        this.head.add(this.goggles);

        // Left Lens frame
        const frameGeo = new THREE.TorusGeometry(0.2, 0.05, 8, 24);
        const frameL = new THREE.Mesh(frameGeo, this.materials.gogglesFrame);
        frameL.position.x = -0.21;
        frameL.castShadow = true;
        
        const lensL = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.18, 0.04, 16), this.materials.gogglesLens);
        lensL.rotation.x = Math.PI * 0.5;
        lensL.position.x = -0.21;
        this.goggles.add(frameL);
        this.goggles.add(lensL);

        // Right Lens frame
        const frameR = new THREE.Mesh(frameGeo, this.materials.gogglesFrame);
        frameR.position.x = 0.21;
        frameR.castShadow = true;
        
        const lensR = new THREE.Mesh(new THREE.CylinderGeometry(0.18, 0.18, 0.04, 16), this.materials.gogglesLens);
        lensR.rotation.x = Math.PI * 0.5;
        lensR.position.x = 0.21;
        this.goggles.add(frameR);
        this.goggles.add(lensR);

        // Nose Bridge strap
        const bridge = new THREE.Mesh(new THREE.BoxGeometry(0.15, 0.05, 0.05), this.materials.gogglesFrame);
        bridge.position.y = -0.02;
        this.goggles.add(bridge);

        // Goggles Elastic Strap wrapping around the skull
        const strapGeo = new THREE.TorusGeometry(0.725, 0.04, 8, 32);
        const strap = new THREE.Mesh(strapGeo, this.materials.gogglesStrap);
        strap.rotation.x = Math.PI * 0.52;
        strap.scale.set(1.01, 1.01, 0.3);
        strap.position.set(0, 0.0, -0.05);
        this.goggles.add(strap);
    }

    buildLimbs() {
        const shoulderGeo = new THREE.SphereGeometry(0.18, 12, 12);
        const limbGeo = new THREE.CylinderGeometry(0.14, 0.12, 0.5, 12);
        const handGeo = new THREE.SphereGeometry(0.16, 12, 12);

        // --- 1. Left Arm (Standard hanging arm) ---
        this.leftArm = new THREE.Group();
        this.leftArm.position.set(-0.55, 0.35, 0);
        this.torso.add(this.leftArm);

        const lShoulder = new THREE.Mesh(shoulderGeo, this.materials.fur);
        this.leftArm.add(lShoulder);

        this.leftForearm = new THREE.Mesh(limbGeo, this.materials.fur);
        this.leftForearm.position.y = -0.3;
        this.leftForearm.castShadow = true;
        this.leftArm.add(this.leftForearm);

        const lHand = new THREE.Mesh(handGeo, this.materials.skin);
        lHand.position.y = -0.58;
        lHand.castShadow = true;
        this.leftArm.add(lHand);

        // --- 2. Right Arm (Active throwing arm, holding a Dart) ---
        this.rightArm = new THREE.Group();
        this.rightArm.position.set(0.55, 0.35, 0);
        this.torso.add(this.rightArm);

        const rShoulder = new THREE.Mesh(shoulderGeo, this.materials.fur);
        this.rightArm.add(rShoulder);

        this.rightForearm = new THREE.Mesh(limbGeo, this.materials.fur);
        this.rightForearm.position.y = -0.3;
        this.rightForearm.castShadow = true;
        this.rightArm.add(this.rightForearm);

        this.rightHand = new THREE.Mesh(handGeo, this.materials.skin);
        this.rightHand.position.y = -0.58;
        this.rightHand.castShadow = true;
        this.rightArm.add(this.rightHand);

        // Build the Dart held in hand
        this.buildDartWeapon();

        // --- 3. Left Leg ---
        this.leftLeg = new THREE.Group();
        this.leftLeg.position.set(-0.25, -0.65, 0.05);
        this.torso.add(this.leftLeg);

        const thighL = new THREE.Mesh(new THREE.SphereGeometry(0.18, 12, 12), this.materials.fur);
        this.leftLeg.add(thighL);

        const shinL = new THREE.Mesh(new THREE.CylinderGeometry(0.15, 0.13, 0.4, 12), this.materials.fur);
        shinL.position.y = -0.22;
        shinL.castShadow = true;
        this.leftLeg.add(shinL);

        const footL = new THREE.Mesh(new THREE.SphereGeometry(0.18, 12, 12), this.materials.skin);
        footL.scale.set(1.0, 0.7, 1.4); // Flat cartoon foot
        footL.position.set(0, -0.42, 0.08);
        footL.castShadow = true;
        this.leftLeg.add(footL);

        // --- 4. Right Leg ---
        this.rightLeg = new THREE.Group();
        this.rightLeg.position.set(0.25, -0.65, 0.05);
        this.torso.add(this.rightLeg);

        const thighR = new THREE.Mesh(new THREE.SphereGeometry(0.18, 12, 12), this.materials.fur);
        this.rightLeg.add(thighR);

        const shinR = new THREE.Mesh(new THREE.CylinderGeometry(0.15, 0.13, 0.4, 12), this.materials.fur);
        shinR.position.y = -0.22;
        shinR.castShadow = true;
        this.rightLeg.add(shinR);

        const footR = new THREE.Mesh(new THREE.SphereGeometry(0.18, 12, 12), this.materials.skin);
        footR.scale.set(1.0, 0.7, 1.4);
        footR.position.set(0, -0.42, 0.08);
        footR.castShadow = true;
        this.rightLeg.add(footR);
    }

    buildDartWeapon() {
        this.heldWeapon = new THREE.Group();
        this.heldWeapon.position.set(0, -0.65, 0.15);
        this.heldWeapon.rotation.x = Math.PI * 0.4;
        this.rightArm.add(this.heldWeapon);

        // --- Standard Wooden Dart ---
        this.heldDart = new THREE.Group();
        this.heldWeapon.add(this.heldDart);

        // Shaft (Wooden stick)
        const shaft = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 0.4, 8), this.materials.dartWood);
        shaft.castShadow = true;
        this.heldDart.add(shaft);

        // Tip (Sharp metal point)
        const tip = new THREE.Mesh(new THREE.ConeGeometry(0.08, 0.2, 8), this.materials.dartMetal);
        tip.position.y = 0.28;
        tip.castShadow = true;
        this.heldDart.add(tip);

        // Feathers (3 red fins)
        for (let i = 0; i < 3; i++) {
            const fin = new THREE.Mesh(new THREE.BoxGeometry(0.01, 0.15, 0.08), this.materials.dartFeather);
            fin.position.y = -0.22;
            fin.rotation.y = (Math.PI * 2 / 3) * i;
            // Shift outward slightly
            fin.translateZ(0.04);
            fin.castShadow = true;
            this.heldDart.add(fin);
        }

        // --- Spiked Ball (Tier 3 weapon replacement) ---
        this.heldSpikedBall = new THREE.Group();
        this.heldSpikedBall.scale.set(0.5, 0.5, 0.5);
        this.heldWeapon.add(this.heldSpikedBall);

        const sphere = new THREE.Mesh(new THREE.SphereGeometry(0.35, 16, 16), this.materials.spikeBall);
        sphere.castShadow = true;
        this.heldSpikedBall.add(sphere);

        // Attach sharp cones around the sphere for spike appearance
        const numSpikes = 12;
        const spikeGeo = new THREE.ConeGeometry(0.08, 0.25, 6);
        for(let i = 0; i < numSpikes; i++) {
            const spike = new THREE.Mesh(spikeGeo, this.materials.spikeBall);
            
            // Distribute on sphere using Fibonacci spiral or random coordinates
            const phi = Math.acos(-1 + (2 * i) / numSpikes);
            const theta = Math.sqrt(numSpikes * Math.PI) * phi;
            
            spike.position.setFromSphericalCoords(0.35, phi, theta);
            spike.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), spike.position.clone().normalize());
            spike.castShadow = true;
            this.heldSpikedBall.add(spike);
        }
    }

    buildTail() {
        this.tail = new THREE.Group();
        this.tail.position.set(0, -0.4, -0.45);
        this.tail.rotation.x = -Math.PI * 0.15;
        this.torso.add(this.tail);

        // Segmented tail to create a smooth organic wiggling effect
        this.tailSegments = [];
        let currentParent = this.tail;
        const numSegments = 6;
        
        for (let i = 0; i < numSegments; i++) {
            const thickness = 0.1 - (i * 0.012);
            const segGeo = new THREE.CylinderGeometry(thickness - 0.01, thickness, 0.25, 8);
            segGeo.translate(0, 0.125, 0); // Offset origin to bottom pivot
            
            const seg = new THREE.Mesh(segGeo, this.materials.fur);
            seg.castShadow = true;
            seg.position.y = i === 0 ? 0 : 0.22;
            
            currentParent.add(seg);
            this.tailSegments.push(seg);
            currentParent = seg;
        }
    }

    /**
     * Tier management system to visually update the monkey model based on upgrades purchased
     */
    setTier(tier) {
        this.tier = tier;

        // Headband visibility (Tier 1+)
        this.headband.visible = (tier >= 1);

        // Goggles visibility (Tier 2+)
        this.goggles.visible = (tier >= 2);

        // Handle held weapons (Tier 3 is Spike-o-pult)
        if (tier >= 3) {
            this.heldDart.visible = false;
            this.heldSpikedBall.visible = true;
        } else {
            this.heldDart.visible = true;
            this.heldSpikedBall.visible = false;
        }
    }

    /**
     * Triggers the fast dart throwing swing animation
     */
    triggerThrow() {
        if (this.isThrowing) return;
        this.isThrowing = true;
        this.throwTimer = 0;
    }

    /**
     * Main updates for procedural animations and looking behavior
     * @param {number} time - Elapsed time in seconds
     * @param {THREE.Vector3} targetPos - Current target coordinate in the 3D space
     */
    update(time, targetPos) {
        // --- 1. Idle Breathing Animation (gentle sinusoidal movement) ---
        const breathSpeed = 1.8;
        const breath = Math.sin(time * breathSpeed);
        
        this.torso.position.y = 0.85 + breath * 0.025;
        this.head.position.y = 0.85 + breath * 0.01;
        this.head.rotation.z = Math.sin(time * 0.9) * 0.015; // gentle head sway

        // Left arm idle breathing swing
        this.leftArm.rotation.z = -Math.PI * 0.08 + Math.sin(time * 1.5) * 0.03;
        this.leftArm.rotation.x = Math.sin(time * 1.2) * 0.05;

        // Legs slight breathing bounce
        this.leftLeg.rotation.x = breath * 0.02;
        this.rightLeg.rotation.x = -breath * 0.02;

        // --- 2. Tail Wagging (traveling sine wave through segments) ---
        this.tailSegments.forEach((seg, index) => {
            const wiggleFactor = 0.15;
            const speed = 4.5;
            seg.rotation.z = Math.sin(time * speed - index * 0.6) * wiggleFactor;
            seg.rotation.x = Math.cos(time * speed * 0.5 - index * 0.3) * 0.08;
        });

        // Flapping headband ribbon tails in the wind
        if (this.headband.visible) {
            this.ribbonLeft.rotation.y = Math.sin(time * 6.0) * 0.12;
            this.ribbonRight.rotation.y = Math.cos(time * 5.0) * 0.12;
        }

        // --- 3. Interactive Head & Eye Look-At Tracking ---
        if (targetPos) {
            // Localized vector direction relative to the head
            const localTarget = targetPos.clone();
            this.head.parent.localToWorld(localTarget); // Get absolute target position
            
            // Calculate direction angles
            const dx = targetPos.x - this.position.x;
            const dz = targetPos.z - this.position.z;
            const yaw = Math.atan2(dx, dz);

            const dy = targetPos.y - (this.position.y + 1.7); // Approximate head height
            const dist = Math.sqrt(dx * dx + dz * dz);
            const pitch = -Math.atan2(dy, dist);

            // Interpolate smooth rotation (damping) towards target
            const maxYaw = 0.6; // Restrict head yaw to 35 degrees
            const maxPitch = 0.35; // Restrict pitch
            
            const targetYaw = Math.max(-maxYaw, Math.min(maxYaw, yaw));
            const targetPitch = Math.max(-maxPitch, Math.min(maxPitch, pitch));

            this.head.rotation.y += (targetYaw - this.head.rotation.y) * 0.08;
            this.head.rotation.x += (targetPitch - this.head.rotation.x) * 0.08;

            // Pupils move in their eye sockets for cartoon look-at
            const pupOffsetX = Math.max(-0.06, Math.min(0.06, yaw * 0.08));
            const pupOffsetY = Math.max(-0.06, Math.min(0.06, pitch * 0.08));

            this.leftPupil.position.x = pupOffsetX;
            this.leftPupil.position.y = pupOffsetY;
            this.rightPupil.position.x = pupOffsetX;
            this.rightPupil.position.y = pupOffsetY;
        } else {
            // Smoothly snap back to looking forward
            this.head.rotation.y += (0 - this.head.rotation.y) * 0.05;
            this.head.rotation.x += (0 - this.head.rotation.x) * 0.05;
            this.leftPupil.position.set(0, 0, 0.14);
            this.rightPupil.position.set(0, 0, 0.14);
        }

        // --- 4. Weapon Throwing Swipe Keyframe Controller ---
        if (this.isThrowing) {
            this.throwTimer += 0.06; // Increment transition frame
            
            if (this.throwTimer < 0.25) {
                // Phase 1: Wind-up (pull arm back)
                const t = this.throwTimer / 0.25;
                this.rightArm.rotation.x = Math.max(-Math.PI * 0.4, -Math.PI * 0.4 * t);
                this.rightArm.rotation.y = Math.sin(t * Math.PI) * -0.25;
            } else if (this.throwTimer < 0.55) {
                // Phase 2: Snap Throw (whip arm forward)
                const t = (this.throwTimer - 0.25) / 0.3;
                this.rightArm.rotation.x = -Math.PI * 0.4 + (Math.PI * 1.0 * t); // Swift overhead throw
                this.rightArm.rotation.y = -0.25 + (0.5 * t);
                
                // Shrink weapon briefly to represent it exiting hand
                const weaponScale = Math.max(0, 1 - t * 1.5);
                this.heldWeapon.scale.set(weaponScale, weaponScale, weaponScale);
            } else if (this.throwTimer < 0.9) {
                // Phase 3: Recover and reload next projectile
                const t = (this.throwTimer - 0.55) / 0.35;
                this.rightArm.rotation.x = (-Math.PI * 0.4 + Math.PI * 1.0) * (1 - t);
                this.rightArm.rotation.y = 0.25 * (1 - t);
                
                // Weapon reappears / reloads in hand
                this.heldWeapon.scale.set(t, t, t);
            } else {
                // Animation complete
                this.isThrowing = false;
                this.heldWeapon.scale.set(1, 1, 1);
            }
        } else {
            // Normal right arm idle drift
            this.rightArm.rotation.x = Math.PI * 0.05 + Math.sin(time * 1.1) * 0.04;
            this.rightArm.rotation.z = Math.PI * 0.08 + Math.sin(time * 1.4) * 0.02;
            this.rightArm.rotation.y = 0;
        }
    }
}

// Attach to window so app.js can access it
window.DartMonkey3D = DartMonkey3D;
