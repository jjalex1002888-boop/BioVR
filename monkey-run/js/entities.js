/**
 * Banana Run 2D - Procedural 2D Rendering & Skeletal Animation Library
 */

/**
 * 1. DYNAMIC PARALLAX BACKGROUND SYSTEM
 */
export class ParallaxBackground {
    constructor() {
        this.layers = [
            { yOffset: 0, scrollRatio: 0.02, draw: this.drawSky.bind(this) },
            { yOffset: 40, scrollRatio: 0.12, draw: this.drawDistantMountains.bind(this) },
            { yOffset: 80, scrollRatio: 0.35, draw: this.drawMidgroundRuins.bind(this) },
            { yOffset: 0, scrollRatio: 1.45, draw: this.drawForegroundLeaves.bind(this) }
        ];
        this.stars = [];
        this.generateStars();
    }

    generateStars() {
        for (let i = 0; i < 40; i++) {
            this.stars.push({
                x: Math.random(),
                y: Math.random() * 0.4,
                size: 1 + Math.random() * 1.5,
                alpha: 0.3 + Math.random() * 0.7
            });
        }
    }

    draw(ctx, canvasWidth, canvasHeight, scrollX) {
        this.layers.forEach(layer => {
            ctx.save();
            const posX = -(scrollX * layer.scrollRatio) % canvasWidth;
            
            // Draw dual scrolling panels to prevent seams
            ctx.translate(posX, layer.yOffset);
            layer.draw(ctx, canvasWidth, canvasHeight, scrollX);
            
            ctx.translate(canvasWidth, 0);
            layer.draw(ctx, canvasWidth, canvasHeight, scrollX);
            
            ctx.restore();
        });
    }

    drawSky(ctx, width, height) {
        // Deep twilight gradient
        const grad = ctx.createLinearGradient(0, 0, 0, height * 0.8);
        grad.addColorStop(0, '#020503'); // deep black green
        grad.addColorStop(0.5, '#06130b'); // very dark moss green
        grad.addColorStop(1, '#0e2316'); // jungle teal sky
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, width, height);

        // Stars
        ctx.fillStyle = '#ffffff';
        this.stars.forEach(star => {
            ctx.globalAlpha = star.alpha * (0.6 + 0.4 * Math.sin(Date.now() * 0.001 * star.size));
            ctx.fillRect(star.x * width, star.y * height, star.size, star.size);
        });
        ctx.globalAlpha = 1.0;

        // Big crescent bioluminescent moon
        ctx.shadowColor = '#00ffcc';
        ctx.shadowBlur = 25;
        const moonX = width * 0.75;
        const moonY = height * 0.2;
        ctx.fillStyle = 'rgba(200, 255, 235, 0.8)';
        ctx.beginPath();
        ctx.arc(moonX, moonY, 40, 0, Math.PI * 2);
        ctx.fill();

        // Moon crescent mask cut out
        ctx.shadowBlur = 0;
        ctx.fillStyle = '#020503';
        ctx.beginPath();
        ctx.arc(moonX + 15, moonY - 5, 38, 0, Math.PI * 2);
        ctx.fill();
    }

    drawDistantMountains(ctx, width, height, scrollX) {
        // Silhouetted mountain spires and temples
        ctx.fillStyle = '#06150d';
        ctx.beginPath();
        ctx.moveTo(0, height * 0.7);

        // Procedural peaks
        const step = width / 6;
        for (let i = 0; i <= 6; i++) {
            const hOffset = Math.sin(i * 12.3 + 45) * 45;
            // Draw triangular structures resembling ancient ruins
            if (i % 2 === 1) {
                // Ancient pyramid silhouette
                ctx.lineTo(i * step - step * 0.5, height * 0.42 + hOffset);
                ctx.lineTo(i * step, height * 0.7);
            } else {
                ctx.lineTo(i * step - step * 0.3, height * 0.52 + hOffset);
                ctx.lineTo(i * step, height * 0.7);
            }
        }
        ctx.lineTo(width, height);
        ctx.lineTo(0, height);
        ctx.closePath();
        ctx.fill();

        // Fog overlay layer
        const fog = ctx.createLinearGradient(0, height * 0.5, 0, height);
        fog.addColorStop(0, 'rgba(10, 35, 22, 0.0)');
        fog.addColorStop(1, 'rgba(10, 35, 22, 0.45)');
        ctx.fillStyle = fog;
        ctx.fillRect(0, height * 0.4, width, height * 0.6);
    }

    drawMidgroundRuins(ctx, width, height, scrollX) {
        // Pillars and trunks
        ctx.fillStyle = '#0e2b1b';
        ctx.strokeStyle = '#05180f';
        ctx.lineWidth = 4;

        // Draw periodic ruined pillars
        const spacing = width / 4;
        for (let i = 0; i < 4; i++) {
            const pillarX = i * spacing + spacing * 0.3;
            const pHeight = 160 + Math.sin(i * 98.7) * 40;
            const pWidth = 32;

            // Stone Pillar
            ctx.fillRect(pillarX, height * 0.7 - pHeight, pWidth, pHeight);
            
            // Draw horizontal crack lines
            ctx.beginPath();
            ctx.moveTo(pillarX, height * 0.7 - pHeight + pHeight * 0.3);
            ctx.lineTo(pillarX + pWidth, height * 0.7 - pHeight + pHeight * 0.4);
            ctx.moveTo(pillarX, height * 0.7 - pHeight + pHeight * 0.75);
            ctx.lineTo(pillarX + pWidth, height * 0.7 - pHeight + pHeight * 0.68);
            ctx.stroke();

            // Moss crowns
            ctx.fillStyle = '#1c4d32';
            ctx.beginPath();
            ctx.arc(pillarX + pWidth / 2, height * 0.7 - pHeight, pWidth * 0.7, 0, Math.PI, true);
            ctx.fill();
            ctx.fillStyle = '#0e2b1b'; // swap back
        }

        // Draw some midground palm trunks
        ctx.fillStyle = '#143825';
        for (let i = 0; i < 3; i++) {
            const trunkX = i * spacing + spacing * 0.8;
            ctx.beginPath();
            ctx.ellipse(trunkX, height * 0.7, 12, height * 0.5, -0.06, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    drawForegroundLeaves(ctx, width, height) {
        // High parallax close leaves scrolling extremely rapidly
        ctx.fillStyle = 'rgba(3, 15, 8, 0.82)'; // dark silhouette
        
        // Large leafy branches hanging from top corners
        ctx.beginPath();
        // Top Left Branch
        ctx.moveTo(-10, -10);
        ctx.quadraticCurveTo(width * 0.15, height * 0.25, width * 0.28, -20);
        ctx.quadraticCurveTo(width * 0.1, height * 0.08, -10, -10);
        ctx.fill();

        // Hanging fronds
        for (let j = 0; j < 5; j++) {
            ctx.beginPath();
            const startX = width * 0.06 * j;
            const leafLen = 40 + j * 12;
            ctx.ellipse(startX, 10, 8, leafLen, 0.4 + j * 0.15, 0, Math.PI * 2);
            ctx.fill();
        }

        // Bottom right bush
        ctx.beginPath();
        ctx.moveTo(width + 10, height);
        ctx.quadraticCurveTo(width * 0.8, height * 0.8, width * 0.7, height + 10);
        ctx.fill();
    }
}

/**
 * 2. HIERARCHICAL SKELETAL 2D MONKEY CHARACTER
 */
export class MonkeyCharacter2D {
    constructor(skinConfig) {
        this.config = skinConfig;
        this.width = 60;
        this.height = 70;
    }

    draw(ctx, x, y, scale, stateName, actionState, actionTimer, clockTime) {
        ctx.save();
        ctx.translate(x, y);
        ctx.scale(scale, scale);

        // Core pose offsets
        let runCycle = clockTime * 14;
        let legSwing = Math.sin(runCycle);
        let armSwing = -Math.sin(runCycle);
        let headBob = Math.abs(Math.sin(runCycle * 2)) * 3;
        let torsoBob = Math.abs(Math.sin(runCycle * 2)) * 2;
        let tailSwing = Math.sin(runCycle * 0.8) * 0.4;
        
        let torsoRotation = 0;
        let torsoScaleY = 1.0;
        let torsoScaleX = 1.0;
        let headOffsetY = 0;
        let armAngleL = armSwing * 0.7;
        let armAngleR = -armSwing * 0.7;
        let legAngleL = legSwing * 0.8;
        let legAngleR = -legSwing * 0.8;

        // Customize poses based on action states
        if (actionState === 'jump') {
            // Tuck pose
            armAngleL = -Math.PI * 0.7;
            armAngleR = -Math.PI * 0.7;
            legAngleL = Math.PI * 0.3;
            legAngleR = Math.PI * 0.35;
            torsoRotation = -0.15;
            headBob = -2;
            tailSwing = 0.8;
        } else if (actionState === 'slide') {
            // Squash layout
            torsoScaleY = 0.55;
            torsoScaleX = 1.3;
            armAngleL = -Math.PI * 0.45;
            armAngleR = -Math.PI * 0.42;
            legAngleL = -Math.PI * 0.4;
            legAngleR = -Math.PI * 0.45;
            headOffsetY = 8;
            tailSwing = -0.5;
        }

        // Draw body layers from back to front:
        // 1. Back Tail, 2. Back Arm & Leg, 3. Torso, 4. Head & Ears, 5. Front Arm & Leg
        
        ctx.lineWidth = 8;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        // Apply skin styles
        const bodyColor = this.config.colorBody;
        const faceColor = this.config.colorFace;
        const isCyber = this.config.id === 'cyber';
        const isFire = this.config.id === 'fire';
        const isGold = this.config.id === 'gold';

        // Glowing shadow effects for skin trail matches
        if (isCyber) {
            ctx.shadowColor = '#00ffcc';
            ctx.shadowBlur = 8;
        } else if (isFire) {
            ctx.shadowColor = '#ff5500';
            ctx.shadowBlur = 10;
        } else if (isGold) {
            ctx.shadowColor = '#ffe57f';
            ctx.shadowBlur = 8;
        }

        // 1. BACK TAIL
        ctx.save();
        ctx.translate(-15, -torsoBob + 5);
        ctx.rotate(-Math.PI * 0.2 + tailSwing);
        ctx.strokeStyle = bodyColor;
        ctx.lineWidth = 5;
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.quadraticCurveTo(-15, -15, -12, -30);
        ctx.quadraticCurveTo(-8, -45, -25, -45);
        ctx.stroke();
        ctx.restore();

        // 2. BACK ARM & LEG (Background limb offsets for 3D overlay depth)
        ctx.strokeStyle = this.darkenColor(bodyColor, 25);
        ctx.fillStyle = faceColor;

        // Back Arm
        ctx.save();
        ctx.translate(-8, -25 - torsoBob);
        ctx.rotate(armAngleR);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 18);
        ctx.lineTo(6, 32);
        ctx.stroke();
        // Hand
        ctx.beginPath();
        ctx.arc(6, 32, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // Back Leg
        ctx.save();
        ctx.translate(-5, -6 - torsoBob);
        ctx.rotate(legAngleR);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 16);
        ctx.lineTo(8, 28);
        ctx.stroke();
        // Foot
        ctx.beginPath();
        ctx.arc(8, 28, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // 3. TORSO
        ctx.save();
        ctx.translate(0, -torsoBob);
        ctx.rotate(torsoRotation);
        ctx.scale(torsoScaleX, torsoScaleY);
        
        ctx.fillStyle = bodyColor;
        ctx.beginPath();
        ctx.roundRect(-16, -34, 30, 36, [14, 14, 8, 8]);
        ctx.fill();

        // Belly Plate (PEACH)
        ctx.fillStyle = faceColor;
        ctx.beginPath();
        ctx.roundRect(-10, -26, 18, 20, 6);
        ctx.fill();

        // Cyber circuitry line detail
        if (isCyber) {
            ctx.strokeStyle = '#00ffcc';
            ctx.lineWidth = 1.5;
            ctx.beginPath();
            ctx.moveTo(-5, -20);
            ctx.lineTo(5, -20);
            ctx.moveTo(0, -25);
            ctx.lineTo(0, -10);
            ctx.stroke();
        }
        ctx.restore();

        // 4. HEAD, EARS & FACE
        ctx.save();
        ctx.translate(0, -36 - headBob + headOffsetY);
        
        // Back Ear
        ctx.fillStyle = bodyColor;
        ctx.beginPath();
        ctx.arc(-16, -6, 9, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = faceColor;
        ctx.beginPath();
        ctx.arc(-16, -6, 5, 0, Math.PI * 2);
        ctx.fill();

        // Head Sphere
        ctx.fillStyle = bodyColor;
        ctx.beginPath();
        ctx.arc(0, -8, 16, 0, Math.PI * 2);
        ctx.fill();

        // Face plate / cheeks beige contour
        ctx.fillStyle = faceColor;
        ctx.beginPath();
        ctx.arc(-4, -6, 10, 0, Math.PI * 2);
        ctx.arc(6, -6, 10, 0, Math.PI * 2);
        ctx.ellipse(1, -2, 11, 7, 0, 0, Math.PI * 2);
        ctx.fill();

        // Eyes
        let eyeColor = '#1a1a1a';
        if (isCyber) eyeColor = '#00ffcc';
        if (isFire) eyeColor = '#ff1100';
        if (isGold) eyeColor = '#ffe57f';
        ctx.fillStyle = eyeColor;
        ctx.beginPath();
        ctx.arc(-3, -8, 2.5, 0, Math.PI * 2);
        ctx.arc(5, -8, 2.5, 0, Math.PI * 2);
        ctx.fill();

        // Nose Snout
        ctx.fillStyle = this.darkenColor(faceColor, 15);
        ctx.beginPath();
        ctx.ellipse(1, -2, 4, 3, 0, 0, Math.PI * 2);
        ctx.fill();

        // Front Ear
        ctx.fillStyle = bodyColor;
        ctx.beginPath();
        ctx.arc(16, -6, 9, 0, Math.PI * 2);
        ctx.fill();
        ctx.fillStyle = faceColor;
        ctx.beginPath();
        ctx.arc(16, -6, 5, 0, Math.PI * 2);
        ctx.fill();

        ctx.restore();

        // 5. FRONT ARM & LEG (Foreground colors match body color)
        ctx.strokeStyle = bodyColor;
        ctx.fillStyle = faceColor;

        // Front Arm
        ctx.save();
        ctx.translate(6, -25 - torsoBob);
        ctx.rotate(armAngleL);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 19);
        ctx.lineTo(8, 33);
        ctx.stroke();
        // Hand
        ctx.beginPath();
        ctx.arc(8, 33, 5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // Front Leg
        ctx.save();
        ctx.translate(5, -6 - torsoBob);
        ctx.rotate(legAngleL);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 17);
        ctx.lineTo(9, 29);
        ctx.stroke();
        // Foot
        ctx.beginPath();
        ctx.arc(9, 29, 5.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        ctx.restore();
    }

    darkenColor(hex, percent) {
        if (hex.startsWith('#')) hex = hex.substring(1);
        let num = parseInt(hex, 16);
        let amt = Math.round(2.55 * percent);
        let R = (num >> 16) - amt;
        let G = (num >> 8 & 0x00FF) - amt;
        let B = (num & 0x0000FF) - amt;
        return '#' + (0x1000000 + (R < 0 ? 0 : R) * 0x10000 + (G < 0 ? 0 : G) * 0x100 + (B < 0 ? 0 : B)).toString(16).slice(1);
    }
}

/**
 * 3. PROCEDURAL 2D HUMAN CHASER
 */
export class HumanChaser2D {
    constructor() {
        this.width = 65;
        this.height = 80;
    }

    draw(ctx, x, y, scale, speed, clockTime) {
        ctx.save();
        ctx.translate(x, y);
        ctx.scale(scale, scale);

        // Angry rage speeds
        const runCycle = clockTime * speed * 0.9;
        const swing = Math.sin(runCycle);
        const bob = Math.abs(Math.sin(runCycle * 2)) * 4;

        // Limb Angles
        const legAngleL = swing * 0.9;
        const legAngleR = -swing * 0.9;
        const armAngleL = -swing * 0.7 - 0.6; // fist held angry high
        const armAngleR = swing * 0.7 - 0.6;

        ctx.lineWidth = 10;
        ctx.lineCap = 'round';

        // Materials Colors
        const shirtColor = '#bf9c6e'; // Safari khaki
        const skinColor = '#dfb195'; // pink-beige
        const bluePantsColor = '#1f3442'; // navy blue
        const bootColor = '#2b1e15'; // leather brown
        const hatColor = '#523624';

        // Red glow warning shadow for anger
        ctx.shadowColor = '#ff0000';
        ctx.shadowBlur = 12;

        // 1. BACK ARM & LEG
        ctx.strokeStyle = '#18242f'; // darker blue for shadow depth
        ctx.save();
        ctx.translate(-6, -8 - bob);
        ctx.rotate(legAngleR);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 18);
        ctx.lineTo(6, 30);
        ctx.stroke();
        // Boot
        ctx.fillStyle = bootColor;
        ctx.beginPath();
        ctx.roundRect(4, 26, 12, 8, 3);
        ctx.fill();
        ctx.restore();

        // Back Arm
        ctx.strokeStyle = '#907248'; // dark khaki shadow
        ctx.save();
        ctx.translate(-12, -32 - bob);
        ctx.rotate(armAngleR);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 20);
        ctx.lineTo(8, 32);
        ctx.stroke();
        // fist
        ctx.fillStyle = skinColor;
        ctx.beginPath();
        ctx.arc(8, 32, 6, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        // 2. TORSO (Safari Coat with belt)
        ctx.save();
        ctx.translate(0, -bob);
        ctx.fillStyle = shirtColor;
        ctx.beginPath();
        ctx.roundRect(-18, -42, 34, 38, [8, 8, 4, 4]);
        ctx.fill();

        // Dark belt
        ctx.fillStyle = '#111';
        ctx.fillRect(-19, -15, 36, 6);
        ctx.fillStyle = '#ffcc00'; // belt buckle
        ctx.fillRect(-4, -16, 8, 8);
        ctx.restore();

        // 3. HEAD & SAFARI PITH HAT
        ctx.save();
        ctx.translate(0, -44 - bob);

        // Head sphere
        ctx.fillStyle = skinColor;
        ctx.beginPath();
        ctx.arc(0, -8, 14, 0, Math.PI * 2);
        ctx.fill();

        // Angry eyes
        ctx.fillStyle = '#ff0000';
        ctx.beginPath();
        ctx.arc(-4, -11, 2, 0, Math.PI * 2);
        ctx.arc(4, -11, 2, 0, Math.PI * 2);
        ctx.fill();

        // Slanted angry eyebrows
        ctx.strokeStyle = '#422a1d';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(-8, -15);
        ctx.lineTo(-2, -12);
        ctx.moveTo(8, -15);
        ctx.lineTo(2, -12);
        ctx.stroke();

        // Shouting mouth
        ctx.fillStyle = '#220500';
        ctx.beginPath();
        ctx.arc(0, -2, 5, 0, Math.PI, false);
        ctx.fill();

        // Safari Hat Brim
        ctx.fillStyle = hatColor;
        ctx.beginPath();
        ctx.ellipse(0, -17, 24, 5, 0.1, 0, Math.PI * 2);
        ctx.fill();

        // Safari Hat Crown
        ctx.beginPath();
        ctx.arc(0, -20, 13, Math.PI, 0, false);
        ctx.fill();

        ctx.restore();

        // 4. FRONT ARM & LEG
        // Front Leg
        ctx.strokeStyle = bluePantsColor;
        ctx.save();
        ctx.translate(6, -8 - bob);
        ctx.rotate(legAngleL);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 20);
        ctx.lineTo(8, 32);
        ctx.stroke();
        // Boot
        ctx.fillStyle = bootColor;
        ctx.beginPath();
        ctx.roundRect(6, 28, 13, 8, 3);
        ctx.fill();
        ctx.restore();

        // Front Arm
        ctx.strokeStyle = shirtColor;
        ctx.save();
        ctx.translate(12, -32 - bob);
        ctx.rotate(armAngleL);
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.lineTo(0, 22);
        ctx.lineTo(9, 34);
        ctx.stroke();
        // fist
        ctx.fillStyle = skinColor;
        ctx.beginPath();
        ctx.arc(9, 34, 6.5, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();

        ctx.restore();
    }
}

/**
 * 4. DYNAMIC 2D OBSTACLES DRAWING FUNCTIONS
 */

// A. FALLEN LOG (Blocks all lanes - hurdle jump)
export function drawLog(ctx, x, y, width, height) {
    ctx.save();
    ctx.translate(x, y);

    const logHeight = 24;

    // Log Trunk
    const grad = ctx.createLinearGradient(0, 0, 0, logHeight);
    grad.addColorStop(0, '#5a3825'); // brown top
    grad.addColorStop(0.5, '#412618'); // darker trunk
    grad.addColorStop(1, '#2c190f'); // shadow
    ctx.fillStyle = grad;
    
    ctx.beginPath();
    ctx.roundRect(-width / 2, 0, width, logHeight, 6);
    ctx.fill();

    // Wood rings details on ends
    ctx.fillStyle = '#dfb195';
    ctx.beginPath();
    ctx.ellipse(-width / 2, logHeight / 2, 4, logHeight / 2, 0, 0, Math.PI * 2);
    ctx.ellipse(width / 2, logHeight / 2, 4, logHeight / 2, 0, 0, Math.PI * 2);
    ctx.fill();

    // Green Moss growing on top
    ctx.fillStyle = '#2d8a4e';
    ctx.beginPath();
    ctx.ellipse(-width / 4, 1, 25, 5, 0, 0, Math.PI * 2);
    ctx.ellipse(width / 3, 2, 35, 6, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
}

// B. STONE ARCH (Blocks all lanes - slide portal)
export function drawArch(ctx, x, y, width, height) {
    ctx.save();
    ctx.translate(x, y);

    const colWidth = 24;
    const barHeight = 28;

    // Stone textures
    const stoneColor = '#737d77';
    const stoneShadow = '#4b524e';
    
    ctx.fillStyle = stoneShadow;
    // Left column shadow
    ctx.fillRect(-width / 2, -height, colWidth, height);
    // Right column shadow
    ctx.fillRect(width / 2 - colWidth, -height, colWidth, height);

    ctx.fillStyle = stoneColor;
    // Columns front
    ctx.fillRect(-width / 2 + 3, -height, colWidth - 6, height);
    ctx.fillRect(width / 2 - colWidth + 3, -height, colWidth - 6, height);

    // Stone details / bricks lines
    ctx.strokeStyle = '#2b302d';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.moveTo(-width / 2, -height * 0.4);
    ctx.lineTo(-width / 2 + colWidth, -height * 0.4);
    ctx.moveTo(width / 2 - colWidth, -height * 0.7);
    ctx.lineTo(width / 2, -height * 0.7);
    ctx.stroke();

    // Weathered massive lintel crossbar
    const crossGrad = ctx.createLinearGradient(0, -height - barHeight, 0, -height);
    crossGrad.addColorStop(0, '#86918a');
    crossGrad.addColorStop(1, '#535b56');
    ctx.fillStyle = crossGrad;
    ctx.beginPath();
    ctx.roundRect(-width / 2 - 8, -height - barHeight, width + 16, barHeight, 5);
    ctx.fill();

    // Moss layer on top crossbar
    ctx.fillStyle = '#3a5f0b';
    ctx.beginPath();
    ctx.ellipse(0, -height - barHeight + 1, width * 0.4, 4, 0, 0, Math.PI * 2);
    ctx.fill();

    // Hanging forest vines
    ctx.strokeStyle = '#2d8a4e';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    // Vines Left
    ctx.moveTo(-width * 0.2, -height);
    ctx.quadraticCurveTo(-width * 0.22, -height + 30, -width * 0.2 + 5, -height + 55);
    // Vines Right
    ctx.moveTo(width * 0.1, -height);
    ctx.quadraticCurveTo(width * 0.08, -height + 40, width * 0.12, -height + 65);
    ctx.stroke();

    ctx.restore();
}

// C. SPIKED BARRIER (Blocks specific lanes - hurdle jump/lane shift)
export function drawSpikes(ctx, x, y, width, height) {
    ctx.save();
    ctx.translate(x, y);

    // Cross wooden hurdles X
    ctx.strokeStyle = '#422a1d';
    ctx.lineWidth = 6;
    
    // Left support X
    ctx.beginPath();
    ctx.moveTo(-width / 2, 0);
    ctx.lineTo(-width / 2 + 18, -height * 0.95);
    ctx.moveTo(-width / 2 + 18, 0);
    ctx.lineTo(-width / 2, -height * 0.95);
    // Right support X
    ctx.moveTo(width / 2 - 18, 0);
    ctx.lineTo(width / 2, -height * 0.95);
    ctx.moveTo(width / 2, 0);
    ctx.lineTo(width / 2 - 18, -height * 0.95);
    ctx.stroke();

    // Main Crossbar
    ctx.fillStyle = '#5c3d2e';
    ctx.strokeStyle = '#2b1b13';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    ctx.roundRect(-width / 2 - 4, -height * 0.78, width + 8, 12, 3);
    ctx.fill();
    ctx.stroke();

    // Steel Spikes
    ctx.fillStyle = '#cbd5e1';
    ctx.strokeStyle = '#64748b';
    ctx.lineWidth = 1.5;

    const spikeCount = 5;
    const spacing = width / (spikeCount - 1);
    for (let i = 0; i < spikeCount; i++) {
        const spikeX = -width / 2 + i * spacing;
        const spikeY = -height * 0.78;

        ctx.beginPath();
        ctx.moveTo(spikeX - 6, spikeY);
        ctx.lineTo(spikeX, spikeY - 18); // pointed tip
        ctx.lineTo(spikeX + 6, spikeY);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    }

    ctx.restore();
}

// D. LAVA CRACK (Blocks specific lane - jump hurdle/lane shift)
export function drawLava(ctx, x, y, width, height, time) {
    ctx.save();
    ctx.translate(x, y);

    // Glowing magma shadow
    ctx.shadowColor = '#ff2200';
    ctx.shadowBlur = 20;

    // Dark volcanic ash rim
    ctx.fillStyle = '#1e1a17';
    ctx.beginPath();
    ctx.ellipse(0, height / 2, width / 2, height / 2, 0, 0, Math.PI * 2);
    ctx.fill();

    // Pulsing molten centers
    ctx.shadowBlur = 0; // reset
    const pulseFactor = 0.8 + Math.sin(time * 5) * 0.2;
    const lWidth = (width - 12) * pulseFactor;
    const lHeight = (height - 4) * pulseFactor;

    // Outer molten magma
    ctx.fillStyle = '#ff3300';
    ctx.beginPath();
    ctx.ellipse(0, height / 2, lWidth / 2, lHeight / 2, 0, 0, Math.PI * 2);
    ctx.fill();

    // Hot central cores
    ctx.fillStyle = '#ffea00';
    ctx.beginPath();
    ctx.ellipse(0, height / 2, lWidth * 0.3, lHeight * 0.3, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.restore();
}

/**
 * 5. COLLECTIBLES & POWERUPS DRAWING HELPERS
 */

// A. SPINNING GOLDEN BANANA
export function drawBanana(ctx, x, y, sizeMultiplier = 1.0) {
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(sizeMultiplier, sizeMultiplier);

    // Soft neon gold shadow glow
    ctx.shadowColor = '#ffd54f';
    ctx.shadowBlur = 12;

    // Curved crescent paths
    const grad = ctx.createLinearGradient(-10, -10, 10, 10);
    grad.addColorStop(0, '#ffee55');
    grad.addColorStop(0.5, '#ffcc00');
    grad.addColorStop(1, '#e5a93b');
    ctx.fillStyle = grad;

    ctx.beginPath();
    // Inner curve
    ctx.moveTo(-11, -11);
    ctx.quadraticCurveTo(15, 0, -11, 11);
    // Outer curve
    ctx.quadraticCurveTo(9, 0, -11, -11);
    ctx.closePath();
    ctx.fill();

    // Brown stem tips
    ctx.shadowBlur = 0; // reset
    ctx.fillStyle = '#5c3a21';
    ctx.fillRect(-12, -12, 3, 2.5);
    ctx.fillRect(-12, 10, 3, 2.5);

    ctx.restore();
}

// B. SPINNING GLOWING POWERUPS BUBBLES
export function drawPowerup(ctx, x, y, type, time) {
    ctx.save();
    ctx.translate(x, y);

    let color = '#00ffcc'; // Magnet Cyan
    let symbol = '🧲';
    if (type === 'shield') {
        color = '#ff00ff'; // Shield Magenta
        symbol = '🛡️';
    } else if (type === 'boost') {
        color = '#ff7700'; // Boost Orange
        symbol = '⚡';
    }

    // Glowing boundary aura
    ctx.shadowColor = color;
    ctx.shadowBlur = 16 + Math.sin(time * 6) * 6;

    // Orb background circular bubble
    ctx.fillStyle = 'rgba(10, 10, 10, 0.4)';
    ctx.strokeStyle = color;
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.arc(0, 0, 18, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    // Center symbol text icon
    ctx.shadowBlur = 0;
    ctx.font = '16px serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(symbol, 0, 0);

    ctx.restore();
}
