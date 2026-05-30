/**
 * ==========================================
 * GEAR CHRONICLES - MONKEY D. LUFFY 2D RIG
 * ==========================================
 * Dynamic, state-based vector skeletal rig drawn procedurally on Canvas.
 */

class LuffyRig {
    constructor(canvas) {
        this.canvas = canvas;
        this.ctx = canvas.getContext('2d');
        
        // Base coordinate positioning
        this.x = canvas.width / 2;
        this.y = canvas.height / 2 + 50;
        this.scale = 1.0;
        
        // State parameters
        this.gear = 'base'; // 'base', '2', '4', '5'
        this.time = 0;
        
        // Skeletal stretching / posing factors
        this.stretchRightArmX = 0;
        this.stretchRightArmY = 0;
        this.stretchLeftArmX = 0;
        this.stretchLeftArmY = 0;
        
        this.gatlingOffsetIndex = 0;
        
        // Pose modifiers
        this.isCrouching = false;
        this.isAttacking = false;
        this.attackType = null; // 'pistol', 'gatling', 'redhawk'
        this.attackProgress = 0; // 0 to 1
        
        // Bouncing factor for idle state
        this.bobY = 0;
        this.windBreeze = 0;
    }
    
    setGear(gear) {
        this.gear = gear;
        if (gear === '4') {
            this.scale = 1.3;
        } else if (gear === '5') {
            this.scale = 1.1;
        } else {
            this.scale = 1.0;
        }
    }
    
    update(time, sfxEngine = null) {
        this.time = time;
        this.x = this.canvas.width / 2;
        this.y = this.canvas.height / 2 + 50;
        
        // Dynamic wind breeze calculation
        this.windBreeze = Math.sin(time * 3) * 0.15;
        
        // Procedural idling physics depending on Gear
        if (this.gear === 'base') {
            this.bobY = Math.sin(time * 2.5) * 8;
            this.isCrouching = false;
        } 
        else if (this.gear === '2') {
            // Gear 2 fast vibration and crouch pose
            this.bobY = Math.sin(time * 15) * 2; 
            this.isCrouching = true;
        } 
        else if (this.gear === '4') {
            // Gear 4 bouncy rubber ball float
            this.bobY = Math.sin(time * 6) * 15 - 10;
            this.isCrouching = false;
        } 
        else if (this.gear === '5') {
            // Gear 5 rubbery high-amplitude flowy bounce
            this.bobY = Math.sin(time * 4.5) * 25 - 30; // Floats in the air!
            this.isCrouching = false;
        }
        
        // Handle Attack Anim states
        if (this.isAttacking) {
            this.attackProgress += 0.05;
            if (this.attackProgress >= 1) {
                this.isAttacking = false;
                this.attackProgress = 0;
                this.attackType = null;
                this.stretchRightArmX = 0;
                this.stretchRightArmY = 0;
                this.stretchLeftArmX = 0;
                this.stretchLeftArmY = 0;
            } else {
                this.updateAttackPosing();
            }
        }
    }
    
    triggerAttack(type) {
        this.isAttacking = true;
        this.attackType = type;
        this.attackProgress = 0;
        
        if (type === 'gatling') {
            this.gatlingOffsetIndex = 0;
        }
    }
    
    updateAttackPosing() {
        let t = this.attackProgress;
        
        if (this.attackType === 'pistol') {
            // Gum-Gum Pistol: arm goes way back, then snaps forward
            if (t < 0.3) {
                // Wind up back
                let windT = t / 0.3;
                this.stretchRightArmX = -80 * windT;
                this.stretchRightArmY = -10 * windT;
            } else if (t < 0.6) {
                // Extreme stretch punch forward
                let punchT = (t - 0.3) / 0.3;
                this.stretchRightArmX = -80 + (400 * punchT);
                this.stretchRightArmY = -10 + (20 * punchT);
            } else {
                // Retract arm back
                let retractT = (t - 0.6) / 0.4;
                this.stretchRightArmX = 320 - (320 * retractT);
                this.stretchRightArmY = 10 - (10 * retractT);
            }
        } 
        else if (this.attackType === 'gatling') {
            // Gum-Gum Gatling: left/right arms alternating rapid punches
            this.gatlingOffsetIndex = (this.gatlingOffsetIndex + 1) % 4;
            let punchLen = 220 + Math.sin(this.time * 40) * 80;
            if (this.gatlingOffsetIndex < 2) {
                this.stretchRightArmX = punchLen;
                this.stretchRightArmY = (Math.random() - 0.5) * 120;
                this.stretchLeftArmX = 0;
                this.stretchLeftArmY = 0;
            } else {
                this.stretchLeftArmX = punchLen;
                this.stretchLeftArmY = (Math.random() - 0.5) * 120;
                this.stretchRightArmX = 0;
                this.stretchRightArmY = 0;
            }
        }
        else if (this.attackType === 'redhawk') {
            // Gum-Gum Red Hawk: slow cinematic back stretching, then flaming sonic punch!
            if (t < 0.4) {
                // Epic slow stretch back
                let windT = t / 0.4;
                this.stretchRightArmX = -120 * windT;
                this.stretchRightArmY = -30 * windT;
            } else if (t < 0.7) {
                // Sonic explosive thrust forward
                let punchT = (t - 0.4) / 0.3;
                this.stretchRightArmX = -120 + (460 * punchT);
                this.stretchRightArmY = -30 + (40 * punchT);
            } else {
                // Slow burn retract
                let retractT = (t - 0.7) / 0.3;
                this.stretchRightArmX = 340 - (340 * retractT);
                this.stretchRightArmY = 10 - (10 * retractT);
            }
        }
    }
    
    draw(cameraShakeOffset = {x: 0, y: 0}) {
        let ctx = this.ctx;
        
        ctx.save();
        // Center of the canvas viewport
        ctx.translate(this.x + cameraShakeOffset.x, this.y + cameraShakeOffset.y + this.bobY);
        ctx.scale(this.scale, this.scale);
        
        // Define key coordinate joint offsets relative to center
        let headRadius = 24;
        let neckY = -70;
        let torsoY = -40;
        let hipY = 10;
        
        // Crouch stance modifies core joint coordinates
        if (this.isCrouching) {
            neckY = -40;
            torsoY = -25;
            hipY = 0;
        }
        
        let headY = neckY - headRadius;
        
        // Colors & Forms Configurations
        let skinColor = '#fed7aa'; // Nice peach flesh tone
        let shirtColor = '#dc2626'; // Vibrant Luffy Crimson
        let shortColor = '#0284c7'; // Denim Blue
        let sashColor = '#facc15'; // Gold Sash
        let hairColor = '#000000'; // Pitch Black spiky hair
        let shadowColor = 'rgba(0, 0, 0, 0.15)';
        
        if (this.gear === '2') {
            skinColor = '#fba3b6'; // Glowing steam pink
            shirtColor = '#e11d48';
        } else if (this.gear === '4') {
            skinColor = '#fda4af'; // Pinkish-haki undertone
            shirtColor = '#991b1b';
        } else if (this.gear === '5') {
            skinColor = '#fafafa'; // Divine Joyboy White
            shirtColor = '#ffffff'; // Nika White clothes
            shortColor = '#f3f4f6';
            sashColor = '#fbbf24';
            hairColor = '#ffffff'; // Pure Nika cloud hair
        }
        
        // Render background details for Gear 5 (Nika Vapor Hagoromo Cloud)
        if (this.gear === '5') {
            this.drawHagoromo(headY + 10);
        }
        
        // ------------------
        // LEGS DRAWING
        // ------------------
        let leftLegX = -25, leftLegY = hipY;
        let rightLegX = 25, rightLegY = hipY;
        
        ctx.strokeStyle = skinColor;
        ctx.lineCap = 'round';
        ctx.lineWidth = this.gear === '4' ? 18 : 10;
        
        // Left Leg
        ctx.beginPath();
        ctx.moveTo(leftLegX, leftLegY);
        if (this.isCrouching) {
            ctx.lineTo(-45, leftLegY + 15);
            ctx.lineTo(-30, leftLegY + 45); // Crouched foot
        } else if (this.gear === '5') {
            // Rubbery squiggly cartoon floaty leg
            let wobble = Math.sin(this.time * 6) * 12;
            ctx.quadraticCurveTo(-35 + wobble, leftLegY + 25, -20, leftLegY + 50);
        } else {
            ctx.lineTo(-30, leftLegY + 25);
            ctx.lineTo(-25, leftLegY + 55);
        }
        ctx.stroke();
        
        // Right Leg
        ctx.beginPath();
        ctx.moveTo(rightLegX, rightLegY);
        if (this.isCrouching) {
            ctx.lineTo(15, rightLegY + 20); // Arm touches ground
            ctx.lineTo(25, rightLegY + 45);
        } else if (this.gear === '5') {
            let wobble = Math.cos(this.time * 6) * 12;
            ctx.quadraticCurveTo(35 + wobble, rightLegY + 25, 20, rightLegY + 50);
        } else {
            ctx.lineTo(30, rightLegY + 25);
            ctx.lineTo(25, rightLegY + 55);
        }
        ctx.stroke();
        
        // Draw Shorts (Blue)
        ctx.strokeStyle = shortColor;
        ctx.lineWidth = this.gear === '4' ? 22 : 12;
        ctx.beginPath();
        ctx.moveTo(-20, hipY);
        ctx.lineTo(-28, hipY + 16);
        ctx.moveTo(20, hipY);
        ctx.lineTo(28, hipY + 16);
        ctx.stroke();
        
        // Draw fluff cuffs for shorts
        ctx.strokeStyle = this.gear === '5' ? '#e5e7eb' : '#ffffff';
        ctx.lineWidth = this.gear === '4' ? 24 : 14;
        ctx.beginPath();
        ctx.moveTo(-28, hipY + 16);
        ctx.lineTo(-29, hipY + 18);
        ctx.moveTo(28, hipY + 16);
        ctx.lineTo(29, hipY + 18);
        ctx.stroke();
        
        // Haki arms and legs coating for Gear 4 (Boundman Metallic dark-red/black)
        if (this.gear === '4') {
            this.drawHakiLimbs(leftLegX - 5, leftLegY + 15, -30, leftLegY + 55, 'leg');
            this.drawHakiLimbs(rightLegX + 5, rightLegY + 15, 25, rightLegY + 55, 'leg');
        }
        
        // ------------------
        // TORSO DRAWING
        // ------------------
        let torsoWidth = this.gear === '4' ? 55 : 22;
        ctx.fillStyle = shirtColor;
        ctx.beginPath();
        ctx.moveTo(-torsoWidth, torsoY);
        ctx.lineTo(torsoWidth, torsoY);
        ctx.lineTo(15, hipY);
        ctx.lineTo(-15, hipY);
        ctx.closePath();
        ctx.fill();
        
        // Open shirt crease (Yellow sash/chest open skin area)
        ctx.fillStyle = skinColor;
        ctx.beginPath();
        ctx.moveTo(-5, torsoY);
        ctx.lineTo(5, torsoY);
        ctx.lineTo(0, hipY - 10);
        ctx.closePath();
        ctx.fill();
        
        // Luffy X Chest Scar (starts drawing in base / visible in all forms)
        ctx.strokeStyle = this.gear === '5' ? 'rgba(239, 68, 68, 0.4)' : '#b91c1c';
        ctx.lineWidth = this.gear === '4' ? 4 : 2;
        ctx.beginPath();
        ctx.moveTo(-8, torsoY + 10);
        ctx.lineTo(8, torsoY + 22);
        ctx.moveTo(8, torsoY + 10);
        ctx.lineTo(-8, torsoY + 22);
        ctx.stroke();
        
        // Yellow Sash around waist
        ctx.fillStyle = sashColor;
        ctx.fillRect(-16, hipY - 6, 32, 7);
        // Hanging sash cloth with wind breeze
        ctx.beginPath();
        ctx.moveTo(10, hipY);
        ctx.quadraticCurveTo(15 + this.windBreeze * 30, hipY + 15, 8 + this.windBreeze * 40, hipY + 32);
        ctx.lineTo(2 + this.windBreeze * 40, hipY + 30);
        ctx.quadraticCurveTo(9 + this.windBreeze * 30, hipY + 12, 4, hipY);
        ctx.closePath();
        ctx.fill();
        
        // ------------------
        // ARMS DRAWING (ELASTIC / ATTACK POSING)
        // ------------------
        let leftShoulderX = -torsoWidth;
        let leftShoulderY = torsoY + 8;
        let rightShoulderX = torsoWidth;
        let rightShoulderY = torsoY + 8;
        
        ctx.strokeStyle = skinColor;
        ctx.lineWidth = this.gear === '4' ? 22 : 9;
        
        // Left Arm (Dynamic stretch)
        ctx.beginPath();
        ctx.moveTo(leftShoulderX, leftShoulderY);
        if (this.stretchLeftArmX !== 0 || this.stretchLeftArmY !== 0) {
            // Stretching punch! In Gatling or Pistol
            ctx.lineTo(leftShoulderX - 25, leftShoulderY - 5);
            // Draw a wavy rubber stretching line
            let controlX = (leftShoulderX - 25 + (leftShoulderX - 25 - this.stretchLeftArmX)) / 2;
            let controlY = (leftShoulderY - 5 + (leftShoulderY - 5 + this.stretchLeftArmY)) / 2 + Math.sin(this.time * 25) * 15;
            ctx.quadraticCurveTo(controlX, controlY, leftShoulderX - 25 - this.stretchLeftArmX, leftShoulderY + this.stretchLeftArmY);
        } else if (this.isCrouching) {
            // Touch ground in crouch
            ctx.lineTo(-30, leftShoulderY + 20);
            ctx.lineTo(-35, leftShoulderY + 45); // Fist touches ground
        } else if (this.gear === '5') {
            // Rubbery floaty floaty arms
            let wobble = Math.sin(this.time * 5) * 15;
            ctx.quadraticCurveTo(leftShoulderX - 20, leftShoulderY + 10 + wobble, leftShoulderX - 35, leftShoulderY + 25 + wobble);
        } else {
            // Normal arm resting or in wind
            ctx.lineTo(leftShoulderX - 15, leftShoulderY + 15 + this.windBreeze * 10);
            ctx.lineTo(leftShoulderX - 22, leftShoulderY + 35 + this.windBreeze * 12);
        }
        ctx.stroke();
        
        // Right Arm (Dynamic stretch)
        ctx.beginPath();
        ctx.moveTo(rightShoulderX, rightShoulderY);
        if (this.stretchRightArmX !== 0 || this.stretchRightArmY !== 0) {
            // Stretching punch! Red Hawk or Pistol
            ctx.lineTo(rightShoulderX + 25, rightShoulderY - 5);
            let controlX = (rightShoulderX + 25 + (rightShoulderX + 25 + this.stretchRightArmX)) / 2;
            let controlY = (rightShoulderY - 5 + (rightShoulderY - 5 + this.stretchRightArmY)) / 2 + Math.sin(this.time * 25) * 12;
            ctx.quadraticCurveTo(controlX, controlY, rightShoulderX + 25 + this.stretchRightArmX, rightShoulderY + this.stretchRightArmY);
        } else if (this.isCrouching) {
            ctx.lineTo(rightShoulderX + 15, rightShoulderY - 5);
            ctx.lineTo(rightShoulderX + 12, rightShoulderY - 25); // Resting on knee
        } else if (this.gear === '5') {
            let wobble = Math.cos(this.time * 5) * 15;
            ctx.quadraticCurveTo(rightShoulderX + 20, rightShoulderY + 10 + wobble, rightShoulderX + 35, rightShoulderY + 25 + wobble);
        } else {
            ctx.lineTo(rightShoulderX + 15, rightShoulderY + 15 - this.windBreeze * 10);
            ctx.lineTo(rightShoulderX + 22, rightShoulderY + 35 - this.windBreeze * 12);
        }
        ctx.stroke();
        
        // Draw Red Vest sleeves cuffs
        ctx.fillStyle = shirtColor;
        ctx.beginPath();
        ctx.arc(leftShoulderX, leftShoulderY, this.gear === '4' ? 14 : 7, 0, Math.PI * 2);
        ctx.arc(rightShoulderX, rightShoulderY, this.gear === '4' ? 14 : 7, 0, Math.PI * 2);
        ctx.fill();
        
        // Haki arms for Gear 4
        if (this.gear === '4') {
            this.drawHakiLimbs(leftShoulderX, leftShoulderY, leftShoulderX - 22, leftShoulderY + 35, 'arm');
            this.drawHakiLimbs(rightShoulderX, rightShoulderY, rightShoulderX + 22, rightShoulderY + 35, 'arm');
        }
        
        // Draw stretched Haki arm in Red Hawk / Pistol if in Gear 4
        if (this.gear === '4' && this.isAttacking) {
            if (this.stretchRightArmX !== 0) {
                this.drawHakiLimbs(rightShoulderX + 25, rightShoulderY - 5, rightShoulderX + 25 + this.stretchRightArmX, rightShoulderY + this.stretchRightArmY, 'arm');
            }
            if (this.stretchLeftArmX !== 0) {
                this.drawHakiLimbs(leftShoulderX - 25, leftShoulderY - 5, leftShoulderX - 25 - this.stretchLeftArmX, leftShoulderY + this.stretchLeftArmY, 'arm');
            }
        }
        
        // ------------------
        // HEAD & NECK DRAWING
        // ------------------
        ctx.strokeStyle = skinColor;
        ctx.lineWidth = this.gear === '4' ? 16 : 8;
        ctx.beginPath();
        ctx.moveTo(0, neckY);
        ctx.lineTo(0, headY + 5);
        ctx.stroke();
        
        // Draw Head base
        ctx.fillStyle = skinColor;
        ctx.beginPath();
        ctx.arc(0, headY, headRadius, 0, Math.PI * 2);
        ctx.fill();
        
        // Scar under left eye
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(-10, headY + 5);
        ctx.lineTo(-7, headY + 7);
        ctx.moveTo(-11, headY + 6);
        ctx.lineTo(-6, headY + 6);
        ctx.stroke();
        
        // Eyes and Smile (Nika vs standard vs grit)
        if (this.gear === '5') {
            // Joyboy cute curly/laughing face
            // Eyes: giant white curves with circles
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2.5;
            
            // Left eye laughing crescent
            ctx.beginPath();
            ctx.arc(-11, headY - 3, 6, Math.PI, 0, false);
            ctx.stroke();
            
            // Right eye laughing crescent
            ctx.beginPath();
            ctx.arc(11, headY - 3, 6, Math.PI, 0, false);
            ctx.stroke();
            
            // Huge Nika smile
            ctx.fillStyle = '#ffffff';
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2.5;
            ctx.beginPath();
            ctx.arc(0, headY + 6, 12, 0, Math.PI, false);
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
        } 
        else if (this.gear === '4') {
            // Gear 4 angry grit
            ctx.fillStyle = '#ffffff';
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2;
            
            // White glaring eyes with red outline
            ctx.strokeStyle = '#ef4444';
            ctx.beginPath();
            ctx.arc(-10, headY - 4, 5, 0, Math.PI * 2);
            ctx.arc(10, headY - 4, 5, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
            
            ctx.fillStyle = '#000000';
            ctx.beginPath();
            ctx.arc(-10, headY - 4, 2, 0, Math.PI * 2);
            ctx.arc(10, headY - 4, 2, 0, Math.PI * 2);
            ctx.fill();
            
            // Angry brow lines
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2.5;
            ctx.beginPath();
            ctx.moveTo(-16, headY - 11);
            ctx.lineTo(-4, headY - 6);
            ctx.moveTo(16, headY - 11);
            ctx.lineTo(4, headY - 6);
            ctx.stroke();
            
            // Gritting teeth line
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(-10, headY + 8);
            ctx.lineTo(10, headY + 8);
            ctx.stroke();
        } 
        else {
            // Standard Base / Gear 2 smile
            ctx.fillStyle = '#ffffff';
            ctx.beginPath();
            ctx.arc(-9, headY - 2, 5, 0, Math.PI * 2);
            ctx.arc(9, headY - 2, 5, 0, Math.PI * 2);
            ctx.fill();
            
            ctx.fillStyle = '#000000';
            ctx.beginPath();
            ctx.arc(-9, headY - 2, 2.5, 0, Math.PI * 2);
            ctx.arc(9, headY - 2, 2.5, 0, Math.PI * 2);
            ctx.fill();
            
            // Classic Luffy grin
            ctx.strokeStyle = '#000000';
            ctx.lineWidth = 2.5;
            ctx.beginPath();
            ctx.arc(0, headY + 6, 9, 0.1 * Math.PI, 0.9 * Math.PI, false);
            ctx.stroke();
            
            // Grin tick lines
            ctx.beginPath();
            ctx.moveTo(-8, headY + 6);
            ctx.lineTo(-9, headY + 3);
            ctx.moveTo(8, headY + 6);
            ctx.lineTo(9, headY + 3);
            ctx.stroke();
        }
        
        // ------------------
        // HAIR DRAWING
        // ------------------
        ctx.fillStyle = hairColor;
        ctx.beginPath();
        if (this.gear === '5') {
            // Fluffy flowing cloud hair
            let timeFactor = this.time * 6;
            ctx.arc(0, headY - 22, 18 + Math.sin(timeFactor) * 2, 0, Math.PI * 2);
            ctx.arc(-16, headY - 12 + Math.cos(timeFactor) * 2, 14, 0, Math.PI * 2);
            ctx.arc(16, headY - 12 + Math.sin(timeFactor + 1) * 2, 14, 0, Math.PI * 2);
            ctx.arc(-22, headY + 2, 10, 0, Math.PI * 2);
            ctx.arc(22, headY + 2, 10, 0, Math.PI * 2);
            ctx.fill();
            
            // Outlines for fluffy hair
            ctx.strokeStyle = '#e2e8f0';
            ctx.lineWidth = 2;
            ctx.stroke();
        } 
        else {
            // Spiky black Luffy hair
            ctx.beginPath();
            ctx.moveTo(-headRadius - 4, headY);
            ctx.lineTo(-headRadius - 1, headY - 14);
            ctx.lineTo(-14, headY - 22);
            ctx.lineTo(-12, headY - 32);
            ctx.lineTo(0, headY - 28);
            ctx.lineTo(12, headY - 32);
            ctx.lineTo(14, headY - 22);
            ctx.lineTo(headRadius + 1, headY - 14);
            ctx.lineTo(headRadius + 4, headY);
            
            // Forehead bangs
            ctx.lineTo(12, headY - 10);
            ctx.lineTo(8, headY - 6);
            ctx.lineTo(0, headY - 14);
            ctx.lineTo(-8, headY - 6);
            ctx.lineTo(-12, headY - 10);
            ctx.closePath();
            ctx.fill();
        }
        
        // ------------------
        // STRAW HAT DRAWING (Only on Base, Gear 2, Gear 4. On Gear 5 it hangs back or turns white!)
        // ------------------
        if (this.gear !== '5') {
            ctx.save();
            ctx.translate(0, headY - 22);
            ctx.rotate(this.windBreeze);
            
            // Brim
            ctx.fillStyle = '#eab308'; // Dark straw yellow
            ctx.strokeStyle = '#ca8a04';
            ctx.lineWidth = 1.5;
            
            ctx.beginPath();
            ctx.ellipse(0, 5, headRadius + 16, 8, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
            
            // Hat Dome
            ctx.beginPath();
            ctx.arc(0, 0, headRadius - 2, Math.PI, 0, false);
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
            
            // Red Ribbon
            ctx.fillStyle = '#dc2626'; // Ribbon red
            ctx.beginPath();
            ctx.rect(-headRadius + 2.5, -4, (headRadius - 2.5) * 2, 8);
            ctx.fill();
            
            ctx.restore();
        } else {
            // Gear 5: Straw hat glows white/golden, hanging behind neck
            ctx.save();
            ctx.translate(0, headY + 12);
            
            ctx.fillStyle = '#fafafa';
            ctx.strokeStyle = '#fbbf24';
            ctx.lineWidth = 2;
            
            ctx.beginPath();
            ctx.ellipse(0, 10, headRadius + 10, 6, 0, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
            
            ctx.beginPath();
            ctx.arc(0, 12, headRadius - 8, 0, Math.PI, false);
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
            
            ctx.restore();
        }
        
        ctx.restore(); // Restore rig translation
    }
    
    // Draw the Gear 5 (Nika) dynamic back vapor ring (Hagoromo)
    drawHagoromo(yOffset) {
        let ctx = this.ctx;
        ctx.save();
        ctx.translate(0, yOffset);
        
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.7)';
        ctx.shadowColor = 'rgba(186, 230, 253, 0.6)';
        ctx.shadowBlur = 15;
        ctx.lineWidth = 14;
        ctx.lineCap = 'round';
        
        // Large cartoon ribbon swirling behind shoulder
        let w = 110 + Math.sin(this.time * 5) * 8;
        let h = 55 + Math.cos(this.time * 5) * 5;
        
        ctx.beginPath();
        // A dynamic infinity-like or wide oval cloud path wrapping behind neck
        ctx.ellipse(0, -10, w, h, Math.sin(this.time * 2.5) * 0.05, 0, Math.PI * 2);
        ctx.stroke();
        
        // Layer fluffy circles onto the vapor ring
        ctx.fillStyle = 'rgba(255, 255, 255, 0.9)';
        ctx.beginPath();
        for (let angle = 0; angle < Math.PI * 2; angle += Math.PI / 4) {
            let cx = Math.cos(angle) * w;
            let cy = Math.sin(angle) * h - 10;
            ctx.arc(cx, cy, 14 + Math.sin(this.time * 6 + angle) * 4, 0, Math.PI * 2);
        }
        ctx.fill();
        
        ctx.restore();
    }
    
    // Draw Armament Haki limb styling for Gear 4 (Gradients and tribal spirals)
    drawHakiLimbs(startX, startY, endX, endY, type) {
        let ctx = this.ctx;
        ctx.save();
        
        let hakiGrad = ctx.createLinearGradient(startX, startY, endX, endY);
        hakiGrad.addColorStop(0, '#000000'); // Shiny black
        hakiGrad.addColorStop(0.5, '#450a0a'); // Dark crimson-maroon
        hakiGrad.addColorStop(1, '#991b1b'); // Bright red edge
        
        ctx.strokeStyle = hakiGrad;
        ctx.lineWidth = type === 'arm' ? 18 : 22;
        ctx.lineCap = 'round';
        
        ctx.beginPath();
        ctx.moveTo(startX, startY);
        ctx.lineTo(endX, endY);
        ctx.stroke();
        
        // Tribal Haki flame swirls drawn on top
        ctx.strokeStyle = 'rgba(239, 68, 68, 0.5)';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(startX, startY);
        ctx.quadraticCurveTo((startX + endX) / 2 + 10, (startY + endY) / 2 - 10, endX, endY);
        ctx.stroke();
        
        ctx.restore();
    }
}
