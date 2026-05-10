// Pure WebAudio synthesis — no asset loading. Layered adaptive "music",
// 3D spatial SFX, and procedural gunshot/arrow/footstep sounds.

export class AudioEngine {
  constructor() {
    this.ctx = null;
    this.master = null;
    this.sfxBus = null;
    this.musicBus = null;
    this.layers = {};           // { ambient, tension, combat }
    this.tension = 0;           // 0..1
    this.combat = 0;            // 0..1
    this.listener = { pos: [0, 0, 0], fwd: [0, 0, -1] };
  }

  async start() {
    if (this.ctx) return;
    this.ctx = new (window.AudioContext || window.webkitAudioContext)();
    this.master = this.ctx.createGain(); this.master.gain.value = 0.8;
    this.master.connect(this.ctx.destination);
    this.sfxBus = this.ctx.createGain(); this.sfxBus.gain.value = 0.9; this.sfxBus.connect(this.master);
    this.musicBus = this.ctx.createGain(); this.musicBus.gain.value = 0.5; this.musicBus.connect(this.master);

    // listener
    if (this.ctx.listener.positionX) {
      this.ctx.listener.positionX.value = 0;
    }

    this._startAmbient();
  }

  setListener(pos, fwd) {
    this.listener.pos = [pos.x, pos.y, pos.z];
    this.listener.fwd = [fwd.x, fwd.y, fwd.z];
    if (!this.ctx) return;
    const L = this.ctx.listener;
    if (L.positionX) {
      L.positionX.value = pos.x; L.positionY.value = pos.y; L.positionZ.value = pos.z;
      L.forwardX.value = fwd.x; L.forwardY.value = fwd.y; L.forwardZ.value = fwd.z;
      L.upX.value = 0; L.upY.value = 1; L.upZ.value = 0;
    }
  }

  // ---------- ambient/music layers ----------
  _makeNoiseBuffer(dur = 2, type = 'brown') {
    const len = this.ctx.sampleRate * dur;
    const buf = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
    const d = buf.getChannelData(0);
    let last = 0;
    for (let i = 0; i < len; i++) {
      const w = Math.random() * 2 - 1;
      if (type === 'brown') { last = (last + 0.02 * w) / 1.02; d[i] = last * 3.5; }
      else if (type === 'pink') { d[i] = w * 0.5; }
      else d[i] = w;
    }
    return buf;
  }

  _startAmbient() {
    const C = this.ctx;
    // layer 1: wind / brown noise filtered
    const noise = C.createBufferSource();
    noise.buffer = this._makeNoiseBuffer(5, 'brown');
    noise.loop = true;
    const lp = C.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 450;
    const g1 = C.createGain(); g1.gain.value = 0.25;
    noise.connect(lp).connect(g1).connect(this.musicBus);
    noise.start();

    // slow modulated filter = creaking tone
    const lfo = C.createOscillator(); lfo.frequency.value = 0.08;
    const lfoG = C.createGain(); lfoG.gain.value = 120;
    lfo.connect(lfoG).connect(lp.frequency); lfo.start();
    this.layers.ambient = g1;

    // layer 2: low drone pad (tension)
    const d1 = C.createOscillator(); d1.type = 'sawtooth'; d1.frequency.value = 55;
    const d2 = C.createOscillator(); d2.type = 'sawtooth'; d2.frequency.value = 55.3;
    const lp2 = C.createBiquadFilter(); lp2.type = 'lowpass'; lp2.frequency.value = 220;
    const tg = C.createGain(); tg.gain.value = 0;
    d1.connect(lp2); d2.connect(lp2); lp2.connect(tg).connect(this.musicBus);
    d1.start(); d2.start();
    this.layers.tension = tg;

    // layer 3: combat pulse — detuned square, gated LFO
    const p = C.createOscillator(); p.type = 'square'; p.frequency.value = 82;
    const pg = C.createGain(); pg.gain.value = 0;
    const pf = C.createBiquadFilter(); pf.type = 'lowpass'; pf.frequency.value = 1200;
    const plfo = C.createOscillator(); plfo.frequency.value = 2;
    const plfoG = C.createGain(); plfoG.gain.value = 0.3;
    plfo.connect(plfoG).connect(pg.gain); plfo.start();
    p.connect(pf).connect(pg).connect(this.musicBus);
    p.start();
    this.layers.combat = pg;
  }

  setMood({ tension, combat }) {
    if (!this.ctx) return;
    const t = this.ctx.currentTime;
    this.tension = tension;
    this.combat = combat;
    this.layers.tension.gain.cancelScheduledValues(t);
    this.layers.tension.gain.linearRampToValueAtTime(tension * 0.35, t + 1.2);
    this.layers.combat.gain.cancelScheduledValues(t);
    this.layers.combat.gain.linearRampToValueAtTime(combat * 0.25, t + 0.6);
  }

  // ---------- spatial sfx ----------
  _panner() {
    if (!this.ctx) return null;
    const p = this.ctx.createPanner();
    p.panningModel = 'HRTF';
    p.distanceModel = 'inverse';
    p.refDistance = 2;
    p.rolloffFactor = 1.2;
    p.maxDistance = 80;
    return p;
  }

  _playAt(pos, build, vol = 1) {
    if (!this.ctx) return;
    const p = this._panner();
    if (p && p.positionX) { p.positionX.value = pos.x; p.positionY.value = pos.y; p.positionZ.value = pos.z; }
    else if (p) p.setPosition(pos.x, pos.y, pos.z);
    const g = this.ctx.createGain(); g.gain.value = vol;
    build(this.ctx, g);
    g.connect(p); p.connect(this.sfxBus);
  }

  gunshot(pos) {
    this._playAt(pos || { x: 0, y: 0, z: 0 }, (C, out) => {
      const t = C.currentTime;
      // noise burst
      const n = C.createBufferSource(); n.buffer = this._makeNoiseBuffer(0.2, 'white');
      const hp = C.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 900;
      const ng = C.createGain(); ng.gain.setValueAtTime(1.2, t);
      ng.gain.exponentialRampToValueAtTime(0.001, t + 0.25);
      n.connect(hp).connect(ng).connect(out); n.start(t); n.stop(t + 0.3);
      // low punch
      const o = C.createOscillator(); o.type = 'triangle'; o.frequency.setValueAtTime(180, t);
      o.frequency.exponentialRampToValueAtTime(50, t + 0.18);
      const og = C.createGain(); og.gain.setValueAtTime(0.8, t);
      og.gain.exponentialRampToValueAtTime(0.001, t + 0.22);
      o.connect(og).connect(out); o.start(t); o.stop(t + 0.25);
    }, 0.55);
  }

  bowShot(pos) {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const o = C.createOscillator(); o.type = 'sine'; o.frequency.setValueAtTime(220, t);
      o.frequency.exponentialRampToValueAtTime(110, t + 0.15);
      const g = C.createGain(); g.gain.setValueAtTime(0.4, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.3);
      o.connect(g).connect(out); o.start(t); o.stop(t + 0.35);
    });
  }

  arrowImpact(pos, surface = 'wood') {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const n = C.createBufferSource(); n.buffer = this._makeNoiseBuffer(0.08, 'white');
      const f = C.createBiquadFilter();
      f.type = surface === 'metal' ? 'bandpass' : 'lowpass';
      f.frequency.value = surface === 'metal' ? 2200 : 600;
      const g = C.createGain(); g.gain.setValueAtTime(0.7, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.18);
      n.connect(f).connect(g).connect(out); n.start(t); n.stop(t + 0.2);
    });
  }

  footstep(pos, surface = 'concrete') {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const n = C.createBufferSource(); n.buffer = this._makeNoiseBuffer(0.08, 'white');
      const f = C.createBiquadFilter(); f.type = 'lowpass';
      f.frequency.value = surface === 'metal' ? 2500 : surface === 'gravel' ? 1800 : 900;
      const g = C.createGain(); g.gain.setValueAtTime(surface === 'metal' ? 0.28 : 0.18, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
      n.connect(f).connect(g).connect(out); n.start(t); n.stop(t + 0.15);
    }, 0.7);
  }

  thud(pos) {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const o = C.createOscillator(); o.type = 'sine'; o.frequency.setValueAtTime(80, t);
      o.frequency.exponentialRampToValueAtTime(40, t + 0.18);
      const g = C.createGain(); g.gain.setValueAtTime(0.8, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.22);
      o.connect(g).connect(out); o.start(t); o.stop(t + 0.25);
    });
  }

  glassBreak(pos) {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const n = C.createBufferSource(); n.buffer = this._makeNoiseBuffer(0.3, 'white');
      const hp = C.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 3000;
      const g = C.createGain(); g.gain.setValueAtTime(0.6, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.45);
      n.connect(hp).connect(g).connect(out); n.start(t); n.stop(t + 0.5);
    });
  }

  hurt() {
    if (!this.ctx) return;
    const t = this.ctx.currentTime;
    const o = this.ctx.createOscillator(); o.type = 'sawtooth'; o.frequency.setValueAtTime(240, t);
    o.frequency.exponentialRampToValueAtTime(90, t + 0.25);
    const g = this.ctx.createGain(); g.gain.setValueAtTime(0.4, t);
    g.gain.exponentialRampToValueAtTime(0.001, t + 0.3);
    o.connect(g).connect(this.sfxBus); o.start(t); o.stop(t + 0.35);
  }

  engineStart(pos) {
    this._playAt(pos, (C, out) => {
      const t = C.currentTime;
      const o = C.createOscillator(); o.type = 'sawtooth'; o.frequency.setValueAtTime(45, t);
      const lfo = C.createOscillator(); lfo.frequency.value = 12;
      const lfoG = C.createGain(); lfoG.gain.value = 18;
      lfo.connect(lfoG).connect(o.frequency); lfo.start(t);
      const f = C.createBiquadFilter(); f.type = 'lowpass'; f.frequency.value = 400;
      const g = C.createGain(); g.gain.setValueAtTime(0, t);
      g.gain.linearRampToValueAtTime(0.6, t + 0.5);
      g.gain.linearRampToValueAtTime(0.4, t + 3);
      o.connect(f).connect(g).connect(out); o.start(t); o.stop(t + 4);
      lfo.stop(t + 4);
    });
  }
}

export const audio = new AudioEngine();
