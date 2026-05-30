/**
 * Banana Run 3D - Web Audio Synthesizer Engine
 */

class AudioEngine {
    constructor() {
        this.ctx = null;
        this.musicInterval = null;
        this.tempo = 120; // Beats per minute
        this.beatDuration = 60 / this.tempo;
        this.step = 0; // sequencer step (0 to 15)
        this.muted = false;
        
        // Sequencer variables
        this.isPlayingMusic = false;
        
        // White noise node helper
        this.noiseBuffer = null;
    }

    init() {
        if (this.ctx) return;
        
        // Initialize Web Audio Context
        const AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextClass) {
            console.warn('Web Audio API not supported in this browser.');
            return;
        }

        this.ctx = new AudioContextClass();
        this.createNoiseBuffer();
        
        // Resume context on interaction if suspended (browser security)
        if (this.ctx.state === 'suspended') {
            const resume = () => {
                this.ctx.resume();
                window.removeEventListener('keydown', resume);
                window.removeEventListener('mousedown', resume);
                window.removeEventListener('touchstart', resume);
            };
            window.addEventListener('keydown', resume);
            window.addEventListener('mousedown', resume);
            window.addEventListener('touchstart', resume);
        }
    }

    createNoiseBuffer() {
        if (!this.ctx) return;
        const bufferSize = this.ctx.sampleRate * 2; // 2 seconds of noise
        const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            data[i] = Math.random() * 2 - 1;
        }
        this.noiseBuffer = buffer;
    }

    toggleMute() {
        this.muted = !this.muted;
        if (this.muted) {
            this.stopMusic();
        } else {
            this.startMusic();
        }
        return this.muted;
    }

    // Dynamic scale helper
    playTone(freq, type, duration, volume, slideTo = null, delay = 0) {
        if (this.muted || !this.ctx) return;
        this.init(); // safety check
        if (this.ctx.state === 'suspended') return;

        const osc = this.ctx.createOscillator();
        const gainNode = this.ctx.createGain();

        osc.type = type;
        osc.frequency.setValueAtTime(freq, this.ctx.currentTime + delay);
        
        if (slideTo) {
            osc.frequency.exponentialRampToValueAtTime(slideTo, this.ctx.currentTime + delay + duration);
        }

        gainNode.gain.setValueAtTime(volume, this.ctx.currentTime + delay);
        gainNode.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + delay + duration);

        osc.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        osc.start(this.ctx.currentTime + delay);
        osc.stop(this.ctx.currentTime + delay + duration);
    }

    playNoise(duration, volume, lowpassFreq = 1000, slideFilterTo = null) {
        if (this.muted || !this.ctx || !this.noiseBuffer) return;
        
        const noiseNode = this.ctx.createBufferSource();
        noiseNode.buffer = this.noiseBuffer;

        const filter = this.ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(lowpassFreq, this.ctx.currentTime);
        
        if (slideFilterTo) {
            filter.frequency.exponentialRampToValueAtTime(slideFilterTo, this.ctx.currentTime + duration);
        }

        const gainNode = this.ctx.createGain();
        gainNode.gain.setValueAtTime(volume, this.ctx.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.001, this.ctx.currentTime + duration);

        noiseNode.connect(filter);
        filter.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        noiseNode.start();
        noiseNode.stop(this.ctx.currentTime + duration);
    }

    // Play action sounds
    playCollect() {
        // Double sweet glowing sound
        this.playTone(523.25, 'sine', 0.1, 0.25, 1046.5, 0); // C5 to C6
        this.playTone(783.99, 'sine', 0.15, 0.15, 1568.0, 0.05); // G5 to G6
    }

    playJump() {
        // Fast upward pitch sweep
        this.playTone(200, 'triangle', 0.25, 0.35, 800);
    }

    playSlide() {
        // Low scratchy brush sweep
        this.playNoise(0.3, 0.25, 400, 100);
    }

    playMagnet() {
        // Magical spacey upward chime
        for (let i = 0; i < 4; i++) {
            this.playTone(300 + i * 150, 'sine', 0.3, 0.15, 1200 + i * 100, i * 0.08);
        }
    }

    playShield() {
        // High crystalline resonance chime
        this.playTone(880, 'sine', 0.4, 0.25, 1760);
        this.playTone(1320, 'sine', 0.4, 0.15, 2640, 0.05);
    }

    playShieldBreak() {
        // Shuttering crash
        this.playNoise(0.4, 0.4, 3000, 200);
        this.playTone(987.77, 'triangle', 0.3, 0.3, 220); // descending drop
    }

    playBoost() {
        // Jet-engine sound
        this.playNoise(0.5, 0.3, 100, 2000);
        this.playTone(150, 'sawtooth', 0.8, 0.3, 600);
    }

    playStumble() {
        // Double pulse warning "thump-thump"
        this.playTone(80, 'sine', 0.15, 0.5, 40);
        this.playTone(80, 'sine', 0.15, 0.5, 40, 0.2);
    }

    playCrash() {
        // Heavy explosion crash
        this.playNoise(0.8, 0.5, 1500, 50);
        this.playTone(220, 'sawtooth', 0.6, 0.4, 40);
    }

    // Dynamic Sequenced Background Soundtrack
    startMusic() {
        this.init();
        if (this.muted || this.isPlayingMusic) return;
        this.isPlayingMusic = true;
        this.step = 0;

        const scheduleNextBeats = () => {
            if (!this.isPlayingMusic || this.muted) return;

            // Rhythmic tempo scales with running speed!
            // Calculate BPM based on current game running speed (represented elsewhere or passed in)
            const speedFactor = window.gameSpeedFactor || 1.0;
            this.tempo = Math.min(180, 115 + (speedFactor - 1) * 35);
            this.beatDuration = 60 / this.tempo;
            const stepDuration = this.beatDuration / 4; // 16th notes

            const now = this.ctx.currentTime;
            this.playSequencerStep(this.step, now);

            this.step = (this.step + 1) % 16;
            this.musicTimeout = setTimeout(scheduleNextBeats, stepDuration * 1000);
        };

        scheduleNextBeats();
    }

    stopMusic() {
        this.isPlayingMusic = false;
        if (this.musicTimeout) {
            clearTimeout(this.musicTimeout);
            this.musicTimeout = null;
        }
    }

    playSequencerStep(step, time) {
        if (!this.ctx || this.muted) return;
        
        // KICK DRUM (Steps 0, 4, 8, 12 - standard four on the floor)
        if (step === 0 || step === 4 || step === 8 || step === 12) {
            this.playSynthKick(time);
        }

        // SNARE (Steps 4, 12 - offbeat pop)
        if (step === 4 || step === 12) {
            this.playSynthSnare(time);
        }

        // HI-HAT (Quarter note offsets, or rolling rhythms)
        if (step % 2 === 1) {
            this.playSynthHihat(time);
        }

        // AMBIENT JUNGLE SYNTH BASSLINE (Minor progression in A minor / D minor)
        // Step notes mapping
        const notes = [
            110.00, 110.00, 130.81, 110.00, // A2, A2, C3, A2
            146.83, 146.83, 164.81, 146.83, // D3, D3, E3, D3
            110.00, 110.00, 130.81, 164.81, // A2, A2, C3, E3
            196.00, 196.00, 220.00, 164.81  // G3, G3, A3, E3
        ];
        
        if (step % 4 === 0 || step % 4 === 2) {
            const freq = notes[step];
            this.playSynthBass(freq, time);
        }
    }

    playSynthKick(time) {
        const osc = this.ctx.createOscillator();
        const gainNode = this.ctx.createGain();
        osc.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        osc.frequency.setValueAtTime(120, time);
        osc.frequency.exponentialRampToValueAtTime(0.01, time + 0.15);

        gainNode.gain.setValueAtTime(0.4, time);
        gainNode.gain.exponentialRampToValueAtTime(0.001, time + 0.18);

        osc.start(time);
        osc.stop(time + 0.2);
    }

    playSynthSnare(time) {
        if (!this.noiseBuffer) return;
        const noiseNode = this.ctx.createBufferSource();
        noiseNode.buffer = this.noiseBuffer;

        const filter = this.ctx.createBiquadFilter();
        filter.type = 'highpass';
        filter.frequency.setValueAtTime(800, time);

        const gainNode = this.ctx.createGain();
        noiseNode.connect(filter);
        filter.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        gainNode.gain.setValueAtTime(0.12, time);
        gainNode.gain.exponentialRampToValueAtTime(0.001, time + 0.12);

        // Add a mid-frequency tone crack to make the snare punchy
        const toneOsc = this.ctx.createOscillator();
        const toneGain = this.ctx.createGain();
        toneOsc.type = 'triangle';
        toneOsc.frequency.setValueAtTime(180, time);
        toneOsc.frequency.exponentialRampToValueAtTime(90, time + 0.08);
        toneGain.gain.setValueAtTime(0.15, time);
        toneGain.gain.exponentialRampToValueAtTime(0.001, time + 0.08);
        
        toneOsc.connect(toneGain);
        toneGain.connect(this.ctx.destination);

        noiseNode.start(time);
        noiseNode.stop(time + 0.15);
        toneOsc.start(time);
        toneOsc.stop(time + 0.1);
    }

    playSynthHihat(time) {
        if (!this.noiseBuffer) return;
        const noiseNode = this.ctx.createBufferSource();
        noiseNode.buffer = this.noiseBuffer;

        const filter = this.ctx.createBiquadFilter();
        filter.type = 'bandpass';
        filter.frequency.setValueAtTime(8000, time);

        const gainNode = this.ctx.createGain();
        noiseNode.connect(filter);
        filter.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        gainNode.gain.setValueAtTime(0.04, time);
        gainNode.gain.exponentialRampToValueAtTime(0.001, time + 0.04);

        noiseNode.start(time);
        noiseNode.stop(time + 0.05);
    }

    playSynthBass(freq, time) {
        const osc = this.ctx.createOscillator();
        const subOsc = this.ctx.createOscillator();
        const gainNode = this.ctx.createGain();
        
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(freq, time);

        subOsc.type = 'sine';
        subOsc.frequency.setValueAtTime(freq / 2, time); // Sub harmonic

        gainNode.gain.setValueAtTime(0.12, time);
        gainNode.gain.exponentialRampToValueAtTime(0.001, time + 0.2);

        osc.connect(gainNode);
        subOsc.connect(gainNode);
        gainNode.connect(this.ctx.destination);

        osc.start(time);
        subOsc.start(time);
        osc.stop(time + 0.22);
        subOsc.stop(time + 0.22);
    }
}

export const audio = new AudioEngine();
window.gameAudio = audio; // expose for simple global triggers
