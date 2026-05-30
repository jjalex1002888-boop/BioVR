/**
 * Banana Run 3D - Game State Management
 */

class GameState {
    constructor() {
        this.reset();
        this.loadLocalStorage();
    }

    reset() {
        this.score = 0;
        this.bananasCollectedThisRun = 0;
        this.distance = 0;
        this.speed = 15; // base running speed
        this.maxSpeed = 45; // top terminal speed
        this.speedIncrement = 0.05; // speed multiplier growth per second
        this.isPlaying = false;
        this.isPaused = false;
        this.isGameOver = false;
        this.stumbled = false; // chaser closeness state
        this.stumbleTime = 0;
        
        // Power-ups
        this.activePowerups = {
            magnet: 0, // remaining time in ms
            shield: false, // is shield currently active
            boost: 0   // remaining time in ms
        };
        
        this.powerupDurations = {
            magnet: 10000, // 10 seconds
            boost: 6000    // 6 seconds
        };
    }

    loadLocalStorage() {
        try {
            this.totalBananas = parseInt(localStorage.getItem('br3d_total_bananas')) || 0;
            this.highScore = parseInt(localStorage.getItem('br3d_highscore')) || 0;
            this.activeSkin = localStorage.getItem('br3d_active_skin') || 'default';
            this.graphicsQuality = localStorage.getItem('br3d_graphics_quality') || 'high';
            
            // Skins purchase records
            const purchased = localStorage.getItem('br3d_purchased_skins');
            this.purchasedSkins = purchased ? JSON.parse(purchased) : ['default'];
        } catch (e) {
            console.warn('LocalStorage not available, running in-memory only.', e);
            this.totalBananas = 0;
            this.highScore = 0;
            this.activeSkin = 'default';
            this.graphicsQuality = 'high';
            this.purchasedSkins = ['default'];
        }

        // Available Skins Catalog
        this.skinsCatalog = {
            default: {
                id: 'default',
                name: 'Jungle Chimpy',
                price: 0,
                colorBody: '#5a3825', // brown
                colorFace: '#dfb195', // beige
                description: 'Classic agile brown chimpanzee. Ready for action!',
                trailEffect: null
            },
            cyber: {
                id: 'cyber',
                name: 'Cyber Chimp v2',
                price: 150,
                colorBody: '#001122', // dark metallic blue
                colorFace: '#00ffcc', // glowing teal
                description: 'A digitized primate equipped with chrome joints and neon circuitry.',
                trailEffect: 'cyan'
            },
            fire: {
                id: 'fire',
                name: 'Flame Gorilla',
                price: 300,
                colorBody: '#220500', // charcoaled wood
                colorFace: '#ff3300', // lava orange
                description: 'Born from volcanic ash. Leaves a trail of glowing red embers.',
                trailEffect: 'orange'
            },
            gold: {
                id: 'gold',
                name: 'Midas Kong',
                price: 500,
                colorBody: '#d4af37', // gold metallic
                colorFace: '#fff3a8', // glowing yellow gold
                description: 'Touched by the legendary King Midas. Pure gold from snout to tail.',
                trailEffect: 'gold'
            }
        };
    }

    saveLocalStorage() {
        try {
            localStorage.setItem('br3d_total_bananas', this.totalBananas);
            localStorage.setItem('br3d_highscore', this.highScore);
            localStorage.setItem('br3d_active_skin', this.activeSkin);
            localStorage.setItem('br3d_graphics_quality', this.graphicsQuality);
            localStorage.setItem('br3d_purchased_skins', JSON.stringify(this.purchasedSkins));
        } catch (e) {
            console.error('Failed to save to LocalStorage', e);
        }
    }

    addBananas(count) {
        this.bananasCollectedThisRun += count;
        this.totalBananas += count;
    }

    updateDistance(dt) {
        if (!this.isPlaying || this.isPaused || this.isGameOver) return;
        
        let multiplier = 1;
        if (this.activePowerups.boost > 0) {
            multiplier = 2.5; // sprinting moves faster
        }

        const distanceMoved = this.speed * multiplier * dt;
        this.distance += distanceMoved;
        
        // Score = bananas * 50 + distance
        this.score = Math.floor(this.bananasCollectedThisRun * 50 + this.distance);

        // Dynamic speed progression
        if (this.speed < this.maxSpeed && this.activePowerups.boost <= 0) {
            this.speed += this.speedIncrement * dt;
        }

        // Highscore tracking
        if (this.score > this.highScore) {
            this.highScore = this.score;
        }
    }

    triggerStumble() {
        if (this.activePowerups.boost > 0) return false; // immune
        if (this.activePowerups.shield) {
            this.activePowerups.shield = false;
            // Shield saved us!
            return 'shield_break';
        }
        
        if (this.stumbled) {
            // Already stumbled! The human catches us!
            this.isGameOver = true;
            this.isPlaying = false;
            this.saveLocalStorage();
            return 'caught';
        } else {
            // First stumble
            this.stumbled = true;
            this.stumbleTime = 5000; // 5 seconds to recover
            return 'stumbled';
        }
    }

    updatePowerups(dtMs) {
        // Magnet countdown
        if (this.activePowerups.magnet > 0) {
            this.activePowerups.magnet = Math.max(0, this.activePowerups.magnet - dtMs);
        }

        // Boost countdown
        if (this.activePowerups.boost > 0) {
            this.activePowerups.boost = Math.max(0, this.activePowerups.boost - dtMs);
        }

        // Stumble recovery countdown
        if (this.stumbled) {
            this.stumbleTime = Math.max(0, this.stumbleTime - dtMs);
            if (this.stumbleTime <= 0) {
                this.stumbled = false; // Recovered! Chaser backs off
            }
        }
    }

    activatePowerup(type) {
        if (type === 'magnet') {
            this.activePowerups.magnet = this.powerupDurations.magnet;
        } else if (type === 'shield') {
            this.activePowerups.shield = true;
        } else if (type === 'boost') {
            this.activePowerups.boost = this.powerupDurations.boost;
            // clear stumble when boosting
            this.stumbled = false;
            this.stumbleTime = 0;
        }
    }

    purchaseSkin(skinId) {
        const skin = this.skinsCatalog[skinId];
        if (!skin) return { success: false, reason: 'Skin not found' };
        
        if (this.purchasedSkins.includes(skinId)) {
            return { success: false, reason: 'Already purchased' };
        }

        if (this.totalBananas >= skin.price) {
            this.totalBananas -= skin.price;
            this.purchasedSkins.push(skinId);
            this.activeSkin = skinId;
            this.saveLocalStorage();
            return { success: true };
        } else {
            return { success: false, reason: `Need ${skin.price - this.totalBananas} more bananas!` };
        }
    }

    selectSkin(skinId) {
        if (this.purchasedSkins.includes(skinId)) {
            this.activeSkin = skinId;
            this.saveLocalStorage();
            return true;
        }
        return false;
    }
}

export const state = new GameState();
