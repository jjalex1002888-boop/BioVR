/**
 * Dart Monkey Sound Synthesizer
 * Uses native Web Audio API for completely self-contained, lightweight retro sound synthesis.
 */
class MonkeyAudioEngine {
    constructor() {
        this.ctx = null;
        this.fxEnabled = true;
        this.bgmEnabled = false;
        this.bgmNode = null;
        this.bgmGain = null;
        this.bgmLfo = null;
    }

    // Initialize AudioContext on first user interaction to comply with browser safety standards
    init() {
        if (this.ctx) return;
        
        try {
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            this.ctx = new AudioCtx();
        } catch (e) {
            console.warn("Web Audio API not supported in this browser:", e);
        }
    }

    resumeContext() {
        this.init();
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    toggleFX(enabled) {
        this.fxEnabled = enabled;
    }

    toggleBGM(enabled) {
        this.bgmEnabled = enabled;
        if (enabled) {
            this.startBGM();
        } else {
            this.stopBGM();
        }
    }

    // Dynamic "Pop!" Sound Effect
    playPop() {
        if (!this.fxEnabled) return;
        this.resumeContext();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        
        // 1. Pop Transient (High-frequency sweep)
        const osc1 = this.ctx.createOscillator();
        const gain1 = this.ctx.createGain();
        
        osc1.type = 'sine';
        osc1.frequency.setValueAtTime(150, now);
        osc1.frequency.exponentialRampToValueAtTime(1100, now + 0.05);
        osc1.frequency.linearRampToValueAtTime(120, now + 0.12);
        
        gain1.gain.setValueAtTime(0.001, now);
        gain1.gain.linearRampToValueAtTime(0.3, now + 0.01);
        gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.15);
        
        osc1.connect(gain1);
        gain1.connect(this.ctx.destination);
        
        osc1.start(now);
        osc1.stop(now + 0.16);

        // 2. Extra High Plop Sparkle for satisfying sound texture
        const osc2 = this.ctx.createOscillator();
        const gain2 = this.ctx.createGain();
        
        osc2.type = 'triangle';
        osc2.frequency.setValueAtTime(600, now);
        osc2.frequency.exponentialRampToValueAtTime(1500, now + 0.02);
        
        gain2.gain.setValueAtTime(0.12, now);
        gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.06);
        
        osc2.connect(gain2);
        gain2.connect(this.ctx.destination);
        
        osc2.start(now);
        osc2.stop(now + 0.07);
    }

    // Dart Throw "Whoosh" Sound Effect
    playThrow() {
        if (!this.fxEnabled) return;
        this.resumeContext();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        const duration = 0.18;
        
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(320, now);
        osc.frequency.exponentialRampToValueAtTime(80, now + duration);
        
        gain.gain.setValueAtTime(0.001, now);
        gain.gain.linearRampToValueAtTime(0.2, now + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.001, now + duration);
        
        // Lowpass filter to emulate drag/air-resistance
        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(1800, now);
        filter.frequency.exponentialRampToValueAtTime(400, now + duration);
        
        osc.connect(filter);
        filter.connect(gain);
        gain.connect(this.ctx.destination);
        
        osc.start(now);
        osc.stop(now + duration + 0.02);
    }

    // Upgrade Fanfare Chord Progressions
    playUpgrade() {
        if (!this.fxEnabled) return;
        this.resumeContext();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        
        // Play an energetic major chord arpeggio (C - E - G - C)
        const notes = [261.63, 329.63, 392.00, 523.25]; // C4, E4, G4, C5
        
        notes.forEach((freq, idx) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            const filter = this.ctx.createBiquadFilter();
            
            const noteStart = now + idx * 0.07;
            const noteDuration = 0.35;
            
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(freq, noteStart);
            
            // Subtle vibrato/detune to make it feel rich
            osc.frequency.linearRampToValueAtTime(freq + 4, noteStart + noteDuration * 0.5);
            osc.frequency.linearRampToValueAtTime(freq - 4, noteStart + noteDuration);

            filter.type = 'lowpass';
            filter.frequency.setValueAtTime(1000, noteStart);
            filter.frequency.exponentialRampToValueAtTime(2500, noteStart + 0.1);
            
            gain.gain.setValueAtTime(0.001, noteStart);
            gain.gain.linearRampToValueAtTime(0.12, noteStart + 0.04);
            gain.gain.exponentialRampToValueAtTime(0.001, noteStart + noteDuration);
            
            osc.connect(filter);
            filter.connect(gain);
            gain.connect(this.ctx.destination);
            
            osc.start(noteStart);
            osc.stop(noteStart + noteDuration + 0.05);
        });
    }

    // Ambient Synth Background Loop
    startBGM() {
        this.resumeContext();
        if (!this.ctx) return;

        this.stopBGM(); // Safety check: prevent double triggers
        
        const now = this.ctx.currentTime;
        
        // Setup background node
        this.bgmGain = this.ctx.createGain();
        this.bgmGain.gain.setValueAtTime(0.001, now);
        this.bgmGain.gain.linearRampToValueAtTime(0.06, now + 1.5); // Smooth fade in
        this.bgmGain.connect(this.ctx.destination);

        // Core oscillator 1 (Root note: C3 = 130.81Hz)
        const osc1 = this.ctx.createOscillator();
        osc1.type = 'sine';
        osc1.frequency.setValueAtTime(130.81, now);
        
        // Core oscillator 2 (G3 = 196.00Hz, Perfect Fifth detuned)
        const osc2 = this.ctx.createOscillator();
        osc2.type = 'triangle';
        osc2.frequency.setValueAtTime(196.05, now);

        // Gentle Filter
        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(450, now);
        
        // LFO modulating filter cutoff to create dynamic ambient "breathing"
        this.bgmLfo = this.ctx.createOscillator();
        const lfoGain = this.ctx.createGain();
        this.bgmLfo.frequency.setValueAtTime(0.15, now); // Super slow 0.15Hz
        lfoGain.gain.setValueAtTime(200, now); // Sweep filter between 250Hz and 650Hz

        this.bgmLfo.connect(lfoGain);
        lfoGain.connect(filter.frequency);
        
        osc1.connect(filter);
        osc2.connect(filter);
        filter.connect(this.bgmGain);

        osc1.start(now);
        osc2.start(now);
        this.bgmLfo.start(now);

        // Keep references to terminate them later
        this.bgmNode = [osc1, osc2];
    }

    stopBGM() {
        if (!this.ctx) return;
        const now = this.ctx.currentTime;
        
        // Fade out BGM smoothly
        if (this.bgmGain) {
            try {
                this.bgmGain.gain.cancelScheduledValues(now);
                this.bgmGain.gain.setValueAtTime(this.bgmGain.gain.value, now);
                this.bgmGain.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
            } catch(e) {}
        }
        
        setTimeout(() => {
            if (this.bgmNode) {
                this.bgmNode.forEach(node => {
                    try { node.stop(); } catch(e) {}
                });
                this.bgmNode = null;
            }
            if (this.bgmLfo) {
                try { this.bgmLfo.stop(); } catch(e) {}
                this.bgmLfo = null;
            }
        }, 650);
    }
}

// Global Singleton
window.MonkeyAudio = new MonkeyAudioEngine();
