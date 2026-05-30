/**
 * ==========================================
 * GEAR CHRONICLES - PROCEDURAL AUDIO SYNTH
 * ==========================================
 * Pure Web Audio API procedural sound synthesizer.
 * No external sound assets are needed. Highly performant.
 */

class AudioSynth {
    constructor() {
        this.ctx = null;
        this.masterGain = null;
        this.isMuted = false;
        
        // Dynamic White Noise Buffer cached for procedural steam/explosion effects
        this.noiseBuffer = null;
        
        // Background melody sequencer state
        this.sequencerInterval = null;
        this.seqStep = 0;
        this.melodyChords = [
            [220, 261, 329], // Am
            [174, 220, 261], // F
            [196, 246, 293], // G
            [165, 220, 261]  // Em/A
        ];
    }
    
    init() {
        if (this.ctx) return;
        
        try {
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            this.ctx = new AudioContextClass();
            this.masterGain = this.ctx.createGain();
            this.masterGain.gain.setValueAtTime(0.3, this.ctx.currentTime); // Standard 30% volume master
            this.masterGain.connect(this.ctx.destination);
            
            // Build noise buffer
            this.noiseBuffer = this.createNoiseBuffer();
            
            // Start background pad
            this.startBackgroundPad();
            
            console.log("AudioSynth initialized successfully.");
        } catch (e) {
            console.error("Web Audio API is not supported in this browser:", e);
        }
    }
    
    createNoiseBuffer() {
        if (!this.ctx) return null;
        let bufferSize = this.ctx.sampleRate * 2; // 2 seconds of noise
        let buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
        let data = buffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            data[i] = Math.random() * 2 - 1;
        }
        return buffer;
    }
    
    toggleMute() {
        this.isMuted = !this.isMuted;
        if (this.masterGain && this.ctx) {
            this.masterGain.gain.setValueAtTime(this.isMuted ? 0 : 0.3, this.ctx.currentTime);
        }
        return this.isMuted;
    }
    
    // ----------------------------------------
    // STEAM HISS (Gear 2 Jet)
    // ----------------------------------------
    playSteamHiss(duration = 1.2) {
        if (this.isMuted || !this.ctx) return;
        this.init(); // Fallback auto-init
        
        let ctx = this.ctx;
        let now = ctx.currentTime;
        
        // Noise source
        let noiseSource = ctx.createBufferSource();
        noiseSource.buffer = this.noiseBuffer;
        
        // Filter
        let filter = ctx.createBiquadFilter();
        filter.type = 'bandpass';
        filter.Q.setValueAtTime(6, now);
        
        // Sweep filter from 1000Hz up to 6000Hz
        filter.frequency.setValueAtTime(800, now);
        filter.frequency.exponentialRampToValueAtTime(7000, now + duration * 0.4);
        filter.frequency.exponentialRampToValueAtTime(1200, now + duration);
        
        // Volume Envelope
        let gain = ctx.createGain();
        gain.gain.setValueAtTime(0, now);
        gain.gain.linearRampToValueAtTime(0.8, now + 0.1);
        gain.gain.exponentialRampToValueAtTime(0.01, now + duration);
        
        // Connections
        noiseSource.connect(filter);
        filter.connect(gain);
        gain.connect(this.masterGain);
        
        noiseSource.start(now);
        noiseSource.stop(now + duration);
    }
    
    // ----------------------------------------
    // WHOOSH / CHARGE EFFECT (Gear 4 Haki / Red Hawk winding)
    // ----------------------------------------
    playWhoosh(duration = 0.8, frequencyOffset = 1.0) {
        if (this.isMuted || !this.ctx) return;
        this.init();
        
        let ctx = this.ctx;
        let now = ctx.currentTime;
        
        let osc = ctx.createOscillator();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(80 * frequencyOffset, now);
        osc.frequency.exponentialRampToValueAtTime(600 * frequencyOffset, now + duration);
        
        let filter = ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.Q.setValueAtTime(8, now);
        filter.frequency.setValueAtTime(200, now);
        filter.frequency.exponentialRampToValueAtTime(1500, now + duration);
        
        let gain = ctx.createGain();
        gain.gain.setValueAtTime(0, now);
        gain.gain.linearRampToValueAtTime(0.5, now + 0.1);
        gain.gain.exponentialRampToValueAtTime(0.01, now + duration);
        
        osc.connect(filter);
        filter.connect(gain);
        gain.connect(this.masterGain);
        
        osc.start(now);
        osc.stop(now + duration);
    }
    
    // ----------------------------------------
    // EXPLOSION (Gum-Gum Red Hawk landing blast)
    // ----------------------------------------
    playExplosion() {
        if (this.isMuted || !this.ctx) return;
        this.init();
        
        let ctx = this.ctx;
        let now = ctx.currentTime;
        let duration = 1.6;
        
        // Noise Explosion (High Frequency crackle and fire roar)
        let noiseSource = ctx.createBufferSource();
        noiseSource.buffer = this.noiseBuffer;
        
        let noiseFilter = ctx.createBiquadFilter();
        noiseFilter.type = 'lowpass';
        noiseFilter.frequency.setValueAtTime(900, now);
        noiseFilter.frequency.exponentialRampToValueAtTime(100, now + duration);
        
        let noiseGain = ctx.createGain();
        noiseGain.gain.setValueAtTime(1.0, now);
        noiseGain.gain.exponentialRampToValueAtTime(0.005, now + duration);
        
        noiseSource.connect(noiseFilter);
        noiseFilter.connect(noiseGain);
        noiseGain.connect(this.masterGain);
        
        // Sine Bass Sub Boom (Physical thud)
        let bassOsc = ctx.createOscillator();
        bassOsc.type = 'sine';
        bassOsc.frequency.setValueAtTime(140, now);
        bassOsc.frequency.linearRampToValueAtTime(10, now + 0.6);
        
        let bassGain = ctx.createGain();
        bassGain.gain.setValueAtTime(1.5, now);
        bassGain.gain.exponentialRampToValueAtTime(0.005, now + 0.8);
        
        bassOsc.connect(bassGain);
        bassGain.connect(this.masterGain);
        
        // Trigger plays
        noiseSource.start(now);
        noiseSource.stop(now + duration);
        bassOsc.start(now);
        bassOsc.stop(now + duration);
    }
    
    // ----------------------------------------
    // DRUMS OF LIBERATION (Gear 5 Joyboy Heartbeat)
    // ----------------------------------------
    playDrumsOfLiberation() {
        if (this.isMuted || !this.ctx) return;
        this.init();
        
        let ctx = this.ctx;
        let now = ctx.currentTime;
        
        // Play the triple-beat rhythm: DUM-DUM-TUMP!
        this.playSingleDrum(now, 1.0);       // Beat 1 (Dum)
        this.playSingleDrum(now + 0.22, 0.9); // Beat 2 (Dum)
        this.playSingleDrum(now + 0.44, 1.4); // Beat 3 (Tump - accent!)
    }
    
    playSingleDrum(time, velocity = 1.0) {
        if (!this.ctx) return;
        let ctx = this.ctx;
        
        let osc = ctx.createOscillator();
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(90, time);
        osc.frequency.exponentialRampToValueAtTime(30, time + 0.15);
        
        let filter = ctx.createBiquadFilter();
        filter.type = 'lowpass';
        filter.frequency.setValueAtTime(120, time);
        
        let gain = ctx.createGain();
        gain.gain.setValueAtTime(0, time);
        gain.gain.linearRampToValueAtTime(velocity * 0.8, time + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.005, time + 0.18);
        
        osc.connect(filter);
        filter.connect(gain);
        gain.connect(this.masterGain);
        
        osc.start(time);
        osc.stop(time + 0.2);
    }
    
    // ----------------------------------------
    // AMBIENT Battle soundtrack pads
    // ----------------------------------------
    startBackgroundPad() {
        if (this.sequencerInterval) return;
        
        // Loop ambient notes every 3 seconds to establish the A-minor epic space
        this.sequencerInterval = setInterval(() => {
            if (this.isMuted || !this.ctx) return;
            
            let chord = this.melodyChords[this.seqStep];
            let now = this.ctx.currentTime;
            
            // Arpeggiate chord notes slightly in background
            chord.forEach((freq, i) => {
                let osc = this.ctx.createOscillator();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(freq, now + i * 0.15);
                
                let gain = this.ctx.createGain();
                gain.gain.setValueAtTime(0, now);
                gain.gain.linearRampToValueAtTime(0.04, now + 0.5); // Very soft background
                gain.gain.exponentialRampToValueAtTime(0.001, now + 2.8);
                
                osc.connect(gain);
                gain.connect(this.masterGain);
                
                osc.start(now);
                osc.stop(now + 3.0);
            });
            
            this.seqStep = (this.seqStep + 1) % this.melodyChords.length;
        }, 3200);
    }
}
